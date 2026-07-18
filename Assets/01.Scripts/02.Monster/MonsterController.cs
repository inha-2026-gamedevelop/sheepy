// System
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Item;
using Minsung.Player;
using Minsung.Sound;
using Minsung.TimeSystem;

namespace Minsung.Monster
{
    // 일반 몬스터 몸통 - 물리 이동/공격 판정/모션을 처리한다.
    [RequireComponent(typeof(Rigidbody2D))]
    public class MonsterController : MonoBehaviour, IRewindable
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("이동")]
        [SerializeField] private float _moveSpeed            = Constants.Combat.ENEMY_PATROL_SPEED;
        [SerializeField] private float _chaseSpeedMultiplier = Constants.Combat.ENEMY_CHASE_SPEED_MULT;

        [Header("FSM")]
        [SerializeField] private float _patrolDistance = Constants.Combat.ENEMY_PATROL_DISTANCE;
        [SerializeField] private float _detectRange    = Constants.Combat.ENEMY_DETECT_RANGE;
        [SerializeField] private float _attackRange    = Constants.Combat.ENEMY_ATTACK_RANGE;
        [SerializeField] private float _attackCooldown = Constants.Combat.ENEMY_ATTACK_COOLDOWN;

        [Header("판정/참조")]
        [SerializeField] private Renderer _renderer;

        private Rigidbody2D _rb;
        private MonsterHealth _health;
        private MonsterAnimator _monsterAnimator; // Idle/Move/Attack/Hit 모션 래퍼 (없으면 판정만 동작)
        private LocalSfxEmitter _sfxEmitter;
        private RingBuffer<MonsterTick> _rewindBuffer; // 위치 + 체력 기록
        private bool _isRewinding;
        private int _deadTicks; // 비활성 사망 후 경과 틱 - 기록 창을 벗어나면 리와인드 부활 불가
        private PlayerHealth _playerHealth;
        private Transform _playerTransform;
        private MonsterState _currentState;
        private MonsterPatrolState _patrolState;
        private MonsterChaseState _chaseState;
        private MonsterAttackState _attackState;
        private string _objectId;

        // 한 틱의 몬스터 기록. 위치에 더해 체력을 남겨, 살아있던 틱으로 되감으면 부활시킨다.
        private readonly struct MonsterTick
        {
            public readonly Vector2 Position;
            public readonly float   Health;

            public bool IsAlive => Health > 0f;

            public MonsterTick(Vector2 position, float health)
            {
                Position = position;
                Health   = health;
            }
        }

        public Transform PlayerTarget  { get; private set; } // 추격/공격 대상
        public Vector3   SpawnPosition { get; private set; } // 순찰 기준점
        public MonsterStateType CurrentStateType { get; private set; }
        public float PatrolDistance => _patrolDistance;
        public float AttackCooldown => _attackCooldown;
        public string ObjectId => _objectId;
        public bool IsPlayerDetected => IsPlayerWithinRange(_detectRange);
        public bool IsPlayerInAttackRange => IsPlayerWithinRange(_attackRange);

        // 사망 모션 재생 중(MonsterHealth가 비활성화를 지연하는 구간)에는 FSM 요청을 무시한다.
        private bool IsDead => (_health != null) && _health.IsDead;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _objectId = ManagedObjectManager.Register(EManagedObjectType.Monster, this);
            _rb = GetComponent<Rigidbody2D>();
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            TryGetComponent(out _health);
            TryGetComponent(out _monsterAnimator);
            TryGetComponent(out _sfxEmitter);

            if (_health != null)
            {
                _health.OnDamaged += HandleDamaged;
                _health.OnDeath   += HandleDeath;
            }

            SpawnPosition = transform.position;
            FindTarget();

            _patrolState = new MonsterPatrolState(this);
            _chaseState  = new MonsterChaseState(this);
            _attackState = new MonsterAttackState(this);
            ChangeState(MonsterStateType.Patrol);
        }

        private void Start()
        {
            // 버퍼 용량은 모든 리와인드 참여자와 동일한 기준(TickCapacity)을 써야 인덱스가 일치한다.
            _rewindBuffer = new RingBuffer<MonsterTick>(RewindManager.TickCapacity);
            RewindManager.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            ManagedObjectManager.Unregister(this);
            if (_health != null)
            {
                _health.OnDamaged -= HandleDamaged;
                _health.OnDeath   -= HandleDeath;
            }
            RewindManager.Instance?.Unregister(this);
        }

        private void FixedUpdate()
        {
            if (_isRewinding || IsDead)
            {
                return;
            }
            FindTarget();

            _currentState?.FixedTick();
        }

        /****************************************
        *                Methods
        ****************************************/

        // FSM 전이는 상태별 객체를 재사용해 런타임 GC를 만들지 않는다.
        public void ChangeState(MonsterStateType nextType)
        {
            MonsterState nextState;
            switch (nextType)
            {
                case MonsterStateType.Chase:
                    nextState = _chaseState;
                    break;
                case MonsterStateType.Attack:
                    nextState = _attackState;
                    break;
                default:
                    nextState = _patrolState;
                    nextType  = MonsterStateType.Patrol;
                    break;
            }
            if ((_currentState == nextState) || (nextState == null))
            {
                return;
            }

            _currentState?.Exit();
            _currentState     = nextState;
            CurrentStateType  = nextType;
            _currentState.Enter();
        }

        /// <summary> 순찰 속도로 수평 이동. 되감기 중/사망 모션 중 무시. </summary>
        public void RequestMove(float horizontal)
        {
            if (_isRewinding || IsDead)
            {
                return;
            }
            ApplyHorizontal(horizontal * _moveSpeed);
        }

        /// <summary> 추격 배속으로 수평 이동. 되감기 중/사망 모션 중 무시. </summary>
        public void RequestChaseMove(float horizontal)
        {
            if (_isRewinding || IsDead)
            {
                return;
            }
            ApplyHorizontal(horizontal * _moveSpeed * _chaseSpeedMultiplier);
        }

        /// <summary> 수평 이동 정지. 되감기 중/사망 모션 중 무시. </summary>
        public void RequestStop()
        {
            if (_isRewinding || IsDead)
            {
                return;
            }
            ApplyHorizontal(0f);
        }

        // 수평 속도 적용 + 바라보는 방향 반전 + Idle/Move 모션 갱신을 한곳에서 처리.
        private void ApplyHorizontal(float velocityX)
        {
            Vector2 v = _rb.linearVelocity;
            v.x = velocityX;
            _rb.linearVelocity = v;

            FaceTo(velocityX);
            if (_monsterAnimator != null)
            {
                _monsterAnimator.SetLocomotion(Mathf.Abs(velocityX));
            }
        }

        // 원본 아트는 왼쪽을 향한다. 오른쪽 이동 시만 스케일을 반전해 자식 히트박스/이펙트도 함께 뒤집는다.
        private void FaceTo(float horizontal)
        {
            if (Mathf.Approximately(horizontal, 0f))
            {
                return;
            }

            Vector3 s = transform.localScale;
            float artFacingSign = (horizontal >= 0f)
                ? Constants.Combat.ENEMY_ART_FACING_SIGN
                : -Constants.Combat.ENEMY_ART_FACING_SIGN;
            s.x = Mathf.Abs(s.x) * artFacingSign;
            transform.localScale = s;
        }

        // 이동하지 않는 공격/대기 중에도 플레이어 방향을 바라본다.
        public void FacePlayer()
        {
            if (PlayerTarget == null)
            {
                return;
            }

            FaceTo(PlayerTarget.position.x - transform.position.x);
        }

        // 피해가 실제로 들어간 순간(MonsterHealth.OnDamaged) 피격 모션 재생.
        private void HandleDamaged()
        {
            if (_monsterAnimator != null)
            {
                _monsterAnimator.TriggerHit();
            }
            _sfxEmitter?.PlayHit();
        }

        // 사망 순간(MonsterHealth.OnDeath) 사망 모션 재생 + 확률적으로 LP 드랍.
        private void HandleDeath()
        {
            if (_monsterAnimator != null)
            {
                _monsterAnimator.TriggerDeath();
            }
            _sfxEmitter?.PlayDeath();
            LpManager.Instance?.TryDropLp(transform.position);
        }

        private bool IsPlayerWithinRange(float range)
        {
            if (PlayerTarget == null)
            {
                return false;
            }

            Collider2D selfCollider = GetComponent<Collider2D>();
            Collider2D targetCollider = PlayerTarget.GetComponent<Collider2D>();
            if ((selfCollider != null) && (targetCollider != null))
            {
                return selfCollider.Distance(targetCollider).distance <= range;
            }

            return Vector2.Distance(transform.position, PlayerTarget.position) <= range;
        }

        // 플레이어와 활성 분신 중 가장 가까운 개체를 현재 타깃으로 잡는다.
        private void FindTarget()
        {
            if (_playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag(Constants.Tag.PLAYER);
                if (playerObj != null)
                {
                    _playerTransform = playerObj.transform;
                }
            }

            Transform closestTarget = _playerTransform;
            float closestSqrDistance = (closestTarget != null)
                ? ((Vector2)(closestTarget.position - transform.position)).sqrMagnitude
                : float.MaxValue;

            IReadOnlyList<CloneController> clones = CloneController.ActiveInstances;
            for (int i = 0; i < clones.Count; ++i)
            {
                CloneController clone = clones[i];
                if ((clone == null) || !clone.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float sqrDistance = ((Vector2)(clone.transform.position - transform.position)).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestTarget      = clone.transform;
                    closestSqrDistance = sqrDistance;
                }
            }

            PlayerTarget  = closestTarget;
            _playerHealth = (PlayerTarget != null) ? PlayerTarget.GetComponent<PlayerHealth>() : null;
        }

        // IRewindable

        public void RecordTick()
        {
            if (!gameObject.activeInHierarchy)
            {
                ++_deadTicks;
                if (_deadTicks >= RewindManager.TickCapacity)
                {
                    ReleaseExpiredDeadMonster();
                }
                return;
            }

            _deadTicks = 0;
            Vector2 pos    = _rb.position;
            float   health = (_health != null) ? _health.CurrentHealth : 1f; // 체력 없으면 항상 생존 취급
            _rewindBuffer.Push(new MonsterTick(pos, health));
        }

        public void OnRewindStart()
        {
            _isRewinding = true;
            if (gameObject.activeInHierarchy)
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.linearVelocity = Vector2.zero;
            }
            if (_monsterAnimator != null)
            {
                _monsterAnimator.SetReversed(true);
            }
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            // 기록이 짧으면(늦게 스폰된 몬스터) 현재 자세 유지
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out MonsterTick tick))
            {
                ApplyTick(tick);
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out MonsterTick tick))
            {
                ApplyTick(tick);
            }

            _isRewinding = false;
            if (gameObject.activeInHierarchy)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.linearVelocity = Vector2.zero;
            }
            if (_monsterAnimator != null)
            {
                _monsterAnimator.SetReversed(false);
                _monsterAnimator.SetLocomotion(0f); // 복귀 직후 Idle부터 시작
            }
            _rewindBuffer.Clear();
            _deadTicks = 0;
        }

        // 기록된 틱을 현재 상태로 적용. 사망 이전 틱에 도달하면 부활시킨다.
        private void ApplyTick(MonsterTick tick)
        {
            if (tick.IsAlive && !gameObject.activeSelf)
            {
                Revive();
            }
            // 이 틱에도 죽어 있으면 죽은 자리 유지
            if (!gameObject.activeSelf)
            {
                return;
            }

            _rb.position = tick.Position;
            if (_health != null)
            {
                _health.RestoreHealth(tick.Health);
            }
        }

        private void Revive()
        {
            gameObject.SetActive(true);
            _deadTicks = 0;
            _rb.bodyType = RigidbodyType2D.Kinematic; // 되감기가 끝나기 전까지는 물리 꺼진 상태 유지
            _rb.linearVelocity = Vector2.zero;
            if (_monsterAnimator != null)
            {
                _monsterAnimator.ResetToIdle(); // Death 등 종단 상태에 갇힌 채 부활하는 것 방지
            }
        }

        // 사망 스냅샷만 기록 창 전체를 채우면 되감기로 부활할 수 없으므로 타임라인과 씬에서 제거
        private void ReleaseExpiredDeadMonster()
        {
            RewindManager.Instance?.Unregister(this);
            _rewindBuffer.Clear();
            Destroy(gameObject);
        }

        /// <summary> 플레이어 공격 (하트 한 칸). 쿨다운은 Attack FSM 상태가 관리한다. </summary>
        public void RequestAttackPlayer()
        {
            if (_isRewinding || IsDead)
            {
                return;
            }

            FacePlayer();
            if (_monsterAnimator != null)
            {
                _monsterAnimator.TriggerAttack();
            }
            _sfxEmitter?.PlayAttack();

            // TODO: 애니메이션 이벤트 시점에 피해를 주는 방식으로 교체 (현재는 모션과 동시에 즉시 적용)
            if (_playerHealth != null)
            {
                // 피해가 실제로 들어갔을 때만 넉백 (무적 중엔 밀리지 않음)
                if (_playerHealth.TakeDamage() && _playerHealth.TryGetComponent(out PlayerController player))
                {
                    player.ApplyKnockback(transform.position);
                }
            }
        }
    }
}
