// System
using System.Collections;

using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 4페이즈 (16,000 ~ 0) - 타임리와인드 발동이 잠긴 채로 진행되는 최종 페이즈
    public class Phase4State : Phase2State
    {
        private RewindManager.RewindLockHandle _rewindLock;

        /****************************************
        *              Constructor
        ****************************************/

        public Phase4State(BossController boss) : base(boss) { }

        /****************************************
        *            Enter / Exit
        ****************************************/

        public override void Enter()
        {
            base.Enter(); // 본체 + 장풍 (임시 - 전용 패턴 확정 시 교체)

            // 리와인드 발동 잠금. 다른 기믹 잠금이 남아 있어도 이 상태의 핸들만 해제한다
            _rewindLock.Dispose();
            if (RewindManager.Instance != null)
            {
                _rewindLock = RewindManager.Instance.AcquireRewindLock(Boss);
            }
        }

        public override void Exit()
        {
            // 보스 처치 후 이 페이즈가 보유한 잠금만 해제
            _rewindLock.Dispose();

            base.Exit();
        }

        /****************************************
        *            종료 기믹
        ****************************************/

        // 4페이즈 종료 = 보스 처치. 2페이즈의 컷신/씬 전환을 상속받지 않도록 비워 둔다
        // TODO: 보스 처치 연출(사망 애니메이션/페이드) 기획 확정 후 여기에 배치
        public override IEnumerator CoPhaseEndGimmick()
        {
            yield break;
        }
    }
}
