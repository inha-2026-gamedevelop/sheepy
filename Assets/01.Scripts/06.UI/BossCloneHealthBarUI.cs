// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Boss;
using Minsung.Common.Data;

namespace Minsung.UI
{
    public class BossCloneHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BossController      _boss;
        [SerializeField] private BossCloneController _clone;
        [SerializeField] private Slider _slider;

        private float _lastHealth = 1f; // 페이즈 전환 시점에도 사망 여부를 판단할 수 있게 마지막 체력을 기억

        private void OnEnable()
        {
            if (_clone == null)
            {
                Redraw(0f, 1f);
                return;
            }

            _clone.OnHealthChanged += Redraw;
            if (_boss != null)
            {
                _boss.OnPhaseChanged += HandlePhaseChanged;
            }
            Redraw(_clone.CurrentHealth, GameDB.Boss.CloneHealth);
        }

        private void OnDisable()
        {
            if (_clone != null)
            {
                _clone.OnHealthChanged -= Redraw;
            }
            if (_boss != null)
            {
                _boss.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void Redraw(float current, float total)
        {
            _lastHealth = current;

            if (_slider != null)
            {
                _slider.value = (total > 0f) ? Mathf.Clamp01(current / total) : 0f;
            }
            RefreshVisibility();
        }

        private void HandlePhaseChanged(int _)
        {
            RefreshVisibility();
        }

        // 사망했거나(체력 0) 1페이즈가 끝났으면(분신 퇴장) 숨긴다
        private void RefreshVisibility()
        {
            bool alive    = _lastHealth > 0f;
            bool inPhase1 = (_boss == null) || (_boss.PhaseIndex == 0);
            SetVisible(alive && inPhase1);
        }

        // 슬라이더 자식(Background/Fill Area)만 껐다 켠다 - 루트(이 컴포넌트)는 계속 활성 상태로 유지해야
        // 부활/페이즈 전환 이벤트를 계속 받을 수 있다 (루트를 끄면 OnDisable에서 구독이 끊긴다)
        private void SetVisible(bool visible)
        {
            if (_slider == null)
            {
                return;
            }
            foreach (Transform child in _slider.transform)
            {
                child.gameObject.SetActive(visible);
            }
        }
    }
}
