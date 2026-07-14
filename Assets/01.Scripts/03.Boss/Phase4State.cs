// System
using System.Collections;

using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 4페이즈 (16,000 ~ 0) - 타임리와인드 발동이 잠긴 채로 진행되는 최종 페이즈
    public class Phase4State : Phase2State
    {
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

            // 리와인드 발동 잠금. 3->4 전환(CoPhaseEnd)의 잠금 해제보다 Enter가 늦게 불리므로 유지된다
            RewindManager.Instance?.SetRewindEnabled(false);
        }

        public override void Exit()
        {
            // 보스 처치 후 리와인드 복구
            RewindManager.Instance?.SetRewindEnabled(true);

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
