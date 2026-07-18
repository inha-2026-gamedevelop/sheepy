// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Player;
using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 보스 근접 개체 공통 몸통 - 사거리 밖이면 추격(도약 슬램 접근), 안이면 정지 + 쿨다운마다 공격 판정, 주기적으로 무적 백스텝(회피)
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public abstract class BossMeleeUnitBase : MonoBehaviour, IDamageable, IRewindable
    {
        private const int   MAX_NEARBY_BOSSES          = 8;
        private const float VERTICAL_ALIGNMENT_EPSILON = 0.01f;
        private const float STUCK_SPEED_EPSILON        = 0.01f;

        /****************************************
        *                Fields
        ****************************************/

        [Header("참조")]
        [SerializeField] protected BossController _boss;
        [SerializeField] private Animator _animator;         // 이동/공격/피격 모션 (미연결 시 판정만 동작)
        [SerializeField] private DamageHazard _attackHitbox; // 공격/도약 슬램 판정 (자식 오브젝트, 평소 비활성)

        [SerializeField] private BoxCollider2D _attackHitboxCollider;

        [SerializeField] private LayerMask _groundLayer;     // 도약 착지 판정용 지면 레이어

        protected Rigidbody2D _rb;
        protected bool _isRewinding;
        private Collider2D _col;
        private Collider2D _playerCollider;
        private bool _isIgnoringPlayerBodyCollision;
        private float _verticalAvoidHorizontalRange;
        private float _verticalAvoidHeight;
        private ContactFilter2D _bossProximityFilter;
        private readonly Collider2D[] _nearbyBosses = new Collider2D[MAX_NEARBY_BOSSES];
        private bool _isGrounded;
        private bool _isAttacking; // 공격 모션 중 이동 정지
        private bool _isJumping;   // 도약 중 - 조향 정지, 착지까지 유지
        private bool _isDodging;   // 무적 백스텝 중 - 조향 정지, 피해 무시
        private bool _isMovementLocked; // BossMovementLockBehaviour가 Enter/Exit로 제어
        private float _stuckEscapeElapsed;
        private float _stuckEscapeDelay;
        private Coroutine _attackLoop;
        private Coroutine _jumpLoop;
        private Coroutine _dodgeLoop;
        private Coroutine _obstacleEscapeLoop;
        private WaitForSeconds _waitAttackCooldown;
        private WaitForSeconds _waitAttackActive;
        private WaitForSeconds _waitJumpCooldown;
        private WaitForSeconds _waitJumpLandActive;
        private WaitForSeconds _waitDodgeCooldown;
        private WaitForSeconds _waitDodgeDuration;
        private string _objectId;

        // 파생 클래스가 결정하는 수치 (Constants.Combat의 개체별 상수를 돌려준다)
        protected abstract float MoveSpeed        { get; }
        protected abstract float AttackRange      { get; }
        protected abstract float AttackCooldown   { get; }
        protected abstract float AttackActiveTime { get; }
        protected abstract int   AttackHalves     { get; } // 공격 피해 (하트 반칸 단위, 도약 슬램도 동일하게 적용)

        // 추격/공격을 멈춰야 하는 파생별 추가 조건 (분신 = 사망 상태, 본체 = 없음)
        protected virtual bool IsActionBlocked => false;
        protected virtual bool UsesVerticalCrowdAvoidance => false;

        /// <summary> 무적 백스텝 중 여부 - 파생 클래스의 TakeDamage가 피해 무시 판정에 사용 </summary>
        protected bool IsInvulnerable => _isDodging;
        public string ObjectId => _objectId;
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
            EManagedObjectType objectType = (this is BossCloneController)
                ? EManagedObjectType.BossClone
                : EManagedObjectType.Boss;
            _objectId = ManagedObjectManager.Register(objectType, this);
            _rb  = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _verticalAvoidHorizontalRange = GameDB.Boss.CloneCrowdAvoidHorizontalRange;
            _verticalAvoidHeight          = GameDB.Boss.CloneCrowdAvoidVerticalRange;
            _stuckEscapeDelay             = GameDB.Boss.StuckEscapeDelay;
            _bossProximityFilter = ContactFilter2D.noFilter;
            _bossProximityFilter.useTriggers = false;

            _waitAttackCooldown = new WaitForSeconds(AttackCooldown);
            _waitAttackActive   = new WaitForSeconds(AttackActiveTime);

            _waitJumpCooldown   = new WaitForSeconds(GameDB.Boss.JumpCooldown);
            _waitJumpLandActive = new WaitForSeconds(GameDB.Boss.JumpLandActiveTime);
            _waitDodgeCooldown  = new WaitForSeconds(GameDB.Boss.DodgeCooldown);
            _waitDodgeDuration  = new WaitForSeconds(GameDB.Boss.DodgeDuration);

            if (_attackHitbox != null)
            {
                if (!_attackHitbox.TryGetComponent(out _attackHitboxCollider))
                {
                    // 콜라이더 타입이 바뀌는 등의 이유로 캐싱에 실패하면, 판정 사거리 자동 보정이 조용히 무효화되므로 경고로 남긴다
                    Debug.LogWarning("AttackHitbox에 BoxCollider2D가 없어 공격 판정 사거리를 자동으로 맞출 수 없다", this);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            ManagedObjectManager.Unregister(this);
            RewindManager.Instance?.Unregister(this);
        }

        private void FixedUpdate()
        {
            if ((_isRewinding) || (IsActionBlocked))
            {
                return;
            }

            UpdatePlayerBodyCollision();

            _isGrounded = CheckGrounded();
            TryEscapeObstacle();
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
            _isAttacking        = false;
            _isJumping          = false;
            _isDodging          = false;
            _stuckEscapeElapsed = 0f;
            SetPlayerBodyCollisionIgnored(false);
            UpdatePlayerBodyCollision();
            _rb.bodyType       = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;

            if (_attackHitbox != null)
            {
                _attackHitbox.Configure(AttackHalves);
                ConfigureAttackHitboxRange();
                _attackHitbox.gameObject.SetActive(false);
            }

            _attackLoop = StartCoroutine(CoAttackLoop());
            _jumpLoop   = StartCoroutine(CoJumpLoop());
            _dodgeLoop  = StartCoroutine(CoDodgeLoop());
        }

        // 보스의 옆/아래는 막되, 플레이어가 보스 위에 있을 때는 발판처럼 고정되지 않고 아래로 빠진다.
        // 위에서 진입한 뒤에는 완전히 빠져나갈 때까지 충돌 무시를 유지해 몸통 중간에서 다시 걸리지 않게 한다.
        private void UpdatePlayerBodyCollision()
        {
            if ((_col == null) || (_boss == null) || (_boss.Player == null))
            {
                return;
            }

            if ((_playerCollider == null) && (!_boss.Player.TryGetComponent(out _playerCollider)))
            {
                return;
            }

            Bounds bossBounds = _col.bounds;
            Bounds playerBounds = _playerCollider.bounds;

            if (_isIgnoringPlayerBodyCollision)
            {
                if (!bossBounds.Intersects(playerBounds))
                {
                    SetPlayerBodyCollisionIgnored(false);
                }
                return;
            }

            bool isPlayerAboveBoss = playerBounds.min.y >= bossBounds.max.y;
            if (isPlayerAboveBoss)
            {
                SetPlayerBodyCollisionIgnored(true);
            }
        }

        private void SetPlayerBodyCollisionIgnored(bool ignored)
        {
            if ((_col == null) || (_boss == null) || (_boss.Player == null))
            {
                return;
            }

            if ((_playerCollider == null) && (!_boss.Player.TryGetComponent(out _playerCollider)))
            {
                return;
            }

            Physics2D.IgnoreCollision(_col, _playerCollider, ignored);
            _isIgnoringPlayerBodyCollision = ignored;
        }

        private void ConfigureAttackHitboxRange()
        {
            if (_attackHitboxCollider == null)
            {
                return;
            }

            ApplyRangeToHitboxCollider(_attackHitboxCollider, AttackRange);
        }

        private void ApplyRangeToHitboxCollider(BoxCollider2D hitboxCollider, float range)
        {
            Vector2 colliderSize = hitboxCollider.size;
            colliderSize.x = range;
            hitboxCollider.size = colliderSize;

            Vector2 colliderOffset = hitboxCollider.offset;
            colliderOffset.x = (colliderSize.x * 0.5f) * Constants.Combat.BOSS_ART_FACING_SIGN;
            hitboxCollider.offset = colliderOffset;
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
            if (_obstacleEscapeLoop != null)
            {
                StopCoroutine(_obstacleEscapeLoop);
                _obstacleEscapeLoop = null;
            }
            _isAttacking = false;
            _isJumping   = false;
            _isDodging   = false;
            _stuckEscapeElapsed = 0f;
            if (_col != null)
            {
                _col.isTrigger = false;
            }
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

        // 추격/공격 대상 - 플레이어 본체와 활성 상태인 모든 분신(CloneController) 중 가장 가까운 쪽을 선택한다
        protected Transform GetTarget()
        {
            Transform nearest = (_boss != null) ? _boss.Player?.transform : null;
            float nearestSqr = (nearest != null)
                ? ((Vector2)nearest.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            IReadOnlyList<CloneController> clones = CloneController.ActiveInstances;
            for (int i = 0; i < clones.Count; ++i)
            {
                CloneController clone = clones[i];
                if ((clone == null) || !clone.gameObject.activeSelf)
                {
                    continue;
                }

                float sqr = ((Vector2)clone.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = clone.transform;
                }
            }

            return nearest;
        }

        // 사거리 밖이면 대상 쪽으로 수평 이동, 안이면 정지 (공격/도약/회피는 각자 코루틴이 담당)
        private void ChasePlayer()
        {
            if ((_isJumping) || (_isDodging))
            {
                PlayAnimSpeed(Mathf.Abs(_rb.linearVelocity.x));
                return; // 도약/회피 중에는 자체 속도를 유지하고 조향하지 않는다
            }

            Vector2 v = _rb.linearVelocity;
            v.x = 0f;

            Transform target = GetTarget();

            if ((!_isAttacking) && (!_isMovementLocked) && (target != null) && (_boss != null) &&
            (!_boss.IsTransitioning))
            {
                float dx = target.position.x - transform.position.x;
                FaceTo(dx);

                if (Mathf.Abs(dx) > AttackRange)
                {
                    v.x = Mathf.Sign(dx) * MoveSpeed;
                }
                // 두 분신이 상하로 겹치면 대상 추적을 잠시 양보해 좌우로 분리한다.
                if (UsesVerticalCrowdAvoidance)
                {
                    float avoidDirection = GetVerticalCrowdAvoidDirection();
                    if (!Mathf.Approximately(avoidDirection, 0f))
                    {
                        v.x = avoidDirection * MoveSpeed;
                    }
                }
            }

            _rb.linearVelocity = v;
            PlayAnimSpeed(Mathf.Abs(v.x));
        }

        // 수평 추격 중 지형 모서리에 걸려 속도가 멈추면 기존 도약의 충돌 무시를 이용해 전장으로 복귀
        private void TryEscapeObstacle()
        {
            Transform target = GetTarget();
            if ((_isAttacking) || (_isJumping) || (_isDodging) || (_isMovementLocked) ||
                (!_isGrounded) || (target == null) || (_boss == null) ||
                (_boss.IsTransitioning))
            {
                _stuckEscapeElapsed = 0f;
                return;
            }

            float dx = target.position.x - transform.position.x;
            if ((Mathf.Abs(dx) <= AttackRange) || (Mathf.Abs(_rb.linearVelocity.x) > STUCK_SPEED_EPSILON))
            {
                _stuckEscapeElapsed = 0f;
                return;
            }

            _stuckEscapeElapsed += Time.fixedDeltaTime;
            if (_stuckEscapeElapsed < _stuckEscapeDelay)
            {
                return;
            }

            _stuckEscapeElapsed = 0f;
            _obstacleEscapeLoop = StartCoroutine(CoEscapeObstacle());
        }

        private IEnumerator CoEscapeObstacle()
        {
            Transform target = GetTarget();
            if (target != null)
            {
                yield return CoLeapTo(target.position.x, target.position.y, GameDB.Boss.JumpArcHeight);
            }
            _obstacleEscapeLoop = null;
        }

        private float GetVerticalCrowdAvoidDirection()
        {
            Vector2 center = _col.bounds.center;
            float searchRadius = Mathf.Max(_verticalAvoidHorizontalRange, _verticalAvoidHeight);
            int count = Physics2D.OverlapCircle(center, searchRadius, _bossProximityFilter, _nearbyBosses);
            float direction = 0f;

            for (int i = 0; i < count; ++i)
            {
                Collider2D nearby = _nearbyBosses[i];
                if ((nearby == null) || (nearby == _col) ||
                    (!nearby.TryGetComponent(out BossCloneController other)))
                {
                    continue;
                }

                Vector2 offset = center - (Vector2)nearby.bounds.center;
                if ((Mathf.Abs(offset.x) > _verticalAvoidHorizontalRange)
                    || (Mathf.Abs(offset.y) < VERTICAL_ALIGNMENT_EPSILON)
                    || (Mathf.Abs(offset.y) > _verticalAvoidHeight))
                {
                    continue;
                }

                // X가 완전히 같을 때도 두 분신이 반대 방향을 선택하도록 안정적으로 나눈다.
                float side = Mathf.Abs(offset.x) > VERTICAL_ALIGNMENT_EPSILON
                    ? Mathf.Sign(offset.x)
                    : (GetHashCode() < other.GetHashCode() ? -1f : 1f);
                direction += side;
            }

            return Mathf.Clamp(direction, -1f, 1f);
        }

        // 바라보는 방향으로 스케일 반전 - 자식 히트박스도 같이 뒤집힌다
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

                if (_attackHitbox != null)
                {
                    _attackHitbox.gameObject.SetActive(true);
                    yield return _waitAttackActive;
                    _attackHitbox.gameObject.SetActive(false);
                }
                _isAttacking = false;
            }
        }

        // 대상이 범위 밖으로 많이 벗어났을 경우 도약하게 하기 위한 판정
        private bool IsPlayerInRange()
        {
            Transform target = GetTarget();
            if ((_boss == null) || (target == null))
            {
                return false;
            }
            float sqr = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude;
            return sqr <= (AttackRange * AttackRange) * 1.5;
        }

        /****************************************
        *                도약
        ****************************************/

        // 도약 루프: 쿨다운마다 공격 사거리 밖이면(=걷기로 못 붙는 지형에 막혀도) 도약으로 단숨에 접근한다
        private IEnumerator CoJumpLoop()
        {
            while (true)
            {
                yield return _waitJumpCooldown;

                if ((_boss != null) && (_boss.IsTransitioning))
                {
                    continue; // 기믹/컷신 중에는 도약 정지
                }
                if ((IsActionBlocked) || (_isAttacking) || (_isDodging) || (!_isGrounded) ||
                    (_boss == null) || (GetTarget() == null) || (IsPlayerInRange()))
                {
                    continue;
                }

                yield return CoJump();
            }
        }

        // 대상 위치로 도약 접근 - 착지 순간 공격 히트박스로 슬램 판정을 짧게 켠다
        private IEnumerator CoJump()
        {
            Transform target = GetTarget();
            if (target == null)
            {
                yield break;
            }
            yield return CoLeapTo(target.position.x, target.position.y, GameDB.Boss.JumpArcHeight, slamOnLand: true);
        }

        /// <summary> 목표 지점(targetX, targetY)에 정확히 착지하는 포물선 도약 - 거리와 무관하게 지정한 정점 높이로 넘어간다.
        /// slamOnLand면 착지 순간 공격 히트박스를 짧게 켠다 (전투 접근용). 등장 연출(분신 진입)은 false로 사용 </summary>
        protected IEnumerator CoLeapTo(float targetX, float targetY, float arcHeight, bool slamOnLand = false)
        {
            Vector2 start = _rb.position;
            float   dx      = targetX - start.x;
            float   dy      = targetY - start.y;
            float   gravity = Mathf.Abs(Physics2D.gravity.y * _rb.gravityScale);

            float peakHeight = Mathf.Max(arcHeight, dy) + Constants.Combat.BOSS_LEAP_HEIGHT_MARGIN; // 정점이 도착지보다 낮으면 못 넘어가므로 최소 여유를 둔다
            float riseTime   = Mathf.Sqrt((2f * peakHeight) / gravity);
            float fallTime   = Mathf.Sqrt((2f * (peakHeight - dy)) / gravity);
            float totalTime  = riseTime + fallTime;

            float dir = Mathf.Sign(dx);
            FaceTo(dir);

            Vector2 v = _rb.linearVelocity;
            v.x = dx / totalTime;
            v.y = gravity * riseTime;
            _rb.linearVelocity = v;

            _isJumping = true;
            PlayAnimTrigger(Constants.Combat.BOSS_ANIM_JUMP);

            // 벽에 붙은 채로 도약 패턴 나오면 제자리점프해서 강제로 트리거 활성화
            _col.isTrigger = true;

            while ((!_isGrounded) || (_rb.linearVelocity.y > 0f))
            {
                yield return null;
            }

            _col.isTrigger = false;

            if (slamOnLand && (_attackHitbox != null))
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

                // 기믹/컷신 중에는 회피 정지
                if ((_boss != null) && (_boss.IsTransitioning))
                {
                    continue;
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
            Transform target = GetTarget();
            if ((_boss == null) || (target == null))
            {
                return false;
            }
            float dx = Mathf.Abs(target.position.x - transform.position.x);
            return dx <= GameDB.Boss.DodgeTriggerRange;
        }

        // 대상 반대 방향으로 짧게 후퇴하며 그동안 피해를 받지 않는다 - IsPlayerWithinDodgeRange가 이미 대상 존재를 확인했다
        private IEnumerator CoDodge()
        {
            Transform target = GetTarget();
            float dir = -Mathf.Sign(target.position.x - transform.position.x);

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
