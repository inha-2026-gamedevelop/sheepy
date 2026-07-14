// Unity
using UnityEngine;

namespace Minsung.Player
{
    // 플레이어 상호작용 상태 담당.
    // 실제 감지/E키 처리는 PlayerInteractionSensor가, 애니메이션은 대상(LeverInteractive 등)이 맡는다.
    // 여기서는 (1) 연출 재생 중 입력을 잠그는 상태와 (2) 분신 재연을 위한 "이 틱에 상호작용했다" 기록을 관리한다.
    public class PlayerInteraction : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private PlayerMovement _movement;

        private bool       _isInteracting;      // 상호작용 오브젝트(레버 등) 연출 재생 중 입력 잠금
        private bool       _interactedThisTick; // 이번 틱에 상호작용 실행 여부 (되감기 후 분신 재연용)
        private GameObject _interactTarget;     // 방금 상호작용한 대상

        public bool IsInteracting => _isInteracting;

        /****************************************
        *                Methods
        ****************************************/

        public void Init(PlayerMovement movement)
        {
            _movement = movement;
        }

        /// <summary> 상호작용 오브젝트 연출 재생 중 이동/점프/공격 입력을 잠근다. </summary>
        public void SetInteracting(bool interacting)
        {
            _isInteracting = interacting;

            if (interacting)
            {
                _movement?.OnInteractingBegan(); // 진입 순간 물리 정지 (연출 클립 회전에 몸이 밀리는 것 방지)
            }
            else
            {
                _movement?.OnInteractingEnded(); // 연출 종료 시 물리 복구
            }
        }

        /// <summary> 되감기가 상호작용 잠금보다 우선하므로 강제 해제. 물리 복구는 되감기 쪽이 맡는다. </summary>
        public void ForceStop()
        {
            _isInteracting = false;
        }

        /// <summary> E키 상호작용 실행 시 PlayerInteractionSensor가 호출. 다음 기록 틱에 반영된다. </summary>
        public void NotifyInteracted(GameObject target)
        {
            _interactedThisTick = true;
            _interactTarget     = target;
        }

        /// <summary> 되감기 기록(RecordTick)이 이번 틱의 상호작용 여부를 읽고 비운다. </summary>
        public bool ConsumeInteract(out GameObject target)
        {
            target = _interactTarget;
            bool had = _interactedThisTick;

            _interactedThisTick = false;
            _interactTarget     = null;
            return had;
        }
    }
}
