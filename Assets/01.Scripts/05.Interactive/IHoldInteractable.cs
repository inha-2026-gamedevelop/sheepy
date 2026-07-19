// Unity
using UnityEngine;

namespace Minsung.Interactive
{
    // 상호작용 키를 누른 채 유지해야 완료되는 오브젝트의 공통 계약
    public interface IHoldInteractable
    {
        bool CanHoldInteract { get; }

        /// <summary> 홀드 상호작용을 시작한다. 시작 가능하면 true를 반환한다 </summary>
        bool OnHoldStart(GameObject interactor);

        /// <summary> 홀드 진행을 갱신한다. 완료되어 일반 상호작용을 실행했으면 true를 반환한다 </summary>
        bool OnHoldUpdate(GameObject interactor, float deltaTime);

        /// <summary> 키를 놓거나 대상에서 벗어나 홀드 상호작용을 취소한다 </summary>
        void OnHoldCancel(GameObject interactor);
    }
}
