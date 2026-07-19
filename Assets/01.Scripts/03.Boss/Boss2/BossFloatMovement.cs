// System
using System.Collections;

// Unity
using UnityEngine;

// 부유체 보스(Boss2, 3~4페이즈)의 자유 이동 - 스폰 지점 주변 랜덤 지점을 골라 SmoothDamp로 감속 이동하다 도착하면 잠시 멈추고,
// 그 동안에도 상하/좌우로 둥실둥실 흔들린다. 플레이어 위치와는 무관하게 동작하며, _maxHeightAnchor 위로는 올라가지 않는다
// TODO: 리와인드 타임라인 미연동 - 이동 패턴이 확정되면 IRewindable 구현 + Register/Unregister 추가 (배회 랜덤도 결정 로그 필요)
public class BossFloatMovement : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("참조")]
    [SerializeField] private Transform _target; // 배회 중심(_origin)이 느슨하게 따라갈 대상(플레이어) - 미연결 시 스폰 지점에 고정

    [Header("데이터")]
    [SerializeField] private Boss2DataSO _dataSo;

    [Header("아트")]
    [SerializeField] private float _artFacingSign = 1f; // 원본 아트가 오른쪽을 보고 있으면 1, 왼쪽이면 -1

    [Header("높이 제한")]
    [SerializeField] private Transform _maxHeightAnchor; // 이 오브젝트 y + Boss2DataSO.MaxHeightMargin보다 위로 못 올라간다 (미연결 시 제한 없음)

    private Rigidbody2D _rb;
    private Vector2     _origin;   // 배회 반경의 중심 - Start 시점 위치
    private Vector2     _waypoint; // 현재 목표 지점
    private Vector2     _velocity; // SmoothDamp 내부 속도 상태
    private float       _baseX;    // 배회로 이동하는 수평 기준선 - 흔들림의 중심
    private float       _baseY;    // 배회로 이동하는 수직 기준선 - 흔들림의 중심
    private float       _baseZ;    // Rigidbody2D 미보유 시 transform.position에 유지할 z값(정렬 순서 등)
    private float       _elapsed;  // 사인파 위상 계산용 경과 시간(초)
    private Coroutine   _roamLoop;

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

        _origin   = origin;
        _baseX    = origin.x;
        _baseY    = origin.y;
        _baseZ    = transform.position.z;
        _waypoint = origin;

        if (_dataSo != null)
        {
            _roamLoop = StartCoroutine(CoRoamLoop());
        }
    }

    private void OnDestroy()
    {
        if (_roamLoop != null)
        {
            StopCoroutine(_roamLoop);
            _roamLoop = null;
        }
    }

    private void FixedUpdate()
    {
        if (_dataSo == null)
        {
            return;
        }

        _elapsed += Time.fixedDeltaTime;
        FollowTarget();
        MoveTowardWaypoint();

        float offsetY = Mathf.Sin(_elapsed * PeriodToAngularSpeed(_dataSo.VerticalPeriod)) * _dataSo.VerticalAmplitude;

        float offsetX = 0f;
        if (_dataSo.HorizontalAmplitude > 0f)
        {
            offsetX = Mathf.Sin(_elapsed * PeriodToAngularSpeed(_dataSo.HorizontalPeriod)) * _dataSo.HorizontalAmplitude;
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
    *                Methods
    ****************************************/

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

    // _maxHeightAnchor가 있으면 그 y + MaxHeightMargin을 상한으로 클램프한다 (미연결 시 무제한)
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
