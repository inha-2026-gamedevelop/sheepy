// System
using System;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

using Minsung.Common.Data;
using Minsung.Player;
using Minsung.Sound;
using Minsung.TimeSystem;
using Minsung.Utility;

namespace Minsung.Item
{
    // 포션(회복 소비 아이템) 전역 매니저. 게임 실행 중 수량은 씬 전환에도 유지하고, 리와인드 시 수량과 쿨타임을 함께 복원한다.
    [DefaultExecutionOrder(-90)]
    public class PotionManager : PersistentSingleton<PotionManager>, IRewindable
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

        // 수량과 쿨타임을 한 시점으로 묶어 기록한다. 되감기 후 과거의 수량, 사용 가능 상태가 함께 복원된다.
        private readonly struct PotionTick
        {
            public readonly int   Count;
            public readonly bool  OnCooldown;
            public readonly float CooldownRemaining;

            public PotionTick(int count, bool onCooldown, float cooldownRemaining)
            {
                Count             = count;
                OnCooldown        = onCooldown;
                CooldownRemaining = cooldownRemaining;
            }
        }

        /****************************************
        *                Fields
        ****************************************/

        private float _dropChance;
        private float _magnetRadius;
        private float _magnetSpeed;
        private float _collectRadius;
        private float _useCooldown;
        private int   _maxCarryCount;
        private int   _healHalves;
        private int   _poolSize;
        private AudioClip _pickupClip;

        private PotionPickupPool _pool;
        private Transform        _player;
        private PlayerHealth     _playerHealth;

        private int   _potionCount;
        private bool  _isPotionOnCooldown;
        private float _potionCooldownEndTime;

        public int PotionCount       => _potionCount;
        public int MaxCarryCount     => _maxCarryCount;
        public bool IsPotionReady    => !_isPotionOnCooldown;
        public float PotionCooldownDuration => _useCooldown;

        /// <summary> 쿨타임 중 남은 시간</summary>
        public float PotionCooldownRemaining =>
            _isPotionOnCooldown ? Mathf.Max(0f, _potionCooldownEndTime - Time.unscaledTime) : 0f;

        /// <summary> 개수가 바뀔 때마다 호출 </summary>
        public event Action<int> OnPotionChanged;

