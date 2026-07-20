// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.TimeSystem;

// 부유체 보스(Boss2, 3~4페이즈)의 이동 - 스폰 지점 주변을 배회하며 플레이어를 느슨하게 추적하고,
// 주기적으로 플레이어를 향해 빠르게 돌진하는 몸통박치기를 시도한다 (AttackHitBox 활성화 - DamageHazard가 판정)
// 리와인드: 매 틱 위치만 기록/복원한다 (배회/돌진 랜덤은 결정 로그가 없어 되감기 종료 시 그 자리에서 새로 시작)
public class BossFloatMovement : MonoBehaviour, IRewindable
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("참조")]
    [SerializeField] private Transform   _target;      // 배회 중심(_origin)이 느슨하게 따라갈 대상(플레이어) - 미연결 시 스폰 지점에 고정
    [SerializeField] private GameObject  _attackHitBox; // 돌진 중 활성화할 판정(AttackHitBox 자식, DamageHazard 보유) - 미연결 시 판정 없이 이동만

    [Header("데이터")]
    [SerializeField] private Boss2DataSO _dataSo;

    [Header("아트")]
    [SerializeField] private float _artFacingSign = 1f; // 원본 아트가 오른쪽을 보고 있으면 1, 왼쪽이면 -1

    [Header("높이 제한")]
    [SerializeField] private Transform _maxHeightAnchor; // 이 오브젝트 y + Boss2DataSO.MaxHeightMargin보다 위로 못 올라간다 (미연결 시 제한 없음)

    private Rigidbody2D _rb;
    private Vector2     _spawnOrigin;   // Start 시점 스폰 위치(불변) - _origin과 달리 FollowTarget/돌진으로 바뀌지 않는다. 3페이즈 재시작 복귀 지점
    private Vector2     _origin;        // 배회 반경의 중심 - Start 시점 위치
    private Vector2     _waypoint;      // 현재 목표 지점
    private Vector2     _velocity;      // SmoothDamp 내부 속도 상태
    private float       _baseX;         // 배회/돌진으로 이동하는 수평 기준선 - 흔들림의 중심
    private float       _baseY;         // 배회/돌진으로 이동하는 수직 기준선 - 흔들림의 중심
    private float       _baseZ;         // Rigidbody2D 미보유 시 transform.position에 유지할 z값(정렬 순서 등)
    private float       _elapsed;       // 사인파 위상 계산용 경과 시간(초)
    private bool         _isCharging;   // 몸통박치기 돌진 중 - true면 배회/추적/흔들림을 멈추고 직선 돌진만 수행
    private Vector2      _chargeTarget; // 돌진 시작 시점에 스냅샷한 목표 지점 (도중 방향을 바꾸지 않는다)
    private bool         _isRewinding;  // 되감기 중 - true면 배회/추적/돌진 로직을 멈추고 기록된 위치만 따른다
    private Coroutine    _roamLoop;
    private Coroutine    _chargeLoop;
    private RingBuffer<Vector2> _rewindBuffer; // 틱마다 최종 위치(흔들림 포함) 기록

    /****************************************
    *              Unity Event
    ****************************************/

    private void Awake()
    {
        TryGetComponent(out _rb);
        if (_rb != null)
        {
            _rb.bodyType      = RigidbodyType2D.Kinematic;                  // 부유체는 자체 힘으로만 움직인다
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;       // FixedUpdate 틱 사이를 렌더 프레임에 맞춰 보간 - 없으면 계단식으로 끊겨 보인다
        }

        if (_attackHitBox != null)
        {
            _attackHitBox.SetActive(false); // 평소 비활성 - 돌진 중에만 켠다
        }
    }

    private void Start()
    {
        Vector2 origin;
        if (_rb != null)
        {
            origin = _rb.position;
        }
        else
        {
            origin = transform.position;
        }

        _spawnOrigin = origin;
        _origin   = origin;
        _baseX    = origin.x;
        _baseY    = origin.y;
        _baseZ    = transform.position.z;
        _waypoint = origin;

        if (_dataSo != null)
        {
            _roamLoop      = StartCoroutine(CoRoamLoop());
            _chargeLoop    = StartCoroutine(CoChargeLoop());
            _rewindBuffer  = new RingBuffer<Vector2>(RewindManager.TickCapacity);
            RewindManager.Instance?.Register(this);
        }
    }

    private void OnDestroy()
    {
        StopMovementLoops();
        RewindManager.Instance?.Unregister(this);
    }

    private void FixedUpdate()
    {
        if ((_dataSo == null) || (_isRewinding))
        {
            return;
        }

        _elapsed += Time.fixedDeltaTime;

        float offsetX = 0f;
        float offsetY = 0f;

        if (_isCharging)
        {
            UpdateCharge();
        }
        else
        {
            FollowTarget();
            MoveTowardWaypoint();

            offsetY = Mathf.Sin(_elapsed * PeriodToAngularSpeed(_dataSo.VerticalPeriod)) * _dataSo.VerticalAmplitude;
            if (_dataSo.HorizontalAmplitude > 0f)
            {
                offsetX = Mathf.Sin(_elapsed * PeriodToAngularSpeed(_dataSo.HorizontalPeriod)) * _dataSo.HorizontalAmplitude;
            }
        }

        Vector2 targetPosition = new Vector2(_baseX + offsetX, _baseY + offsetY);
        ClampHeightCeiling(ref targetPosition);

        if (_rb != null)
        {
            _rb.MovePosition(targetPosition);
        }
        else
        {
            transform.position = new Vector3(targetPosition.x, targetPosition.y, _baseZ);
        }
    }

    /****************************************
    *            IRewindable
    ****************************************/

    // 흔들림까지 포함한 최종 위치를 그대로 기록한다 - 배회/돌진 결정 로그는 아직 없어 위치만 되감는다
    public void RecordTick()
    {
        Vector2 pos;
        if (_rb != null)
        {
            pos = _rb.position;
        }
        else
        {
            pos = transform.position;
        }
        _rewindBuffer.Push(pos);
    }

    public void OnRewindStart()
    {
        _isRewinding = true;
        StopMovementLoops();
    }

    public void ApplyRewindTick(int orderedIndex)
    {
        if (_rewindBuffer.TryGetOrdered(orderedIndex, out Vector2 pos))
        {
            ApplyPosition(pos);
        }
    }

    // 되감기 종료 - 복원된 위치를 새 배회 기준으로 삼고 코루틴을 그 자리에서 재시작한다
    public void OnRewindEnd(int orderedIndex)
    {
        if (_rewindBuffer.TryGetOrdered(orderedIndex, out Vector2 pos))
        {
            ApplyPosition(pos);
            _origin   = pos;
            _waypoint = pos;
            _baseX    = pos.x;
            _baseY    = pos.y;
        }
        _rewindBuffer.Clear();

        _isRewinding = false;
        _roamLoop     = StartCoroutine(CoRoamLoop());
        _chargeLoop   = StartCoroutine(CoChargeLoop());
    }

    /****************************************
    *                Methods
    ****************************************/

    // 3페이즈 재시작(Boss2BrandController - 낙인 7스택 즉사 후) - 스폰 지점으로 순간이동하고 배회/돌진을 그 자리에서 새로 시작한다
    public void ResetToSpawn()
    {
        StopMovementLoops();
        ApplyPosition(_spawnOrigin);
        _origin   = _spawnOrigin;
        _waypoint = _spawnOrigin;
        _baseX    = _spawnOrigin.x;
        _baseY    = _spawnOrigin.y;

        if (_dataSo != null)
        {
            _roamLoop   = StartCoroutine(CoRoamLoop());
            _chargeLoop = StartCoroutine(CoChargeLoop());
        }
    }

    // 배회 중심을 플레이어 쪽으로 FollowSpeed만큼 느리게 옮긴다 - 실제 이동(MoveTowardWaypoint)보다 훨씬 느려서
    // "쫓아가되 그 자리에 종속되지는 않는" 느낌을 낸다. 다음 웨이포인트부터 이 갱신된 중심 기준으로 뽑힌다
    private void FollowTarget()
    {
        if (_target == null)
        {
            return;
        }

        _origin = Vector2.MoveTowards(_origin, _target.position, _dataSo.FollowSpeed * Time.fixedDeltaTime);
    }

    // 현재 기준선을 목표 지점 쪽으로 SmoothDamp 이동시킨다 (흔들림과는 별개로 누적) - 등속 이동 대신
    // 감속하며 도착해 정지/전환 시 순간적으로 속도가 뚝 끊기지 않는다
    private void MoveTowardWaypoint()
    {
        Vector2 current = new Vector2(_baseX, _baseY);
        float   dx      = _waypoint.x - current.x;
        FaceTo(dx);

        Vector2 next = Vector2.SmoothDamp(current, _waypoint, ref _velocity,
            _dataSo.MoveSmoothTime, _dataSo.MoveSpeed, Time.fixedDeltaTime);
        _baseX = next.x;
        _baseY = next.y;
    }

    // 배회 루프: 반경 안 랜덤 지점으로 이동 -> 도착 대기 -> 반복. 플레이어 위치를 참조하지 않는다
    private IEnumerator CoRoamLoop()
    {
        while (true)
        {
            Vector2 candidate = _origin + (Random.insideUnitCircle * _dataSo.RoamRadius);
            ClampHeightCeiling(ref candidate);
            _waypoint = candidate;

            while (Vector2.Distance(new Vector2(_baseX, _baseY), _waypoint) > _dataSo.RoamArriveThreshold)
            {
                yield return null;
            }

            float waitTime = Random.Range(_dataSo.RoamWaitMin, _dataSo.RoamWaitMax);
            yield return new WaitForSeconds(waitTime);
        }
    }

    // 돌진 루프: 쿨다운마다 목표가 ChargeRange 안에 있으면 몸통박치기 시도
    private IEnumerator CoChargeLoop()
    {
        WaitForSeconds waitCooldown = new WaitForSeconds(_dataSo.ChargeCooldown);

        while (true)
        {
            yield return waitCooldown;

            if (_target == null)
            {
                continue;
            }

            float dist = Vector2.Distance(new Vector2(_baseX, _baseY), _target.position);
            if (dist > _dataSo.ChargeRange)
            {
                continue;
            }

            yield return CoBodySlam();
        }
    }

    // 목표 위치를 스냅샷해 방향을 고정한 뒤(예고 정지 -> 직선 돌진), 도달하거나 최대 시간을 넘기면 종료
    // 돌진 중에는 AttackHitBox를 켜서 DamageHazard 판정이 들어가게 한다
    private IEnumerator CoBodySlam()
    {
        _chargeTarget = _target.position;
        FaceTo(_chargeTarget.x - _baseX);

        yield return new WaitForSeconds(_dataSo.ChargeTelegraphTime); // 예고 - 제자리에서 잠깐 멈춤

        _isCharging = true;
        if (_attackHitBox != null)
        {
            _attackHitBox.SetActive(true);
        }

        float elapsed = 0f;
        while ((elapsed < _dataSo.ChargeDuration) &&
            (Vector2.Distance(new Vector2(_baseX, _baseY), _chargeTarget) > _dataSo.ChargeArriveThreshold))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_attackHitBox != null)
        {
            _attackHitBox.SetActive(false);
        }
        _isCharging = false;

        // 돌진이 끝난 위치를 새 배회 중심으로 - 원래 자리로 순간이동하지 않는다
        _origin   = new Vector2(_baseX, _baseY);
        _waypoint = _origin;
    }

    // 돌진 중 매 틱 목표 스냅샷 지점으로 직선 이동 (ChargeSpeed, 등속 - 회피 여지를 주기 위해 감속 없이 일정 속도)
    private void UpdateCharge()
    {
        Vector2 current = new Vector2(_baseX, _baseY);
        Vector2 next = Vector2.MoveTowards(current, _chargeTarget, _dataSo.ChargeSpeed * Time.fixedDeltaTime);
        _baseX = next.x;
        _baseY = next.y;
    }

    // 배회/돌진 코루틴을 정지하고 진행 중이던 돌진 판정을 끈다 (되감기 시작/파괴 공용)
    private void StopMovementLoops()
    {
        if (_roamLoop != null)
        {
            StopCoroutine(_roamLoop);
            _roamLoop = null;
        }
        if (_chargeLoop != null)
        {
            StopCoroutine(_chargeLoop);
            _chargeLoop = null;
        }
        if (_attackHitBox != null)
        {
            _attackHitBox.SetActive(false);
        }
        _isCharging = false;
    }

    // 기록된 위치를 그대로 적용 (되감기 스크럽용 - 보간 없이 즉시 이동)
    private void ApplyPosition(Vector2 pos)
    {
        if (_rb != null)
        {
            _rb.position = pos;
        }
        else
        {
            transform.position = new Vector3(pos.x, pos.y, _baseZ);
        }
    }

    // _maxHeightAnchor가 있으면 그 y + MaxHeightMargin을 상한으로 클램프한다 (미연결 시 제한 없음)
    private void ClampHeightCeiling(ref Vector2 point)
    {
        if (_maxHeightAnchor == null)
        {
            return;
        }

        float ceiling = _maxHeightAnchor.position.y + _dataSo.MaxHeightMargin;
        if (point.y > ceiling)
        {
            point.y = ceiling;
        }
    }

    // 바라보는 방향으로 스케일 반전
    private void FaceTo(float dx)
    {
        if (Mathf.Approximately(dx, 0f))
        {
            return;
        }

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * Mathf.Sign(dx) * _artFacingSign;
        transform.localScale = s;
    }

    // 주기(초) -> 사인 함수용 각속도(라디안/초). 0 이하 주기는 흔들림 없음으로 취급
    private static float PeriodToAngularSpeed(float period)
    {
        if (period > 0f)
        {
            return 2f * Mathf.PI / period;
        }
        return 0f;
    }
}
