// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

using TMPro;

namespace Minsung.Visual
{
    // 공간찢기 '보스 침식' 시네마틱 - 게임 화면을 중앙의 가짜 창(1280x720 비율)으로 줄이고, 사선으로 두 조각을 갈라
    // 보스 조각이 자기 창 경계를 부수고 사라진 뒤, 보스가 창 밖에서 플레이어 창을 삼키러 넘어온다
    // 실제 OS 창 크기/해상도/창 모드는 바꾸지 않는다. 창 바깥은 레이어드 윈도의 키 색상 투명 영역이며,
    // 그 뒤로 Windows가 합성하는 실제 데스크톱이 보인다(읽거나 캡처하지 않는다). 불가능한 환경에서는 FakeDesktopBackground로 폴백한다
    public class SpaceTearWindowPresentation : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        // 창 크롬(타이틀 바)은 창 안의 모든 UI보다 앞이어야 한다
        private const int CHROME_SORTING_ORDER = short.MaxValue - 1;

        // 연출 도중 새로 생기는 캔버스를 다시 훑는 주기(프레임)
        private const int CANVAS_RESCAN_FRAME_INTERVAL = 10;

        [Header("참조")]
        [SerializeField] private Camera _sourceCamera;             // Main Camera - Cinemachine이 위치를 구동한다
        [SerializeField] private RectTransform _presentationRoot;  // 풀스크린 RectTransform(Canvas 아래)
        [SerializeField] private Transform _boss;
        [SerializeField] private FakeDesktopBackground _fakeDesktop; // 투명 창을 못 쓸 때의 폴백 배경

        [Header("가짜 창")]
        [SerializeField] private Vector2 _windowAspect = new Vector2(1280f, 720f);
        [Tooltip("화면 높이 대비 가짜 창 높이 비율")]
        [SerializeField, Range(0.2f, 1f)] private float _windowHeightRatio = 0.62f;
        [SerializeField] private Color _windowFrameColor = new Color(0.55f, 0.24f, 1f, 0.9f);
        [SerializeField] private float _windowFrameThickness = 3f;
        [SerializeField] private Color _windowShadowColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private float _windowShadowSpread = 18f;

        [Header("타이틀 바 - OS 창처럼 보이게 하는 상단 띠")]
        [SerializeField] private bool   _showTitleBar   = true;
        [SerializeField] private string _windowTitle    = "The Last Re:wind";
        [SerializeField] private Sprite _windowIcon;                 // 비우면 단색 사각형으로 대체
        [SerializeField] private TMP_FontAsset _titleFont;           // 비우면 TMP 기본 폰트
        [SerializeField] private float _titleBarHeight = 34f;
        [SerializeField] private Color _titleBarColor  = new Color(0.96f, 0.96f, 0.97f, 1f);
        [SerializeField] private Color _titleTextColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        [SerializeField] private Color _titleIconColor = new Color(0.35f, 0.55f, 0.95f, 1f);

        [Header("실제 데스크톱 노출")]
        [Tooltip("이 색과 정확히 같은 픽셀만 투명해진다. Bloom/AA가 오염시키지 않도록 배경 카메라는 포스트를 끈다")]
        [SerializeField] private Color _keyColor = new Color(0f, 1f, 0f, 1f); // #00FF00
        [Tooltip("OS 창의 캡션/프레임을 제거할지 - 가짜 타이틀 바와 겹치지 않게 기본으로 켠다. 종료 시 원래 스타일로 되돌린다")]
        [SerializeField] private bool _removeWindowBorder = true;

        [Header("타임라인 (unscaled 초)")]
        [SerializeField] private float _revealTime = 0.45f; // 화면 축소
        [SerializeField] private float _cutTime    = 2.5f;  // 보스가 절단선을 지나며 화면이 갈라지는 시간
        [Tooltip("낙하 시간 상한 - 실제 낙하 시간은 중력으로 정해지고 이 값은 안전장치다. 중력보다 짧으면 작업표시줄에 닿기 전에 끊긴다")]
        [SerializeField] private float _dropTime   = 3f;

        [Header("보스 이동 범위")]
        [Tooltip("화면 중앙에서 바깥으로 나가는 거리(뷰포트 비율) - 보스 퇴장 지점과 절단선 길이를 함께 정한다")]
        [SerializeField] private float _cutTravelViewport = 0.75f;

        [Header("위쪽 조각 낙하")]
        [Tooltip("px/s^2 - 낙하 거리(창 아래변에서 작업표시줄까지, 약 157px)에 대해 170이면 약 1.35초 걸린다")]
        [SerializeField] private float _dropGravity  = 170f;
        [SerializeField] private float _dropSpinDeg  = 26f;   // 떨어지며 기우는 각도
        [SerializeField] private int   _dropShards   = 44;    // 작업표시줄 충돌 시 산산조각 나는 파편 수
        [SerializeField] private int   _dropSparkles = 60;
        [Tooltip("화면 아래 작업표시줄 높이(px) - 조각의 아래 끝이 이 선에 닿으면 산산조각이 난다")]
        [SerializeField] private float _taskbarHeight = 48f;
        [Tooltip("조각이 부서진 뒤 HUD가 돌아온 화면을 보여주는 시간(unscaled 초)")]
        [SerializeField] private float _holdAfterDropTime = 1f;

        [Header("연출 강도")]
        [Tooltip("보스의 베기 방향과 맞춘 사선 절단 각도(도)")]
        [SerializeField] private float _cutAngleDeg = -28f;
        [SerializeField, Range(0.2f, 1f)] private float _rtResolutionScale = 0.75f;

        [Header("프로토타입")]
        [SerializeField] private bool _playOnStart; // 켜면 Start에서 바로 재생(테스트용)

