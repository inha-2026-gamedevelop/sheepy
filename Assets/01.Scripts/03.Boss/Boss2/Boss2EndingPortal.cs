// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

using Minsung.Interactive;
using Minsung.Player;
using Minsung.UI;
using Minsung.Visual;
using Minsung.Achievement;

namespace Minsung.Boss2
{
    // 엔딩 포탈 상호작용 - Boss2AltarInteractive와 동일한 IHoldInteractable 홀드 패턴 재사용.
    // E키를 EndingHoldDuration(기본 5초)만큼 홀드하면 완료 - 홀드 중엔 포커스 카메라가 홀드 시작 시점 크기에서
    // EndingZoomEndSize까지 진행도에 비례해 점점 줌인되고, 완료되면 화면이 암전되며 엔딩 업적을 해제한다.
    // 이 오브젝트(포탈 그룹)는 Boss2DeathSequence가 사망 연출 종료 후 SetActive(true)로 켜기 전까지 비활성 상태라
    // BaseInteractive.OnEnable/OnDisable을 통해 InteractableRegistry에도 등록되지 않는다.
    //
    // CameraManager/ScreenFade/AchievementTrigger(공용 파일, 타 작업자와 충돌 우려)는 건드리지 않고,
    // 포커스 카메라(Boss2DeathSequence가 Focus()로 이미 우선순위를 올려둔 바로 그 CinemachineCamera)를
    // 직접 참조해 Lens/위치만 조정한다 - CameraManager.Focus()가 담당하는 우선순위/블렌드에는 관여하지 않는다.
    public class Boss2EndingPortal : BaseInteractive, IHoldInteractable
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("데이터")]
        [SerializeField] private Boss2DataSO _dataSo;

        [Header("카메라 (CameraManager가 쓰는 FocusCamera와 동일 오브젝트를 연결)")]
        [SerializeField] private CinemachineCamera _focusCamera;
        [Tooltip("홀드 시작 시 카메라 포커스 위치에 더할 오프셋 - 포탈 중심 그대로면 줌인했을 때 아래에 서있는 캐릭터가 프레임 밖으로 벗어나므로 아래쪽으로 살짝 내려서 잡는다")]
        [SerializeField] private Vector2 _cameraFocusOffset = new Vector2(0f, -1f);

        [Header("홀드 UI (선택 - 미배치 시 카메라 줌인만으로 진행도 표현)")]
        [SerializeField] private GameObject _holdUi;
        [SerializeField] private Slider     _progressSlider;

        private GameObject _interactor;
        private float      _holdElapsed;
        private bool       _isHolding;
        private bool       _completed; // 완료 후 재상호작용 차단(1회성)
        private float      _zoomStartSize;
        private Coroutine  _zoomResetRoutine;

        public bool CanHoldInteract => !_completed;

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void Awake()
        {
            base.Awake();
            SetHoldUiVisible(false);
            SetProgress(0f);
        }

        /****************************************
        *            IInteractable
        ****************************************/

        public override void OnFocus()
        {
            if (_completed)
            {
                return;
            }
            KeyGuideManager.Instance.ShowKeyGuide(EKeyGuide.Interactive);
            SetHoldUiVisible(true);
        }

        public override void OnInteract(GameObject interactor) { } // 홀드 전용 - 즉시 상호작용 없음

        public override void OnUnfocus()
        {
            OnHoldCancel(_interactor);
            KeyGuideManager.Instance.HideKeyGuide();
            SetHoldUiVisible(false);
        }

        /****************************************
        *            IHoldInteractable
        ****************************************/

        public bool OnHoldStart(GameObject interactor)
        {
            if (_completed)
            {
                return false;
            }

            _interactor  = interactor;
            _holdElapsed = 0f;
            _isHolding   = true;
            SetProgress(0f);

            if (_zoomResetRoutine != null)
            {
                StopCoroutine(_zoomResetRoutine);
                _zoomResetRoutine = null;
            }
            _zoomStartSize = GetFocusOrthographicSize();
            SetFocusPosition(transform.position + (Vector3)_cameraFocusOffset);

            if (interactor.TryGetComponent(out PlayerController playerController))
            {
                playerController.SetInteracting(true);
            }
            return true;
        }

        public bool OnHoldUpdate(GameObject interactor, float deltaTime)
        {
            if (!_isHolding)
            {
                OnHoldCancel(interactor);
                return false;
            }

            float holdDuration = (_dataSo != null) ? _dataSo.EndingHoldDuration : 5f;
            _holdElapsed += deltaTime;
            float progress = Mathf.Clamp01(_holdElapsed / holdDuration);
            SetProgress(progress);

            float endSize = (_dataSo != null) ? _dataSo.EndingZoomEndSize : 1.5f;
            SetFocusOrthographicSize(Mathf.Lerp(_zoomStartSize, endSize, progress));

            if (_holdElapsed < holdDuration)
            {
                return false;
            }

            _isHolding = false;
            _completed = true;
            ReleaseInteractor(interactor);
            KeyGuideManager.Instance.HideKeyGuide();
            SetHoldUiVisible(false);

            float fadeDuration = (_dataSo != null) ? _dataSo.EndingFadeDuration : 1.5f;
            EnsureScreenFade().FadeOut(fadeDuration, () => AchievementManager.Instance?.Unlock(AchievementIds.ENDING_CREDITS));
            return true;
        }

        public void OnHoldCancel(GameObject interactor)
        {
            if (!_isHolding || _completed)
            {
                return;
            }
            _isHolding   = false;
            _holdElapsed = 0f;
            SetProgress(0f);
            ReleaseInteractor(interactor);

            if (_zoomResetRoutine != null)
            {
                StopCoroutine(_zoomResetRoutine);
            }
            _zoomResetRoutine = StartCoroutine(CoResetZoom(_zoomStartSize));
        }

        /****************************************
        *                Methods
        ****************************************/

        // 홀드 취소 시 카메라를 홀드 시작 시점 크기로 부드럽게 복귀
        private IEnumerator CoResetZoom(float targetSize)
        {
            float current = GetFocusOrthographicSize();
            const float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetFocusOrthographicSize(Mathf.Lerp(current, targetSize, elapsed / duration));
                yield return null;
            }

            SetFocusOrthographicSize(targetSize);
            _zoomResetRoutine = null;
        }

        // CameraManager.SetOrthographicSize와 달리 매 프레임 호출을 전제로 로그를 남기지 않는다
        private float GetFocusOrthographicSize()
        {
            return (_focusCamera != null) ? _focusCamera.Lens.OrthographicSize : 0f;
        }

        private void SetFocusOrthographicSize(float size)
        {
            if (_focusCamera == null)
            {
                return;
            }
            LensSettings lens = _focusCamera.Lens;
            lens.OrthographicSize = size;
            _focusCamera.Lens = lens;
        }

        private void SetFocusPosition(Vector3 worldPosition)
        {
            if (_focusCamera == null)
            {
                return;
            }
            worldPosition.z = _focusCamera.transform.position.z;
            _focusCamera.transform.position = worldPosition;
        }

        // GameManager.EnsureScreenFade와 동일한 관례 - 부팅 씬을 거치지 않고 이 씬을 단독 실행/테스트해도 페이드가 항상 동작하도록 보장한다
        private static ScreenFade EnsureScreenFade()
        {
            if (ScreenFade.Instance == null)
            {
                new GameObject("ScreenFade").AddComponent<ScreenFade>();
            }
            return ScreenFade.Instance;
        }

        private void ReleaseInteractor(GameObject interactor)
        {
            if ((interactor != null) && interactor.TryGetComponent(out PlayerController playerController))
            {
                playerController.SetInteracting(false);
            }
            _interactor = null;
        }

        private void SetHoldUiVisible(bool visible)
        {
            if (_holdUi != null)
            {
                _holdUi.SetActive(visible);
            }
        }

        private void SetProgress(float progress)
        {
            if (_progressSlider != null)
            {
                _progressSlider.value = Mathf.Clamp01(progress);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_dataSo == null)
            {
                Debug.LogWarning("[Boss2EndingPortal] _dataSo 미배치 - 홀드 지속시간/줌/암전 설정을 Boss2DB에서 가져올 수 없습니다.", this);
            }
            if (_focusCamera == null)
            {
                Debug.LogWarning("[Boss2EndingPortal] _focusCamera 미배치 - CameraManager의 FocusCamera를 연결해야 홀드 중 줌인이 재생됩니다.", this);
            }
        }
#endif
    }
}
