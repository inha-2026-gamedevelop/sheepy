// Unity
using UnityEngine;

using Minsung.Interactive;

namespace Minsung.TimeSystem
{
    // 분신이 클립을 재생하다 이 틱에 도달하면 그대로 재연한다.
    public readonly struct InteractCommand
    {
        public readonly GameObject Target;

        public InteractCommand(GameObject target)
        {
            Target = target;
        }

        public void Execute(GameObject interactor)
        {
            if ((Target != null) && Target.TryGetComponent(out IInteractable interactable))
            {
                interactable.OnInteract(interactor);
            }
        }
    }
}