        private RectTransform  _windowRect;
        private Image[]        _frameImages;  // 창 테두리 4변 - 안쪽을 채우지 않는다(조각이 사라진 자리로 데스크톱이 보여야 하므로)
        private Image[]        _shadowImages; // 테두리 바깥 얕은 그림자 4변
        private readonly List<Graphic> _titleBarGraphics = new List<Graphic>(); // 타이틀 바 구성 요소 - 창과 함께 페이드된다
        private readonly List<float>   _titleBarBaseAlphas = new List<float>(); // 요소별 원래 알파(페이드 기준값)
        private ScreenTearShard _playerFragment;
        private ScreenTearShard _bossFragment;
        private RenderTexture  _bossFragmentSource; // 보스 조각이 지금 보여주는 소스 - 절단 직후 잠깐은 플레이어 화면을 쓴다
        private Camera         _playerCamera;
        private Camera         _hudCamera;    // HUD 전용 - 조각과 별개로 렌더해 절단돼도 사라지지 않는다
        private RenderTexture  _playerRt;
        private RenderTexture  _hudRt;
        // HUD도 조각과 똑같은 모양으로 잘린다 - 각 조각의 자식이라 이동/회전을 그대로 따라간다
        private ScreenTearShard _hudLower;
        private ScreenTearShard _hudUpper;

        private readonly List<Vector2> _windowPoly = new List<Vector2>();
        private List<Vector2> _playerPoly;
        private List<Vector2> _bossPoly;
        private Vector2 _playerFragmentOffset;
        private Vector2 _bossFragmentOffset;

        // 원래대로 되돌리기 위해 저장하는 Main Camera 상태
        private int                _savedCullingMask;
        private CameraClearFlags   _savedClearFlags;
        private Color              _savedBackgroundColor;
        private bool               _savedPostProcessing;
        private bool               _savedAllowMsaa;
        private AntialiasingMode   _savedAntialiasing;
        private bool               _cameraStateSaved;
        private GameObject         _blackBarsRoot;
        // 연출 동안 UI 레이어로 옮겨두는 루트 Canvas들과 원래 레이어
        private readonly List<GameObject> _movedCanvasRoots = new List<GameObject>();
        private readonly List<int>        _movedCanvasLayers = new List<int>();

        // 연출 동안 창 안(RenderTexture)으로 옮겨 그리는 UI 캔버스와 원래 설정
        private readonly List<Canvas>     _capturedCanvases = new List<Canvas>();
        private readonly List<RenderMode> _capturedRenderModes = new List<RenderMode>();
        private readonly List<Camera>     _capturedCameras = new List<Camera>();
        private readonly List<float>      _capturedPlaneDistances = new List<float>();

        private Canvas _chromeCanvas; // 타이틀 바 전용 - 플레이어 카메라로 렌더해 게임 화면과 함께 잘린다
        private int                _uiLayer = -1;
        private bool               _transparentApplied;
        private bool               _split;
        private bool               _running;

        /// <summary> 시네마틱이 재생 중인가 </summary>
        public bool IsPlaying => _running;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            if (_playOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Cleanup(); // 씬 언로드/비활성에서도 창 스타일과 카메라가 잔류하지 않게 한다
            _running = false;
        }

        private void OnApplicationQuit()
        {
            TransparentWindowController.Restore(); // 종료 경로에서 창 스타일이 남지 않도록 마지막 방어
        }

        // 조각 카메라를 매 프레임 대상 위치에 맞춘다 - Cinemachine Brain이 Main Camera를 옮긴 뒤에 실행되도록 LateUpdate
        private void LateUpdate()
        {
            if (!_running)
            {
                return;
            }

            if ((_playerCamera != null) && (_sourceCamera != null))
            {
                _playerCamera.transform.position = _sourceCamera.transform.position;
                _playerCamera.orthographicSize   = _sourceCamera.orthographicSize;
            }
            ReassertCapturedCanvases();
        }

        // FixedAspectRatioController는 해상도가 바뀌면 캔버스를 Main Camera로 되돌린다
        // 데스크톱 노출 때 창 모드로 전환하면서 실제로 해상도가 바뀌므로, 창 안에 잡아둔 UI를 매 프레임 다시 붙인다
        private void ReassertCapturedCanvases()
        {
            if (_hudCamera == null)
            {
                return;
            }

            // 연출 도중 새로 생기거나 놓친 캔버스(FPS 카운터 등)도 창 안으로 끌어온다 - 매 프레임은 비싸서 주기적으로만
            if ((Time.frameCount % CANVAS_RESCAN_FRAME_INTERVAL) == 0)
            {
                CaptureNewCanvases();
            }

            for (int i = 0; i < _capturedCanvases.Count; ++i)
            {
                Canvas canvas = _capturedCanvases[i];
                if ((canvas == null) || (canvas.worldCamera == _hudCamera))
                {
                    continue;
                }
                canvas.renderMode  = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _hudCamera;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 보스 침식 시네마틱을 처음부터 재생한다(이미 재생 중이면 무시) </summary>
        public void Play()
        {
            if (_running)
            {
                return;
            }
            StartCoroutine(CoPlay());
        }

        /// <summary> 재생을 중단하고 화면/창을 즉시 원래대로 되돌린다 </summary>
        public void Stop()
        {
            if (!_running)
            {
                return;
            }
            StopAllCoroutines();
            Cleanup();
            _running = false;
        }

        // 보스 이동 없이 화면 연출만 순서대로 재생한다(_playOnStart 테스트용)
        // 실제 전투에서는 Boss2SpaceTearPattern이 보스 이동과 아래 단계들을 번갈아 호출한다
        private IEnumerator CoPlay()
        {
            yield return CoBeginWindow();
            if (!_running)
            {
                yield break; // 참조 누락 등으로 시작하지 못한 경우
            }

            yield return CoShrinkScreen();
            yield return CoCutAlongLine(_cutTime);
            yield return CoDropUpperFragment();

            EndWindow();
        }

        /****************************************
        *          단계별 공개 API (패턴이 구동)
        ****************************************/

        /// <summary> 창/카메라를 만들고 창 바깥을 실제 데스크톱(또는 폴백 배경)으로 전환한다. 이 시점엔 아직 화면 전체 크기다 </summary>
        public IEnumerator CoBeginWindow()
        {
            if (!EnsureRunnable())
            {
                yield break;
            }
            Build();
            yield return CoEnableDesktopReveal();
        }

        /// <summary> 게임 화면을 가짜 창 크기까지 줄인다 </summary>
        public IEnumerator CoShrinkScreen()
        {
            yield return CoRevealFakeWindow();
        }

        /// <summary> 연출을 끝내고 화면/창을 원래대로 되돌린다 </summary>
        public void EndWindow()
        {
            Cleanup();
            _running = false;
        }

        /// <summary>
        /// 절단선을 월드 좌표로 돌려준다(화면 밖까지 연장). 보스가 이 선을 따라 돌진하면 화면이 갈라지는 선과 일치한다.
        /// 보스가 오른쪽 밖에서 들어오므로 start가 오른쪽, end가 왼쪽이다.
        /// </summary>
        public bool TryGetCutLineWorld(out Vector3 start, out Vector3 end)
        {
            start = Vector3.zero;
            end   = Vector3.zero;
            if (_sourceCamera == null)
            {
                return false;
            }

            float rad = _cutAngleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // 화면 중앙을 지나는 선을 뷰포트 밖까지 연장한다
            Vector3 center = _sourceCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Mathf.Abs(_sourceCamera.nearClipPlane) + 1f));
            Vector3 edge   = _sourceCamera.ViewportToWorldPoint(new Vector3(0.5f + _cutTravelViewport, 0.5f, Mathf.Abs(_sourceCamera.nearClipPlane) + 1f));
            float half = Mathf.Abs(edge.x - center.x);

            Vector3 offset = new Vector3(dir.x, dir.y, 0f) * half;
            start = new Vector3(center.x, center.y, 0f) + offset;  // 오른쪽 바깥
            end   = new Vector3(center.x, center.y, 0f) - offset;  // 왼쪽 바깥
            return true;
        }

