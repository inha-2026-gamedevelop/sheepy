// System
using System;

// Unity
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Minsung.Common;
using Minsung.Player;

namespace Minsung.UI
{
    // Map1 최초 진입 시 PlayerHUD(하트/분신/포션)를 순서대로 스포트라이트로 설명하는 튜토리얼 오버레이. 평생 1회만 표시된다.
    public class PlayerHudTutorialUI : MonoBehaviour
    {
        /****************************************
        *             Inner Types
        ****************************************/

        [Serializable]
        private struct PlayerHudTutorialStep
        {
            [SerializeField] private RectTransform _target;         // 강조할 HUD 요소
            [SerializeField, TextArea] private string _description; // 설명 텍스트
            [SerializeField] private Vector2 _textBoxOffset;        // 기본 배치 보정값
            [SerializeField] private float _cutoutPadding;          // 이 단계만의 스포트라이트 여백 - 0 이하면 전역 기본값 사용

            public RectTransform Target => _target;
            public string Description => _description;
            public Vector2 TextBoxOffset => _textBoxOffset;
            public float CutoutPadding => _cutoutPadding;
        }

        /****************************************
        *                Fields
        ****************************************/

        [Header("단계")]
        [SerializeField] private PlayerHudTutorialStep[] _steps; // Hearts -> CloneCounter -> PotionHUD 순서

        [Header("연출 튜닝")]
        [SerializeField] private float _dimAlpha       = 0.7f;
        [SerializeField] private float _cutoutPadding  = 16f;
        [SerializeField] private float _lineThickness  = 4f;
        [SerializeField] private float _lineEndGap     = 8f;
        [SerializeField] private float _textBoxOffsetX = 400f;

        [Header("참조 - 스포트라이트")]
        [SerializeField] private Image _dimTop;
        [SerializeField] private Image _dimBottom;
        [SerializeField] private Image _dimLeft;
        [SerializeField] private Image _dimRight;

        [Header("참조 - 연결선/텍스트")]
        [SerializeField] private RectTransform _lineRect;    // pivot (0, 0.5)
        [SerializeField] private RectTransform _textBoxRect; // pivot (0, 0.5)
        [SerializeField] private TMP_Text      _descriptionText;
        [SerializeField] private TMP_Text      _hintText; // 화면 중앙 하단 - 스텝과 무관하게 항상 표시

        private RectTransform    _overlayRect;   // 자기 자신의 RectTransform = 풀스크린 오버레이
        private Canvas           _overlayCanvas; // FixedAspectRatioController가 런타임에 ScreenSpaceCamera로 전환하므로 매 프레임 worldCamera를 다시 읽는다
        private PlayerController _player;
        private RectTransform    _currentTarget;
        private int _stepIndex = -1;

        private readonly Vector3[] _worldCorners = new Vector3[4]; // GetWorldCorners 재사용 버퍼 (GC 회피)

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _overlayRect   = (RectTransform)transform;
            _overlayCanvas = GetComponent<Canvas>();

            Color dimColor = new Color(0f, 0f, 0f, _dimAlpha);
            _dimTop.color    = dimColor;
            _dimBottom.color = dimColor;
            _dimLeft.color   = dimColor;
            _dimRight.color  = dimColor;

            if (_hintText != null)
            {
                _hintText.text = "스페이스바를 눌러 다음거 보기";
            }
        }

        private void Start()
        {
            if ((_steps == null) || (_steps.Length == 0))
            {
                gameObject.SetActive(false);
                return;
            }
            if ((SaveManager.Instance != null) && SaveManager.Instance.IsHudTutorialSeen())
            {
                gameObject.SetActive(false);
                return;
            }

            _player = FindAnyObjectByType<PlayerController>();
            _player?.SetControlsFrozen(true);

            ShowStep(0);
        }

