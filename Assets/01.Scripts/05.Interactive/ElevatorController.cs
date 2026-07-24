// System
using Minsung.Achievement;
using Minsung.Common;
using Minsung.Sound;
using Minsung.TimeSystem;
using System.Collections.Generic;
using Unity.Cinemachine;
// Unity
using UnityEngine;
using Unity.Cinemachine;

namespace Minsung.Interactive
{
    // ElevatorId로 레버와 버튼을 연결하고, 지정된 경로를 따라 엘리베이터 본체를 이동시킨다
    public class ElevatorController : MonoBehaviour, IRewindable
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 한 틱의 엘리베이터 상태. 이동 위치와 다음 경유지를 함께 보존해 되감기 중 정확히 복원한다
        private readonly struct ElevatorTick
        {
            public readonly bool    LeverPulled;
            public readonly bool    IsMoving;
            public readonly bool    HasArrived;
            public readonly bool    IsDoorClosed;
            public readonly Vector3 Position;

            public ElevatorTick(bool leverPulled, bool isMoving, bool hasArrived, bool isDoorClosed,
                Vector3 position)
            {
                LeverPulled  = leverPulled;
                IsMoving     = isMoving;
                HasArrived   = hasArrived;
                IsDoorClosed = isDoorClosed;
                Position     = position;
            }
        }

        /****************************************
        *                Fields
        ****************************************/

        private const float ARRIVAL_DISTANCE_SQR = 0.0001f; // 경유지 도착 판정 거리 제곱

        [Header("식별자")]
        [SerializeField, Min(1)] private int _elevatorId = 1; // 레버와 버튼이 공유하는 엘리베이터 ID
        [SerializeField, Min(1)] private int _requiredLeverCount = 1; // 출발에 필요한 서로 다른 레버 수
        [SerializeField, Min(0.1f)] private float _leverActivationWindow = 2f; // 모든 레버가 켜져야 하는 제한 시간

        [Header("이동")]
        [SerializeField] private Transform   _platform; // 실제 이동할 elevator1 오브젝트
        [SerializeField] private Rigidbody2D _platformRigidbody; // 플랫폼 충돌체가 있으면 지정
        [SerializeField] private Transform   _startPoint; // 엘리베이터 초기 위치 마커
        [SerializeField] private Transform   _endPoint; // 레버와 버튼 완료 후 이동할 도착 위치 마커
        [SerializeField, Min(0.01f)] private float _moveSpeed = 2f; // 이동 속도 (unit/s)

        [Header("문")]
        [SerializeField] private GameObject _closedDoor; // 이동 중 켜질 ElevatorBodyClose
        [SerializeField] private GameObject _openDoor; // 대기 및 도착 시 켜질 ElevatorBodyOpen

        private bool _isLeverPulled;
        private bool _isMoving;
        private bool _hasArrived;
        private bool _isDoorClosed;
        private float _leverWindowRemaining;

        // 같은 레버의 중복 통지는 한 번으로 계산하고, 각 레버의 되감기 상태도 독립적으로 반영한다.
        private readonly HashSet<LeverInteractive> _pulledLevers = new HashSet<LeverInteractive>();

        private RingBuffer<ElevatorTick> _rewindBuffer;
        private LocalSfxEmitter _sfxEmitter;
        private string _objectId;

        public int  ElevatorId => _elevatorId;
        public bool CanStart => _isLeverPulled && !_isMoving && !_hasArrived && HasValidRoute();
        public string ObjectId => _objectId;

        private CinemachineImpulseSource impulseSource;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _objectId = ManagedObjectManager.Register(EManagedObjectType.Elevator, this);
            if (_platform == null)
            {
                _platform = transform;
            }

            if (_platformRigidbody == null)
            {
                _platform.TryGetComponent(out _platformRigidbody);
            }
            TryGetComponent(out _sfxEmitter);

