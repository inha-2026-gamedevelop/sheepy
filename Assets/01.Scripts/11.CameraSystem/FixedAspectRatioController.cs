// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Minsung.Common;
using Minsung.UI;
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

        private static readonly Rect FULL_SCREEN_VIEWPORT = new Rect(0f, 0f, 1f, 1f);

        // ScreenSpaceCamera로 바꾸면 캔버스가 월드 스프라이트와 같은 정렬 규칙을 타므로
        // 최상위 Sorting Layer + 이 여유값만큼 올려 맵 오브젝트에 가리지 않게 한다
        private const int OVERLAY_SORTING_ORDER_OFFSET = 10000;

        private readonly Dictionary<Canvas, CanvasState> _overlayCanvasStates = new();
        private readonly List<Canvas> _releasedCanvases = new();

        private Camera           _targetCamera;
        private Camera           _uiCamera;   // HUD 전용 오버레이 카메라(PP off) - UI가 Bloom/색보정/비네트에 물들지 않게 한다
        private int              _uiLayer = -1;
        private RectTransform[]  _blackBars;
        private int              _screenWidth;
        private int              _screenHeight;
        private int              _topSortingLayerId;
        private bool             _spaceTearViewportActive;

        private struct CanvasState
        {
            public RenderMode RenderMode;
            public Camera     WorldCamera;
            public float      PlaneDistance;
            public int        SortingLayerId;
            public int        SortingOrder;
            public int        GameObjectLayer;
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
            _uiLayer           = LayerMask.NameToLayer("UI");
            _topSortingLayerId = FindTopSortingLayerId();
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

            UpdateUiCamera();

            if (_spaceTearViewportActive)
            {
                if ((_screenWidth != Screen.width) || (_screenHeight != Screen.height) || (_targetCamera.rect != FULL_SCREEN_VIEWPORT))
                {
                    ApplyFullScreenViewport();
                }
                return;
            }

            if ((_screenWidth != Screen.width) || (_screenHeight != Screen.height))
            {
                ApplyViewport();
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary>
        /// 씬 로드/해상도 변경 이후에 루트 Canvas를 새로 만들었다면 호출한다 - 그 캔버스도 16:9 영역 기준으로 맞춘다.
        /// (씬 배치 UI와 Awake에서 만드는 UI는 씬 로드 시 자동으로 잡히므로 호출할 필요 없다)
        /// </summary>
        public void RefreshCanvases()
        {
            ApplyOverlayCanvasViewport();
        }

        /// <summary>
        /// 공간 찢기 연출 동안 16:9 뷰포트와 검정 여백을 해제한다.
        /// 연출이 끝나면 현재 해상도 기준의 일반 레터박스 상태로 즉시 되돌린다.
        /// </summary>
        public void SetSpaceTearViewport(bool isActive)
        {
            if (_spaceTearViewportActive == isActive)
            {
                return;
            }

            _spaceTearViewportActive = isActive;
            FindTargetCamera();
            if (_targetCamera == null)
            {
                _screenWidth  = 0;
                _screenHeight = 0;
                return;
            }

            if (_spaceTearViewportActive)
            {
                ApplyFullScreenViewport();
                return;
            }

            ApplyViewport();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PruneDestroyedCanvasStates();
            StartCoroutine(CoApplyAfterSceneLoad());
        }

        // 씬이 바뀌면 이전 씬의 캔버스는 파괴된다 - 원본 상태 기록을 계속 들고 있으면 계속 쌓인다
        private void PruneDestroyedCanvasStates()
        {
            _releasedCanvases.Clear();
            foreach (KeyValuePair<Canvas, CanvasState> pair in _overlayCanvasStates)
            {
                if (pair.Key == null)
                {
                    _releasedCanvases.Add(pair.Key);
                }
            }

            foreach (Canvas canvas in _releasedCanvases)
            {
                _overlayCanvasStates.Remove(canvas);
            }
            _releasedCanvases.Clear();
        }

        private IEnumerator CoApplyAfterSceneLoad()
        {
            yield return null;
            _targetCamera = null;
            FindTargetCamera();
            if (_spaceTearViewportActive)
            {
                ApplyFullScreenViewport();
            }
            else
            {
                ApplyViewport();
            }

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
            UpdateUiCamera();
        }

        // HUD 전용 오버레이 카메라를 보장하고, 현재 메인 카메라 스택에 얹은 뒤 투영/뷰포트를 메인과 맞춘다
        // 이 카메라는 PP를 끄고 UI 레이어만 그려, 월드 포스트프로세싱은 그대로 두고 UI만 물들지 않게 한다
        private void UpdateUiCamera()
        {
            if (_targetCamera == null)
            {
                return;
            }

            EnsureUiCamera();
            RegisterUiCameraToStack();

            Transform src = _targetCamera.transform;
            _uiCamera.transform.SetPositionAndRotation(src.position, src.rotation);
            _uiCamera.orthographic     = _targetCamera.orthographic;
            _uiCamera.orthographicSize = _targetCamera.orthographicSize;
            _uiCamera.nearClipPlane    = _targetCamera.nearClipPlane;
            _uiCamera.farClipPlane     = _targetCamera.farClipPlane;

            // 공간찢기 연출 중에도 켜 둔다 - 가짜 창/조각/폴백 데스크톱이 담긴 프레젠테이션 캔버스가 이 카메라로 그려지기 때문
            // (연출 중 창 안으로 캡처되는 HUD는 별도의 _hudCamera가 RenderTexture로 처리한다)
            _uiCamera.enabled = true;
        }

        private void EnsureUiCamera()
        {
            if (_uiCamera != null)
            {
                return;
            }

            GameObject go = new GameObject("@UICamera");
            go.transform.SetParent(transform, false);

            _uiCamera = go.AddComponent<Camera>();
            _uiCamera.orthographic = true;
            _uiCamera.clearFlags   = CameraClearFlags.Depth;
            _uiCamera.cullingMask  = (_uiLayer >= 0) ? (1 << _uiLayer) : 0;

            UniversalAdditionalCameraData data = _uiCamera.GetUniversalAdditionalCameraData();
            data.renderType           = CameraRenderType.Overlay;
            data.renderPostProcessing = false;
        }

        // 오버레이 카메라는 스택에 등록돼야 렌더된다 - 씬마다 메인 카메라가 바뀌므로 매번 등록을 보장한다
        private void RegisterUiCameraToStack()
        {
            if ((_targetCamera == null) || (_uiCamera == null) || (_targetCamera == _uiCamera))
            {
                return;
            }

            UniversalAdditionalCameraData baseData = _targetCamera.GetUniversalAdditionalCameraData();
            if (baseData == null)
            {
                return;
            }
            if (!baseData.cameraStack.Contains(_uiCamera))
            {
                baseData.cameraStack.Add(_uiCamera);
            }

            // 메인 카메라가 UI 레이어를 함께 그리면 HUD가 두 번(월드 PP + 오버레이) 렌더돼 겹쳐 보인다 - UI는 오버레이 카메라에만 맡긴다
            if (_uiLayer >= 0)
            {
                _targetCamera.cullingMask &= ~(1 << _uiLayer);
            }
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

        private void ApplyFullScreenViewport()
        {
            if ((_targetCamera == null) || (Screen.height <= 0))
            {
                return;
            }

            _targetCamera.rect = FULL_SCREEN_VIEWPORT;

            UpdateBlackBars(FULL_SCREEN_VIEWPORT);
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

                // FPS 카운터는 letterbox 뷰포트에 갇히지 않고 항상 화면 전체 위에 떠 있어야 한다
                if (canvas.TryGetComponent(out FpsCounterUI _))
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
                        SortingLayerId = canvas.sortingLayerID,
                        SortingOrder = canvas.sortingOrder,
                        GameObjectLayer = canvas.gameObject.layer,
                    };
                    _overlayCanvasStates.Add(canvas, state);
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                // 월드+PP 메인 카메라가 아니라 PP 꺼진 UI 오버레이 카메라로 그린다 - HUD가 포스트프로세싱에 물들지 않는다
                canvas.worldCamera = (_uiCamera != null) ? _uiCamera : _targetCamera;
                canvas.planeDistance = state.PlaneDistance;

                // UI 오버레이 카메라는 UI 레이어만 그리므로 캔버스도 UI 레이어에 있어야 렌더된다
                if (_uiLayer >= 0)
                {
                    canvas.gameObject.layer = _uiLayer;
                }

                // Overlay였을 때는 항상 월드 위에 그려졌지만 ScreenSpaceCamera는 스프라이트와 같은 정렬을 탄다.
                // 최상위 Sorting Layer로 올리되 원본 순서에 offset만 더해 캔버스끼리의 상대 순서는 유지한다
                canvas.sortingLayerID = _topSortingLayerId;
                canvas.sortingOrder = state.SortingOrder + OVERLAY_SORTING_ORDER_OFFSET;
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
                canvas.sortingLayerID = pair.Value.SortingLayerId;
                canvas.sortingOrder = pair.Value.SortingOrder;
                canvas.gameObject.layer = pair.Value.GameObjectLayer;
            }

            _overlayCanvasStates.Clear();
        }

        // 프로젝트에 정의된 Sorting Layer 중 가장 나중에 그려지는(=최상위) 레이어 id
        private static int FindTopSortingLayerId()
        {
            SortingLayer[] layers = SortingLayer.layers;
            if ((layers == null) || (layers.Length == 0))
            {
                return 0;
            }

            return layers[layers.Length - 1].id;
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
