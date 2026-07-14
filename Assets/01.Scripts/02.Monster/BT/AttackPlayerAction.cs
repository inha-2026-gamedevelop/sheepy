// System
using System;

// Unity
using Unity.Behavior;
using UnityEngine;

using Minsung.Common;

using Action = Unity.Behavior.Action;

namespace Minsung.Monster.BT
{
    // 공격 사거리 안에서 호출. 쿨다운은 타임스탬프 비교로 관리한다(틱마다 깎는 카운터 대신).
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [NodeDescription(name: "Attack Player", story: "[Agent] attacks the player", category: "Monster Actions", id: "monster-attack-player-action")]
    public partial class AttackPlayerAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeField] private float _cooldown = Constants.Combat.ENEMY_ATTACK_COOLDOWN;

        private MonsterController _controller;
        private float _nextAttackTime;

        protected override Status OnStart()
        {
            _controller = Agent.Value.GetComponent<MonsterController>();
            _controller.RequestStop();

            if (Time.time < _nextAttackTime)
            {
                return Status.Failure; // 쿨다운 중 - Selector가 추격/순찰로 넘어가게 둔다
            }

            _controller.RequestAttackPlayer();
            _nextAttackTime = Time.time + _cooldown;
            return Status.Success;
        }
    }
}
