// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Boss;
using Minsung.Common.Data;

namespace Minsung.UI
{
    // BossCloneController 개별 체력을 Slider.value(0~1 정규화)로 표시한다 (1페이즈 분신 전용)
    public class BossCloneHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BossCloneController _clone;
        [SerializeField] private Slider _slider;

        private void OnEnable()
        {
            if (_clone == null)
            {
                Redraw(0f, 1f);
                return;
            }

            _clone.OnHealthChanged += Redraw;
            Redraw(_clone.CurrentHealth, GameDB.Boss.CloneHealth);
        }

        private void OnDisable()
        {
            if (_clone != null)
            {
                _clone.OnHealthChanged -= Redraw;
            }
        }

        private void Redraw(float current, float total)
        {
            if (_slider == null)
            {
                return;
            }

            _slider.value = (total > 0f) ? Mathf.Clamp01(current / total) : 0f;
        }
    }
}
