// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

using Minsung.Interactive;
using Minsung.UI;

namespace Minsung.Player
{
    // 플레이어 주변의 IInteractable 감지 + E키 상호작용 실행.
    public class PlayerInteractionSensor : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const int MAX_HIT_BUFFER_SIZE = 10; // CircleCast 결과 버퍼 크기

        [Header("Sensor Settings")]
        [FormerlySerializedAs("fItemSensorRadius")]
        [SerializeField] private float      _itemSensorRadius  = 0.5f;
        [FormerlySerializedAs("fItemRangeOffset")]
        [SerializeField] private float      _itemRangeOffset   = 0.0f; // 바라보는 방향 오프셋
        [FormerlySerializedAs("itemLayer")]
        [SerializeField] private LayerMask  _itemLayer;                // 상호작용 오브젝트 레이어
        [FormerlySerializedAs("fSphereCastYOffset")]
        [SerializeField] private float      _sphereCastYOffset = 0.5f; // 발밑이 아닌 몸통 기준 감지용 Y 오프셋
        [SerializeField] private KeyCode    _interactKey       = KeyCode.E;

        [Header("키 가이드")]
        [SerializeField] private GameObject _keyGuidePanel; // 머리 위 키 가이드 패널 루트
        [SerializeField] private Image      _keyGuideImage;

        [Header("Debug")]
        [FormerlySerializedAs("bShowGizmos")]
        [SerializeField] private bool  _showGizmos     = true;
        [FormerlySerializedAs("isSensorActive")]
        [SerializeField] private bool  _isSensorActive = true;

        private PlayerController _playerController;
        private Vector3 _keyGuideOffset; // 키 가이드의 플레이어 기준 월드 오프셋 (루트 회전과 무관하게 머리 위 고정용)

        private IInteractable _currentInteractable;
        private IInteractable _previousInteractable;
        private IHoldInteractable _activeHoldInteractable;
        private GameObject        _activeHoldTarget;

        private ContactFilter2D _itemContactFilter;
        private readonly RaycastHit2D[] _sphereHits = new RaycastHit2D[MAX_HIT_BUFFER_SIZE]; // NonAlloc용 버퍼

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            if (!TryGetComponent(out _playerController))
            {
                Debug.LogWarning($"[{nameof(PlayerInteractionSensor)}] {nameof(PlayerController)}를 찾을 수 없습니다.", this);
            }