        /// <summary> 화면 중앙 월드 좌표(보스를 정중앙으로 보낼 때 쓴다) </summary>
        public Vector3 GetScreenCenterWorld()
        {
            if (_sourceCamera == null)
            {
                return Vector3.zero;
            }
            Vector3 p = _sourceCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Mathf.Abs(_sourceCamera.nearClipPlane) + 1f));
            return new Vector3(p.x, p.y, 0f);
        }

        /// <summary> 화면 오른쪽 바깥 월드 좌표(보스가 퇴장할 지점) </summary>
        public Vector3 GetOffscreenRightWorld()
        {
            if (_sourceCamera == null)
            {
                return Vector3.zero;
            }
            Vector3 p = _sourceCamera.ViewportToWorldPoint(new Vector3(0.5f + _cutTravelViewport, 0.5f, Mathf.Abs(_sourceCamera.nearClipPlane) + 1f));
            return new Vector3(p.x, p.y, 0f);
        }

        private bool EnsureRunnable()
        {
            if ((_sourceCamera == null) || (_presentationRoot == null))
            {
                return false;
            }

            _uiLayer = LayerMask.NameToLayer("UI");
            if (_uiLayer < 0)
            {
                Debug.LogWarning("[SpaceTearWindowPresentation] UI 레이어를 찾지 못해 연출을 시작하지 않습니다", this);
                return false;
            }

            _running = true;
            return true;
        }

        // 카메라/RenderTexture/가짜 창/조각을 만들고, 창 바깥을 투명(또는 폴백 배경)으로 전환한다
        private void Build()
        {
            _split = false;
            _playerFragmentOffset = Vector2.zero;
            _bossFragmentOffset   = Vector2.zero;

            CreateCameras();
            CreateChromeCanvas(); // 타이틀 바를 게임 화면과 같은 RenderTexture에 그린다
            CreateWindow();

            // 처음에는 창이 화면 전체 크기 - 이 상태에서 배경을 키 색상으로 바꿔야 초록이 한 프레임도 보이지 않는다
            SetWindowSize(_presentationRoot.rect.size);
            RebuildFragments();

            ForceCanvasLayers();
            CaptureUiIntoWindow(); // HUD 등 모든 UI를 창 안으로 - 축소·절단을 게임 화면과 함께 받는다
            ApplyKeyColorBackground();
            HideAspectRatioBars();
        }

        // 실제 데스크톱을 드러낸다 - 전체화면에서는 DWM이 레이어드 컬러키를 무시하므로 창 모드로 바꾼 뒤 적용한다
        private IEnumerator CoEnableDesktopReveal()
        {
            if (TransparentWindowController.CanReveal)
            {
                TransparentWindowController.EnterWindowedForReveal();
                // Screen.SetResolution은 다음 프레임에 반영된다 - 창 모드가 실제로 적용된 뒤에 스타일을 걸어야 한다
                yield return null;
                yield return null;
            }

            _transparentApplied = TransparentWindowController.TryEnable(_keyColor, _removeWindowBorder);
            if (!_transparentApplied && (_fakeDesktop != null))
            {
                _fakeDesktop.Show(_presentationRoot); // 실제 데스크톱을 못 쓰는 환경 - 준비된 배경으로 대체
            }
        }

        // A - 게임 화면을 중앙의 가짜 창 크기까지 줄인다. 줄어든 바깥이 투명해지며 실제 데스크톱이 드러난다
        private IEnumerator CoRevealFakeWindow()
        {
            Vector2 fullSize   = _presentationRoot.rect.size;
            Vector2 targetSize = GetFakeWindowSize();

            float duration = Mathf.Max(0.01f, _revealTime);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetWindowSize(Vector2.Lerp(fullSize, targetSize, t));
                SetFrameAlpha(t);
                RebuildFragments();
                yield return null;
            }
            SetWindowSize(targetSize);
            SetFrameAlpha(1f);
            RebuildFragments();
        }

        /// <summary>
        /// 보스가 절단선을 지나가는 동안 그 자국이 오른쪽에서 왼쪽으로 그어지고, 다 지나가면 화면이 두 조각으로 갈라진다.
        /// duration은 보스 돌진 시간과 맞춰 호출자가 넘긴다.
        /// </summary>
        public IEnumerator CoCutAlongLine(float duration)
        {
            if (_windowRect == null)
            {
                yield break;
            }

            float rad = _cutAngleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // 창 안에서 실제로 지나가는 구간만 그린다 - 창 밖까지 선이 삐져나가면 빈 공간을 가로지른다
            RebuildWindowPolygon();
            if (!ConvexPolygonSplitter.TryGetCrackSegment(_windowPoly, Vector2.zero, dir, out Vector2 edgeA, out Vector2 edgeB))
            {
                yield break;
            }

            // 보스와 같은 방향(오른쪽 -> 왼쪽)으로 그어지도록 시작점을 맞춘다
            bool aIsRight = Vector2.Dot(edgeA, dir) >= Vector2.Dot(edgeB, dir);
            Vector2 from = aIsRight ? edgeA : edgeB;
            Vector2 to   = aIsRight ? edgeB : edgeA;

            ScreenTearGlassBurst burst = CreateBurst();
            RectTransform crack = CreateCrackStroke();

            float time = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < time)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / time);

                // 자국이 보스를 따라 자라난다
                Vector2 tip = Vector2.Lerp(from, to, t);
                UpdateCrackStroke(crack, from, tip);
                burst.Emit(tip, tip + ((to - from).normalized * 8f), 1, 2, 220f, 520f, 0.4f, 0.8f);
                yield return null;
            }

            UpdateCrackStroke(crack, from, to);

            // 다 지나갔으면 실제로 둘로 나눈다
            _split = true;
            RebuildFragments();
            StartCoroutine(CoDestroyWhenFinished(burst));
        }

        /// <summary> 위쪽 조각이 중력을 받아 바닥으로 떨어지고, 창 아래에 닿는 순간 유리처럼 부서진다 </summary>
        public IEnumerator CoDropUpperFragment()
        {
            if (_bossFragment == null)
            {
                yield break;
            }

            Rect rect     = _windowRect.rect;
            Rect rootRect = _presentationRoot.rect;

            // 바닥 = 작업표시줄 윗변. 조각의 아래 끝이 여기 닿으면 산산조각이 난다
            float taskbarTop = rootRect.yMin + _taskbarHeight;
            float fragmentBottom = _windowRect.anchoredPosition.y + rect.yMin;
            float fallLimit = Mathf.Max(1f, fragmentBottom - taskbarTop);

            ScreenTearGlassBurst burst = CreateBurst();

            float velocity = 0f;
            float fallen   = 0f;
            float elapsed  = 0f;
            float duration = Mathf.Max(0.01f, _dropTime);

            while ((elapsed < duration) && (fallen < fallLimit))
            {
                float dt = Time.unscaledDeltaTime;
                elapsed  += dt;
                velocity += _dropGravity * dt;
                fallen   += velocity * dt;

                float t = Mathf.Clamp01(fallen / fallLimit);
                _bossFragmentOffset = new Vector2(0f, -fallen);
                _bossFragment.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _dropSpinDeg * t);
                ApplyFragmentOffsets();
                yield return null;
            }

            // 작업표시줄에 충돌 - 조각을 지우고 화면 폭 전체에 파편이 터진다
            burst.Emit(new Vector2(rootRect.xMin, taskbarTop), new Vector2(rootRect.xMax, taskbarTop),
                _dropShards, _dropSparkles, 320f, 780f, 0.6f, 1.1f);

            _bossFragment.gameObject.SetActive(false);
            if (_hudUpper != null)
            {
                _hudUpper.gameObject.SetActive(false); // 위쪽 조각과 함께 떨어진 HUD 조각도 정리
            }
            StartCoroutine(CoDestroyWhenFinished(burst));

            RestoreHudToFullWindow(); // 조각이 부서지고 나면 남은 화면에 HUD를 다시 채운다
            yield return new WaitForSecondsRealtime(_holdAfterDropTime);
        }

        // 절단으로 잘려 나갔던 HUD를 창 전체 범위로 되돌린다 - 낙하가 끝난 뒤 남은 화면에서 다시 읽을 수 있어야 한다
        private void RestoreHudToFullWindow()
        {
            if (_hudLower == null)
            {
                return;
            }
            RebuildWindowPolygon();
            _hudLower.Setup(_hudRt, new List<Vector2>(_windowPoly));
        }

        // 절단 자국 스트로크 하나를 만든다(창 좌표계)
        private RectTransform CreateCrackStroke()
        {
            GameObject go = new GameObject("CutStroke", typeof(RectTransform));
            ApplyUiLayer(go);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(_windowRect, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.SetAsLastSibling();

            Image image = go.AddComponent<Image>();
            image.color = _windowFrameColor;
            image.raycastTarget = false;
            return rt;
        }

        private void UpdateCrackStroke(RectTransform stroke, Vector2 from, Vector2 tip)
        {
            if (stroke == null)
            {
                return;
            }
            Vector2 delta = tip - from;
            stroke.anchoredPosition = from;
            stroke.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            stroke.sizeDelta        = new Vector2(delta.magnitude, _windowFrameThickness);
        }

        /****************************************
        *            Build Helpers
        ****************************************/

        private void CreateCameras()
        {
            _playerRt = CreateRenderTexture("SpaceTearWindow_PlayerRT");

            _playerCamera = CreateWorldCamera("PlayerFragmentCamera");
            _playerCamera.targetTexture = _playerRt;

            // HUD는 조각과 따로 렌더한다 - 절단으로 조각이 갈라지거나 파괴돼도 HUD는 그대로 남아야 한다
            _hudRt = CreateRenderTexture("SpaceTearWindow_HudRT");
            _hudCamera = CreateWorldCamera("HudCamera");
            _hudCamera.targetTexture   = _hudRt;
            _hudCamera.cullingMask     = (_uiLayer >= 0) ? (1 << _uiLayer) : 0; // 월드는 빼고 UI만
            _hudCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);             // 투명 배경 - HUD 픽셀만 조각 위에 합성된다
        }

        // 소스 카메라를 복제해 월드를 RenderTexture로 렌더한다(화면에는 직접 출력하지 않는다)
        private Camera CreateWorldCamera(string cameraName)
        {
            GameObject go = new GameObject(cameraName);
            go.transform.SetParent(_sourceCamera.transform.parent, false);

            Camera cam = go.AddComponent<Camera>();
            cam.CopyFrom(_sourceCamera);
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.rect            = new Rect(0f, 0f, 1f, 1f);

            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderType           = CameraRenderType.Base;
                data.renderPostProcessing = false;
            }
            return cam;
        }

        private RenderTexture CreateRenderTexture(string textureName)
        {
            int width  = Mathf.Max(1, Mathf.RoundToInt(Screen.width * _rtResolutionScale));
            int height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * _rtResolutionScale));

            RenderTexture rt = new RenderTexture(width, height, 16, RenderTextureFormat.Default);
            rt.name = textureName;
            rt.Create();
            return rt;
        }

        // 연출용으로 만든 오브젝트도 UI 레이어에 둔다 - Main Camera가 UI만 그리기 때문
        private void ApplyUiLayer(GameObject go)
        {
            if ((go != null) && (_uiLayer >= 0))
            {
                go.layer = _uiLayer;
            }
        }

        // 타이틀 바 전용 캔버스 - 플레이어 카메라(RenderTexture)로 렌더해 게임 화면의 일부가 된다
        // 그래서 창이 축소되면 함께 줄고, 사선 절단이 들어가면 타이틀 바도 같이 잘린다
        private void CreateChromeCanvas()
        {
            if (!_showTitleBar || (_playerCamera == null))
            {
                return;
            }

            GameObject go = new GameObject("WindowChromeCanvas");
            ApplyUiLayer(go);

            _chromeCanvas = go.AddComponent<Canvas>();
            _chromeCanvas.renderMode    = RenderMode.ScreenSpaceCamera;
            _chromeCanvas.worldCamera   = _playerCamera;
            _chromeCanvas.planeDistance = Mathf.Max(_playerCamera.nearClipPlane + 0.1f, 1f);
            _chromeCanvas.sortingOrder  = CHROME_SORTING_ORDER; // HUD보다 위 - 창 크롬이 가장 앞이다

            CreateTitleBar(_chromeCanvas.GetComponent<RectTransform>());
        }

        // 연출 동안 모든 UI 캔버스를 플레이어 카메라로 옮겨 그린다 - 축소된 창 안에 HUD가 함께 들어가고 절단도 같이 받는다
        // 월드 스페이스 캔버스는 이미 게임 화면에 포함되므로 건드리지 않는다
        private void CaptureUiIntoWindow()
        {
            if ((_capturedCanvases.Count > 0) || (_hudCamera == null))
            {
                return;
            }
            CaptureNewCanvases();
        }

        // 아직 잡지 않은 루트 캔버스를 찾아 창 안(HUD 카메라)으로 넘긴다
        private void CaptureNewCanvases()
        {
            Canvas presentation = _presentationRoot.GetComponentInParent<Canvas>();
            Canvas presentationRoot = (presentation != null) ? presentation.rootCanvas : null;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; ++i)
            {
                Canvas canvas = canvases[i];
                if ((canvas == null) || !canvas.isRootCanvas)
                {
                    continue;
                }
                if ((canvas == presentationRoot) || (canvas == _chromeCanvas))
                {
                    continue; // 가짜 창 자체와 창 크롬은 화면에 그대로 남아야 한다
                }
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }
                if (_capturedCanvases.Contains(canvas))
                {
                    continue; // 이미 잡아둔 캔버스
                }

                _capturedCanvases.Add(canvas);
                _capturedRenderModes.Add(canvas.renderMode);
                _capturedCameras.Add(canvas.worldCamera);
                _capturedPlaneDistances.Add(canvas.planeDistance);

                canvas.renderMode  = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _hudCamera;
            }
        }

        private void RestoreUiCanvases()
        {
            for (int i = 0; i < _capturedCanvases.Count; ++i)
            {
                Canvas canvas = _capturedCanvases[i];
                if (canvas == null)
                {
                    continue;
                }
                canvas.renderMode    = _capturedRenderModes[i];
                canvas.worldCamera   = _capturedCameras[i];
                canvas.planeDistance = _capturedPlaneDistances[i];
            }
            _capturedCanvases.Clear();
            _capturedRenderModes.Clear();
            _capturedCameras.Clear();
            _capturedPlaneDistances.Clear();
        }

        private void CreateWindow()
        {
            GameObject windowGo = new GameObject("FakeWindow", typeof(RectTransform));
            ApplyUiLayer(windowGo);
            _windowRect = windowGo.GetComponent<RectTransform>();
            _windowRect.SetParent(_presentationRoot, false);
            _windowRect.anchorMin = _windowRect.anchorMax = new Vector2(0.5f, 0.5f);

            // 그림자를 먼저(더 바깥) 만들고 그 위에 테두리를 얹는다
            _shadowImages = CreateBorder("WindowShadow", _windowShadowColor, _windowFrameThickness + _windowShadowSpread, _windowFrameThickness);
            _frameImages  = CreateBorder("WindowFrame", _windowFrameColor, _windowFrameThickness, 0f);

            // 아래 조각(플레이어가 남는 쪽) / 위 조각(잘려 떨어지는 쪽) - 둘 다 같은 화면을 보여 이음매가 없다
            _playerFragment     = CreateFragment("LowerFragmentMask", _playerRt);
            _bossFragment       = CreateFragment("UpperFragmentMask", _playerRt);
            _bossFragmentSource = _playerRt;
            _bossFragment.gameObject.SetActive(false); // 절단 전까지는 한 조각처럼 보인다

            CreateHudShards(); // 조각의 자식으로 붙어 절단 모양대로 함께 잘린다

            SetFrameAlpha(0f);
        }

        // 창 테두리를 4변 스트립으로 만든다 - 안쪽을 채우지 않아야 조각이 사라진 자리로 바깥(데스크톱)이 보인다
        // thickness는 선 두께, expand는 창 경계에서 바깥으로 밀어내는 거리
        private Image[] CreateBorder(string borderName, Color color, float thickness, float expand)
        {
            Vector2[] anchorMin = { new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
            Vector2[] anchorMax = { new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            float offset = expand + (thickness * 0.5f);
            Vector2[] position = { new Vector2(0f, offset), new Vector2(0f, -offset), new Vector2(-offset, 0f), new Vector2(offset, 0f) };
            Vector2[] size =
            {
                new Vector2((expand + thickness) * 2f, thickness),
                new Vector2((expand + thickness) * 2f, thickness),
                new Vector2(thickness, (expand + thickness) * 2f),
                new Vector2(thickness, (expand + thickness) * 2f),
            };

            Image[] images = new Image[4];
            for (int i = 0; i < 4; ++i)
            {
                GameObject go = new GameObject(borderName + "_" + i, typeof(RectTransform));
                ApplyUiLayer(go);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.SetParent(_windowRect, false);
                rt.anchorMin        = anchorMin[i];
                rt.anchorMax        = anchorMax[i];
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.sizeDelta        = size[i];
                rt.anchoredPosition = position[i];

                images[i] = go.AddComponent<Image>();
                images[i].color = color;
                images[i].raycastTarget = false;
            }
            return images;
        }

        // OS 창처럼 보이게 하는 상단 타이틀 바 - 게임 화면 위쪽 안쪽에 얹혀 화면과 함께 축소되고 함께 잘린다
        private void CreateTitleBar(RectTransform parent)
        {
            GameObject barGo = new GameObject("TitleBar", typeof(RectTransform));
            ApplyUiLayer(barGo);
            RectTransform bar = barGo.GetComponent<RectTransform>();
            bar.SetParent(parent, false);
            bar.anchorMin        = new Vector2(0f, 1f);
            bar.anchorMax        = new Vector2(1f, 1f);
            bar.pivot            = new Vector2(0.5f, 1f); // 위쪽 끝이 화면 상단에 붙는다
            bar.sizeDelta        = new Vector2(0f, _titleBarHeight);
            bar.anchoredPosition = Vector2.zero;

            Image background = barGo.AddComponent<Image>();
            background.color = _titleBarColor;
            background.raycastTarget = false;
            _titleBarGraphics.Add(background);

            float inset    = _titleBarHeight * 0.28f;
            float iconSize = _titleBarHeight * 0.5f;

            // 앱 아이콘
            RectTransform icon = CreateBarChild(bar, "TitleIcon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            icon.sizeDelta        = new Vector2(iconSize, iconSize);
            icon.anchoredPosition = new Vector2(inset + (iconSize * 0.5f), 0f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite         = _windowIcon;
            iconImage.preserveAspect = true;
            iconImage.color          = (_windowIcon != null) ? Color.white : _titleIconColor;
            iconImage.raycastTarget  = false;
            _titleBarGraphics.Add(iconImage);

            // 창 제목
            RectTransform title = CreateBarChild(bar, "TitleText", new Vector2(0f, 0f), new Vector2(1f, 1f));
            title.offsetMin = new Vector2(inset + iconSize + (inset * 0.6f), 0f);
            title.offsetMax = new Vector2(-_titleBarHeight * 3.6f, 0f);
            TextMeshProUGUI text = title.gameObject.AddComponent<TextMeshProUGUI>();
            text.text                 = _windowTitle;
            text.color                = _titleTextColor;
            text.fontSize             = _titleBarHeight * 0.42f;
            text.alignment            = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode      = TextWrappingModes.NoWrap;
            text.overflowMode         = TextOverflowModes.Ellipsis;
            text.raycastTarget        = false;
            if (_titleFont != null)
            {
                text.font = _titleFont;
            }
            _titleBarGraphics.Add(text);

            // 최소화 / 최대화 / 닫기 - 실제 동작은 없고 형태만 흉내낸다
            float slot = _titleBarHeight;
            CreateBarGlyph(bar, "Minimize", slot * 2.5f, new Vector2(_titleBarHeight * 0.32f, 1.5f), 0f);
            CreateBarGlyph(bar, "Maximize", slot * 1.5f, new Vector2(_titleBarHeight * 0.3f, _titleBarHeight * 0.3f), 0f);
            CreateBarGlyph(bar, "Close_0", slot * 0.5f, new Vector2(_titleBarHeight * 0.34f, 1.5f), 45f);
            CreateBarGlyph(bar, "Close_1", slot * 0.5f, new Vector2(_titleBarHeight * 0.34f, 1.5f), -45f);

            // 페이드 기준이 될 원래 알파를 한 번만 기록해 둔다
            _titleBarBaseAlphas.Clear();
            for (int i = 0; i < _titleBarGraphics.Count; ++i)
            {
                _titleBarBaseAlphas.Add(_titleBarGraphics[i].color.a);
            }
        }

        private RectTransform CreateBarChild(RectTransform parent, string childName, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(childName, typeof(RectTransform));
            ApplyUiLayer(go);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        // 타이틀 바 우측 버튼 글리프 하나 - rightOffset만큼 오른쪽 끝에서 떨어뜨리고 필요하면 회전시킨다(닫기 X)
        private void CreateBarGlyph(RectTransform bar, string glyphName, float rightOffset, Vector2 size, float rotationDeg)
        {
            RectTransform rt = CreateBarChild(bar, glyphName, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            rt.sizeDelta        = size;
            rt.anchoredPosition = new Vector2(-rightOffset, 0f);
            rt.localRotation    = Quaternion.Euler(0f, 0f, rotationDeg);

            Image image = rt.gameObject.AddComponent<Image>();
            image.color = _titleTextColor;
            image.raycastTarget = false;
            _titleBarGraphics.Add(image);
        }

        // HUD를 조각별로 하나씩 얹는다 - 조각의 자식이므로 절단 모양 그대로 잘리고, 이동·회전도 함께 따라간다
        private void CreateHudShards()
        {
            if (_hudRt == null)
            {
                return;
            }
            _hudLower = CreateHudShard(_playerFragment, "HudLower");
            _hudUpper = CreateHudShard(_bossFragment, "HudUpper");
        }

        private ScreenTearShard CreateHudShard(ScreenTearShard parentFragment, string shardName)
        {
            if (parentFragment == null)
            {
                return null;
            }

            GameObject go = new GameObject(shardName, typeof(RectTransform));
            ApplyUiLayer(go);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parentFragment.rectTransform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            ScreenTearShard shard = go.AddComponent<ScreenTearShard>();
            shard.raycastTarget = false;
            shard.Setup(_hudRt, null);
            return shard;
        }

        private ScreenTearShard CreateFragment(string fragmentName, RenderTexture source)
        {
            GameObject go = new GameObject(fragmentName, typeof(RectTransform));
            ApplyUiLayer(go);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(_windowRect, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            ScreenTearShard shard = go.AddComponent<ScreenTearShard>();
            shard.raycastTarget = false;
            shard.Setup(source, null);
            return shard;
        }

        private ScreenTearGlassBurst CreateBurst()
        {
            GameObject go = new GameObject("WindowGlassBurst", typeof(RectTransform));
            ApplyUiLayer(go);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(_presentationRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            ScreenTearGlassBurst burst = go.AddComponent<ScreenTearGlassBurst>();
            burst.raycastTarget = false;
            return burst;
        }


        private IEnumerator CoDestroyWhenFinished(ScreenTearGlassBurst burst)
        {
            while ((burst != null) && !burst.IsFinished)
            {
                yield return null;
            }
            if (burst != null)
            {
                Destroy(burst.gameObject);
            }
        }

        /****************************************
        *            Layout Helpers
        ****************************************/

        // 화면 높이 비율과 지정 종횡비(1280x720)로 가짜 창 크기를 구한다
        private Vector2 GetFakeWindowSize()
        {
            float height = _presentationRoot.rect.height * _windowHeightRatio;
            float aspect = (_windowAspect.y > 0f) ? (_windowAspect.x / _windowAspect.y) : (16f / 9f);
            return new Vector2(height * aspect, height);
        }

        private void SetWindowSize(Vector2 size)
        {
            _windowRect.sizeDelta = size; // 테두리/그림자는 창 rect에 앵커돼 있어 함께 늘어난다
        }

        private void SetFrameAlpha(float alpha)
        {
            float t = Mathf.Clamp01(alpha);
            SetBorderAlpha(_frameImages, _windowFrameColor.a * t);
            SetBorderAlpha(_shadowImages, _windowShadowColor.a * t);

            for (int i = 0; i < _titleBarGraphics.Count; ++i)
            {
                if ((_titleBarGraphics[i] == null) || (i >= _titleBarBaseAlphas.Count))
                {
                    continue;
                }
                Color c = _titleBarGraphics[i].color;
                c.a = _titleBarBaseAlphas[i] * t;
                _titleBarGraphics[i].color = c;
            }
        }

        private static void SetBorderAlpha(Image[] images, float alpha)
        {
            if (images == null)
            {
                return;
            }
            for (int i = 0; i < images.Length; ++i)
            {
                if (images[i] == null)
                {
                    continue;
                }
                Color c = images[i].color;
                c.a = alpha;
                images[i].color = c;
            }
        }

        // 현재 창 크기에 맞춰 조각 폴리곤을 다시 만든다(절단 전에는 한 조각, 후에는 사선으로 나뉜 두 조각)
        private void RebuildFragments()
        {
            RebuildWindowPolygon();

            if (!_split)
            {
                _playerPoly = new List<Vector2>(_windowPoly);
                _playerFragment.Setup(_playerRt, _playerPoly);
                _hudLower?.Setup(_hudRt, _playerPoly);
                return;
            }

            float rad = _cutAngleDeg * Mathf.Deg2Rad;
            Vector2 lineDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            List<List<Vector2>> source = new List<List<Vector2>> { _windowPoly };
            List<List<Vector2>> split = ConvexPolygonSplitter.SplitByLine(source, Vector2.zero, lineDir);

            if (split.Count != 2)
            {
                return; // 절단선이 창을 가로지르지 않는 예외 상황 - 이전 폴리곤을 유지한다
            }

            // 위쪽 조각이 떨어져 나가고 아래쪽이 남는다
            int upperIndex = ResolveUpperPolygonIndex(split);
            _bossPoly   = split[upperIndex];
            _playerPoly = split[1 - upperIndex];

            _playerFragment.Setup(_playerRt, _playerPoly);
            if (!_bossFragment.gameObject.activeSelf)
            {
                _bossFragment.gameObject.SetActive(true);
            }
            _bossFragment.Setup(_bossFragmentSource, _bossPoly);

            // HUD도 같은 폴리곤으로 잘라 각 조각에 실어 보낸다
            _hudLower?.Setup(_hudRt, _playerPoly);
            _hudUpper?.Setup(_hudRt, _bossPoly);
        }

        // 현재 창 rect를 폴리곤으로 만든다(절단선 구간 계산과 조각 분할이 같은 기준을 쓰도록)
        private void RebuildWindowPolygon()
        {
            Rect rect = _windowRect.rect;
            _windowPoly.Clear();
            _windowPoly.Add(new Vector2(rect.xMin, rect.yMin));
            _windowPoly.Add(new Vector2(rect.xMax, rect.yMin));
            _windowPoly.Add(new Vector2(rect.xMax, rect.yMax));
            _windowPoly.Add(new Vector2(rect.xMin, rect.yMax));
        }

        // 무게중심이 더 위에 있는 조각을 '위쪽 조각'으로 고른다 - 이 조각이 바닥으로 떨어져 부서진다
        private static int ResolveUpperPolygonIndex(List<List<Vector2>> split)
        {
            return (ConvexPolygonSplitter.Centroid(split[0]).y >= ConvexPolygonSplitter.Centroid(split[1]).y) ? 0 : 1;
        }

        private void ApplyFragmentOffsets()
        {
            if (_playerFragment != null)
            {
                _playerFragment.rectTransform.anchoredPosition = _playerFragmentOffset;
            }
            if (_bossFragment != null)
            {
                _bossFragment.rectTransform.anchoredPosition = _bossFragmentOffset;
            }
        }

        /****************************************
        *          Screen State Helpers
        ****************************************/

        // Main Camera를 '키 색상만 출력하는 투명 배경 카메라'로 바꾼다 - 월드는 복제 카메라가 RenderTexture로 그린다
        // UI 레이어는 계속 그려야 가짜 창과 HUD가 보이므로 컬링 마스크에서 UI만 남긴다
        private void ApplyKeyColorBackground()
        {
            if (_cameraStateSaved)
            {
                return;
            }

            _savedCullingMask     = _sourceCamera.cullingMask;
            _savedClearFlags      = _sourceCamera.clearFlags;
            _savedBackgroundColor = _sourceCamera.backgroundColor;

            UniversalAdditionalCameraData data = _sourceCamera.GetUniversalAdditionalCameraData();
            _savedPostProcessing = (data != null) && data.renderPostProcessing;
            _savedAntialiasing   = (data != null) ? data.antialiasing : AntialiasingMode.None;
            if (data != null)
            {
                // Bloom/색수차가 키 색상을 오염시키면 초록 가장자리가 남아 투명 처리가 깨진다
                data.renderPostProcessing = false;
                data.antialiasing         = AntialiasingMode.None;
            }

            // MSAA도 경계 픽셀을 섞어 키 색상을 어긋나게 하므로 연출 동안만 끈다
            _savedAllowMsaa = _sourceCamera.allowMSAA;
            _sourceCamera.allowMSAA = false;

            // UI 레이어만 남긴다 - 월드를 한 픽셀이라도 그리면 키 색상이 덮여 투명 영역이 생기지 않는다
            _sourceCamera.cullingMask     = 1 << _uiLayer;
            _sourceCamera.clearFlags      = CameraClearFlags.SolidColor;
            _sourceCamera.backgroundColor = _keyColor;
            _cameraStateSaved = true;
        }

        // 카메라가 UI 레이어만 그리므로, 화면에 남아야 할 캔버스도 전부 UI 레이어에 있어야 한다(HUD·배너 포함)
        // 캔버스 컬링 기준은 루트 Canvas 오브젝트의 레이어라, 루트만 잠시 옮겼다가 그대로 되돌린다
        private void ForceCanvasLayers()
        {
            if (_movedCanvasRoots.Count > 0)
            {
                return;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; ++i)
            {
                Canvas canvas = canvases[i];
                if ((canvas == null) || !canvas.isRootCanvas || (canvas.gameObject.layer == _uiLayer))
                {
                    continue;
                }
                _movedCanvasRoots.Add(canvas.gameObject);
                _movedCanvasLayers.Add(canvas.gameObject.layer);
                canvas.gameObject.layer = _uiLayer;
            }
        }

        private void RestoreCanvasLayers()
        {
            for (int i = 0; i < _movedCanvasRoots.Count; ++i)
            {
                if (_movedCanvasRoots[i] != null)
                {
                    _movedCanvasRoots[i].layer = _movedCanvasLayers[i];
                }
            }
            _movedCanvasRoots.Clear();
            _movedCanvasLayers.Clear();
        }

        private void RestoreKeyColorBackground()
        {
            if (!_cameraStateSaved || (_sourceCamera == null))
            {
                _cameraStateSaved = false;
                return;
            }

            _sourceCamera.cullingMask     = _savedCullingMask;
            _sourceCamera.clearFlags      = _savedClearFlags;
            _sourceCamera.backgroundColor = _savedBackgroundColor;
            _sourceCamera.allowMSAA       = _savedAllowMsaa;

            UniversalAdditionalCameraData data = _sourceCamera.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = _savedPostProcessing;
                data.antialiasing         = _savedAntialiasing;
            }
            _cameraStateSaved = false;
        }

        // 16:9 레터박스 검정 바는 불투명이라 투명 창 연출을 가린다 - 연출 동안만 숨기고 끝나면 되돌린다
        private void HideAspectRatioBars()
        {
            if (_blackBarsRoot != null)
            {
                return;
            }

            GameObject bars = GameObject.Find("Aspect Ratio Black Bars");
            if (bars == null)
            {
                return;
            }
            _blackBarsRoot = bars;
            bars.SetActive(false);
        }

        private void RestoreAspectRatioBars()
        {
            if (_blackBarsRoot == null)
            {
                return;
            }
            _blackBarsRoot.SetActive(true);
            _blackBarsRoot = null;
        }

        // 정상 종료/중단/비활성 어디서 불려도 동일하게 원상복구(두 번 호출해도 안전)
        private void Cleanup()
        {
            if (_transparentApplied || TransparentWindowController.IsActive)
            {
                TransparentWindowController.Restore();
                _transparentApplied = false;
            }
            if (_fakeDesktop != null)
            {
                _fakeDesktop.Hide();
            }

            RestoreKeyColorBackground();
            RestoreUiCanvases(); // 카메라 파괴 전에 원래 렌더 모드로 되돌려야 UI가 사라지지 않는다
            RestoreCanvasLayers();
            RestoreAspectRatioBars();

            if (_chromeCanvas != null)
            {
                Destroy(_chromeCanvas.gameObject);
                _chromeCanvas = null;
            }

            DestroyCamera(ref _playerCamera, ref _playerRt);
            DestroyCamera(ref _hudCamera, ref _hudRt);

            if (_windowRect != null)
            {
                Destroy(_windowRect.gameObject);
                _windowRect = null;
            }

            _titleBarGraphics.Clear();
            _titleBarBaseAlphas.Clear();
            _hudLower           = null;
            _hudUpper           = null;
            _playerFragment     = null;
            _bossFragment       = null;
            _frameImages        = null;
            _shadowImages       = null;
            _bossFragmentSource = null;
            _playerPoly         = null;
            _bossPoly           = null;
            _split              = false;
        }

        private void DestroyCamera(ref Camera cam, ref RenderTexture rt)
        {
            if (cam != null)
            {
                cam.targetTexture = null;
                Destroy(cam.gameObject);
                cam = null;
            }
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
                rt = null;
            }
        }
    }
}
