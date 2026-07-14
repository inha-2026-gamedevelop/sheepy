// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.Events;

using Minsung.Common;
using Minsung.Player;
using Minsung.TimeSystem;
using Minsung.UI;
using Minsung.Utility;

namespace Minsung.Interactive
{
    // 레버 상호작용 오브젝트. 되감기 시 당김 상태를 복원한다.
    public class LeverInteractive : BaseInteractive, IRewindable
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("레버 설정")]
        [SerializeField] private bool  _isOneShot               = true; // true: 한 번 당기면 다시 반응하지 않음 (스토리 진행용)
        [SerializeField] private float _interactionLockDuration = 1f;  // DoLever 애니메이션 재생 시간과 맞춰 조절
        [SerializeField] private SpriteRenderer _renderer; // 당김 상태 표시용 (비우면 같은 오브젝트에서 자동 취득)
        [SerializeField] private Sprite _spriteUnpulled; // 당기기 전 레버 - LeverAction 연출이 손잡이를 위로 올리므로 손잡이 아래(lever_down)
        [SerializeField] private Sprite _spritePulled;   // 당긴 후 레버 - 연출 마지막 프레임과 같은 손잡이 위(lever_up)

        [Header("이벤트")]
        [SerializeField] private UnityEvent _onLeverPulled; // 문 열기 등 실제 효과 (Inspector에서 연결)
        [SerializeField] private UnityEvent _onLeverReset;   // 되감기로 당기기 전 상태로 되돌아갔을 때 (문 닫기 등)

        private bool _isPulled;
        private RingBuffer<bool> _rewindBuffer; // 틱마다 _isPulled 기록 - 되감기 시 당김 상태를 복원한다

        private Coroutine _coUnlockInteraction;
        private WaitForSeconds _waitInteractionLock;

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void Awake()
        {
            base.Awake();
            _waitInteractionLock = new WaitForSeconds(_interactionLockDuration);

            if (_renderer == null)
            {
                TryGetComponent(out _renderer);
            }
            UpdateVisual();
        }

        private void Start()
        {
            // 버퍼 용량은 다른 리와인드 참여자와 동일한 기준(TickCapacity)을 써야 인덱스가 일치한다.
            _rewindBuffer = new RingBuffer<bool>(RewindManager.TickCapacity);
            RewindManager.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            RewindManager.Instance?.Unregister(this);
        }

        /****************************************
        *                Methods
        ****************************************/

        public override void OnFocus()
        {
            if ((_isOneShot) && (_isPulled))
            {
                return;
            }
            KeyGuideManager.Instance.ShowKeyGuide(EKeyGuide.Interactive);
        }

        public override void OnInteract(GameObject interactor)
        {
            if ((_isOneShot) && (_isPulled))
            {
                return;
            }

            if (interactor.TryGetComponent(out PlayerAnimator playerAnimator))
            {
                playerAnimator.SetFacing(1f); // LeverAction 아트는 오른쪽 바라보기 기준
                playerAnimator.TriggerLever();
            }

            // 연출 재생 시간 동안 상호작용한 대상의 이동/점프/공격/재상호작용 입력을 잠근다.
            if (interactor.TryGetComponent(out PlayerController playerController))
            {
                playerController.SetInteracting(true);
                // LeverAction 아트(플레이어+레버 합본)의 피벗이 레버 받침점이라, 레버 위치로 스냅하면 연출 속 레버가 실제 레버 자리에 겹친다
                playerController.SetPose(transform.position, Vector2.zero, true);
            }

            _isPulled = true;
            UpdateVisual();
            SetRendererVisible(false); // 연출 아트에 레버가 포함되므로 연출 중엔 실제 레버를 숨긴다
            UtilCoroutine.CheckRunCoroutine(ref _coUnlockInteraction, StartCoroutine(CoUnlockInteraction(playerController)), this);
            _onLeverPulled?.Invoke();
        }

        public override void OnUnfocus()
        {
            KeyGuideManager.Instance.HideKeyGuide();
        }

        private IEnumerator CoUnlockInteraction(PlayerController playerController)
        {
            yield return _waitInteractionLock;
            if (playerController != null)
            {
                playerController.SetInteracting(false);
            }
            SetRendererVisible(true); // 연출이 끝나면 당겨진 레버를 다시 보여준다
            _coUnlockInteraction = null;
        }

        private void SetRendererVisible(bool visible)
        {
            if (_renderer != null)
            {
                _renderer.enabled = visible;
            }
        }

        // IRewindable - 레버 당김 상태를 틱마다 기록해 되감으면 당기기 전 상태로 복원한다.

        public void RecordTick()
        {
            _rewindBuffer.Push(_isPulled);
        }

        public void OnRewindStart() { }

        public void ApplyRewindTick(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out bool wasPulled))
            {
                ApplyPulledState(wasPulled);
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out bool wasPulled))
            {
                ApplyPulledState(wasPulled);
            }
            _rewindBuffer.Clear();
        }

        // 기록된 당김 상태로 복원. true -> false로 되돌아간 순간에만 리셋 이벤트를 발생시킨다.
        private void ApplyPulledState(bool wasPulled)
        {
            // 원샷 레버(스토리 진행용)는 한 번 당기면 되감기로도 되돌리지 않는다.
            // 당긴 이미지가 유지되고, 분신 재연(OnInteract)도 위의 원샷 가드에 막혀 다시 돌아가지 않는다.
            if (_isOneShot && _isPulled)
            {
                return;
            }
            if (_isPulled == wasPulled)
            {
                return;
            }
            _isPulled = wasPulled;
            UpdateVisual();

            if (!_isPulled)
            {
                _onLeverReset?.Invoke();
            }
        }

        // 당김 상태에 따라 스프라이트를 교체한다. 스프라이트 미지정 시 색 표시로 폴백(프로토타입용)
        private void UpdateVisual()
        {
            #if UNITY_EDITOR
            Debug.Log(_isPulled ? "레버 켜짐" : "레버 꺼짐");
            #endif

            if (_renderer == null)
            {
                return;
            }

            if ((_spriteUnpulled != null) && (_spritePulled != null))
            {
                _renderer.sprite = _isPulled ? _spritePulled : _spriteUnpulled;
                return;
            }
            _renderer.color = _isPulled ? Constants.Interactive.LEVER_PULLED_COLOR : Constants.Interactive.LEVER_UNPULLED_COLOR;
        }
    }
}
