// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Interactive;
using Minsung.Player;
using Minsung.UI;

namespace Minsung.Boss2
{
    // 3페이즈 낙인 정화 제단 - E키 3초 홀드로 Boss2BrandController의 낙인 스택을 0으로 초기화한다
    // Minsung.Interactive.BaseInteractive/IHoldInteractable을 그대로 재사용(ElevatorButtonInteractive와 동일한 홀드 패턴)
    // 정화해도 제단 자체는 사라지지 않는다 - 보스 본체가 닿았을 때만(OnTriggerEnter2D) 소멸한다(기획 6번)
    public class Boss2AltarInteractive : BaseInteractive, IHoldInteractable
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("데이터")]
        [SerializeField] private Boss2DataSO _dataSo;
        [SerializeField] private Boss2BrandController _brandController;

        [Header("홀드 UI")]
        [SerializeField] private GameObject _holdUi;         // 진행도 UI 루트
        [SerializeField] private Slider     _progressSlider; // 홀드 진행도 0~1

        private GameObject _interactor;
        private float      _holdElapsed;
        private bool       _isHolding;

        public bool CanHoldInteract => true;

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
            if (!_isHolding)
            {
                OnHoldCancel(interactor);
                return false;
            }

            float holdDuration = (_dataSo != null) ? _dataSo.AltarHoldDuration : 3f;
            _holdElapsed += deltaTime;
            SetProgress(_holdElapsed / holdDuration);

            if (_holdElapsed < holdDuration)
            {
                return false;
            }

            _isHolding = false;
            _brandController?.ClearStacks();
            ReleaseInteractor(interactor);
            SetHoldUiVisible(false);
            return true;
        }

        public void OnHoldCancel(GameObject interactor)
        {
            if (!_isHolding)
            {
                return;
            }
            _isHolding   = false;
            _holdElapsed = 0f;
            SetProgress(0f);
            ReleaseInteractor(interactor);
        }

        /****************************************
        *                Methods
        ****************************************/

        // 보스 본체(BossFloatMovement 보유 루트)와 닿으면 소멸
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<BossFloatMovement>() == null)
            {
                return;
            }
            OnHoldCancel(_interactor);
            gameObject.SetActive(false);
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
    }
}
