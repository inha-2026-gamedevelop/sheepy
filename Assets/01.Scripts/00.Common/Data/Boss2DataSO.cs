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
    [SerializeField] private float _followSpeed = 1f; // 배회 중심(_origin)이 플레이어를 따라가는 속도(유닛/초) - 실제 이동 속도(MoveSpeed)보단 느리게

    [Header("몸통박치기 (돌진 공격)")]
    [SerializeField] private float _chargeCooldown        = 6f;   // 재사용 대기시간(초)
    [SerializeField] private float _chargeRange           = 6f;   // 목표가 이 거리 안에 있어야 돌진을 시도(유닛)
    [SerializeField] private float _chargeTelegraphTime   = 0.4f; // 돌진 전 예고 정지 시간(초)
    [SerializeField] private float _chargeSpeed           = 9f;   // 돌진 속도(유닛/초) - 평소 이동(MoveSpeed)보다 훨씬 빠르게
    [SerializeField] private float _chargeDuration        = 1.2f; // 돌진 최대 지속시간(초) - 목표에 못 미쳐도 이 시간 지나면 종료
    [SerializeField] private float _chargeArriveThreshold = 0.2f; // 돌진 도달 판정 거리(유닛)

    [Header("체력 / 페이즈")]
    [SerializeField] private float _maxHealth  = 5000f; // 보스 최대 체력 TODO: 밸런싱/페이즈 확정 전 임시값
    [SerializeField] private int   _phaseCount = 2;      // 이 보스가 담당하는 페이즈 수 - 피통을 이 값으로 균등 분할해 페이즈 하한을 계산한다 (3~4페이즈 = 2)

    [Header("낙뢰 (예고 후 즉발 강타, 플레이어 주변 낙하)")]
    [SerializeField] private float _lightningInterval        = 4f;    // 발생 간격(초)
    [SerializeField] private float _lightningTelegraphTime   = 0.8f;  // 예고 장판 노출 시간(초)
    [SerializeField] private float _lightningTelegraphHeight = 0.15f; // 예고 장판 세로 두께(지면 기준)
    [SerializeField] private float _lightningActiveTime      = 0.15f; // 강타 판정 유지 시간(초)
    [SerializeField] private float _lightningStunDuration    = 0.5f;  // 피격 시 이동 불가 시간(초)
    [SerializeField] private int   _lightningDamageHalves    = 2;     // 피격 시 하트 차감(반칸 단위)
    [SerializeField] private float _lightningWidth           = 0.5f;  // 강타/예고 공통 가로 폭
    [SerializeField] private float _lightningHeight          = 9f;    // 강타 세로 길이
    [SerializeField] private float _lightningGroundEmbed     = 0.4f;  // 강타 스프라이트 하단 여백을 가리기 위해 지면 아래로 밀어넣는 깊이
    [SerializeField] private float _lightningPlayerRadius    = 3f;    // 낙하 지점을 플레이어 x 기준 이 반경 안에서 랜덤 결정

    [SerializeField] private Color _lightningColor          = new Color(1f, 1f, 1f); // 흰색 - 스프라이트 원색(보라)을 그대로 살린다. 곱색이라 틴트를 넣으면 색이 틀어진다
    [SerializeField] private Color _lightningTelegraphColor = new Color(1f, 0.9f, 0.2f, 0.35f);

    [SerializeField] private Sprite[] _lightningStrikeSprites;         // 강타 중 순환할 프레임 (비우면 단색 사각형 폴백)
    [SerializeField] private float    _lightningFrameInterval = 0.02f; // 프레임 전환 간격(초)

    [SerializeField] private float _lightningParticleSize = 0.08f;
    [SerializeField] private Color[] _lightningParticleColors = new Color[]
    {
        new Color(0.88f, 0.67f, 1f),
        new Color(0.78f, 0.49f, 1f),
        new Color(0.61f, 0.31f, 0.87f),
        new Color(0.48f, 0.17f, 0.75f),
    };

    [Header("강타 (예고 파티클 후 즉발 폭발 강타)")]
    [SerializeField] private float _waveInterval          = 3f;    // 발생 간격(초)
    [SerializeField] private float _waveWidth              = 2.5f;  // 강타/예고 공통 판정 폭
    [SerializeField] private float _waveHeight             = 2.5f;  // 강타/예고 공통 판정 높이
    [SerializeField] private float _waveGroundEmbed        = 0.75f; // 강타 y좌표를 지면 아래로 밀어넣는 값
    [SerializeField] private float _waveTelegraphTime      = 1f;    // 예고 파티클 표시 시간(초)
    [SerializeField] private float _waveActiveTime         = 0.3f;  // 강타 연출 유지 시간(초)
    [SerializeField] private float _waveFrameInterval      = 0.0333f; // 프레임 전환 간격(초)
    [SerializeField] private int   _waveActiveFrameCount   = 5;     // 앞 N프레임만 피해 판정
    [SerializeField] private int   _waveDamageHalves       = 2;     // 피격 시 하트 차감(반칸 단위)

    [SerializeField] private Color _waveColor = new Color(1f, 1f, 1f);

    [SerializeField] private Sprite[] _waveStrikeSprites; // 강타 중 순환할 폭발 프레임 (비우면 단색 사각형 폴백)

    [SerializeField] private float   _waveParticleSize = 0.1f;
    [SerializeField] private Color[] _waveParticleColors = new Color[]
    {
        new Color(0.88f, 0.67f, 1f),
        new Color(0.78f, 0.49f, 1f),
        new Color(0.61f, 0.31f, 0.87f),
        new Color(0.48f, 0.17f, 0.75f),
    };

    [Header("레이저 (아레나를 가로지르는 판정)")]
    [SerializeField] private float _laserInterval         = 5f;    // 발사 간격(초)
    [SerializeField] private float _laserWarningTime      = 1.5f;  // 경고(빨간 깜빡임) 시간(초)
    [SerializeField] private float _laserBlinkInterval    = 0.1f;  // 깜빡임 주기(초)
    [SerializeField] private float _laserActiveTime       = 1f;    // 레이저 지속(초)
    [SerializeField] private float _laserRetractTime      = 0.25f; // 회수 연출 시간(초)
    [SerializeField] private float _laserThickness        = 0.5f;  // 레이저 두께
    [SerializeField] private float _laserWarningThickness = 0.05f; // 경고 실선 두께
    [SerializeField] private float _laserMaxHeight        = 6f;    // 시작/도착 지점 y 랜덤 상한(지면 기준)
    [SerializeField] private int   _laserDamageHalves     = 2;     // 피격 시 하트 차감(반칸 단위)

    [SerializeField] private Color _laserWarningColor = new Color(1f, 0.05f, 0.05f, 0.6f);
    [SerializeField] private Color _laserColor        = new Color(0.95f, 0f, 0.02f);

    [SerializeField] private float   _laserFlowParticleSize = 0.12f;
    [SerializeField] private float   _laserFlowSpeed        = 14f;
    [SerializeField] private float   _laserFlowRate         = 80f;
    [SerializeField] private Color[] _laserFlowColors = new Color[]
    {
        new Color(1f, 0.15f, 0.15f),
        new Color(0.85f, 0f, 0.05f),
        new Color(1f, 0.35f, 0.05f),
        new Color(0.55f, 0f, 0.12f),
    };

    [Header("감정 - 공통 (반사/낙뢰 배율)")]
    [SerializeField] private int   _reflectHalves         = 2;   // 감정 반사 시 공격자가 입는 피해(반칸 단위)
    [SerializeField] private float _emotionInterval       = 8f;  // 자동 감정 전환 주기(초)
    [SerializeField] private float _lightningRatePinkMult = 2f;  // 핑크 감정: 낙뢰 발생 비율 x2
    [SerializeField] private float _lightningRateBlueMult = 0.5f; // 파랑 감정: 낙뢰 발생 비율 /2

    [Header("감정 - 화남(혼란) / 파랑(하트 픽업)")]
    [SerializeField] private float _confusionInterval = 10f;  // 키반전 발동 주기(초)
    [SerializeField] private float _confusionDuration  = 1f;   // 키반전 지속 시간(초)
    [SerializeField] private float _heartPickupHeight  = 0.5f; // 픽업 배치 높이(지면 기준)

    [Header("상하 흔들림")]
    [SerializeField] private float _verticalAmplitude = 0.3f; // 상하 왕복 폭(유닛)
    [SerializeField] private float _verticalPeriod     = 2f;   // 상하 왕복 1회 주기(초)

    [Header("좌우 흔들림 (Amplitude 0이면 비활성)")]
    [SerializeField] private float _horizontalAmplitude = 0.15f; // 좌우 왕복 폭(유닛)
    [SerializeField] private float _horizontalPeriod     = 3f;    // 좌우 왕복 1회 주기(초)

    [Header("공간찢기 (4페이즈 즉사기)")]
    [SerializeField, Range(0.01f, 0.5f)] private float _spaceTearHealthPercent = 0.1f; // 이 체력 비율 첫 통과 시 1회 발동 + 동결
    [SerializeField] private float _spaceTearBannerTime    = 2f;   // 배너 예고 노출 시간(초)
    [SerializeField] private float _spaceTearTelegraphTime = 1.2f; // 5개 라인 예고 표시 시간(초)
    [SerializeField] private int   _spaceTearDashCount     = 5;    // 총 돌진 횟수(고정 4 + 플레이어 조준 1)
    [SerializeField] private float _spaceTearDashSpeed     = 26f;  // 고정 4라인 돌진 속도(유닛/초) - 결정타가 아닌 연출용이라 빠르게 유지
    [SerializeField] private float _spaceTearDashInterval  = 0.35f; // 돌진 사이 간격(초)
    [SerializeField] private float _spaceTearPlayerDashSpeed   = 11f; // 마지막 플레이어 조준 돌진 전용 속도(유닛/초) - 실제 파훼 대상이라 고정 라인보다 느리게(2026-07-21 피드백: 너무 빨라 반응 불가)
    [SerializeField] private float _spaceTearPlayerWarningTime = 0.6f; // 컬러 복귀(예고) 후 실제 돌진 시작까지 대기 시간(초) - 무적키 타이밍을 잡을 여유
    [SerializeField] private Vector2 _spaceTearHitboxSize  = new Vector2(1.6f, 1.6f); // 돌진 즉사 판정 크기(유닛)
    [SerializeField] private float   _spaceTearTelegraphThickness = 0.12f; // 예고선 두께(유닛)
    [SerializeField] private float   _spaceTearTelegraphBlink     = 0.12f; // 예고선 점멸 주기(초)
    [SerializeField] private Color   _spaceTearTelegraphColor = new Color(1f, 0.15f, 0.15f, 0.55f); // 예고선 색(경고 - 빨강 계열)

    [Header("낙인 (3페이즈 전용)")]
    [SerializeField] private float _brandInterval       = 10f; // 낙인 스택 부여 간격(초)
    [SerializeField] private int   _brandMaxStack       = 7;   // 즉사 임계 스택 수
    [SerializeField] private float _altarSpawnInterval  = 30f; // 제단 출현 간격(초)
    [SerializeField] private float _altarHoldDuration   = 3f;  // 낙인 초기화에 필요한 E키 홀드 시간(초)

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
    public int   PhaseCount  => _phaseCount;

    public float ChargeCooldown        => _chargeCooldown;
    public float ChargeRange           => _chargeRange;
    public float ChargeTelegraphTime   => _chargeTelegraphTime;
    public float ChargeSpeed           => _chargeSpeed;
    public float ChargeDuration        => _chargeDuration;
    public float ChargeArriveThreshold => _chargeArriveThreshold;

    public float VerticalAmplitude   => _verticalAmplitude;
    public float VerticalPeriod      => _verticalPeriod;
    public float HorizontalAmplitude => _horizontalAmplitude;
    public float HorizontalPeriod    => _horizontalPeriod;

    public int   ReflectHalves         => _reflectHalves;
    public float EmotionInterval       => _emotionInterval;
    public float LightningRatePinkMult => _lightningRatePinkMult;
    public float LightningRateBlueMult => _lightningRateBlueMult;

    public float ConfusionInterval => _confusionInterval;
    public float ConfusionDuration => _confusionDuration;
    public float HeartPickupHeight => _heartPickupHeight;

    public float LightningInterval        => _lightningInterval;
    public float LightningTelegraphTime   => _lightningTelegraphTime;
    public float LightningTelegraphHeight => _lightningTelegraphHeight;
    public float LightningActiveTime      => _lightningActiveTime;
    public float LightningStunDuration    => _lightningStunDuration;
    public int   LightningDamageHalves    => _lightningDamageHalves;
    public float LightningWidth           => _lightningWidth;
    public float LightningHeight          => _lightningHeight;
    public float LightningGroundEmbed     => _lightningGroundEmbed;
    public float LightningPlayerRadius    => _lightningPlayerRadius;
    public Color LightningColor           => _lightningColor;
    public Color LightningTelegraphColor  => _lightningTelegraphColor;

    public Sprite[] LightningStrikeSprites => _lightningStrikeSprites;
    public float    LightningFrameInterval => _lightningFrameInterval;

    public float   LightningParticleSize   => _lightningParticleSize;
    public Color[] LightningParticleColors => _lightningParticleColors;

    public float WaveInterval        => _waveInterval;
    public float WaveWidth           => _waveWidth;
    public float WaveHeight          => _waveHeight;
    public float WaveGroundEmbed     => _waveGroundEmbed;
    public float WaveTelegraphTime   => _waveTelegraphTime;
    public float WaveActiveTime      => _waveActiveTime;
    public float WaveFrameInterval   => _waveFrameInterval;
    public int   WaveActiveFrameCount => _waveActiveFrameCount;
    public int   WaveDamageHalves    => _waveDamageHalves;
    public Color WaveColor           => _waveColor;

    public Sprite[] WaveStrikeSprites   => _waveStrikeSprites;
    public float    WaveParticleSize    => _waveParticleSize;
    public Color[]  WaveParticleColors  => _waveParticleColors;

    public float LaserInterval         => _laserInterval;
    public float LaserWarningTime      => _laserWarningTime;
    public float LaserBlinkInterval    => _laserBlinkInterval;
    public float LaserActiveTime       => _laserActiveTime;
    public float LaserRetractTime      => _laserRetractTime;
    public float LaserThickness        => _laserThickness;
    public float LaserWarningThickness => _laserWarningThickness;
    public float LaserMaxHeight        => _laserMaxHeight;
    public int   LaserDamageHalves     => _laserDamageHalves;
    public Color LaserWarningColor     => _laserWarningColor;
    public Color LaserColor            => _laserColor;

    public float   LaserFlowParticleSize => _laserFlowParticleSize;
    public float   LaserFlowSpeed        => _laserFlowSpeed;
    public float   LaserFlowRate         => _laserFlowRate;
    public Color[] LaserFlowColors       => _laserFlowColors;

    public float   SpaceTearHealthPercent => _spaceTearHealthPercent;
    public float   SpaceTearBannerTime    => _spaceTearBannerTime;
    public float   SpaceTearTelegraphTime => _spaceTearTelegraphTime;
    public int     SpaceTearDashCount     => _spaceTearDashCount;
    public float   SpaceTearDashSpeed     => _spaceTearDashSpeed;
    public float   SpaceTearDashInterval  => _spaceTearDashInterval;
    public float   SpaceTearPlayerDashSpeed   => _spaceTearPlayerDashSpeed;
    public float   SpaceTearPlayerWarningTime => _spaceTearPlayerWarningTime;
    public Vector2 SpaceTearHitboxSize    => _spaceTearHitboxSize;
    public float   SpaceTearTelegraphThickness => _spaceTearTelegraphThickness;
    public float   SpaceTearTelegraphBlink     => _spaceTearTelegraphBlink;
    public Color   SpaceTearTelegraphColor     => _spaceTearTelegraphColor;

    public float BrandInterval      => _brandInterval;
    public int   BrandMaxStack      => _brandMaxStack;
    public float AltarSpawnInterval => _altarSpawnInterval;
    public float AltarHoldDuration  => _altarHoldDuration;
}
