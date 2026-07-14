// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Player;
using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 보스 근접 개체 공통 몸통 - 사거리 밖이면 추격, 안이면 정지 + 쿨다운마다 공격 모션/판정을 처리한다
    // 추가로 사거리 밖에서는 도약 슬램으로 접근하고, 사거리 안에서는 주기적으로 무적 백스텝(회피)으로 거리를 벌린다
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public abstract class BossMeleeUnitBase : MonoBehaviour, IDamageable, IRewindable
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("참조")]
        [SerializeField] protected BossController _boss;
        [SerializeField] private Animator _animator;         // 이동/공격/피격 모션 (미연결 시 판정만 동작)
        [SerializeField] private DamageHazard _attackHitbox; // 공격/도약 슬램 판정 (자식 오브젝트, 평소 비활성)
        [SerializeField] private LayerMask _groundLayer;     // 도약 착지 판정용 지면 레이어

        protected Rigidbody2D _rb;
        protected bool _isRewinding;
        private Collider2D _col;
        private bool _isGrounded;
        private bool _isAttacking; // 공격 모션 중 이동 정지
        private bool _isJumping;   // 도약 중 - 조향 정지, 착지까지 유지
        private bool _isDodging;   // 무적 백스텝 중 - 조향 정지, 피해 무시
        private bool _isMovementLocked; // BossMovementLockBehaviour가 Enter/Exit로 제어 // kjw
        private Coroutine _attackLoop;
        private Coroutine _jumpLoop;
        private Coroutine _dodgeLoop;
        private WaitForSeconds _waitAttackCooldown;
        private WaitForSeconds _waitAttackActive;
        private WaitForSeconds _waitJumpCooldown;
        private WaitForSeconds _waitJumpLandActive;
        private WaitForSeconds _waitDodgeCooldown;
        private WaitForSeconds _waitDodgeDuration;

        // 파생 클래스가 결정하는 수치 (Constants.Combat의 개체별 상수를 돌려준다)
        protected abstract float MoveSpeed        { get; }
        protected abstract float AttackRange      { get; }
        protected abstract float AttackCooldown   { get; }
        protected abstract float AttackActiveTime { get; }
        protected abstract int   AttackHalves     { get; } // 공격 피해 (하트 반칸 단위, 도약 슬램도 동일하게 적용)

        // 추격/공격을 멈춰야 하는 파생별 추가 조건 (분신 = 사망 상태, 본체 = 없음)
        protected virtual bool IsActionBlocked => false;

        /// <summary> 무적 백스텝 중 여부 - 파생 클래스의 TakeDamage가 피해 무시 판정에 사용 </summary>
        protected bool IsInvulnerable => _isDodging;
        /// <summary> Combat/Intro/Casting/Damaged 상태 재생 중 여부 - BossMovementLockBehaviour가 Enter/Exit에서 설정 </summary>
        public void SetMovementLocked(bool locked)
        {
            _isMovementLocked = locked;
        }
        /****************************************
        *              Unity Event
        ****************************************/

        protected virtual void Awake()
        {
            _rb  = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            _waitAttackCooldown = new WaitForSeconds(AttackCooldown);
            _waitAttackActive   = new WaitForSeconds(AttackActiveTime);

            _waitJumpCooldown   = new WaitForSeconds(GameDB.Boss.JumpCooldown);
            _waitJumpLandActive = new WaitForSeconds(GameDB.Boss.JumpLandActiveTime);
            _waitDodgeCooldown  = new WaitForSeconds(GameDB.Boss.DodgeCooldown);
            _waitDodgeDuration  = new WaitForSeconds(GameDB.Boss.DodgeDuration);
        }

        protected virtual void OnDestroy()
        {
            RewindManager.Instance?.Unregister(this);
        }

        private void FixedUpdate()
        {
            if ((_isRewinding) || (IsActionBlocked))
            {
                return;
            }
            _isGrounded = CheckGrounded();
            ChasePlayer();
        }

        /****************************************
        *      IDamageable / IRewindable
        ****************************************/

        // 피해 처리와 틱 기록은 개체마다 다르다 (분신 = 자기 피통 + 부활, 본체 = 총 피통 위임)
        public abstract bool TakeDamage(float dmg, DamageSource source = DamageSource.Player, PlayerHealth attacker = null);
        public abstract void RecordTick();
        public abstract void ApplyRewindTick(int orderedIndex);
        public abstract void OnRewindEnd(int orderedIndex);

        public virtual void OnRewindStart()
        {
            EnterRewindPose();
        }

        /****************************************
        *          공통 헬퍼 (파생용)
        ****************************************/

        /// <summary> 전투 개시 - 물리 복귀 + 히트박스 피해 설정 + 공격/도약/회피 루프 시작. Activate에서 호출한다 </summary>
        protected void BeginCombat()
        {
            _isAttacking       = false;
            _isJumping         = false;
            _isDodging         = false;
            _rb.bodyType       = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;

            if (_attackHitbox != null)
            {
                _attackHitbox.Configure(AttackHalves);
                _attackHitbox.gameObject.SetActive(false);
            }

            _attackLoop = StartCoroutine(CoAttackLoop());
            _jumpLoop   = StartCoroutine(CoJumpLoop());
            _dodgeLoop  = StartCoroutine(CoDodgeLoop());
        }

        /// <summary> 공격/도약/회피 루프 전부 정지 + 켜져 있던 히트박스 정리. 사망/퇴장/되감기에서 호출한다 </summary>
        protected void StopCombatLoops()
        {
            if (_attackLoop != null)
            {
                StopCoroutine(_attackLoop);
                _attackLoop = null;
            }
            if (_jumpLoop != null)
            {
                StopCoroutine(_jumpLoop);
                _jumpLoop = null;
            }
            if (_dodgeLoop != null)
            {
                StopCoroutine(_dodgeLoop);
                _dodgeLoop = null;
            }
            _isAttacking = false;
            _isJumping   = false;
            _isDodging   = false;
            if (_attackHitbox != null)
            {
                _attackHitbox.gameObject.SetActive(false);
            }
        }

        /// <summary> 되감기 시작 공통 처리 - 루프 정지 + 물리 끄기(키네마틱) </summary>
        protected void EnterRewindPose()
        {
            _isRewinding = true;
            StopCombatLoops();
            if (gameObject.activeInHierarchy)
            {
                _rb.bodyType       = RigidbodyType2D.Kinematic;
                _rb.linearVelocity = Vector2.zero;
            }
        }

        /// <summary> 되감기 종료 공통 처리 - 물리 복귀 + (생존 시) 공격/도약/회피 루프 재시작 </summary>
        protected void ExitRewindPose(bool resumeCombat)
        {
            _isRewinding = false;

            if ((resumeCombat) && (gameObject.activeInHierarchy))
            {
                _rb.bodyType       = RigidbodyType2D.Dynamic;
                _rb.linearVelocity = Vector2.zero;
                _attackLoop        = StartCoroutine(CoAttackLoop());
                _jumpLoop          = StartCoroutine(CoJumpLoop());
                _dodgeLoop         = StartCoroutine(CoDodgeLoop());
            }
        }

        /// <summary> 트리거 파라미터 재생. Animator 미연결 시 무시 (파생 클래스/피해 처리에서 공용으로 사용) </summary>
        protected void PlayAnimTrigger(string trigger)
        {
            if (_animator != null)
            {
                _animator.SetTrigger(trigger);
            }
        }

        // Idle <-> Run 로코모션 구동
        private void PlayAnimSpeed(float speed)
        {
            if (_animator != null)
            {
                _animator.SetFloat(Constants.Combat.BOSS_ANIM_SPEED, speed);
            }
        }

        /****************************************
        *             추격 / 공격
        ****************************************/

        // 지면 판정 - 도약 착지 여부 확인용 (Player.CheckGrounded와 동일 방식)
        private bool CheckGrounded()
        {
            float dist = _col.bounds.extents.y + Constants.Combat.GROUND_CHECK_EXTRA;
            return Physics2D.Raycast(_col.bounds.center, Vector2.down, dist, _groundLayer);
        }

        // 사거리 밖이면 플레이어 쪽으로 수평 이동, 안이면 정지 (공격/도약/회피는 각자 코루틴이 담당)
        private void ChasePlayer()
        {
            if ((_isJumping) || (_isDodging))
            {
                PlayAnimSpeed(Mathf.Abs(_rb.linearVelocity.x));
                return; // 도약/회피 중에는 자체 속도를 유지하고 조향하지 않는다
            }

            Vector2 v = _rb.linearVelocity;
            v.x = 0f;


            // kjw 조건식 변경. 
            // 변경
            if ((!_isAttacking) && (!_isMovementLocked) && (_boss != null) && (_boss.Player != null) && 
            (!_boss.IsTransitioning))
            {
                float dx = _boss.Player.transform.position.x - transform.position.x;
                FaceTo(dx);

                if (Mathf.Abs(dx) > AttackRange)
                {
                    v.x = Mathf.Sign(dx) * MoveSpeed;
                }
            }

            _rb.linearVelocity = v;
            PlayAnimSpeed(Mathf.Abs(v.x));
        }

        // 바라보는 방향으로 스케일 반전 - 자식 히트박스도 같이 뒤집힌다 (원본 아트 응시 방향은 BOSS_ART_FACING_SIGN 참조)
        private void FaceTo(float dx)
        {
            if (Mathf.Approximately(dx, 0f))
            {
                return;
            }
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * Mathf.Sign(dx) * Constants.Combat.BOSS_ART_FACING_SIGN;
            transform.localScale = s;
        }

        // 근거리 공격 루프: 쿨다운마다 사거리 안이면 공격 모션 + 판정을 짧게 켠다
        private IEnumerator CoAttackLoop()
        {
            while (true)
            {
                yield return _waitAttackCooldown;

                if ((_boss != null) && (_boss.IsTransitioning))
                {
                    continue; // 기믹/컷신 중에는 공격 정지
                }
                if ((IsActionBlocked) || (_isJumping) || (_isDodging) || (!IsPlayerInRange()))
                {
                    continue;
                }

                _isAttacking = true;
                PlayAnimTrigger(Constants.Combat.BOSS_ANIM_ATTACK);

                // TODO: 애니메이션 이벤트 시점에 히트박스를 켜고 모션 종료 시 끄는 방식으로 교체
                if (_attackHitbox != null)
                {
                    _attackHitbox.gameObject.SetActive(true);
                    yield return _waitAttackActive;
                    _attackHitbox.gameObject.SetActive(false);
                }
                _isAttacking = false;
            }
        }

        private bool IsPlayerInRange()
        {
            if ((_boss == null) || (_boss.Player == null))
            {
                return false;
            }
            float sqr = ((Vector2)_boss.Player.transform.position - (Vector2)transform.position).sqrMagnitude;
            return sqr <= (AttackRange * AttackRange);
        }

        /****************************************
        *                도약
        ****************************************/

        // 도약 루프: 쿨다운마다 사거리가 JumpTriggerRange보다 멀면 도약 슬램으로 단숨에 접근한다
        private IEnumerator CoJumpLoop()
        {
            while (true)
            {
                yield return _waitJumpCooldown;

                if ((_boss != null) && (_boss.IsTransitioning))
                {
                    continue; // 기믹/컷신 중에는 도약 정지
                }
                if ((IsActionBlocked) || (_isAttacking) || (_isDodging) || (!_isGrounded) || (!IsPlayerBeyondJumpRange()))
                {
                    continue;
                }

                yield return CoJump();
            }
        }

        private bool IsPlayerBeyondJumpRange()
        {
            if ((_boss == null) || (_boss.Player == null))
            {
                return false;
            }
            float dx = Mathf.Abs(_boss.Player.transform.position.x - transform.position.x);
            return dx > GameDB.Boss.JumpTriggerRange;
        }

        // 플레이어 방향으로 뛰어올라 접근 - 착지 순간 기존 공격 히트박스로 슬램 판정을 짧게 켠다
        private IEnumerator CoJump()
        {
            float dir = Mathf.Sign(_boss.Player.transform.position.x - transform.position.x);
            FaceTo(dir);

            Vector2 v = _rb.linearVelocity;
            v.x = dir * GameDB.Boss.JumpForwardSpeed;
            v.y = GameDB.Boss.JumpForce;
            _rb.linearVelocity = v;

            _isJumping = true;
            PlayAnimTrigger(Constants.Combat.BOSS_ANIM_JUMP);

            while ((!_isGrounded) || (_rb.linearVelocity.y > 0f))
            {
                yield return null;
            }

            if (_attackHitbox != null)
            {
                _attackHitbox.gameObject.SetActive(true);
                yield return _waitJumpLandActive;
                _attackHitbox.gameObject.SetActive(false);
            }
            _isJumping = false;
        }

        /****************************************
        *                회피
        ****************************************/

        // 회피 루프: 쿨다운마다 플레이어가 DodgeTriggerRange 안으로 붙으면 무적 백스텝으로 거리를 벌린다
        private IEnumerator CoDodgeLoop()
        {
            while (true)
            {
                yield return _waitDodgeCooldown;

                if ((_boss != null) && (_boss.IsTransitioning))
                {
                    continue; // 기믹/컷신 중에는 회피 정지
                }
                if ((IsActionBlocked) || (_isAttacking) || (_isJumping) || (!IsPlayerWithinDodgeRange()))
                {
                    continue;
                }

                yield return CoDodge();
            }
        }

        private bool IsPlayerWithinDodgeRange()
        {
            if ((_boss == null) || (_boss.Player == null))
            {
                return false;
            }
            float dx = Mathf.Abs(_boss.Player.transform.position.x - transform.position.x);
            return dx <= GameDB.Boss.DodgeTriggerRange;
        }

        // 플레이어 반대 방향으로 짧게 후퇴하며 그동안 피해를 받지 않는다 (IsInvulnerable - 파생 TakeDamage가 참조)
        private IEnumerator CoDodge()
        {
            float dir = -Mathf.Sign(_boss.Player.transform.position.x - transform.position.x);

            Vector2 v = _rb.linearVelocity;
            v.x = dir * (GameDB.Boss.DodgeBackDistance / GameDB.Boss.DodgeDuration);
            _rb.linearVelocity = v;

            _isDodging = true;
            PlayAnimTrigger(Constants.Combat.BOSS_ANIM_DODGE);

            yield return _waitDodgeDuration;

            v = _rb.linearVelocity;
            v.x = 0f;
            _rb.linearVelocity = v;
            _isDodging = false;
        }
    }
}
