// Unity
using UnityEngine;

namespace Minsung.Monster
{
    // 일반 몬스터 AI 상태 종류. 전이와 디버그 확인에 함께 사용한다.
    public enum MonsterStateType
    {
        Patrol,
        Chase,
        Attack,
    }

    // 일반 몬스터 행동 하나를 캡슐화하는 FSM 상태 기반 클래스.
    public abstract class MonsterState
    {
        /****************************************
        *                Fields
        ****************************************/

        protected readonly MonsterController Monster;

        /****************************************
        *              Constructor
        ****************************************/

        protected MonsterState(MonsterController monster)
        {
            Monster = monster;
        }

        /****************************************
        *                Methods
        ****************************************/

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void FixedTick() { }
    }

    // 플레이어를 감지하지 못했을 때 스폰 지점을 기준으로 좌우 왕복한다.
    public sealed class MonsterPatrolState : MonsterState
    {
        /****************************************
        *                Fields
        ****************************************/

        private float _direction = 1f;

        /****************************************
        *              Constructor
        ****************************************/

        public MonsterPatrolState(MonsterController monster) : base(monster) { }

        /****************************************
        *                Methods
        ****************************************/

        public override void FixedTick()
        {
            if (Monster.IsPlayerInAttackRange)
            {
                Monster.ChangeState(MonsterStateType.Attack);
                return;
            }
            if (Monster.IsPlayerDetected)
            {
                Monster.ChangeState(MonsterStateType.Chase);
                return;
            }

            float offset = Monster.transform.position.x - Monster.SpawnPosition.x;
            if (Mathf.Abs(offset) >= Monster.PatrolDistance)
            {
                _direction = -Mathf.Sign(offset);
            }
            Monster.RequestMove(_direction);
        }
    }

    // 플레이어를 감지했지만 공격 사거리 밖일 때 플레이어 방향으로 이동한다.
    public sealed class MonsterChaseState : MonsterState
    {
        /****************************************
        *              Constructor
        ****************************************/

        public MonsterChaseState(MonsterController monster) : base(monster) { }

        /****************************************
        *                Methods
        ****************************************/

        public override void FixedTick()
        {
            if (!Monster.IsPlayerDetected)
            {
                Monster.ChangeState(MonsterStateType.Patrol);
                return;
            }
            if (Monster.IsPlayerInAttackRange)
            {
                Monster.ChangeState(MonsterStateType.Attack);
                return;
            }

            float direction = Mathf.Sign(Monster.PlayerTarget.position.x - Monster.transform.position.x);
            Monster.RequestChaseMove(direction);
        }

        public override void Exit()
        {
            Monster.RequestStop();
        }
    }

    // 플레이어가 공격 사거리 안에 있는 동안 쿨다운마다 한 번씩 공격한다.
    public sealed class MonsterAttackState : MonsterState
    {
        /****************************************
        *                Fields
        ****************************************/

        private float _nextAttackTime;

        /****************************************
        *              Constructor
        ****************************************/

        public MonsterAttackState(MonsterController monster) : base(monster) { }

        /****************************************
        *                Methods
        ****************************************/

        public override void Enter()
        {
            Monster.FacePlayer();
            Monster.RequestStop();
            _nextAttackTime = 0f;
        }

        public override void FixedTick()
        {
            if (!Monster.IsPlayerDetected)
            {
                Monster.ChangeState(MonsterStateType.Patrol);
                return;
            }
            if (!Monster.IsPlayerInAttackRange)
            {
                Monster.ChangeState(MonsterStateType.Chase);
                return;
            }

            Monster.FacePlayer();
            if (Time.time < _nextAttackTime)
            {
                return;
            }

            Monster.RequestAttackPlayer();
            _nextAttackTime = Time.time + Monster.AttackCooldown;
        }

        public override void Exit()
        {
            Monster.RequestStop();
        }
    }
}
