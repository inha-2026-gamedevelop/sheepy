// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Minsung.Common;
using Minsung.Utility;

namespace Minsung.UI
{
    // 화면 우상단에 실시간 프레임 수를 "60 FPS" 형태로 띄우는 개발용 오버레이.
    // 씬마다 배치할 필요 없이 게임 시작 시 자동 생성되어 DontDestroyOnLoad로 모든 씬을 따라다닌다.
    public class FpsCounterUI : PersistentSingleton<FpsCounterUI>
    {
        /****************************************
        *                Fields
        ****************************************/

        private TMP_Text _label;
        private float    _elapsed; // 갱신 주기 누적 시간(unscaled)
        private int      _frames;  // 갱신 주기 동안 그린 프레임 수
        private int      _lastFps = -1; // 값이 바뀔 때만 텍스트를 갱신해 문자열 할당을 줄인다

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 깨끗하게 초기화되도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetStatic();
        }

        // 씬에 배치하지 않아도 게임 시작 시 자동 생성 (이후 모든 씬을 따라다닌다)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            EnsureCreated("FpsCounterUI");
        }

        protected override void OnSingletonAwake()
        {
            BuildOverlay();
        }

        private void Update()
        {
            // 슬로우모션/일시정지(timeScale)에 영향받지 않는 실제 렌더 프레임을 재기 위해 unscaled 사용
            _elapsed += Time.unscaledDeltaTime;
            ++_frames;

            if (_elapsed < Constants.UI.FPS_REFRESH_INTERVAL)
            {
                return;
            }

            int fps  = (_elapsed > 0f) ? Mathf.RoundToInt(_frames / _elapsed) : 0;
            _frames  = 0;
            _elapsed = 0f;

            if (fps != _lastFps)
            {
                _lastFps    = fps;
                _label.text = fps + " FPS";
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        // 우상단에 검정 배경 박스 + 흰 글자를 코드로 구성한다
        private void BuildOverlay()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Constants.UI.FPS_CANVAS_SORT_ORDER;

            // 12pt를 해상도와 무관하게 12px로 고정 (다른 캔버스의 스케일러 영향 배제)
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            GameObject box = new GameObject("Box");
            box.transform.SetParent(transform, false);

            RectTransform boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin        = Vector2.one;
            boxRect.anchorMax        = Vector2.one;
            boxRect.pivot            = Vector2.one;
            boxRect.anchoredPosition = new Vector2(-Constants.UI.FPS_EDGE_PADDING, -Constants.UI.FPS_EDGE_PADDING);

            Image bg = box.AddComponent<Image>();
            bg.color         = Color.black;
            bg.raycastTarget = false; // 입력을 가로막지 않게

            // 글자 크기에 맞춰 검정 박스가 자동으로 줄고 늘어나게 한다 (한 자리수/세 자리수 대응)
            HorizontalLayoutGroup layout = box.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(
                Constants.UI.FPS_BOX_PADDING, Constants.UI.FPS_BOX_PADDING,
                Constants.UI.FPS_BOX_PADDING, Constants.UI.FPS_BOX_PADDING);
            layout.childAlignment     = TextAnchor.MiddleRight;
            layout.childControlWidth  = true;
            layout.childControlHeight = true;

            ContentSizeFitter fitter = box.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(box.transform, false);

            _label = labelObject.AddComponent<TextMeshProUGUI>();
            _label.fontSize      = Constants.UI.FPS_FONT_SIZE;
            _label.color         = Color.white;
            _label.alignment     = TextAlignmentOptions.Right;
            _label.raycastTarget = false;
            _label.text          = "-- FPS";
        }
    }
}
