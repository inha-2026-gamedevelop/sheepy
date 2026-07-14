// System
using System;

// Unity
using Unity.Behavior;
using UnityEngine;

using Action = Unity.Behavior.Action;

namespace Minsung.Monster.BT
{
    // 플레이어 쪽으로 계속 이동. 항상 Running.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [NodeDescription(name: "Chase Player", story: "[Agent] chases the player", category: "Monster Actions", id: "monster-chase-action")]
    public partial class ChaseAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private MonsterController _controller;

        protected override Status OnStart()
        {
            _controller = Agent.Value.GetComponent<MonsterController>();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_controller.PlayerTarget == null)
            {
                return Status.Failure;
            }

            float direction = Mathf.Sign(_controller.PlayerTarget.position.x - Agent.Value.transform.position.x);
            _controller.RequestChaseMove(direction);
            return Status.Running;
        }
    }
}
