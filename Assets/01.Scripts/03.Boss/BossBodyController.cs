// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Player;
using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 보스 본체 - 2페이즈부터 필드에 등장하는 근거리 개체
    // BossController/BossEmotionHUD와 같은 오브젝트에 있어 gameObject.SetActive는 쓰지 않는다 -
    // 대신 시각(Visual)/충돌/물리만 켜고 꺼서 "등장 여부"를 표현한다 (씬 배치, 비활성 상태로 시작)
    public class BossBodyController : BossMeleeUnitBase
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private GameObject _visual; // 스프라이트+애니메이터 자식(Visual) - 비활성 시 숨김

        private Collider2D _selfCollider; // 본체 자신의 물리 충돌 - 비활성 시 꺼서 플레이어가 그냥 통과
        private RingBuffer<Vector2> _rewindBuffer; // 위치 기록

        private bool _isPresent; // 필드 등장 여부 - IsActionBlocked가 참조

        // 근접 개체 공통 수치 (BossMeleeUnitBase)
        protected override float MoveSpeed        => GameDB.Boss.MoveSpeed;
        protected override float AttackRange      => GameDB.Boss.AttackRange;
        protected override float AttackCooldown   => GameDB.Boss.AttackCooldown;
        protected override float AttackActiveTime => GameDB.Boss.AttackActiveTime;
        protected override int   AttackHalves     => GameDB.Boss.AttackHalves; // 본체 공격은 한 칸

        // 등장 전(1페이즈)에는 추격/공격/도약/회피를 전부 정지
        protected override bool IsActionBlocked => !_isPresent;

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _selfCollider);

            // 초기 상태 - Phase2State.Enter가 Activate할 때까지 숨김 + 물리 정지
            if (_visual != null)
            {
                _visual.SetActive(false);
            }
            if (_selfCollider != null)
            {
                _selfCollider.enabled = false;
            }
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        /****************************************
        *             수명 관리
        ****************************************/

        /// <summary> 필드 등장. Phase2State.Enter가 호출한다. 이미 등장해 있으면(3·4페이즈 재진입) 무시 </summary>
        public void Activate()
        {
            if (_isPresent)
            {
                return;
            }
            _isPresent = true;

            if (_visual != null)
            {
                _visual.SetActive(true);
            }
            if (_selfCollider != null)
            {
                _selfCollider.enabled = true;
            }

            // 등장하는 순간부터 타임라인 참여자로 등록
            if (_rewindBuffer == null)
            {
                _rewindBuffer = new RingBuffer<Vector2>(RewindManager.TickCapacity);
            }
            _rewindBuffer.Clear();
            _isRewinding = false;
            RewindManager.Instance?.Register(this);

            BeginCombat();
        }

        /// <summary> 퇴장 + 타임라인 이탈. 페이즈 정리(Phase2State.Exit)에서 호출한다 </summary>
        public void Deactivate()
        {
            if (!_isPresent)
            {
                return;
            }
            _isPresent = false;

            StopCombatLoops();
            RewindManager.Instance?.Unregister(this);

            if (_visual != null)
            {
                _visual.SetActive(false);
            }
            if (_selfCollider != null)
            {
                _selfCollider.enabled = false;
            }
            _rb.bodyType       = RigidbodyType2D.Kinematic; // 숨겨진 채로 중력 등에 밀리지 않도록 정지
            _rb.linearVelocity = Vector2.zero;
        }

        /****************************************
        *                피해
        ****************************************/

        /// <summary> 본체 피격 (IDamageable). 자기 피통 없이 보스 총 피통으로 그대로 넘긴다 </summary>
        public override bool TakeDamage(float dmg, DamageSource source = DamageSource.Player, PlayerHealth attacker = null)
        {
            if (IsInvulnerable) // 무적 백스텝 중
            {
                return false;
            }

            bool applied = (_boss != null) && _boss.TakeDamage(dmg, source, attacker);
            if (applied)
            {
                PlayAnimTrigger(Constants.Combat.BOSS_ANIM_HIT);
            }
            return applied;
        }

        /// <summary> 원거리 패턴(장풍/레이저) 발사 시 캐스팅 모션 재생. Phase2/3 상태가 발사 시점마다 호출한다 </summary>
        public void PlayCastTrigger()
        {
            PlayAnimTrigger(Constants.Combat.BOSS_ANIM_CAST);
        }

        /****************************************
        *            IRewindable
        ****************************************/

        public override void RecordTick()
        {
            _rewindBuffer.Push(_rb.position);
        }

        public override void ApplyRewindTick(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out Vector2 pos))
            {
                _rb.position = pos;
            }
        }

        public override void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out Vector2 pos))
            {
                _rb.position = pos;
            }
            _rewindBuffer.Clear();

            ExitRewindPose(resumeCombat: true);
        }
    }
}
