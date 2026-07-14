// Unity
using UnityEngine;

namespace Minsung.TimeSystem
{
    // Move/Attack 커맨드가 적용되는 대상이 구현해야 하는 최소 동작.
    public interface ICommandActor
    {
        void SetPose(Vector2 position, Vector2 velocity, bool grounded);

        // charged = 차지공격 (피해 배율 적용, 분신 재연/역재생에도 그대로 전달)
        void PlayAttack(bool reversed, bool charged);
    }
}
