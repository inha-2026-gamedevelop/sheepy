// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Player;
using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 보스 본체 - 2페이즈부터 필드에 등장하는 근거리 개체 (씬 배치, 비활성 시작)
    public class BossBodyController : BossMeleeUnitBase
    {
        /****************************************
        *                Fields
        ****************************************/

        private RingBuffer<Vector2> _rewindBuffer; // 위치 기록

        // 근접 개체 공통 수치 (BossMeleeUnitBase)
        protected override float MoveSpeed        => GameDB.Boss.MoveSpeed;
        protected override float AttackRange      => GameDB.Boss.AttackRange;
        protected override float AttackCooldown   => GameDB.Boss.AttackCooldown;
        protected override float AttackActiveTime => GameDB.Boss.AttackActiveTime;
        protected override int   AttackHalves     => GameDB.Boss.AttackHalves; // 본체 공격은 한 칸

        /****************************************
        *             수명 관리
        ****************************************/

        /// <summary> 필드 등장. Phase2State.Enter가 호출한다. 이미 등장해 있으면(3·4페이즈 재진입) 무시 </summary>
        public void Activate()
        {
            if (gameObject.activeSelf)
            {
                return;
            }

            gameObject.SetActive(true);

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
            StopCombatLoops();
            RewindManager.Instance?.Unregister(this);
            gameObject.SetActive(false);
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
