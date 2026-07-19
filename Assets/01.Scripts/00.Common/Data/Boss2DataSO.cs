// Unity
using UnityEngine;

// 부유 보스(Boss2, 3~4페이즈) 밸런싱 데이터 - 에셋: 08.Data/Boss2/Boss2DB.asset
// GameDB(Minsung.Common.Data)의 GameDatabaseSO에는 연결하지 않는다 - 컴포넌트 인스펙터에 직접 참조를 드래그해서 연결
[CreateAssetMenu(fileName = "Boss2DB", menuName = "TheLastRewind/Boss2/Boss2DB")]
public class Boss2DataSO : ScriptableObject
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("자유 이동 (배회)")]
    [SerializeField] private float _moveSpeed          = 1.5f; // 목표 속도 상한(유닛/초) - SmoothDamp의 maxSpeed
    [SerializeField] private float _moveSmoothTime      = 0.8f; // 목표 속도에 도달하는 데 걸리는 대략적인 시간(초) - 클수록 더 부드럽고 느긋하게 가속/감속
    [SerializeField] private float _roamRadius          = 4f;   // 스폰 지점 기준 배회 반경(유닛)
    [SerializeField] private float _roamArriveThreshold = 0.1f; // 목표 지점 도착 판정 거리(유닛)
    [SerializeField] private float _roamWaitMin         = 1f;   // 도착 후 대기 시간 최소(초)
    [SerializeField] private float _roamWaitMax         = 3f;   // 도착 후 대기 시간 최대(초)
    [SerializeField] private float _maxHeightMargin     = 1f;   // _maxHeightAnchor(BossFloatMovement) 기준 오브젝트 위로 허용하는 여유 높이(유닛)

    [Header("플레이어 추적")]
    [SerializeField] private float _followSpeed = 0.6f; // 배회 중심(_origin)이 플레이어를 따라가는 속도(유닛/초) - 실제 이동 속도(MoveSpeed)보다 훨씬 느리게

    [Header("체력")]
    [SerializeField] private float _maxHealth = 5000f; // 보스 최대 체력 TODO: 밸런싱/페이즈 확정 전 임시값

    [Header("상하 흔들림")]
    [SerializeField] private float _verticalAmplitude = 0.3f; // 상하 왕복 폭(유닛)
    [SerializeField] private float _verticalPeriod     = 2f;   // 상하 왕복 1회 주기(초)

    [Header("좌우 흔들림 (Amplitude 0이면 비활성)")]
    [SerializeField] private float _horizontalAmplitude = 0.15f; // 좌우 왕복 폭(유닛)
    [SerializeField] private float _horizontalPeriod     = 3f;    // 좌우 왕복 1회 주기(초)

    /****************************************
    *              Properties
    ****************************************/

    public float MoveSpeed          => _moveSpeed;
    public float MoveSmoothTime      => _moveSmoothTime;
    public float RoamRadius          => _roamRadius;
    public float RoamArriveThreshold => _roamArriveThreshold;
    public float RoamWaitMin         => _roamWaitMin;
    public float RoamWaitMax         => _roamWaitMax;
    public float MaxHeightMargin     => _maxHeightMargin;

    public float FollowSpeed => _followSpeed;
    public float MaxHealth   => _maxHealth;

    public float VerticalAmplitude   => _verticalAmplitude;
    public float VerticalPeriod      => _verticalPeriod;
    public float HorizontalAmplitude => _horizontalAmplitude;
    public float HorizontalPeriod    => _horizontalPeriod;
}
