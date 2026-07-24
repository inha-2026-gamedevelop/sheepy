// Unity
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TextMeshPro
using TMPro;

namespace Minsung.UI
{
    /// <summary>4페이즈 전용 무적 아이콘의 마우스오버 설명을 표시한다.</summary>
    [RequireComponent(typeof(Image))]
    public class Boss2InvincibilityIconTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string Description = "'S' 키로 30초마다 사용할 수 있는 무적기입니다.\n2.5초간 무적이 됩니다.\n어쩌면 정말 특별한 것을 막을 수도...";

        [Header("TextMeshPro 설정")]
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField, Min(1f)] private float _fontSize = 18f;

        private GameObject _tooltipPanel;

        private void Awake()
        {
            Image icon = GetComponent<Image>();
            if (icon != null)
            {
                icon.raycastTarget = true;
            }

            CreateTooltip();
        }

        private void OnDisable()
        {
            SetTooltipVisible(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetTooltipVisible(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetTooltipVisible(false);
        }

        private void CreateTooltip()
        {
            _tooltipPanel = new GameObject("Phase4InvincibilityTooltip[Runtime]", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _tooltipPanel.transform.SetParent(transform, false);

            RectTransform panelTransform = _tooltipPanel.GetComponent<RectTransform>();
            panelTransform.anchorMin = new Vector2(0f, 1f);
            panelTransform.anchorMax = new Vector2(0f, 1f);
            panelTransform.pivot = new Vector2(0f, 1f);
            panelTransform.anchoredPosition = new Vector2(68f, 0f);
            panelTransform.sizeDelta = new Vector2(340f, 116f);

            Image background = _tooltipPanel.GetComponent<Image>();
            background.color = new Color(0.06f, 0.06f, 0.09f, 0.94f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_tooltipPanel.transform, false);

            RectTransform textTransform = textObject.GetComponent<RectTransform>();
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = new Vector2(14f, 10f);
            textTransform.offsetMax = new Vector2(-14f, -10f);

            TextMeshProUGUI tooltipText = textObject.GetComponent<TextMeshProUGUI>();
            tooltipText.font = (_font != null) ? _font : TMP_Settings.defaultFontAsset;
            tooltipText.fontSize = _fontSize;
            tooltipText.alignment = TextAlignmentOptions.Center;
            tooltipText.textWrappingMode = TextWrappingModes.Normal;
            tooltipText.raycastTarget = false;
            tooltipText.text = Description;

            SetTooltipVisible(false);
        }

        private void SetTooltipVisible(bool visible)
        {
            if (_tooltipPanel != null)
            {
                _tooltipPanel.SetActive(visible);
            }
        }
    }
}
