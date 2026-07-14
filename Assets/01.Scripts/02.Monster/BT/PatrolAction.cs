// System
using System;

// Unity
using Unity.Behavior;
using UnityEngine;

using Minsung.Common;

using Action = Unity.Behavior.Action;

namespace Minsung.Monster.BT
{
    // 스폰 지점 기준 좌우로 왕복. 경계에 닿으면 방향을 뒤집는다.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [NodeDescription(name: "Patrol", story: "[Agent] patrols around its spawn point", category: "Monster Actions", id: "monster-patrol-action")]
    public partial class PatrolAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeField] private float _patrolDistance = Constants.Combat.ENEMY_PATROL_DISTANCE;

        private MonsterController _controller;
        private float _direction;

        protected override Status OnStart()
        {
            _controller = Agent.Value.GetComponent<MonsterController>();
            _direction  = (_direction == 0f) ? 1f : _direction;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            float offsetFromSpawn = Agent.Value.transform.position.x - _controller.SpawnPosition.x;
            if (Mathf.Abs(offsetFromSpawn) >= _patrolDistance)
            {
                _direction = -Mathf.Sign(offsetFromSpawn);
            }

            _controller.RequestMove(_direction);
            return Status.Running;
        }
    }
}
