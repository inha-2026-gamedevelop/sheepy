// System
using System;
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Player;

// 4페이즈 전용 "낙인" 스택 시스템 - 10초마다 스택 1개 부여, 최대치 도달 시 즉사 + 4페이즈 처음부터 재시작
// Boss2Health.OnPhaseChanged로 최종 페이즈 진입을 감지해 시작하고, 재시작 이후에도 계속 같은 루프로 돈다
// 제단(Boss2AltarInteractive)이 스택만 정화하고, 플레이어 사망(PlayerHealth.OnDeath) 시엔 보스 체력/위치까지 함께 초기화한다
public class Boss2BrandController : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("참조")]
    [SerializeField] private Transform   _target; // 플레이어
    [SerializeField] private Boss2Health _health;  // 페이즈 판정용 (HitCenter 자식)
    [SerializeField] private Boss2DataSO _dataSo;

    private BossFloatMovement _movement;
    private PlayerHealth      _playerHealth;
    private int               _stack;
    private Coroutine         _brandLoop;
    private WaitForSeconds    _waitBrandInterval;

    public int Stack => _stack;
    public event Action<int, int> OnStackChanged; // (현재, 최대)

    /****************************************
    *              Unity Event
    ****************************************/

    private void Awake()
    {
        TryGetComponent(out _movement);
    }

    private void Start()
    {
        if ((_target != null) && _target.TryGetComponent(out _playerHealth))
        {
            _playerHealth.OnDeath += HandlePlayerDeath;
        }
        if (_health != null)
        {
            _health.OnPhaseChanged += HandlePhaseChanged;
        }
        if (_dataSo != null)
        {
            _waitBrandInterval = new WaitForSeconds(_dataSo.BrandInterval);
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath -= HandlePlayerDeath;
        }
        if (_health != null)
        {
            _health.OnPhaseChanged -= HandlePhaseChanged;
        }
        StopBrandLoop();
    }

    /****************************************
    *              Public API
    ****************************************/

    // 제단 정화 완료 시 호출 - 스택만 0으로, 체력/위치는 그대로 둔다
    public void ClearStacks()
    {
        SetStack(0);
    }

    /****************************************
    *                Methods
    ****************************************/

    private void HandlePhaseChanged(int phaseIndex)
    {
        if ((_health != null) && _health.IsFinalPhase && (_brandLoop == null) && (_dataSo != null))
        {
            _brandLoop = StartCoroutine(CoBrandLoop());
        }
    }

    private IEnumerator CoBrandLoop()
    {
        while (true)
        {
            yield return _waitBrandInterval;
            SetStack(_stack + 1);

            if (_stack >= _dataSo.BrandMaxStack)
            {
                // TODO: 즉사 연출(카운트 UI 점멸 등) 확정 시 여기에 추가
                _playerHealth?.Kill();
            }
        }
    }

    // 4페이즈 중 플레이어 사망 - 낙인/보스 체력/보스 위치를 전부 4페이즈 시작 상태로 되돌린다
    private void HandlePlayerDeath()
    {
        if ((_health == null) || !_health.IsFinalPhase)
        {
            return;
        }

        SetStack(0);
        _health.ResetToPhaseStart();
        _movement?.ResetToSpawn();
    }

    private void SetStack(int value)
    {
        int max = (_dataSo != null) ? _dataSo.BrandMaxStack : value;
        _stack = Mathf.Clamp(value, 0, max);
        OnStackChanged?.Invoke(_stack, max);
    }

    private void StopBrandLoop()
    {
        if (_brandLoop != null)
        {
            StopCoroutine(_brandLoop);
            _brandLoop = null;
        }
    }
}
