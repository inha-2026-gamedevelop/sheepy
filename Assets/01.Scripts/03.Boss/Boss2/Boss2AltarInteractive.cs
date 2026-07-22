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

        [Header("타임아웃 & 페이드")]
        [SerializeField] private float _activeDuration = 15f; // 제단 유지 시간
        [SerializeField] private float _fadeDuration   = 0.5f; // 페이드 아웃 시간

        private GameObject _interactor;
        private float      _holdElapsed;
        private bool       _isHolding;

        private Coroutine        _timeoutRoutine;
        private Coroutine        _fadeRoutine;
        private bool             _isFading;
        private SpriteRenderer[] _renderers;
        private CanvasGroup      _uiCanvasGroup;

        public bool CanHoldInteract => !_isFading;

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void Awake()
        {
            base.Awake();
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (_holdUi != null)
            {
                _uiCanvasGroup = _holdUi.GetComponent<CanvasGroup>();
                if (_uiCanvasGroup == null)
                {
                    _uiCanvasGroup = _holdUi.AddComponent<CanvasGroup>();
                }
            }
            SetHoldUiVisible(false);
            SetProgress(1f);
        }

        protected override void OnEnable()
        {
            base.OnEnable(); // 베이스의 InteractableRegistry 등록(E키 감지) - 반드시 호출해야 상호작용이 동작한다
            if (_activeDuration <= 0f) _activeDuration = 15f;
            if (_fadeDuration <= 0f) _fadeDuration = 0.5f;

            _isFading = false;

            foreach (var r in _renderers)
            {
                if (r != null)
                {
                    Color c = r.color;
                    c.a = 1f;
                    r.color = c;
                }
            }
            if (_uiCanvasGroup != null)
            {
                _uiCanvasGroup.alpha = 1f;
            }

            if (_timeoutRoutine != null) StopCoroutine(_timeoutRoutine);
            _timeoutRoutine = StartCoroutine(CoTimeout());
        }

        private System.Collections.IEnumerator CoTimeout()
        {
            yield return new WaitForSeconds(_activeDuration);
            StartFadeOut();
        }

        private void StartFadeOut()
        {
            if (_isFading) return;
            _isFading = true;

            if (_timeoutRoutine != null)
            {
                StopCoroutine(_timeoutRoutine);
                _timeoutRoutine = null;
            }

            OnHoldCancel(_interactor);

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(CoFadeOut());
        }

        private System.Collections.IEnumerator CoFadeOut()
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsed / _fadeDuration));

                foreach (var r in _renderers)
                {
                    if (r != null)
                    {
                        Color c = r.color;
                        c.a = alpha;
                        r.color = c;
                    }
                }
                if (_uiCanvasGroup != null)
                {
                    _uiCanvasGroup.alpha = alpha;
                }

                yield return null;
            }

            gameObject.SetActive(false);
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
            SetProgress(1f);

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
            SetProgress(1f - (_holdElapsed / holdDuration));

            if (_holdElapsed < holdDuration)
            {
                return false;
            }

            _isHolding = false;
            _brandController?.ClearStacks();
            ReleaseInteractor(interactor);
            StartFadeOut();
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
            SetProgress(1f);
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
            StartFadeOut();
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
