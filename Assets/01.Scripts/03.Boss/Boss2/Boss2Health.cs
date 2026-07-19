// System
using System;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Player;
using Minsung.TimeSystem;

// 부유 보스(Boss2) 체력 - 플레이어 AttackHitbox가 IDamageable로 인식해 피해를 꽂는 대상
// 페이즈 하한: 원본 BossController와 동일한 방식 - 피통을 PhaseCount로 균등 분할해 현재 페이즈 하한 아래로는 안 깎인다
// TODO: 페이즈 전환(하한 도달 시 기믹 -> _phaseIndex 증가)/피격 리액션/사망 연출 미구현 - 지금은 3페이즈(_phaseIndex=0) 하한 동결만 제공
public class Boss2Health : MonoBehaviour, IDamageable, IRewindable
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("데이터")]
    [SerializeField] private Boss2DataSO _dataSo;

    private float _currentHealth;
    private int   _phaseIndex; // 현재 페이즈 인덱스(0부터) - 지금은 항상 0(3페이즈), 4페이즈 전환 로직은 추후 _phaseIndex를 올리는 방식으로 확장
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

    public event Action<float, float> OnHealthChanged; // (현재, 최대)
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

        if (_currentHealth <= 0f)
        {
            GameManager.Instance?.StopBossTimer();
            OnDefeated?.Invoke();
        }
        return true;
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
