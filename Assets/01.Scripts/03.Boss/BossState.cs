// System
using System.Collections;

namespace Minsung.Boss
{
    // 보스 페이즈 하나의 행동을 캡슐화하는 추상 상태
    // Enter/Exit는 페이즈 진입·퇴장 시 1회, Tick/FixedTick은 매 프레임 위임된다
    public abstract class BossState
    {
        /****************************************
        *                Fields
        ****************************************/

        protected readonly BossController Boss;

        /****************************************
        *              Constructor
        ****************************************/

        protected BossState(BossController boss)
        {
            Boss = boss;
        }

        /****************************************
        *                Methods
        ****************************************/

        public abstract void Enter();
        public abstract void Exit();
        public virtual void Tick() { }
        public virtual void FixedTick() { }

        // 피통이 페이즈 하한에 도달하면 BossController가 자동으로 TriggerPhaseEnd를 호출할지 여부
        // 자체 트리거 조건(예: Phase1State의 분신 전멸)을 쓰는 페이즈는 false로 오버라이드한다
        public virtual bool UsesHealthFloorTrigger => true;

        /// <summary> 페이즈 피통 하한 도달 시 진행되는 종료 기믹(1페이즈: 즉사 레이저, 2페이즈: 컷신/씬 전환) - 끝나면 BossController가 다음 페이즈로 전환, 그동안 리와인드 잠김 </summary>
        public virtual IEnumerator CoPhaseEndGimmick()
        {
            yield break;
        }

        // 리와인드 훅 - 각 페이즈에서 필요한 경우만 오버라이드
        public virtual void RecordTick() { }
        public virtual void OnRewindStart() { }
        public virtual void ApplyRewindTick(int orderedIndex) { }
        public virtual void OnRewindEnd(int orderedIndex) { }
    }
}
