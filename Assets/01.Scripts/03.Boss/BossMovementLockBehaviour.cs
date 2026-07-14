using UnityEngine;

namespace Minsung.Boss
{
    // Combat/Intro/Casting/Damaged 상태에 붙여서 재생 중엔 이동을 잠근다
    public class BossMovementLockBehaviour : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            BossMeleeUnitBase unit = animator.GetComponentInParent<BossMeleeUnitBase>();
            if(unit != null)
            {
                unit.SetMovementLocked(true);
            }
            
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            BossMeleeUnitBase unit = animator.GetComponentInParent<BossMeleeUnitBase>();
            if(unit != null)
            {
                unit.SetMovementLocked(false);
            }
        }
    }
}