// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Player;
using Minsung.Sound;
using Minsung.UI;

namespace Minsung.Interactive
{
    // ElevatorId로 엘리베이터를 찾아 3초 홀드 입력 후 이동을 시작하는 버튼
    public class ElevatorButtonInteractive : BaseInteractive, IHoldInteractable
    {
        /****************************************
        *                Fields
        ****************************************/

        private const float HOLD_DURATION = 3f; // 엘리베이터 호출에 필요한 홀드 시간 (초)

        [Header("식별자")]
        [SerializeField, Min(1)] private int _elevatorId = 1; // 연결할 ElevatorController와 동일한 ID

        [Header("홀드 UI")]
        [SerializeField] private GameObject _holdUi; // elevator_ui_1 오브젝트
        [SerializeField] private Slider     _progressSlider; // 선택 항목. 지정 시 홀드 진행도를 0~1로 표시

        private ElevatorController _controller;
        private GameObject         _interactor;
        private float              _holdElapsed;
        private bool               _isHolding;
        private LocalSfxEmitter    _sfxEmitter;

        public bool CanHoldInteract => TryGetController(out ElevatorController controller) && controller.CanStart;

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _sfxEmitter);
            SetHoldUiVisible(false);
            SetProgress(0f);
        }

        /****************************************
        *                Methods
        ****************************************/

        public override void OnFocus()
        {
            if (!CanHoldInteract)
            {
                return;
            }
            KeyGuideManager.Instance.ShowKeyGuide(EKeyGuide.Interactive);
            SetHoldUiVisible(true);
        }

        public override void OnInteract(GameObject interactor)
        {
            // 분신은 완료된 홀드 상호작용만 커맨드로 재연하므로 즉시 실행한다
            if (TryGetController(out ElevatorController controller))
            {
                if (controller.TryStartJourney())
                {
                    _sfxEmitter?.PlayInteract();
                }
            }
        }

        public override void OnUnfocus()
        {
            OnHoldCancel(_interactor);
            KeyGuideManager.Instance.HideKeyGuide();
            SetHoldUiVisible(false);
        }

        public bool OnHoldStart(GameObject interactor)
        {
            if (!CanHoldInteract)
            {
                return false;
            }

            _interactor  = interactor;
            _holdElapsed = 0f;
            _isHolding   = true;
            SetProgress(0f);

            if (interactor.TryGetComponent(out PlayerController playerController))
            {
                playerController.SetInteracting(true);
            }
            return true;
        }

        public bool OnHoldUpdate(GameObject interactor, float deltaTime)
        {
            if (!_isHolding || !CanHoldInteract)
            {
                OnHoldCancel(interactor);
                return false;
            }

            _holdElapsed += deltaTime;
            SetProgress(_holdElapsed / HOLD_DURATION);

            if (_holdElapsed < HOLD_DURATION)
            {
                return false;
            }

            _isHolding = false;
            bool started = _controller.TryStartJourney();
            if (started)
            {
                _sfxEmitter?.PlayInteract();
            }
            ReleaseInteractor(interactor);
            SetHoldUiVisible(false);
            return started;
        }

        public void OnHoldCancel(GameObject interactor)
        {
            if (!_isHolding)
            {
                return;
            }

            _isHolding = false;
            _holdElapsed = 0f;
            SetProgress(0f);
            ReleaseInteractor(interactor);
        }

        private bool TryGetController(out ElevatorController controller)
        {
            if (_controller == null)
            {
                ElevatorManager.Instance?.TryGetController(_elevatorId, out _controller);
            }

            controller = _controller;
            return controller != null;
        }

        private void ReleaseInteractor(GameObject interactor)
        {
            if (interactor != null)
            {
                if (interactor.TryGetComponent(out PlayerController playerController))
                {
                    playerController.SetInteracting(false);
                }
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
    }
}
