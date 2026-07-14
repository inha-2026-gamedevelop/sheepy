// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Boss;
using Minsung.Common.Data;

namespace Minsung.UI
{
    // BossController의 단일 총 HP를 Slider.value(0~1 정규화)로 표시한다
    public class BossHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BossController _boss;
        [SerializeField] private Slider _slider;

        private void OnEnable()
        {
            if (_boss == null)
            {
                Redraw(0f, 1f);
                return;
            }

            _boss.OnHealthChanged += Redraw;
            Redraw(_boss.CurrentHealth, GameDB.Boss.TotalHealth);
        }

        private void OnDisable()
        {
            if (_boss != null)
            {
                _boss.OnHealthChanged -= Redraw;
            }
        }

        // 인스펙터 미지정이면 씬의 보스에서 자동 연결 (HUD 프리팹 드롭인용)
        private void Start()
        {
            if (_boss == null)
            {
                _boss = FindAnyObjectByType<BossController>();
                if (_boss == null)
                {
                    gameObject.SetActive(false); // 보스 없는 맵에서는 바를 숨긴다
                    return;
                }
                _boss.OnHealthChanged += Redraw;
            }
            Redraw(_boss.CurrentHealth, GameDB.Boss.TotalHealth);
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
