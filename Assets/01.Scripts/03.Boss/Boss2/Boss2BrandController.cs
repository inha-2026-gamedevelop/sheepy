// System
using System;
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Player;

namespace Minsung.Boss2
{
    // 3페이즈 전용 "낙인" 스택 시스템 - 보스 조우 시작(3페이즈)부터 바로 10초마다 스택 1개 부여, 최대치 도달 시 즉사 + 3페이즈 처음부터 재시작
    // 4페이즈는 별도 기믹으로 대체될 예정이라 Boss2Health.OnPhaseChanged(3->4 전환) 시점에 낙인 루프를 완전히 정지한다
    // 제단(Boss2AltarInteractive)이 스택만 정화하고, 3페이즈 중 플레이어 사망(PlayerHealth.OnDeath) 시엔 보스 체력/위치까지 함께 초기화한다
    // 되감기는 3페이즈 중 계속 사용 가능 - 낙인 스택은 IRewindable이 아니라 게임 시간 기준으로만 흐른다(PlayerStatusEffectController 디버프와 동일한 관례)
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
                _brandLoop = StartCoroutine(CoBrandLoop()); // 3페이즈(보스 조우 시작)부터 바로 낙인 시작
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

        // 3->4페이즈 전환 시점 - 낙인 시스템은 3페이즈 전용이라 여기서 완전히 정지한다(4페이즈는 별도 기믹)
        private void HandlePhaseChanged(int phaseIndex)
        {
            StopBrandLoop();
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

        // 3페이즈 중 플레이어 사망 - 낙인/보스 체력/보스 위치를 전부 3페이즈 시작 상태로 되돌린다
        // 이미 4페이즈로 넘어간 뒤(IsFinalPhase)면 낙인 시스템이 종료된 상태라 관여하지 않는다
        private void HandlePlayerDeath()
        {
            if ((_health == null) || _health.IsFinalPhase)
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
}
