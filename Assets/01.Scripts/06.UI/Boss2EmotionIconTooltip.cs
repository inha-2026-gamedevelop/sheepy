// Unity
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

using Minsung.Boss2;

namespace Minsung.UI
{
    /// <summary>Boss2 HUD 감정 아이콘의 마우스 오버 설명을 표시한다.</summary>
    [RequireComponent(typeof(Image))]
    public class Boss2EmotionIconTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Boss2EmotionController _emotionController;
        private GameObject _tooltipPanel;
        private TextMeshProUGUI _tooltipText;

        private void Awake()
        {
            Image icon = GetComponent<Image>();
            if (icon != null)
            {
                icon.raycastTarget = true;
            }

            CreateTooltip();
        }

        public void Configure(Boss2EmotionController emotionController)
        {
            _emotionController = emotionController;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if ((_emotionController == null) || (_tooltipPanel == null) || (_tooltipText == null))
            {
                return;
            }

            if (!TryGetDescription(_emotionController.CurrentEmotion, out string description))
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

        private void CreateTooltip()
        {
            _tooltipPanel = new GameObject("EmotionTooltip[Runtime]", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _tooltipPanel.transform.SetParent(transform, false);

            RectTransform panelTransform = _tooltipPanel.GetComponent<RectTransform>();
            panelTransform.anchorMin = Vector2.zero;
            panelTransform.anchorMax = Vector2.zero;
            panelTransform.pivot = new Vector2(0.5f, 1f);
            panelTransform.anchoredPosition = new Vector2(24f, -54f);
            panelTransform.sizeDelta = new Vector2(280f, 72f);

            Image background = _tooltipPanel.GetComponent<Image>();
            background.color = new Color(0.06f, 0.06f, 0.09f, 0.92f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_tooltipPanel.transform, false);

            RectTransform textTransform = textObject.GetComponent<RectTransform>();
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = new Vector2(12f, 8f);
            textTransform.offsetMax = new Vector2(-12f, -8f);

            _tooltipText = textObject.GetComponent<TextMeshProUGUI>();
            _tooltipText.font = TMP_Settings.defaultFontAsset;
            _tooltipText.fontSize = 18f;
            _tooltipText.alignment = TextAlignmentOptions.Center;
            _tooltipText.textWrappingMode = TextWrappingModes.Normal;
            _tooltipText.raycastTarget = false;

            _tooltipPanel.SetActive(false);
        }

        private static bool TryGetDescription(Boss2Emotion emotion, out string description)
        {
            switch (emotion)
            {
                case Boss2Emotion.Black:
                    description = "검정: 모든 공격을 반사합니다.";
                    return true;
                case Boss2Emotion.White:
                    description = "흰색: 보스 본체 공격을 반사합니다.";
                    return true;
                case Boss2Emotion.Navy:
                    description = "남색: 분신 공격을 반사합니다.";
                    return true;
                case Boss2Emotion.Pink:
                    description = "분홍: 공격 속도가 2배가 됩니다.";
                    return true;
                case Boss2Emotion.Blue:
                    description = "파랑: 공격 속도가 느려지고, 피격 시 마비됩니다.";
                    return true;
                case Boss2Emotion.Angry:
                    description = "화남: 주기적으로 조작이 반전됩니다.";
                    return true;
                default:
                    description = null;
                    return false;
            }
        }
    }
}
