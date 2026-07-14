// System
using System;

// Unity
using Unity.Behavior;
using UnityEngine;

using Minsung.Common;

using Condition = Unity.Behavior.Condition;

namespace Minsung.Monster.BT
{
    // 공격 사거리 안에 플레이어가 있는지 확인하는 조건 노드.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [NodeDescription(name: "Is Player In Attack Range", story: "[Agent] can reach the player", category: "Monster Conditions", id: "monster-is-player-in-attack-range-condition")]
    public partial class IsPlayerInAttackRangeCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeField] private float _attackRange = Constants.Combat.ENEMY_ATTACK_RANGE;

        private MonsterController _controller;

        public override bool IsTrue()
        {
            if (_controller == null)
            {
                _controller = Agent.Value.GetComponent<MonsterController>();
            }
            if (_controller.PlayerTarget == null)
            {
                return false;
            }

            float distance = Vector3.Distance(Agent.Value.transform.position, _controller.PlayerTarget.position);
            return (distance <= _attackRange);
        }
    }
}
