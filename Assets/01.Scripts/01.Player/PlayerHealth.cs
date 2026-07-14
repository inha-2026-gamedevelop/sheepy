// System
using System;
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.TimeSystem;

namespace Minsung.Player
{
    // 하트 기반 체력. 내부는 반칸 단위로 관리한다 - 하트 6칸 = 반칸 12개.
    public class PlayerHealth : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private int _maxHalves; // PlayerDB 밸런싱(GameDB.Player) - Awake에서 로드
        private int _currentHalves;
        private bool _isInvincible;
        private WaitForSeconds _waitInvincible;

        public int MaxHalves => _maxHalves;
        public int CurrentHalves => _currentHalves;
        public bool IsInvincible => _isInvincible;

        /// <summary> (현재 반칸, 최대 반칸). UI는 반칸 단위로 하트를 그린다. </summary>
        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnDamaged; // 피해가 실제로 들어간 순간 1회 (피격 플래시 등 리액션용)

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            PlayerDataSO playerSo = GameDB.Player;
            _maxHalves      = playerSo.MaxHeartHalves;
            _currentHalves  = _maxHalves;
            _waitInvincible = new WaitForSeconds(playerSo.InvincibleDuration);
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 하트 단위 피격(기본 1칸). 무적/사망/되감기 중이면 무시하고 false. </summary>
        public bool TakeDamage(int hearts = 1)
        {
            return TakeDamageHalves(hearts * Constants.Player.HALVES_PER_HEART);
        }

        /// <summary> 반칸 단위 피격. 실제로 피해가 들어갔으면 true - 넉백/경직 등 리액션 게이트로 사용. </summary>
        public bool TakeDamageHalves(int halves)
        {
            if (_isInvincible || _currentHalves <= 0)
            {
                return false;
            }
            // 되감기 중에는 세상이 역재생 중이므로 새 피해가 끼어들면 기록과 어긋난다.
            if ((RewindManager.Instance != null) && RewindManager.Instance.IsRewinding)
            {
                return false;
            }

            _currentHalves = Mathf.Max(0, _currentHalves - halves);
            OnHealthChanged?.Invoke(_currentHalves, _maxHalves);
            OnDamaged?.Invoke();

            if (_currentHalves <= 0)
            {
                OnDeath?.Invoke();
                return true;
            }

            StartCoroutine(CoInvincibility());
            return true;
        }

        /// <summary> 반칸 단위 회복 (파랑 감정 하트 픽업 등). </summary>
        public void Heal(int halves)
        {
            if (_currentHalves <= 0)
            {
                return;
            }
            _currentHalves = Mathf.Min(_maxHalves, _currentHalves + halves);
            OnHealthChanged?.Invoke(_currentHalves, _maxHalves);
        }

        /// <summary> 즉사. 무적/되감기 가드를 무시한다 - 즉사 기믹 실패 / 보스전 시간 초과 전용. </summary>
        public void Kill()
        {
            if (_currentHalves <= 0)
            {
                return;
            }
            _currentHalves = 0;
            OnHealthChanged?.Invoke(_currentHalves, _maxHalves);
            OnDeath?.Invoke();
        }

        /// <summary> 기록된 시점의 반칸 수로 되돌린다 (리와인드 복원). </summary>
        public void RestoreHalves(int halves)
        {
            _currentHalves = Mathf.Clamp(halves, 0, _maxHalves);
            OnHealthChanged?.Invoke(_currentHalves, _maxHalves);
        }

        /// <summary> 체력을 최대치로 되돌리고 무적 상태를 해제한다. 분신 풀 재사용 시 호출. </summary>
        public void ResetHearts()
        {
            StopAllCoroutines(); // 진행 중이던 무적 타이머 정리
            _isInvincible  = false;
            _currentHalves = _maxHalves;
            OnHealthChanged?.Invoke(_currentHalves, _maxHalves);
        }

        // 피격 후 무적 타이머. 같은 공격에 연속으로 하트가 깎이는 것을 막는다.
        private IEnumerator CoInvincibility()
        {
            _isInvincible = true;
            yield return _waitInvincible;
            _isInvincible = false;
        }
    }
}