            if (_startPoint != null)
            {
                SetPlatformPosition(_startPoint.position);
            }
            SetDoorClosed(false);
        }

        private void OnEnable()
        {
            ElevatorManager.Instance?.Register(this);
        }

        private void Start()
        {
            // AfterSceneLoad 자동 생성보다 씬 오브젝트 OnEnable이 먼저 실행되는 경우를 보완한다
            ElevatorManager.Instance?.Register(this);
            _rewindBuffer = new RingBuffer<ElevatorTick>(RewindManager.TickCapacity);
            RewindManager.Instance?.Register(this);

            // 화면 흔들림을 위한 컴포넌트를 멤버 변수에 저장
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private void Update()
        {
            UpdateLeverActivationWindow();

            if ((_platformRigidbody != null) || !CanMoveForward())
            {
                return;
            }
            MovePlatform(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if ((_platformRigidbody == null) || !CanMoveForward())
            {
                return;
            }
            MovePlatform(Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            ElevatorManager.Instance?.Unregister(this);
        }

        private void OnDestroy()
        {
            ManagedObjectManager.Unregister(this);
            RewindManager.Instance?.Unregister(this);
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary>어떤 레버가 당겨졌는지 개별 상태를 전달받는다.</summary>
        public void SetLeverPulled(LeverInteractive lever, bool pulled)
        {
            if (lever == null)
            {
                return;
            }

            if (pulled)
            {
                _pulledLevers.Add(lever);
                if (_pulledLevers.Count >= 2)
                {
                    AchievementTrigger.LeversSynchronized(); // 서로 다른 레버 2개가 창 안에서 동시에 당겨짐 - "싱크로나이즈드 올림픽"
                }
            }
            else
            {
                _pulledLevers.Remove(lever);
            }

            _isLeverPulled = _pulledLevers.Count >= _requiredLeverCount;

            if (_isLeverPulled || (_pulledLevers.Count == 0))
            {
                _leverWindowRemaining = 0f;
            }
            else if (pulled)
            {
                // 첫 레버가 켜진 순간에만 제한 시간을 시작한다. 이후 레버 입력은 같은 창을 공유한다.
                if (_leverWindowRemaining <= 0f)
                {
                    _leverWindowRemaining = _leverActivationWindow;
                }
            }

            if (!_isLeverPulled && !_isMoving && !_hasArrived)
            {
                SetDoorClosed(false);
            }
        }

        /// <summary> 버튼 홀드 또는 분신 재연으로 엘리베이터 이동을 시작한다 </summary>
        public bool TryStartJourney()
        {
            if (!CanStart)
            {
                return false;
            }

            // 엘리베이터가 움직이기 시작하면 화면을 흔들어준다.
            TriggerShake();

            SetDoorClosed(true);
            _isMoving = true;
            _sfxEmitter?.PlayActivate();
            _sfxEmitter?.PlayLoop();
            return true;
        }

        // IRewindable

        public void RecordTick()
        {
            _rewindBuffer.Push(new ElevatorTick(
                _isLeverPulled,
                _isMoving,
                _hasArrived,
                _isDoorClosed,
                _platform.position
            ));
        }

        public void OnRewindStart()
        {
            _sfxEmitter?.StopLoop();
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out ElevatorTick tick))
            {
                ApplyTick(tick);
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out ElevatorTick tick))
            {
                ApplyTick(tick);
            }
            _rewindBuffer.Clear();

            if (_isMoving)
            {
                _sfxEmitter?.PlayLoop();
            }
        }

        private bool CanMoveForward()
        {
            if (!_isMoving)
            {
                return false;
            }
            return (RewindManager.Instance == null) || !RewindManager.Instance.IsRewinding;
        }

        private void UpdateLeverActivationWindow()
        {
            if ((_leverWindowRemaining <= 0f) || _isLeverPulled || (_pulledLevers.Count == 0))
            {
                return;
            }

            if ((RewindManager.Instance != null) && RewindManager.Instance.IsRewinding)
            {
                return;
            }

            _leverWindowRemaining -= Time.deltaTime;
            if (_leverWindowRemaining > 0f)
            {
                return;
            }

            LeverInteractive[] timedOutLevers = new LeverInteractive[_pulledLevers.Count];
            _pulledLevers.CopyTo(timedOutLevers);
            _pulledLevers.Clear();
            _isLeverPulled = false;
            _leverWindowRemaining = 0f;

            foreach (LeverInteractive lever in timedOutLevers)
            {
                lever?.ResetByElevatorTimeout();
            }

            if (!_isMoving && !_hasArrived)
            {
                SetDoorClosed(false);
            }
        }

        private bool HasValidRoute()
        {
            return (_startPoint != null) && (_endPoint != null) && (_moveSpeed > 0f);
        }

        private void MovePlatform(float deltaTime)
        {
            if (!HasValidRoute())
            {
                _isMoving = false;
                return;
            }

            Vector3 currentPosition = _platform.position;
            Vector3 nextPosition = Vector3.MoveTowards(
                currentPosition,
                _endPoint.position,
                _moveSpeed * deltaTime
            );

            MovePlatformPosition(nextPosition);

            if ((_endPoint.position - nextPosition).sqrMagnitude > ARRIVAL_DISTANCE_SQR)
            {
                return;
            }

            // 엘리베이터가 도착하면 화면을 흔들어준다.
            TriggerShake();

            SetPlatformPosition(_endPoint.position);
            _isMoving   = false;
            _hasArrived = true;
            _sfxEmitter?.StopLoop();
            _sfxEmitter?.PlayDeactivate();
            SetDoorClosed(false); // 도착하면 문을 열어 플레이어가 내릴 수 있게 한다
        }

        private void ApplyTick(ElevatorTick tick)
        {
            _isLeverPulled = tick.LeverPulled;
            _isMoving      = tick.IsMoving;
            _hasArrived    = tick.HasArrived;
            SetDoorClosed(tick.IsDoorClosed);
            SetPlatformPosition(tick.Position);
        }

        private void SetDoorClosed(bool closed)
        {
            _isDoorClosed = closed;

            if (_closedDoor != null)
            {
                _closedDoor.SetActive(closed);
            }
            if (_openDoor != null)
            {
                _openDoor.SetActive(!closed);
            }
        }

        private void SetPlatformPosition(Vector3 position)
        {
            if (_platformRigidbody != null)
            {
                _platformRigidbody.position = position;
                return;
            }
            _platform.position = position;
        }

        private void MovePlatformPosition(Vector3 position)
        {
            if (_platformRigidbody != null)
            {
                _platformRigidbody.MovePosition(position);
                return;
            }
            _platform.position = position;
        }

        private void OnDrawGizmosSelected()
        {
            if ((_startPoint == null) || (_endPoint == null))
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_startPoint.position, _endPoint.position);
            Gizmos.DrawWireSphere(_startPoint.position, 0.2f);
            Gizmos.DrawWireSphere(_endPoint.position, 0.2f);
        }

        // 충돌이나 폭발 시 이 메서드를 호출하세요
        public void TriggerShake()
        {
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse();
            }
        }
    }
}
