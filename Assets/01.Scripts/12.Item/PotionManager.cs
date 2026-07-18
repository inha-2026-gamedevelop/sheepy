// System
using System;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Player;
using Minsung.TimeSystem;

namespace Minsung.Item
{
    // 포션(회복 소비 아이템) 전역 매니저 - 씬에 하나만 존재(없으면 자동 생성, LpManager와 동일한 패턴). 드랍/자석 픽업/소지 개수를 소유하고 풀 슬롯 상태를 모두 리와인드에 태운다.
    [DefaultExecutionOrder(-90)] // RewindManager(-100) 다음으로 이르게 - Register가 같은 프레임에 걸리게
    public class PotionManager : MonoBehaviour, IRewindable
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 한 틱의 풀 슬롯 기록. 위치는 자석 이동 중에도 정확히 되돌리기 위해 매 틱 남긴다.
        private readonly struct PotionSlotTick
        {
            public readonly bool    Active;
            public readonly Vector2 Position;

            public PotionSlotTick(bool active, Vector2 position)
            {
                Active   = active;
                Position = position;
            }
        }

        /****************************************
        *                Fields
        ****************************************/

        public static PotionManager Instance { get; private set; }

        private float _dropChance;
        private float _magnetRadius;
        private float _magnetSpeed;
        private float _collectRadius;
        private int   _maxCarryCount;
        private int   _healHalves;

        private PotionPickupPool _pool;
        private Transform        _player;
        private PlayerHealth     _playerHealth;

        private int _potionCount;
        public  int PotionCount    => _potionCount;
        public  int MaxCarryCount  => _maxCarryCount;

        /// <summary> 개수가 바뀔 때마다 호출 (HUD 카운터 갱신용) </summary>
        public event Action<int> OnPotionChanged;

        private RewindManager _rewindManager;
        private RingBuffer<int> _countBuffer;
        private RingBuffer<PotionSlotTick>[] _slotBuffers;

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 초기화되도록.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        // 씬에 배치하지 않아도 동작하도록 씬 로드 후 자동 생성.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject("PotionManager").AddComponent<PotionManager>();
            }
        }

        private void Awake()
        {
            if ((Instance != null) && (Instance != this))
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            PotionDataSO potionSo = GameDB.Potion;
            _dropChance    = potionSo.DropChance;
            _magnetRadius  = potionSo.MagnetRadius;
            _magnetSpeed   = potionSo.MagnetSpeed;
            _collectRadius = potionSo.CollectRadius;
            _maxCarryCount = potionSo.MaxCarryCount;
            _healHalves    = potionSo.HealHalves;

            _pool = new PotionPickupPool(potionSo.PoolSize);

            _countBuffer = new RingBuffer<int>(RewindManager.TickCapacity);
            _slotBuffers = new RingBuffer<PotionSlotTick>[potionSo.PoolSize];
            for (int i = 0; i < _slotBuffers.Length; ++i)
            {
                _slotBuffers[i] = new RingBuffer<PotionSlotTick>(RewindManager.TickCapacity);
            }
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(Constants.Tag.PLAYER);
            if (playerObj != null)
            {
                _player = playerObj.transform;
                playerObj.TryGetComponent(out _playerHealth);
            }

            _rewindManager = RewindManager.Instance;
            _rewindManager?.Register(this);
        }

        private void OnDestroy()
        {
            _rewindManager?.Unregister(this);
            _pool?.Dispose();
        }

        private void FixedUpdate()
        {
            if ((_player == null) || ((_rewindManager != null) && _rewindManager.IsRewinding))
            {
                return;
            }

            bool isFull = _potionCount >= _maxCarryCount;

            for (int i = 0; i < _pool.Size; ++i)
            {
                if (!_pool.IsActive(i) || isFull)
                {
                    continue;
                }

                Vector2 pos  = _pool.GetPosition(i);
                float   dist = Vector2.Distance(pos, _player.position);

                if (dist <= _collectRadius)
                {
                    Collect(i);
                    continue;
                }
                if (dist <= _magnetRadius)
                {
                    Vector2 next = Vector2.MoveTowards(pos, _player.position, _magnetSpeed * Time.fixedDeltaTime);
                    _pool.SetPosition(i, next);
                }
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 몬스터 처치 등에서 호출 - 확률 통과 시 위치에 포션 하나를 드랍한다 </summary>
        public void TryDropPotion(Vector2 position)
        {
            if (UnityEngine.Random.value > _dropChance)
            {
                return;
            }
            _pool.TryAlloc(position);
        }

        /// <summary> 소지한 포션 1개를 소비해 하트를 회복한다. 소지 개수가 0이거나 되감기 중이면 실패 </summary>
        public bool TryUsePotion()
        {
            if (_potionCount <= 0)
            {
                return false;
            }
            if ((_rewindManager != null) && _rewindManager.IsRewinding)
            {
                return false;
            }
            if ((_playerHealth == null) || (_playerHealth.CurrentHalves <= 0) ||
                (_playerHealth.CurrentHalves >= _playerHealth.MaxHalves))
            {
                return false;
            }

            --_potionCount;
            OnPotionChanged?.Invoke(_potionCount);
            _playerHealth.Heal(_healHalves);
            return true;
        }

        private void Collect(int i)
        {
            _pool.Free(i);
            ++_potionCount;
            OnPotionChanged?.Invoke(_potionCount);

            // TODO: 획득 이펙트/사운드 (ParticlePresets / Constants.Audio)
        }

        /****************************************
        *              IRewindable
        ****************************************/

        public void RecordTick()
        {
            _countBuffer.Push(_potionCount);
            for (int i = 0; i < _pool.Size; ++i)
            {
                bool active = _pool.IsActive(i);
                _slotBuffers[i].Push(new PotionSlotTick(active, active ? _pool.GetPosition(i) : Vector2.zero));
            }
        }

        public void OnRewindStart()
        {
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            if (_countBuffer.TryGetOrdered(orderedIndex, out int count) && (count != _potionCount))
            {
                _potionCount = count;
                OnPotionChanged?.Invoke(_potionCount);
            }

            for (int i = 0; i < _pool.Size; ++i)
            {
                if (_slotBuffers[i].TryGetOrdered(orderedIndex, out PotionSlotTick tick))
                {
                    ApplySlotTick(i, tick);
                }
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            ApplyRewindTick(orderedIndex);

            _countBuffer.Clear();
            for (int i = 0; i < _slotBuffers.Length; ++i)
            {
                _slotBuffers[i].Clear();
            }
        }

        // 되감기 틱 하나를 슬롯에 반영. 활성 상태가 늘어나면(과거에 존재) 재등장, 줄어들면(과거엔 없었음) 비활성화.
        private void ApplySlotTick(int i, PotionSlotTick tick)
        {
            bool isActive = _pool.IsActive(i);

            if (tick.Active && !isActive)
            {
                _pool.ActivateAt(i, tick.Position);
            }
            else if (!tick.Active && isActive)
            {
                _pool.Free(i);
            }
            else if (tick.Active)
            {
                _pool.SetPosition(i, tick.Position);
            }
        }
    }
}