            _itemContactFilter = new ContactFilter2D();
            _itemContactFilter.SetLayerMask(_itemLayer);
            _itemContactFilter.useTriggers = Physics2D.queriesHitTriggers;
        }

        private void Start()
        {
            // 싱글톤 Awake 순서 보장이 없으므로 모든 Awake가 끝난 뒤인 Start에서 등록한다.
            KeyGuideManager.Instance?.SetTarget(_keyGuidePanel, _keyGuideImage);

            if (_keyGuidePanel != null)
            {
                _keyGuideOffset = _keyGuidePanel.transform.position - transform.position;
            }
        }

        // 점프/레버 클립이 플레이어 루트를 z회전시켜도 키 가이드는 항상 머리 위에 바로 서 있게 고정
        private void LateUpdate()
        {
            if (_keyGuidePanel == null)
            {
                return;
            }
            _keyGuidePanel.transform.SetPositionAndRotation(transform.position + _keyGuideOffset, Quaternion.identity);
        }

        private void Update()
        {
            // GetKeyDown은 눌린 그 프레임에만 true이므로, FixedUpdate(물리 틱)가 아니라
            // 매 렌더 프레임 도는 Update에서 검사해야 입력이 씹히지 않는다.
            if (!CanInteract())
            {
                return;
            }
            HandleInteractionInput();
        }

        private void FixedUpdate()
        {
            if (!CanInteract())
            {
                return;
            }
            DetectInteractable();
        }

        private bool CanInteract()
        {
            if (!_isSensorActive)
            {
                CancelHoldInteraction();
                return false;
            }
            // 되감기 중에는 플레이어 위치가 RewindManager에 의해 강제로 이동하므로 판정을 쉰다.
            if ((_playerController != null) && _playerController.IsRewinding)
            {
                CancelHoldInteraction();
                return false;
            }
            return true;
        }

        /****************************************
        *                Methods
        ****************************************/

        private void DetectInteractable()
        {
            Vector2 sphereOrigin =
                (Vector2)transform.position
                + ((Vector2)transform.up * _sphereCastYOffset)
                + ((Vector2)transform.right * _itemRangeOffset);

            // 2D CircleCast (ContactFilter2D + 재사용 버퍼 - 매 틱 할당 없음)
            int hitCount = Physics2D.CircleCast(
                sphereOrigin,
                _itemSensorRadius,
                transform.up,
                _itemContactFilter,
                _sphereHits,
                0f
            );

            IInteractable foundTarget = (hitCount > 0) ? FindClosestInteractable(hitCount) : null;

            UpdateTarget(foundTarget);
        }

        /// <summary> 구체형 캐스트 결과에서 가장 가까운 Interactable 찾기 </summary>
        private IInteractable FindClosestInteractable(int hitCount)
        {
            IInteractable closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; ++i)
            {
                IInteractable interactable = InteractableRegistry.Get(_sphereHits[i].collider);

                if (interactable != null)
                {
                    Vector2 targetPos = interactable.GetTransform().position;

                    float distance = Vector2.Distance(transform.position, targetPos);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = interactable;
                    }
                }
            }

            return closest;
        }

        /// <summary> 센서 켜기/끄기 (컷신/상점 진입 등 상호작용을 막아야 할 때 외부에서 호출). </summary>
        public void SetSensorActive(bool active)
        {
            _isSensorActive = active;
            if (!active)
            {
                ClearCurrentTarget(); // 꺼진 동안 포커스 잔상이 남지 않게 정리
            }
        }

        /// <summary> 타겟 변경 시 Focus/Unfocus 처리 </summary>
        private void UpdateTarget(IInteractable newTarget)
        {
            if (ReferenceEquals(_currentInteractable, newTarget))
            {
                return;
            }

            if (_activeHoldInteractable != null)
            {
                CancelHoldInteraction();
            }

            _previousInteractable = _currentInteractable;
            _previousInteractable?.OnUnfocus();

            _currentInteractable = newTarget;
            _currentInteractable?.OnFocus();
        }

        /// <summary> 포커스 중인 대상을 강제로 OnUnfocus 처리하고 비운다 (상점 종료 연출 등 센서 정지 시 포커스 잔상 제거용). </summary>
        public void ClearCurrentTarget()
        {
            CancelHoldInteraction();
            _currentInteractable?.OnUnfocus();
            _currentInteractable = null;
            _previousInteractable = null;
        }

        /// <summary> 상호작용 입력 처리 </summary>
        private void HandleInteractionInput()
        {
            if (_activeHoldInteractable != null)
            {
                HandleHoldInteraction();
                return;
            }

            if ((_playerController != null) && _playerController.IsInteracting)
            {
                return;
            }

            if (Input.GetKeyDown(_interactKey) && (_currentInteractable != null))
            {
                if ((_currentInteractable is IHoldInteractable holdInteractable) && holdInteractable.CanHoldInteract)
                {
                    if (holdInteractable.OnHoldStart(gameObject))
                    {
                        _activeHoldInteractable = holdInteractable;
                        _activeHoldTarget        = _currentInteractable.GetTransform().gameObject;
                    }
                    return;
                }

                GameObject target = _currentInteractable.GetTransform().gameObject;
                _currentInteractable.OnInteract(gameObject);
                _playerController?.NotifyInteracted(target); // 분신 재연용 - 다음 RecordTick에 기록됨
            }
        }

        private void HandleHoldInteraction()
        {
            if (!ReferenceEquals(_activeHoldInteractable, _currentInteractable) || !Input.GetKey(_interactKey))
            {
                CancelHoldInteraction();
                return;
            }

            if (!_activeHoldInteractable.OnHoldUpdate(gameObject, Time.deltaTime))
            {
                return;
            }

            _playerController?.NotifyInteracted(_activeHoldTarget); // 완료된 홀드만 분신 커맨드로 기록
            _activeHoldInteractable = null;
            _activeHoldTarget = null;
        }

        private void CancelHoldInteraction()
        {
            if (_activeHoldInteractable == null)
            {
                return;
            }

            IHoldInteractable holdInteractable = _activeHoldInteractable;
            _activeHoldInteractable = null;
            _activeHoldTarget = null;
            holdInteractable.OnHoldCancel(gameObject);
        }

        // 디버그용 기즈모 - 실제 감지에 쓰는 CircleCast 원점/반경을 그대로 표시한다.
        private void OnDrawGizmos()
        {
            if (!_showGizmos)
            {
                return;
            }

            Gizmos.color = Color.blue;
            Vector3 sphereOrigin = transform.position + (transform.up * _sphereCastYOffset) + (transform.right * _itemRangeOffset);
            Gizmos.DrawWireSphere(sphereOrigin, _itemSensorRadius);
        }
    }
}
