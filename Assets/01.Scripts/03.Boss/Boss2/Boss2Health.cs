// System
using System;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Player;
using Minsung.TimeSystem;

// 부유 보스(Boss2) 체력 - 플레이어 AttackHitbox가 IDamageable로 인식해 피해를 꽂는 대상
// TODO: 페이즈/피격 리액션/사망 연출 미구현 - 지금은 체력 수치와 이벤트만 제공
public class Boss2Health : MonoBehaviour, IDamageable, IRewindable
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("데이터")]
    [SerializeField] private Boss2DataSO _dataSo;

    private float _currentHealth;
    private bool  _isRewinding; // 되감기 중 피해 차단 (플레이어/몬스터 체력 가드와 동일한 관례)
    private RingBuffer<float> _rewindBuffer;

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
        if ((_isRewinding) || (_currentHealth <= 0f))
        {
            return false;
        }

        _currentHealth = Mathf.Max(0f, _currentHealth - dmg);
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);

        if (_currentHealth <= 0f)
        {
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

    private void ApplyHealth(float health)
    {
        _currentHealth = health;
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
    }
}
