// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Item;
using Minsung.Player;
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

        [Header("판정/참조")]
        [SerializeField] private Renderer _renderer;

        private Rigidbody2D _rb;
        private MonsterHealth _health;
        private MonsterAnimator _monsterAnimator; // Idle/Move/Attack/Hit 모션 래퍼 (없으면 판정만 동작)
        private RingBuffer<MonsterTick> _rewindBuffer; // 위치 + 체력 기록
        private bool _isRewinding;
        private PlayerHealth _playerHealth;

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

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            TryGetComponent(out _health);
            TryGetComponent(out _monsterAnimator);

            if (_health != null)
            {
                _health.OnDamaged += HandleDamaged;
                _health.OnDeath   += HandleDeath;
            }

            SpawnPosition = transform.position;

            GameObject playerObj = GameObject.FindGameObjectWithTag(Constants.Tag.PLAYER);
            if (playerObj != null)
            {
                PlayerTarget  = playerObj.transform;
                _playerHealth = playerObj.GetComponent<PlayerHealth>();
            }
        }

        private void Start()
        {
            // 버퍼 용량은 모든 리와인드 참여자와 동일한 기준(TickCapacity)을 써야 인덱스가 일치한다.
            _rewindBuffer = new RingBuffer<MonsterTick>(RewindManager.TickCapacity);
            RewindManager.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamaged -= HandleDamaged;
                _health.OnDeath   -= HandleDeath;
            }
            RewindManager.Instance?.Unregister(this);
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 순찰 속도로 수평 이동. 되감기 중 무시. </summary>
        public void RequestMove(float horizontal)
        {
            if (_isRewinding)
            {
                return;
            }
            ApplyHorizontal(horizontal * _moveSpeed);
        }

        /// <summary> 추격 배속으로 수평 이동. 되감기 중 무시. </summary>
        public void RequestChaseMove(float horizontal)
        {
            if (_isRewinding)
            {
                return;
            }
            ApplyHorizontal(horizontal * _moveSpeed * _chaseSpeedMultiplier);
        }

        /// <summary> 수평 이동 정지. 되감기 중 무시. </summary>
        public void RequestStop()
        {
            if (_isRewinding)
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

        // 이동 방향으로 스케일 반전 - 자식 히트박스/이펙트도 같이 뒤집힌다.
        private void FaceTo(float horizontal)
        {
            if (Mathf.Approximately(horizontal, 0f))
            {
                return;
            }
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * ((horizontal >= 0f) ? 1f : -1f);
            transform.localScale = s;
        }

        // 피해가 실제로 들어간 순간(MonsterHealth.OnDamaged) 피격 모션 재생.
        private void HandleDamaged()
        {
            if (_monsterAnimator != null)
            {
                _monsterAnimator.TriggerHit();
            }
        }

        // 사망 순간(MonsterHealth.OnDeath) 확률적으로 LP 드랍.
        private void HandleDeath()
        {
            LpManager.Instance?.TryDropLp(transform.position);
        }

        // IRewindable

        public void RecordTick()
        {
            // 죽어 있는 동안(비활성)은 Rigidbody가 꺼져 있으므로 transform 위치를 기록한다.
            Vector2 pos    = gameObject.activeInHierarchy ? _rb.position : (Vector2)transform.position;
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
            _rb.bodyType = RigidbodyType2D.Kinematic; // 되감기가 끝나기 전까지는 물리 꺼진 상태 유지
            _rb.linearVelocity = Vector2.zero;
        }

        /// <summary> 플레이어 공격 (하트 한 칸). 쿨다운은 BT 노드 쪽에서 관리한다. </summary>
        public void RequestAttackPlayer()
        {
            if (_isRewinding)
            {
                return;
            }

            if (_monsterAnimator != null)
            {
                _monsterAnimator.TriggerAttack();
            }

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