        private RewindManager _rewindManager;
        private RingBuffer<PotionTick> _potionBuffer;
        private RingBuffer<PotionSlotTick>[] _slotBuffers;

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 초기화되도록.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetStatic();
        }

        // 씬에 배치하지 않아도 동작하도록 씬 로드 후 자동 생성.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            EnsureCreated("PotionManager");
        }

        protected override void OnSingletonAwake()
        {
            PotionDataSO potionSo = GameDB.Potion;
            _dropChance    = potionSo.DropChance;
            _magnetRadius  = potionSo.MagnetRadius;
            _magnetSpeed   = potionSo.MagnetSpeed;
            _collectRadius = potionSo.CollectRadius;
            _useCooldown   = potionSo.UseCooldown;
            _maxCarryCount = potionSo.MaxCarryCount;
            _healHalves    = potionSo.HealHalves;
            _poolSize      = potionSo.PoolSize;
            _potionCount   = Mathf.Clamp(potionSo.InitialCarryCount, 0, _maxCarryCount);
            _pickupClip    = CreatePickupClip();

            _potionBuffer = new RingBuffer<PotionTick>(RewindManager.TickCapacity);
            _slotBuffers  = new RingBuffer<PotionSlotTick>[_poolSize];
            for (int i = 0; i < _slotBuffers.Length; ++i)
            {
                _slotBuffers[i] = new RingBuffer<PotionSlotTick>(RewindManager.TickCapacity);
            }

            CreatePool();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            EnsureRewindRegistration();
            BindPlayerIfNeeded();
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _rewindManager?.Unregister(this);
            _pool?.Dispose();
            base.OnDestroy();
        }

        private void Update()
        {
            EnsureRewindRegistration();
            BindPlayerIfNeeded();

            if ((_rewindManager == null) || (!_rewindManager.IsRewinding))
            {
                RefreshPotionCooldown();
            }
        }

        private void FixedUpdate()
        {
            if ((_pool == null) || (_player == null) || ((_rewindManager != null) && _rewindManager.IsRewinding))
            {
                return;
            }

            for (int i = 0; i < _pool.Size; ++i)
            {
                if ((!_pool.IsActive(i)) || (_potionCount >= _maxCarryCount))
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
            if ((_pool == null) || (UnityEngine.Random.value > _dropChance))
            {
                return;
            }
            _pool.TryAlloc(position);
        }

        /// <summary> 소지한 포션 1개를 소비해 하트 한 칸을 회복한다. 쿨타임, 사망, 되감기 중에는 실패한다. </summary>
        public bool TryUsePotion()
        {
            RefreshPotionCooldown();
            BindPlayerIfNeeded();

            if ((_potionCount <= 0) || (_isPotionOnCooldown))
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

            SetPotionCount(_potionCount - 1);
            StartPotionCooldown();
            _playerHealth.Heal(_healHalves);
            return true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
            {
                return;
            }

            _pool?.Dispose();
            CreatePool();
            ClearRewindBuffers();

            _rewindManager = null;
            _player         = null;
            _playerHealth   = null;
        }

        private void EnsureRewindRegistration()
        {
            RewindManager rewindManager = RewindManager.Instance;
            if (_rewindManager == rewindManager)
            {
                return;
            }

            _rewindManager?.Unregister(this);
            _rewindManager = rewindManager;
            _rewindManager?.Register(this);
        }

        private void BindPlayerIfNeeded()
        {
            if ((_player != null) && (_playerHealth != null))
            {
                return;
            }

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }

            _player = player.transform;
            player.TryGetComponent(out _playerHealth);
        }

        private void CreatePool()
        {
            _pool = new PotionPickupPool(_poolSize, transform);
        }

        private void Collect(int i)
        {
            _pool.Free(i);
            SetPotionCount(_potionCount + 1);
            SoundManager.Instance?.PlaySFX(_pickupClip);

            // TODO: 획득 이펙트/사운드 (ParticlePresets / Constants.Audio)
        }

        private void SetPotionCount(int count)
        {
            int clampedCount = Mathf.Clamp(count, 0, _maxCarryCount);
            if (_potionCount == clampedCount)
            {
                return;
            }

            _potionCount = clampedCount;
            OnPotionChanged?.Invoke(_potionCount);
        }

        private void StartPotionCooldown()
        {
            if (_useCooldown <= 0f)
            {
                return;
            }

            _isPotionOnCooldown  = true;
            _potionCooldownEndTime = Time.unscaledTime + _useCooldown;
        }

        private void RefreshPotionCooldown()
        {
            if ((!_isPotionOnCooldown) || (Time.unscaledTime < _potionCooldownEndTime))
            {
                return;
            }

            _isPotionOnCooldown    = false;
            _potionCooldownEndTime = 0f;
        }

        private void ClearRewindBuffers()
        {
            _potionBuffer?.Clear();
            if (_slotBuffers == null)
            {
                return;
            }

            for (int i = 0; i < _slotBuffers.Length; ++i)
            {
                _slotBuffers[i].Clear();
            }
        }

        private static AudioClip CreatePickupClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.32f;
            const float baseFrequency = 880f;
            const float harmonicFrequency = 1320f;

            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; ++i)
            {
                float time = (float)i / sampleRate;
                float progress = time / duration;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Min(progress * 3f, 1f)) * (1f - progress);
                float primary = Mathf.Sin(Mathf.PI * 2f * (baseFrequency + (600f * progress)) * time);
                float harmonic = Mathf.Sin(Mathf.PI * 2f * (harmonicFrequency + (900f * progress)) * time);
                samples[i] = (primary + (harmonic * 0.35f)) * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create("PotionPickup", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /****************************************
        *              IRewindable
        ****************************************/

        public void RecordTick()
        {
            _potionBuffer.Push(new PotionTick(
                _potionCount,
                _isPotionOnCooldown,
                PotionCooldownRemaining));

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
            if (_potionBuffer.TryGetOrdered(orderedIndex, out PotionTick potionTick))
            {
                SetPotionCount(potionTick.Count);
                _isPotionOnCooldown = potionTick.OnCooldown;
                _potionCooldownEndTime = potionTick.OnCooldown
                    ? Time.unscaledTime + potionTick.CooldownRemaining
                    : 0f;
            }

            for (int i = 0; i < _pool.Size; ++i)
            {
                if (_slotBuffers[i].TryGetOrdered(orderedIndex, out PotionSlotTick slotTick))
                {
                    ApplySlotTick(i, slotTick);
                }
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            ApplyRewindTick(orderedIndex);
            ClearRewindBuffers();
        }

        // 되감기 틱 하나를 슬롯에 반영. 활성 상태가 늘어나면(과거에 존재) 재등장, 줄어들면(과거엔 없었음) 비활성화.
        private void ApplySlotTick(int i, PotionSlotTick slotTick)
        {
            bool isActive = _pool.IsActive(i);

            if ((slotTick.Active) && (!isActive))
            {
                _pool.ActivateAt(i, slotTick.Position);
            }
            else if ((!slotTick.Active) && (isActive))
            {
                _pool.Free(i);
            }
            else if (slotTick.Active)
            {
                _pool.SetPosition(i, slotTick.Position);
            }
        }
    }
}
