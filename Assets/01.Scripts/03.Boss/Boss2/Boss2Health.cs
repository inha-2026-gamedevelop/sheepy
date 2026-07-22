// System
using System;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Player;
using Minsung.TimeSystem;
using Minsung.CameraSystem;

namespace Minsung.Boss2
{
    // 부유 보스(Boss2) 체력
    // TODO: 피격 리액션/사망 연출 미구현
    public class Boss2Health : MonoBehaviour, IDamageable, IRewindable
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("데이터")]
        [SerializeField] private Boss2DataSO _dataSo;

        private float _currentHealth;
        private int   _phaseIndex; // 현재 페이즈 인덱스(0부터)
        private bool  _isRewinding;
        private RingBuffer<float> _rewindBuffer;

        // 공간찢기(4페이즈 즉사기): 체력 임계 첫 통과 시 1회 발동하고, 시퀀스가 끝날 때까지 피해를 동결한다
        private bool _spaceTearTriggered; // 한 전투 1회 - 리와인드 대상 아님(무한 재발동 방지)
        private bool _spaceTearActive;    // 시퀀스 동안 피해 동결
        private bool _phase4IntroActive;  // 4페이즈 진입 연출 동안 피해 동결 (연출이 BeginPhase4IntroFreeze/End로 제어)

        // Boss 루트(부모)에 Boss2AttackPatterns가 붙이는 컴포넌트 - HitCenter는 자식이라 GetComponentInParent로 찾는다
        private Boss2EmotionController _emotionController;

        public float CurrentHealth => _currentHealth;

        public float MaxHealth
        {
            get
            {
                if (_dataSo == null)
                {
                    return 0f;
                }
                return _dataSo.MaxHealth;
            }
        }

        // 페이즈 1개 분량 (피통을 PhaseCount로 균등 분할)
        private float PhaseSpan
        {
            get
            {
                if ((_dataSo == null) || (_dataSo.PhaseCount <= 0))
                {
                    return MaxHealth;
                }
                return MaxHealth / _dataSo.PhaseCount;
            }
        }

        // 현재 페이즈의 피통 하한 - 도달하면 동결(원본 BossController.PhaseFloorHealth와 동일한 공식)
        private float PhaseFloorHealth => MaxHealth - (PhaseSpan * (_phaseIndex + 1));

        // 현재 페이즈의 피통 상한 - 되감기로 이전 페이즈 값이 섞여 들어오지 않도록 클램프에 사용
        private float PhaseCeilHealth => MaxHealth - (PhaseSpan * _phaseIndex);

        // 마지막 페이즈(4페이즈)인지 - 여기서부턴 하한 도달해도 더 이상 전환할 다음 페이즈가 없다
        // public: Boss2BrandController/Boss2AltarSpawner가 3페이즈 전용 낙인/제단 루프의 종료 조건으로 참조한다
        public bool IsFinalPhase => (_dataSo == null) || (_phaseIndex >= _dataSo.PhaseCount - 1);

        public int PhaseIndex => _phaseIndex;

        public bool IsSpaceTearActive => _spaceTearActive;

        public event Action<float, float> OnHealthChanged; // (현재, 최대)
        public event Action<int>          OnPhaseChanged;   // 새 페이즈 인덱스
        public event Action               OnDefeated;
        public event Action               OnSpaceTearTriggered; // 4페이즈 체력 임계 첫 통과 - 공간찢기 시퀀스 시작 트리거

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            if (_dataSo != null)
            {
                _currentHealth = _dataSo.MaxHealth;
                _rewindBuffer  = new RingBuffer<float>(RewindManager.TickCapacity);
                RewindManager.Instance?.Register(this);
            }
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        private void OnDestroy()
        {
            RewindManager.Instance?.Unregister(this);
        }

        /****************************************
        *            IDamageable
        ****************************************/

        public bool TakeDamage(float dmg, DamageSource source = DamageSource.Player, PlayerHealth attacker = null)
        {
            // 공간찢기/4페이즈 진입 연출 동안은 피해 동결 - 각 시퀀스가 End...()로 풀어줄 때까지
            if ((_isRewinding) || (_spaceTearActive) || (_phase4IntroActive) || (_currentHealth <= PhaseFloorHealth))
            {
                return false;
            }

            if (_emotionController == null)
            {
                _emotionController = GetComponentInParent<Boss2EmotionController>();
            }
            if ((_emotionController != null) && _emotionController.ReflectIfNeeded(source, attacker))
            {
                return false;
            }

            float projected = Mathf.Max(PhaseFloorHealth, _currentHealth - dmg);

            // 4페이즈에서 체력 임계(기본 10%)를 처음 통과하는 순간 - 정확히 임계로 클램프하고 동결 + 공간찢기 발동(1회)
            // OnSpaceTearTriggered에 리스너가 없으면(Boss2SpaceTearPattern 비활성화 등) 동결만 걸고 아무도 안 풀어줘 보스가 영구 무적이 되므로,
            // 리스너가 없을 땐 애초에 발동을 건너뛰고 평소처럼 데미지를 통과시킨다
            if ((!_spaceTearTriggered) && IsFinalPhase && (_dataSo != null) && (OnSpaceTearTriggered != null))
            {
                float threshold = MaxHealth * _dataSo.SpaceTearHealthPercent;
                if ((_currentHealth > threshold) && (projected <= threshold))
                {
                    _currentHealth      = threshold;
                    _spaceTearTriggered = true;
                    _spaceTearActive    = true;
                    OnHealthChanged?.Invoke(_currentHealth, MaxHealth); // 이벤트 발행 전에 플래그를 먼저 세팅(콜백이 바로 시퀀스를 시작하므로)
                    OnSpaceTearTriggered?.Invoke();
                    return true;
                }
            }

            _currentHealth = projected;
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);

            if ((_currentHealth <= PhaseFloorHealth) && !IsFinalPhase)
            {
                AdvancePhase();
            }

            if (_currentHealth <= 0f)
            {
                GameManager.Instance?.StopBossTimer();
                CameraManager.Instance?.ResetPlayerZoom(); // Boss1(BossController) 처치 시 줌 복귀와 동일한 처리
                OnDefeated?.Invoke();
            }
            return true;
        }

        /// <summary> 공간찢기 시퀀스 종료 </summary>
        public void EndSpaceTearFreeze()
        {
            _spaceTearActive = false;
        }

        /// <summary> 4페이즈 진입 연출 시작 - 연출 동안 보스 피격을 동결한다 (Boss2Phase4Intro가 제어) </summary>
        public void BeginPhase4IntroFreeze()
        {
            _phase4IntroActive = true;
        }

        /// <summary> 4페이즈 진입 연출 종료 - 피격 동결 해제. 여러 번 호출해도 안전 </summary>
        public void EndPhase4IntroFreeze()
        {
            _phase4IntroActive = false;
        }

        private void AdvancePhase()
        {
            ++_phaseIndex;
            OnPhaseChanged?.Invoke(_phaseIndex);
        }

        // 3페이즈 재시작
        public void ResetToPhaseStart()
        {
            _currentHealth = PhaseCeilHealth;
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        /****************************************
        *            IRewindable
        ****************************************/

        public void RecordTick()
        {
            _rewindBuffer.Push(_currentHealth);
        }

        public void OnRewindStart()
        {
            _isRewinding = true;
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out float health))
            {
                ApplyHealth(health);
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out float health))
            {
                ApplyHealth(health);
            }
            _rewindBuffer.Clear();
            _isRewinding = false;
        }

        // 되감긴 값도 현재 페이즈 범위(하한~상한) 안으로 클램프한다 - 페이즈 전환 전 기록이 섞여 들어와도 경계를 넘지 않는다
        private void ApplyHealth(float health)
        {
            _currentHealth = Mathf.Clamp(health, PhaseFloorHealth, PhaseCeilHealth);
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }
    }
}
