// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Boss;

namespace Minsung.UI
{
    public class BossHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BossController _boss;
        [SerializeField] private Slider _slider;
        [SerializeField] private GameObject[] _phaseNotches;

        private void OnEnable()
        {
            if (_boss == null)
            {
                Redraw(0f, 1f);
                return;
            }

            _boss.OnHealthChanged += Redraw;
            _boss.OnPhaseChanged  += RedrawPhaseVisibility;
            Redraw(_boss.CurrentHealth, _boss.TotalHealth);
            RedrawPhaseVisibility(_boss.PhaseIndex);
        }

        private void OnDisable()
        {
            if (_boss != null)
            {
                _boss.OnHealthChanged -= Redraw;
                _boss.OnPhaseChanged  -= RedrawPhaseVisibility;
            }
        }

        // 인스펙터 미지정이면 씬의 보스에서 자동 연결
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
                _boss.OnPhaseChanged  += RedrawPhaseVisibility;
            }
            Redraw(_boss.CurrentHealth, _boss.TotalHealth);
            RedrawPhaseVisibility(_boss.PhaseIndex);
        }

        private void RedrawPhaseVisibility(int phaseIndex)
        {
            bool visible = (_boss != null) && _boss.IsBattleStarted && (phaseIndex >= 1);
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(visible);
            }

            if (visible && (_boss != null))
            {
                RedrawNotches(_boss.CurrentHealth, _boss.TotalHealth);
            }
        }

        private void Redraw(float current, float total)
        {
            if (_slider == null)
            {
                return;
            }

            _slider.value = (total > 0f) ? Mathf.Clamp01(current / total) : 0f;
            RedrawNotches(current, total);
        }

        // 노치 i는 페이즈 경계(total - PhaseHealth * (i+1)) 지점 - HP가 그 아래로 깎이면 숨긴다 (되감기로 회복하면 다시 나타남)
        private void RedrawNotches(float current, float total)
        {
            if (_phaseNotches == null)
            {
                return;
            }

            for (int i = 0; i < _phaseNotches.Length; ++i)
            {
                if (_phaseNotches[i] == null)
                {
                    continue;
                }
                float boundaryHealth = total - ((_boss != null ? _boss.PhaseHealthSpan : 0f) * (i + 1));
                _phaseNotches[i].SetActive(current > boundaryHealth);
            }
        }
    }
}
