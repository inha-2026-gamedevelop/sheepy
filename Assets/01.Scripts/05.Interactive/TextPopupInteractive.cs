// System
using System.Collections;

// Unity
using UnityEngine;
using TMPro;

using Minsung.CameraSystem;
using Minsung.Common;
using Minsung.Sound;
using Minsung.Utility;

namespace Minsung.Interactive
{
    // 플레이어가 감지 범위에 들어오면 오브젝트 바로 위에 문구 띄우는 표시 전용 상호작용
    
    public class TextPopupInteractive : BaseInteractive
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("표시 대상")]
        [SerializeField] private GameObject _blurOverlay; // 켜고 끌 블러 캔버스
        [SerializeField] private GameObject _textRoot; // 켜고 끌 루트
        [SerializeField] private TMP_Text   _label;    // 문구를 출력할 TMP

        [Header("내용")]
        [SerializeField] [TextArea(1, 4)] private string _message = ""; // 표시할 문구

        [Header("배치")]
        [SerializeField] private bool       _anchorAboveBounds = true;  // 오브젝트 바운즈 윗변 바로 위에 자동 배치
        [SerializeField] private Collider2D _boundsSource;              // 기준 바운즈(비우면 이 오브젝트의 Collider2D -> Renderer 순으로 자동)
        [SerializeField] private float      _verticalPadding   = 0.15f; // 바운즈 윗변에서 띄울 간격
        [SerializeField] private Vector3    _worldOffset       = new Vector3(0f, 1.5f, 0f); // 자동 배치를 끄거나 바운즈를 못 구할 때 쓰는 수동 오프셋
        [SerializeField] private bool       _keepUpright       = true;  // 부모가 회전해도 문구는 똑바로 세운다

        [Header("카메라 연출")]
        [SerializeField] private bool      _useCameraFocus = true;                                   // 라디오처럼 포커스 카메라로 줌인
        [SerializeField] private Transform _cameraTip;                                               // 포커스 위치 마커 (비우면 이 오브젝트 위치)
        [SerializeField] private float     _focusSize      = Constants.Camera.FOCUS_ORTHOGRAPHIC_SIZE; // 줌 정도(작을수록 확대)
        [SerializeField] private float     _blendTime      = Constants.Camera.DEFAULT_BLEND_TIME;      // 포커스 전환 블렌드 시간(초)

        [Header("연출")]
        [SerializeField] private float _fadeDuration = 0.15f; // 0이면 즉시 표시/숨김

        private CanvasGroup     _canvasGroup;
        private Collider2D      _boundsCollider;
        private Renderer        _boundsRenderer;
        private Coroutine       _fadeRoutine;
        private LocalSfxEmitter _sfxEmitter;    // 문구 표시/숨김에 붙일 개별 SFX
        private bool            _isShown;
        private bool            _hasCameraFocus; // 내가 잡은 포커스만 해제하기 위한 플래그

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void Awake()
        {
            base.Awake();

            if ((_textRoot == null) && (_label != null))
            {
                _textRoot = _label.gameObject;
            }

            if (_textRoot == gameObject)
            {
                Debug.LogWarning($"[{nameof(TextPopupInteractive)}] {nameof(_textRoot)}에 자기 자신을 지정할 수 없습니다 - 자식 오브젝트를 지정하세요", this);
                _textRoot = null;
            }

            if (_textRoot != null)
            {
                _textRoot.TryGetComponent(out _canvasGroup);
            }

            TryGetComponent(out _sfxEmitter);

            CacheBoundsSource();
            ApplyHidden();
        }

        // 오브젝트가 꺼지면 표시 중이던 문구와 카메라 포커스도 함께 정리
        protected override void OnDisable()
        {
            base.OnDisable();

            UtilCoroutine.CheckStopCoroutine(ref _fadeRoutine, this);
            ApplyHidden();
            ReleaseCameraFocus();
        }

        private void LateUpdate()
        {
            if (!_isShown)
            {
                return;
            }
            UpdatePlacement();
        }

        /****************************************
        *                Methods
        ****************************************/

        public override void OnFocus()
        {
            Show();
            AcquireCameraFocus();
        }

        public override void OnUnfocus()
        {
            Hide();
            ReleaseCameraFocus();
        }

        public override void OnInteract(GameObject interactor)
        {
            // 표시 전용 오브젝트라 E키 동작 없음
        }

        /// <summary> 문구를 띄운다 </summary>
        public void Show()
        {
            if (_textRoot == null || _blurOverlay == null)
            {
                return;
            }

            bool wasHidden = !_isShown; // 숨김 -> 표시로 넘어가는 순간에만 등장음 재생
            _isShown = true;

            if (wasHidden)
            {
                _sfxEmitter?.PlayActivate();
            }

            if (_label != null)
            {
                _label.text = _message;
            }

            _blurOverlay.SetActive(true);
            _textRoot.SetActive(true);
            UpdatePlacement(); // 켜진 첫 프레임에 엉뚱한 위치로 보이지 않게 즉시 배치

            if ((_fadeDuration <= 0f) || (_canvasGroup == null))
            {
                SetAlpha(1f);
                return;
            }

            UtilCoroutine.CheckRunCoroutine(ref _fadeRoutine, StartCoroutine(CoFade(1f)), this);
        }

        /// <summary> 문구를 숨긴다. </summary>
        public void Hide()
        {
            if ((_textRoot == null) || (!_isShown && !_textRoot.activeSelf))
            {
                return;
            }

            _sfxEmitter?.PlayDeactivate();

            if ((_fadeDuration <= 0f) || (_canvasGroup == null))
            {
                ApplyHidden();
                return;
            }

            _isShown = false;
            UtilCoroutine.CheckRunCoroutine(ref _fadeRoutine, StartCoroutine(CoFade(0f)), this);
        }

        public void SetMessage(string message)
        {
            _message = message;

            if ((_label != null) && _isShown)
            {
                _label.text = _message;
            }
        }

        private void AcquireCameraFocus()
        {
            if (!_useCameraFocus || _hasCameraFocus)
            {
                return;
            }

            // 마커를 따로 두지 않았으면 이 오브젝트 자리를 그대로 포커스 지점으로 쓴다
            Transform tip = (_cameraTip != null) ? _cameraTip : transform;

            CameraManager.Instance?.Focus(tip, _focusSize, _blendTime);
            _hasCameraFocus = true;
        }

        private void ReleaseCameraFocus()
        {
            if (!_hasCameraFocus)
            {
                return;
            }

            CameraManager.Instance?.UnFocus();
            _hasCameraFocus = false;
        }

        private void UpdatePlacement()
        {
            if (_textRoot == null)
            {
                return;
            }

            Vector3 targetPosition = GetAnchorPosition();

            if (_keepUpright)
            {
                _textRoot.transform.SetPositionAndRotation(targetPosition, Quaternion.identity);
            }
            else
            {
                _textRoot.transform.position = targetPosition;
            }
        }

        // 문구가 붙을 기준점
        private Vector3 GetAnchorPosition()
        {
            if (_anchorAboveBounds && TryGetBounds(out Bounds bounds))
            {
                return new Vector3(bounds.center.x, bounds.max.y + _verticalPadding, transform.position.z);
            }

            return transform.position + _worldOffset;
        }

        private void CacheBoundsSource()
        {
            if (_boundsSource != null)
            {
                _boundsCollider = _boundsSource;
                return;
            }

            if (!TryGetComponent(out _boundsCollider))
            {
                TryGetComponent(out _boundsRenderer);
            }
        }

        // 캐시가 비어 있으면 그 자리에서 한 번 조회한다
        private bool TryGetBounds(out Bounds bounds)
        {
            Collider2D col = (_boundsCollider != null) ? _boundsCollider : _boundsSource;
            if (col == null)
            {
                TryGetComponent(out col);
            }

            if (col != null)
            {
                bounds = col.bounds;
                return true;
            }

            Renderer rend = _boundsRenderer;
            if (rend == null)
            {
                TryGetComponent(out rend);
            }

            if (rend != null)
            {
                bounds = rend.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        // 슬로우모션/일시정지에도 UI 연출은 실시간으로 흐르도록 unscaled 기준으로 보간
        private IEnumerator CoFade(float target)
        {
            float start   = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = target;
            _fadeRoutine       = null;

            if (target <= 0f)
            {
                ApplyHidden();
            }
        }

        private void ApplyHidden()
        {
            _isShown = false;
            SetAlpha(0f);

            if (_textRoot != null)
            {
                _textRoot.SetActive(false);
            }

            if (_blurOverlay != null)
            {
                _blurOverlay.SetActive(false);
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
        }

        // 문구가 뜰 위치를 씬 뷰에서 확인
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 anchor = GetAnchorPosition();
            Gizmos.DrawWireSphere(anchor, 0.1f);
            Gizmos.DrawLine(transform.position, anchor);
        }
    }
}
