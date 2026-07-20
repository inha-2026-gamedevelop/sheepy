// System
using System;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Player;
using Minsung.TimeSystem;

// 부유 보스(Boss2) 체력 - 플레이어 AttackHitbox가 IDamageable로 인식해 피해를 꽂는 대상
// 페이즈 하한: 원본 BossController와 동일한 방식 - 피통을 PhaseCount로 균등 분할해 현재 페이즈 하한 아래로는 안 깎인다
// 페이즈 전환: 하한 도달 시 _phaseIndex를 올려 다음 하한까지 데미지가 계속 흐르게 한다(boss.md - 3~4페이즈 경계엔 확정된 기믹이 없어 별도 연출 없이 즉시 전환)
// 4페이즈에서도 타임 리와인드를 정상 사용할 수 있다(boss.md 2026-07-20 변경 - 과거 "4페이즈 리와인드 삭제" 규정 폐기)
// TODO: 피격 리액션/사망 연출 미구현
public class Boss2Health : MonoBehaviour, IDamageable, IRewindable
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("데이터")]
    [SerializeField] private Boss2DataSO _dataSo;

    private float _currentHealth;
    private int   _phaseIndex; // 현재 페이즈 인덱스(0부터) - 0: 3페이즈, PhaseCount-1: 4페이즈(최종)
    private bool  _isRewinding; // 되감기 중 피해 차단 (플레이어/몬스터 체력 가드와 동일한 관례)
    private RingBuffer<float> _rewindBuffer;

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

    public event Action<float, float> OnHealthChanged; // (현재, 최대)
    public event Action<int>          OnPhaseChanged;   // 새 페이즈 인덱스
    public event Action               OnDefeated;

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
        if ((_isRewinding) || (_currentHealth <= PhaseFloorHealth))
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

        _currentHealth = Mathf.Max(PhaseFloorHealth, _currentHealth - dmg);
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);

        if ((_currentHealth <= PhaseFloorHealth) && !IsFinalPhase)
        {
            AdvancePhase();
        }

        if (_currentHealth <= 0f)
        {
            GameManager.Instance?.StopBossTimer();
            OnDefeated?.Invoke();
        }
        return true;
    }

    // 페이즈 하한 도달 - 다음 페이즈로 넘어가 하한을 다시 계산한다(boss.md에 3~4페이즈 경계 전용 기믹이 없어 즉시 전환)
    private void AdvancePhase()
    {
        ++_phaseIndex;
        OnPhaseChanged?.Invoke(_phaseIndex);
    }

    // 3페이즈 재시작(Boss2BrandController - 낙인 7스택 즉사 후) - 현재 페이즈 상한 체력으로 복원
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
