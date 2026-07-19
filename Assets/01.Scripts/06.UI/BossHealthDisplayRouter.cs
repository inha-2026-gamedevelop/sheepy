// Unity
using UnityEngine;

using Minsung.Boss;

namespace Minsung.UI
{
    // 1페이즈(분신 2체)에는 보스 체력바 대신 분신 개별 체력바를 보여주고, 2페이즈부터는 보스 체력바로 전환한다
    public class BossHealthDisplayRouter : MonoBehaviour
    {
        [SerializeField] private BossController _boss;
        [SerializeField] private GameObject _bossHealthBar;
        [SerializeField] private GameObject[] _cloneHealthBars;

        private void OnEnable()
        {
            if (_boss == null)
            {
                return;
            }

            _boss.OnPhaseChanged += ApplyVisibility;
            _boss.OnBattleStarted += RefreshVisibility;
            ApplyVisibility(_boss.PhaseIndex);
        }

        private void OnDisable()
        {
            if (_boss != null)
            {
                _boss.OnPhaseChanged -= ApplyVisibility;
                _boss.OnBattleStarted -= RefreshVisibility;
            }
        }

        private void RefreshVisibility()
        {
            ApplyVisibility(_boss.PhaseIndex);
        }

        private void ApplyVisibility(int phaseIndex)
        {
            bool isPhase1 = _boss.IsBattleStarted && (phaseIndex == 0);
            bool showBossHealth = _boss.IsBattleStarted && !isPhase1;

            if (_bossHealthBar != null)
            {
                _bossHealthBar.SetActive(showBossHealth);
            }
            foreach (GameObject bar in _cloneHealthBars)
            {
                if (bar != null)
                {
                    bar.SetActive(isPhase1);
                }
            }
        }
    }
}
