// Unity
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

using Minsung.Boss;

namespace Minsung.UI
{
    // 보스 감정 아이콘 위에 마우스를 올리면 현재 감정의 효과를 설명하는 정보창을 띄운다
    public class BossEmotionIconTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private BossController _boss;
        [SerializeField] private GameObject _tooltipPanel;
        [SerializeField] private TextMeshProUGUI _tooltipText;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if ((_boss == null) || (_tooltipPanel == null) || (_tooltipText == null))
            {
                return;
            }
            if (!TryGetDescription(_boss.CurrentEmotion, out string description))
            {
                return;
            }

            _tooltipText.text = description;
            _tooltipPanel.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltipPanel != null)
            {
                _tooltipPanel.SetActive(false);
            }
        }

        // 감정별 효과 설명
        private static bool TryGetDescription(BossEmotion emotion, out string description)
        {
            switch (emotion)
            {
                case BossEmotion.Black:
                    description = "검정 - 모든 공격을 반사한다";
                    return true;
                case BossEmotion.White:
                    description = "하양 - 본체 공격을 반사한다";
                    return true;
                case BossEmotion.Navy:
                    description = "남색 - 분신 공격을 반사한다";
                    return true;
                case BossEmotion.Pink:
                    description = "핑크 - 낙뢰 낙하 빈도가 2배로 늘어난다";
                    return true;
                case BossEmotion.Blue:
                    description = "파랑 - 낙뢰 낙하 빈도가 절반으로 줄고, 회복 하트가 맵에 나타난다";
                    return true;
                case BossEmotion.Angry:
                    description = "화남 - 주기적으로 조작이 반전된다";
                    return true;
                default:
                    description = null;
                    return false;
            }
        }
    }
}
