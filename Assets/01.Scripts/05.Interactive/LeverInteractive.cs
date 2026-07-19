// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.Events;

using Minsung.Common;
using Minsung.Player;
using Minsung.Sound;
using Minsung.TimeSystem;
using Minsung.UI;
using Minsung.Utility;

namespace Minsung.Interactive
{
    // 레버 상호작용 오브젝트. 되감기 시 당김 상태를 복원한다.
    public class LeverInteractive : BaseInteractive, IRewindable
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 한 틱의 레버 기록 - 당김 상태와 렌더러 표시 여부(당김 연출 중 실제 레버 숨김을 스크럽에 재현)
        private readonly struct LeverTick
        {
            public readonly bool Pulled;
            public readonly bool Visible;

            public LeverTick(bool pulled, bool visible)
            {
                Pulled  = pulled;
                Visible = visible;
            }
        }

        /****************************************
        *                Fields
        ****************************************/

        [Header("레버 설정")]
        [SerializeField] private bool  _isOneShot               = true; // true: 한 번 당기면 다시 반응하지 않음 (스토리 진행용)
        [SerializeField] private float _interactionLockDuration = 1f;  // DoLever 애니메이션 재생 시간과 맞춰 조절
        [SerializeField] private SpriteRenderer _renderer; // 당김 상태 표시용 (비우면 같은 오브젝트에서 자동 취득)
        [SerializeField] private Sprite _spriteUnpulled; // 당기기 전 레버 - LeverAction 연출이 손잡이를 위로 올리므로 손잡이 아래(lever_down)
        [SerializeField] private Sprite _spritePulled;   // 당긴 후 레버 - 연출 마지막 프레임과 같은 손잡이 위(lever_up)

        [Header("엘리베이터 연동")]
        [SerializeField, Min(0)] private int _elevatorId; // 0이면 엘리베이터 연동 없이 기존 레버 이벤트만 실행

        [Header("이벤트")]
        [SerializeField] private UnityEvent _onLeverPulled; // 문 열기 등 실제 효과 (Inspector에서 연결)
        [SerializeField] private UnityEvent _onLeverReset;   // 되감기로 당기기 전 상태로 되돌아갔을 때 (문 닫기 등)

        private bool _isPulled;
        private RingBuffer<LeverTick> _rewindBuffer; // 틱마다 당김/표시 상태 기록 - 되감기 시 그대로 복원한다
        private LocalSfxEmitter _sfxEmitter;

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
            TryGetComponent(out _sfxEmitter);
            UpdateVisual();
        }

        private void Start()
        {
            // 버퍼 용량은 다른 리와인드 참여자와 동일한 기준(TickCapacity)을 써야 인덱스가 일치한다.
            _rewindBuffer = new RingBuffer<LeverTick>(RewindManager.TickCapacity);
            RewindManager.Instance?.Register(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
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
            _sfxEmitter?.PlayInteract();
            SetRendererVisible(false); // 연출 아트에 레버가 포함되므로 연출 중엔 실제 레버를 숨긴다
            UtilCoroutine.CheckRunCoroutine(ref _coUnlockInteraction, StartCoroutine(CoUnlockInteraction(playerController)), this);
            _onLeverPulled?.Invoke();
            NotifyElevatorLeverState(true);
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
            bool visible = (_renderer == null) || _renderer.enabled;
            _rewindBuffer.Push(new LeverTick(_isPulled, visible));
        }

        public void OnRewindStart()
        {
            // 정방향 연출 도중 되감기가 시작되면 예약된 재표시/잠금해제 코루틴을 취소한다
            // (입력 잠금 해제는 PlayerInteraction.ForceStop 담당, 레버 재표시는 OnRewindEnd 담당)
            UtilCoroutine.CheckStopCoroutine(ref _coUnlockInteraction, this);
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out LeverTick tick))
            {
                ApplyPulledState(tick.Pulled);
                SetRendererVisible(tick.Visible);
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out LeverTick tick))
            {
                ApplyPulledState(tick.Pulled);
            }
            // 연출 도중 틱에 착지하면 기록상 숨김이지만, 재표시 코루틴이 더는 없으므로 항상 표시로 복구한다
            SetRendererVisible(true);
            _rewindBuffer.Clear();
        }

        // 기록된 당김 상태로 복원. true -> false로 되돌아간 순간에만 리셋 이벤트를 발생시킨다.
        // 되감기는 원샷 여부와 무관하게 당김을 되돌린다 - 원샷은 "당겨진 동안 재입력 무시"(OnInteract 가드)만
        // 담당하므로, 되감기로 풀린 뒤에는 플레이어도 분신 재연도 다시 당길 수 있다.
        private void ApplyPulledState(bool wasPulled)
        {
            if (_isPulled == wasPulled)
            {
                return;
            }
            _isPulled = wasPulled;
            UpdateVisual();

            if (_isPulled)
            {
                NotifyElevatorLeverState(true);
            }
            else
            {
                _onLeverReset?.Invoke();
                NotifyElevatorLeverState(false);
            }
        }

        private void NotifyElevatorLeverState(bool pulled)
        {
            if (_elevatorId <= 0)
            {
                return;
            }

            if ((ElevatorManager.Instance != null) && ElevatorManager.Instance.TryGetController(_elevatorId, out ElevatorController controller))
            {
                controller.SetLeverPulled(pulled);
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
