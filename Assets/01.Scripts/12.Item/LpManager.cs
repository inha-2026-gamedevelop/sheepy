// System
using System;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.TimeSystem;

namespace Minsung.Item
{
    // LP(수집 재화) 전역 매니저 - 씬에 하나만 존재(없으면 자동 생성). RewindManager와 동일한 자동 생성 패턴.
    // 드랍/자석 픽업/카운트를 소유하고, 개수와 풀 슬롯 상태를 모두 리와인드에 태운다.
    [DefaultExecutionOrder(-90)] // RewindManager(-100) 다음으로 이르게 - Register가 같은 프레임에 걸리게
    public class LpManager : MonoBehaviour, IRewindable
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 한 틱의 풀 슬롯 기록. 위치는 자석 이동 중에도 정확히 되돌리기 위해 매 틱 남긴다.
        private readonly struct LpSlotTick
        {
            public readonly bool    Active;
            public readonly Vector2 Position;

            public LpSlotTick(bool active, Vector2 position)
            {
                Active   = active;
                Position = position;
            }
        }

        /****************************************
        *                Fields
        ****************************************/

        public static LpManager Instance { get; private set; }

        private float _dropChance;
        private float _magnetRadius;
        private float _magnetSpeed;
        private float _collectRadius;

        private LpPickupPool _pool;
        private Transform    _player;

        private int _lpCount;
        public  int LpCount => _lpCount;

        /// <summary> 개수가 바뀔 때마다 호출 (HUD 카운터 갱신용) </summary>
        public event Action<int> OnLpChanged;

        private RewindManager _rewindManager;
        private RingBuffer<int> _countBuffer;
        private RingBuffer<LpSlotTick>[] _slotBuffers;

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
                new GameObject("LpManager").AddComponent<LpManager>();
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

            LpDataSO lpSo = GameDB.Lp;
            _dropChance    = lpSo.DropChance;
            _magnetRadius  = lpSo.MagnetRadius;
            _magnetSpeed   = lpSo.MagnetSpeed;
            _collectRadius = lpSo.CollectRadius;

            _pool = new LpPickupPool(lpSo.PoolSize);

            _countBuffer = new RingBuffer<int>(RewindManager.TickCapacity);
            _slotBuffers = new RingBuffer<LpSlotTick>[lpSo.PoolSize];
            for (int i = 0; i < _slotBuffers.Length; ++i)
            {
                _slotBuffers[i] = new RingBuffer<LpSlotTick>(RewindManager.TickCapacity);
            }
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(Constants.Tag.PLAYER);
            if (playerObj != null)
            {
                _player = playerObj.transform;
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

            for (int i = 0; i < _pool.Size; ++i)
            {
                if (!_pool.IsActive(i))
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

        /// <summary> 몬스터 처치 등에서 호출 - 확률 통과 시 위치에 LP 하나를 드랍한다 </summary>
        public void TryDropLp(Vector2 position)
        {
            if (UnityEngine.Random.value > _dropChance)
            {
                return;
            }
            _pool.TryAlloc(position);
        }

        private void Collect(int i)
        {
            _pool.Free(i);
            ++_lpCount;
            OnLpChanged?.Invoke(_lpCount);

            // TODO: 획득 이펙트/사운드 (ParticlePresets / Constants.Audio)
        }

        /****************************************
        *              IRewindable
        ****************************************/

        public void RecordTick()
        {
            _countBuffer.Push(_lpCount);
            for (int i = 0; i < _pool.Size; ++i)
            {
                bool active = _pool.IsActive(i);
                _slotBuffers[i].Push(new LpSlotTick(active, active ? _pool.GetPosition(i) : Vector2.zero));
            }
        }

        public void OnRewindStart()
        {
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            if (_countBuffer.TryGetOrdered(orderedIndex, out int count) && (count != _lpCount))
            {
                _lpCount = count;
                OnLpChanged?.Invoke(_lpCount);
            }

            for (int i = 0; i < _pool.Size; ++i)
            {
                if (_slotBuffers[i].TryGetOrdered(orderedIndex, out LpSlotTick tick))
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
        private void ApplySlotTick(int i, LpSlotTick tick)
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
