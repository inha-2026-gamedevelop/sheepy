// Unity
using UnityEngine;

namespace Minsung.TimeSystem
{
    // 한 틱의 위치/속도/접지 커맨드.
    public readonly struct MoveCommand
    {
        public readonly Vector2 Position;
        public readonly Vector2 Velocity;
        public readonly bool    Grounded;

        public MoveCommand(Vector2 position, Vector2 velocity, bool grounded)
        {
            Position = position;
            Velocity = velocity;
            Grounded = grounded;
        }

        public void Apply(ICommandActor actor)
        {
            actor.SetPose(Position, Velocity, Grounded);
        }
    }
}
