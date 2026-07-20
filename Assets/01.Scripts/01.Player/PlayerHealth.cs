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

        // 전용 무적키(보스 즉사기 회피) - 피격 후 자동 무적(_isInvincible)과 별도 플래그로 관리
        // 지속/쿨타임은 슬로우모션(Time.timeScale 변조) 중에도 체감 시간이 변하지 않도록 unscaled 기준으로 센다
        private bool _isDodgeInvincible;
        private bool _dodgeInvincibleOnCooldown;
        private float _dodgeInvincibleDuration;
        private float _dodgeInvincibleCooldown;

        public int MaxHalves => _maxHalves;
        public int CurrentHalves => _currentHalves;
        public bool IsInvincible => _isInvincible;
        public bool IsDodgeInvincible => _isDodgeInvincible;
        public bool IsDodgeInvincibleReady => !_dodgeInvincibleOnCooldown;

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
            _dodgeInvincibleDuration = playerSo.DodgeInvincibleDuration;
            _dodgeInvincibleCooldown = playerSo.DodgeInvincibleCooldown;
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
            if (_isInvincible || _isDodgeInvincible || _currentHalves <= 0)
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
            StopAllCoroutines(); // 진행 중이던 무적/쿨타임 타이머 정리
            _isInvincible  = false;
            _isDodgeInvincible = false;
            _dodgeInvincibleOnCooldown = false;
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

        /// <summary> 전용 무적키 요청. 쿨타임 중이면 무시, 아니면 짧은 무적을 시작하고 쿨타임에 들어간다. </summary>
        public void RequestDodgeInvincible()
        {
            if (_dodgeInvincibleOnCooldown)
            {
                return;
            }
            StartCoroutine(CoDodgeInvincible());
            StartCoroutine(CoDodgeCooldown());
        }

        private IEnumerator CoDodgeInvincible()
        {
            _isDodgeInvincible = true;
            float endTime = Time.unscaledTime + _dodgeInvincibleDuration;
            while (Time.unscaledTime < endTime)
            {
                yield return null;
            }
            _isDodgeInvincible = false;
        }

        // 쿨타임은 무적 지속시간과 별개로, 발동 순간부터 카운트한다.
        private IEnumerator CoDodgeCooldown()
        {
            _dodgeInvincibleOnCooldown = true;
            float endTime = Time.unscaledTime + _dodgeInvincibleCooldown;
            while (Time.unscaledTime < endTime)
            {
                yield return null;
            }
            _dodgeInvincibleOnCooldown = false;
        }
    }
}