        private void Update()
        {
            // HUD 캔버스는 FixedAspectRatioController가 씬 로드 후 몇 프레임 지나 ScreenSpaceCamera로 전환한다 -
            // 스텝이 바뀔 때뿐 아니라 매 프레임 다시 계산해야 그 전환 이전/해상도 변경에도 항상 정확한 위치를 유지한다
            if (_currentTarget != null)
            {
                RefreshSpotlight();
            }

            if (Input.GetKeyDown(Constants.Player.KEY_JUMP))
            {
                AdvanceStep();
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        private void AdvanceStep()
        {
            int nextIndex = _stepIndex + 1;
            if (nextIndex >= _steps.Length)
            {
                Finish();
                return;
            }
            ShowStep(nextIndex);
        }

        private void ShowStep(int index)
        {
            SetCloneCounterPreview(_currentTarget, false); // 이전 단계가 분신 카운터였다면 프리뷰 해제

            _stepIndex     = index;
            _currentTarget = _steps[index].Target;

            SetCloneCounterPreview(_currentTarget, true); // 이번 단계가 분신 카운터라면 빈 상태여도 아이콘을 채워 보여준다

            if (_descriptionText != null)
            {
                _descriptionText.text = _steps[index].Description;
            }

            RefreshSpotlight();
        }

        private void Finish()
        {
            SetCloneCounterPreview(_currentTarget, false);

            SaveManager.Instance?.SetHudTutorialSeen(true);
            _player?.SetControlsFrozen(false);
            _currentTarget = null;
            gameObject.SetActive(false);
        }

        // 대상이 분신 카운터 UI라면 강제 프리뷰를 켜고 끈다 - 튜토리얼 시점엔 분신이 없어 아이콘이 비어있을 수 있어서다
        private static void SetCloneCounterPreview(RectTransform target, bool enabled)
        {
            if (target == null)
            {
                return;
            }
            if (target.TryGetComponent(out CloneCounterUI cloneCounter))
            {
                cloneCounter.SetForcedPreview(enabled);
            }
        }

        private void RefreshSpotlight()
        {
            float padding = (_steps[_stepIndex].CutoutPadding > 0f) ? _steps[_stepIndex].CutoutPadding : _cutoutPadding;
            Rect hole = ComputeHoleRect(_currentTarget, padding);
            ApplyCutout(hole);
            PositionLineAndText(hole, _steps[_stepIndex].TextBoxOffset);
        }

        // 대상 RectTransform의 화면 좌표 경계를 오버레이 로컬 좌표로 환산한다.
        // GameHUD/오버레이 둘 다 FixedAspectRatioController가 붙여준 ScreenSpaceCamera(@UICamera)로 그려지므로,
        // 각 캔버스의 실제 worldCamera를 그대로 넘겨야 두 캔버스의 정렬 순서/카메라 전환 시점과 무관하게 항상 정확하다
        private Rect ComputeHoleRect(RectTransform target, float padding)
        {
            target.GetWorldCorners(_worldCorners);

            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCamera = (targetCanvas != null) ? targetCanvas.worldCamera : null;
            Camera overlayCamera = (_overlayCanvas != null) ? _overlayCanvas.worldCamera : null;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < _worldCorners.Length; ++i)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetCamera, _worldCorners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlayRect, screenPoint, overlayCamera, out Vector2 localPoint);
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            min -= new Vector2(padding, padding);
            max += new Vector2(padding, padding);

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        // 구멍(hole) 주위 4개 프레임을 화면 전체 - 구멍 영역만 덮도록 재배치한다
        private void ApplyCutout(Rect hole)
        {
            Rect overlayRect = _overlayRect.rect;
            float xMin = overlayRect.xMin;
            float xMax = overlayRect.xMax;
            float yMin = overlayRect.yMin;
            float yMax = overlayRect.yMax;

            _dimLeft.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            _dimLeft.rectTransform.sizeDelta        = new Vector2(hole.xMin - xMin, yMax - yMin);

            _dimRight.rectTransform.anchoredPosition = new Vector2(hole.xMax - xMin, 0f);
            _dimRight.rectTransform.sizeDelta        = new Vector2(xMax - hole.xMax, yMax - yMin);

            _dimBottom.rectTransform.anchoredPosition = new Vector2(hole.xMin - xMin, 0f);
            _dimBottom.rectTransform.sizeDelta        = new Vector2(hole.width, hole.yMin - yMin);

            _dimTop.rectTransform.anchoredPosition = new Vector2(hole.xMin - xMin, hole.yMax - yMin);
            _dimTop.rectTransform.sizeDelta        = new Vector2(hole.width, yMax - hole.yMax);
        }

        // 구멍 오른쪽에서 텍스트박스까지 연결선을 긋고, 텍스트박스를 그 옆에 배치한다
        private void PositionLineAndText(Rect hole, Vector2 textBoxOffset)
        {
            float xMin = _overlayRect.rect.xMin;
            float yMin = _overlayRect.rect.yMin;

            Vector2 start   = new Vector2(hole.xMax, (hole.yMin + hole.yMax) * 0.5f);
            Vector2 basePos = start + new Vector2(_textBoxOffsetX, 0f) + textBoxOffset;

            _textBoxRect.anchoredPosition = new Vector2(basePos.x - xMin, basePos.y - yMin);

            Vector2 end = basePos - new Vector2(_lineEndGap, 0f);
            float distance = Vector2.Distance(start, end);
            float angleDeg = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;

            _lineRect.anchoredPosition = new Vector2(start.x - xMin, start.y - yMin);
            _lineRect.sizeDelta        = new Vector2(distance, _lineThickness);
            _lineRect.localEulerAngles = new Vector3(0f, 0f, angleDeg);
        }
    }
}
