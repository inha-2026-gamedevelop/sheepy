// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Minsung.Common;
using Minsung.Utility;

namespace Minsung.CameraSystem
{
    // 1920x1080(16:9) 게임 화면을 유지한다. 울트라와이드는 좌우, 세로가 긴 화면은 상하를 검정 바로 채운다.
    [AddComponentMenu("Minsung/Fixed Aspect Ratio Controller")]
    public class FixedAspectRatioController : PersistentSingleton<FixedAspectRatioController>
    {
        /****************************************
        *                Fields
        ****************************************/

        private const int BAR_SORTING_ORDER = short.MaxValue;
        private const int CANVAS_REFRESH_FRAME_INTERVAL = 30;

        private readonly Dictionary<Canvas, CanvasState> _overlayCanvasStates = new();

        private Camera           _targetCamera;
        private RectTransform[]  _blackBars;
        private int              _screenWidth;
        private int              _screenHeight;

        private struct CanvasState
        {
            public RenderMode RenderMode;
            public Camera     WorldCamera;
            public float      PlaneDistance;
        }

        /****************************************
        *              Unity Event
        ****************************************/

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetStatic();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            EnsureCreated("@FixedAspectRatioController");
        }

        protected override void OnSingletonAwake()
        {
            CreateBlackBars();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            StartCoroutine(CoApplyAfterSceneLoad());
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            RestoreOverlayCanvases();
            base.OnDestroy();
        }

        private void LateUpdate()
        {
            if ((_targetCamera == null) || !_targetCamera.isActiveAndEnabled)
            {
                FindTargetCamera();
            }

            if (_targetCamera == null)
            {
                return;
            }

            if ((_screenWidth != Screen.width) || (_screenHeight != Screen.height))
            {
                ApplyViewport();
            }

            // 런타임에 생성되는 UI(보스 HUD, 팝업 등)도 16:9 카메라 영역을 기준으로 맞춘다.
            if ((Time.frameCount % CANVAS_REFRESH_FRAME_INTERVAL) == 0)
            {
                ApplyOverlayCanvasViewport();
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(CoApplyAfterSceneLoad());
        }

        private IEnumerator CoApplyAfterSceneLoad()
        {
            yield return null;
            _targetCamera = null;
            FindTargetCamera();
            ApplyViewport();

            // Scene Start에서 만들어지는 Canvas까지 포함한다.
            yield return null;
            ApplyOverlayCanvasViewport();
        }

        private void FindTargetCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            _targetCamera = mainCamera;
        }

        private void ApplyViewport()
        {
            if ((_targetCamera == null) || (Screen.height <= 0))
            {
                return;
            }

            float screenAspect = (float)Screen.width / Screen.height;
            Rect viewport = GetViewport(screenAspect);
            _targetCamera.rect = viewport;

            UpdateBlackBars(viewport);
            ApplyOverlayCanvasViewport();
            _screenWidth  = Screen.width;
            _screenHeight = Screen.height;
        }

        private void ApplyOverlayCanvasViewport()
        {
            if (_targetCamera == null)
            {
                return;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in canvases)
            {
                if ((canvas == null) || !canvas.isRootCanvas || canvas.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!_overlayCanvasStates.TryGetValue(canvas, out CanvasState state))
                {
                    if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    {
                        continue;
                    }

                    state = new CanvasState
                    {
                        RenderMode = canvas.renderMode,
                        WorldCamera = canvas.worldCamera,
                        PlaneDistance = canvas.planeDistance,
                    };
                    _overlayCanvasStates.Add(canvas, state);
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _targetCamera;
                canvas.planeDistance = state.PlaneDistance;
            }
        }

        private void RestoreOverlayCanvases()
        {
            foreach (KeyValuePair<Canvas, CanvasState> pair in _overlayCanvasStates)
            {
                Canvas canvas = pair.Key;
                if (canvas == null)
                {
                    continue;
                }

                canvas.renderMode = pair.Value.RenderMode;
                canvas.worldCamera = pair.Value.WorldCamera;
                canvas.planeDistance = pair.Value.PlaneDistance;
            }

            _overlayCanvasStates.Clear();
        }

        private static Rect GetViewport(float screenAspect)
        {
            if (screenAspect > Constants.Camera.REFERENCE_ASPECT)
            {
                float width = Constants.Camera.REFERENCE_ASPECT / screenAspect;
                return new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }

            float height = screenAspect / Constants.Camera.REFERENCE_ASPECT;
            return new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }

        private void CreateBlackBars()
        {
            GameObject canvasObject = new GameObject("Aspect Ratio Black Bars", typeof(RectTransform));
            canvasObject.transform.SetParent(transform);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder    = BAR_SORTING_ORDER;

            _blackBars = new RectTransform[4];
            for (int i = 0; i < _blackBars.Length; ++i)
            {
                GameObject barObject = new GameObject($"Black Bar {i}", typeof(RectTransform));
                barObject.transform.SetParent(canvasObject.transform, false);

                Image image = barObject.AddComponent<Image>();
                image.color         = Color.black;
                image.raycastTarget = false;
                _blackBars[i]       = image.rectTransform;
            }
        }

        private void UpdateBlackBars(Rect viewport)
        {
            SetBlackBar(_blackBars[0], new Vector2(0f, 0f), new Vector2(viewport.xMin, 1f));
            SetBlackBar(_blackBars[1], new Vector2(viewport.xMax, 0f), new Vector2(1f, 1f));
            SetBlackBar(_blackBars[2], new Vector2(0f, 0f), new Vector2(1f, viewport.yMin));
            SetBlackBar(_blackBars[3], new Vector2(0f, viewport.yMax), new Vector2(1f, 1f));
        }

        private static void SetBlackBar(RectTransform bar, Vector2 anchorMin, Vector2 anchorMax)
        {
            bool visible = ((anchorMax.x - anchorMin.x) > 0f) && ((anchorMax.y - anchorMin.y) > 0f);
            bar.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            bar.anchorMin        = anchorMin;
            bar.anchorMax        = anchorMax;
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta        = Vector2.zero;
        }
    }
}
