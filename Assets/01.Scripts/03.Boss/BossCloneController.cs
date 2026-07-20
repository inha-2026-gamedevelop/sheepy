// System
using System;
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Player;
using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 1페이즈 보스 분신 - 근거리 몬스터형 개체. 각자 독립 피통(CloneHealth)을 가지며 보스 본체 총 피통과 분리돼 있다
    // (감정 반사 규칙만 BossController.ReflectIfNeeded로 공유. 2체 전멸 시 1페이즈 종료 기믹 발동)
    public class BossCloneController : BossMeleeUnitBase
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 한 틱의 분신 기록. 위치 + 피통이면 사망/부활까지 복원된다
        private readonly struct CloneTick
        {
            public readonly Vector2 Position;
            public readonly float   Health;

            public bool IsAlive => Health > 0f;

            public CloneTick(Vector2 position, float health)
            {
                Position = position;
                Health   = health;
            }
        }

        /****************************************
        *                Fields
        ****************************************/

        private float _health;
        private bool _isAlive;
        private RingBuffer<CloneTick> _rewindBuffer;

        public bool IsAlive => _isAlive;
        public float CurrentHealth => _health;

        public event Action<BossCloneController> OnCloneDied;              // 사망 연출/사운드 연동용
        public event Action<float, float> OnHealthChanged; // (현재, 최대) - 1페이즈 분신 개별 체력바 UI 연동

        // 근접 개체 공통 수치 (BossMeleeUnitBase)
        protected override float MoveSpeed        => GameDB.Boss.CloneMoveSpeed;
        protected override float AttackRange      => GameDB.Boss.CloneAttackRange;
        protected override float AttackCooldown   => GameDB.Boss.CloneAttackCooldown;
        protected override float AttackActiveTime => GameDB.Boss.CloneAttackActiveTime;
        protected override int   AttackHalves     => GameDB.Boss.CloneAttackHalves; // 분신 공격은 반 칸

        protected override bool IsActionBlocked => !_isAlive; // 사망 중에는 추격/공격 정지
        protected override bool UsesVerticalCrowdAvoidance => true;

        /****************************************
        *             수명 관리
        ****************************************/

        /// <summary> 필드 등장 + 피통 초기화. Phase1State.Enter가 호출한다 </summary>
        public void Activate(float actionStartOffset = 0f)
        {
            _health  = GameDB.Boss.CloneHealth;
            _isAlive = true;
            gameObject.SetActive(true);
            OnHealthChanged?.Invoke(_health, GameDB.Boss.CloneHealth);

            // 등장하는 순간부터 타임라인 참여자로 등록 (사망 후에도 되감기 부활을 위해 유지)
            if (_rewindBuffer == null)
            {
                _rewindBuffer = new RingBuffer<CloneTick>(RewindManager.TickCapacity);
            }
            _rewindBuffer.Clear();
            _isRewinding = false;
            RewindManager.Instance?.Register(this);

            BeginCombat(actionStartOffset);

            // 등장 연출 - 파인 스폰 지점에서 본체가 서 있는 중앙 평지로 한 번 도약해 진입한다
            if ((_boss != null) && (_boss.Body != null))
            {
                StartCoroutine(CoEnterArena(actionStartOffset));
            }
        }

        private IEnumerator CoEnterArena(float actionStartOffset)
        {
            if (actionStartOffset > 0f)
            {
                yield return new WaitForSeconds(actionStartOffset);
            }

            if ((_boss == null) || (_boss.Body == null) || (!_isAlive))
            {
                yield break;
            }

            Vector2 landingPoint = _boss.Body.transform.position;
            yield return CoLeapTo(landingPoint.x, landingPoint.y, GameDB.Boss.CloneEntranceLeapHeight);
        }

        /// <summary> 퇴장 + 타임라인 이탈. 페이즈 정리(Phase1State.Exit)에서 호출한다 </summary>
        public void Deactivate()
        {
            StopCombatLoops();
            RewindManager.Instance?.Unregister(this);
            _isAlive = false;
            gameObject.SetActive(false);
        }

        /****************************************
        *                피해
        ****************************************/

        /// <summary> 분신 피격(IDamageable) - 보스 본체 피통과 무관한 독립 피통을 깎고, 감정 반사 규칙만 보스와 공유한다 </summary>
        public override bool TakeDamage(float dmg, DamageSource source = DamageSource.Player, PlayerHealth attacker = null)
        {
            if ((!_isAlive) || (_boss == null) || (IsInvulnerable)) // IsInvulnerable = 무적 백스텝 중
            {
                return false;
            }
            if (_boss.ReflectIfNeeded(source, attacker))
            {
                return false; // 감정 반사 - 분신 피통 유지
            }

            _health -= dmg;
            OnHealthChanged?.Invoke(Mathf.Max(0f, _health), GameDB.Boss.CloneHealth);
            if (_health <= 0f)
            {
                Die();
            }
            return true;
        }

        // 사망: 파괴하지 않고 비활성화만 한다 (리와인드 부활 대상). 타임라인 등록도 유지
        private void Die()
        {
            StopCombatLoops();
            _isAlive = false;
            gameObject.SetActive(false); // TODO: 사망 애니메이션/파티클 연출 후 비활성화로 교체
            OnCloneDied?.Invoke(this);
        }

        /****************************************
        *            IRewindable
        ****************************************/

        public override void RecordTick()
        {
            // 죽어 있는 동안(비활성)은 Rigidbody가 꺼져 있으므로 transform 위치를 기록한다
            Vector2 pos = gameObject.activeInHierarchy ? _rb.position : (Vector2)transform.position;
            _rewindBuffer.Push(new CloneTick(pos, Mathf.Max(0f, _health)));
        }

        public override void ApplyRewindTick(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out CloneTick tick))
            {
                ApplyTick(tick);
            }
        }

        public override void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out CloneTick tick))
            {
                ApplyTick(tick);
            }
            _rewindBuffer.Clear();

            ExitRewindPose(resumeCombat: _isAlive);
        }

        // 기록된 틱을 현재 상태로 적용. 사망 이전 틱에 도달하면 부활시킨다
        private void ApplyTick(CloneTick tick)
        {
            if ((tick.IsAlive) && (!gameObject.activeSelf))
            {
                gameObject.SetActive(true);
                _rb.bodyType       = RigidbodyType2D.Kinematic; // 되감기가 끝나기 전까지 물리 꺼진 상태 유지
                _rb.linearVelocity = Vector2.zero;
            }
            // 이 틱에도 죽어 있으면 죽은 자리 유지
            if (!gameObject.activeSelf)
            {
                return;
            }

            _rb.position = tick.Position;
            _health      = tick.Health;
            _isAlive     = tick.IsAlive;
            OnHealthChanged?.Invoke(_health, GameDB.Boss.CloneHealth);
        }
    }
}
