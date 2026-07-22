// Unity
using UnityEngine;

namespace Minsung.Common.Data
{
    // 보스(Azathoth) 밸런싱 데이터 DB - 에셋: 08.Data/Boss/BossDB.asset (GameDB.Boss로 접근)
    // GIMMICK_LASER_COLOR_COUNT처럼 enum 구조에 묶인 값은 Constants.Combat에 남는다
    [CreateAssetMenu(fileName = "BossDB", menuName = "TheLastRewind/GameDB/BossDB")]
    public class BossDataSO : ScriptableObject
    {
        [Header("클론 설정")]
        [SerializeField] private float _cloneCrowdAvoidHorizontalRange = 0.75f;
        [SerializeField] private float _cloneCrowdAvoidVerticalRange   = 1.25f;

        /****************************************
        *                F
        ****************************************/

        [Header("공통")]
        [SerializeField] private float _totalHealth = 16000f; // 이 씬이 담당하는 페이즈 구간 총 피통 (BossController._finalPhaseIndex+1로 균등 분할)
        [SerializeField] private float _timeLimit   = 600f;   // 보스전 제한시간(초) - 초과 시 플레이어 즉사, 리와인드/슬로우 무관 실시간

        [SerializeField] private int _attackHalves      = 2; // 보스 공격 하트 차감(반칸 단위, 2 = 한 칸)
        [SerializeField] private int _cloneAttackHalves = 1; // 보스 분신 공격 하트 차감(반칸 단위, 1 = 반 칸)
        [SerializeField] private int _reflectHalves     = 2; // 감정 반사 시 공격자가 입는 피해(반칸 단위) TODO: 기획 확정

        [Header("본체 근거리 (2페이즈부터 등장)")]
        [SerializeField] private float _moveSpeed        = 2.5f;  // 본체 추격 이동 속도 TODO: 밸런싱
        [SerializeField] private float _attackRange      = 2f;    // 본체 근거리 공격 사거리 TODO: 밸런싱
        [SerializeField] private float _attackCooldown   = 1.5f;  // 본체 Combat 재사용 대기시간(초)
        [SerializeField] private float _castCooldown     = 1.5f;  // 본체 Casting 애니메이션 재사용 대기시간(초)
        [SerializeField] private float _attackActiveTime = 0.25f; // 공격 판정 유지 시간(초) - 애니메이션 이벤트 연결 전 임시
        [SerializeField] private Vector2 _combatHitboxSize   = new Vector2(1.738f, 0.959f); // Combat 9프레임 모션 전체 범위
        [SerializeField] private Vector2 _combatHitboxCenter = new Vector2(-0.082f, 0.16f); // 보스 루트 기준 모션 중심

        [Header("근접 유닛 공통 - 점프/회피 (본체·분신 공용, 개별 스탯 아님)")]
        [SerializeField] private float _jumpCooldown       = 4f;   // 최소 재사용 대기시간(초) TODO: 밸런싱
        [SerializeField] private float _jumpArcHeight      = 2f;   // 도약 최대 높이
        [SerializeField] private float _jumpLandActiveTime = 0.2f; // 착지 슬램 판정 유지 시간(초) - 각 유닛 AttackHalves 재사용
        [SerializeField] private float _stuckEscapeDelay    = 0.35f; // 지형에 막혀 수평 이동하지 못할 때 탈출 도약까지 대기 시간(초)

        [SerializeField] private float _dodgeCooldown     = 5f;   // 최소 재사용 대기시간(초) TODO: 밸런싱
        [SerializeField] private float _dodgeTriggerRange = 1.5f; // 이 거리보다 가까우면 후퇴 회피
        [SerializeField] private float _dodgeBackDistance = 3f;   // 후퇴 거리(유닛)
        [SerializeField] private float _dodgeDuration     = 0.3f; // 후퇴 이동 + 무적 지속시간(초)

        [Header("분신 (1페이즈, 2체)")]
        [SerializeField] private float _cloneHealth           = 8000f; // 분신 1체 피통 (2체 합 = 1페이즈 피통)
        [SerializeField] private int   _cloneCount            = 2;
        [SerializeField] private float _cloneAttackRange      = 1.5f;  // 근거리 공격 사거리 TODO: 밸런싱
        [SerializeField] private float _cloneAttackCooldown   = 1.5f;  // 분신 Combat 재사용 대기시간(초)
        [SerializeField] private float _cloneActionOffset     = 0.75f; // 두 번째 분신의 행동 루프 시작 지연(Combat 쿨타임의 절반)
        [SerializeField] private float _cloneAttackActiveTime = 0.2f;  // 공격 판정 유지 시간(초) - 애니메이션 이벤트 연결 전 임시
        [SerializeField] private float _cloneMoveSpeed        = 2.6f;  // 추격 이동 속도 - 플레이어 MoveSpeed(2)보다 느리게 잡아 도망 가능하게 함
        [SerializeField] private float _cloneEntranceLeapHeight = 2f;  // 등장 도약 최대 높이

        [Header("낙뢰 (전 페이즈 공통 패턴 - 예고 후 즉발 강타, 플레이어 주변 낙하)")]
        [SerializeField] private float _lightningInterval        = 4f;    // 기본 발생 간격(초)
        [SerializeField] private float _lightningTelegraphTime   = 0.8f;  // 예고 장판 노출 시간(초) TODO: 밸런싱/가독성 확정
        [SerializeField] private float _lightningTelegraphHeight = 0.15f; // 예고 장판 세로 두께(지면 기준) - 얇은 판 형태 전용 필드
        [SerializeField] private float _lightningActiveTime      = 0.15f; // 강타 판정 유지 시간(초) - 즉시 배치라 매우 짧게
        [SerializeField] private float _lightningStunDuration    = 0.5f;  // 피격 시 이동 불가 시간(초)
        [SerializeField] private int   _lightningDamageHalves    = 2;     // 피격 시 하트 차감(반칸 단위, 2 = 한 칸)
        [SerializeField] private float _lightningWidth           = 0.5f;  // 강타/예고 공통 가로 폭 - 같은 x 열을 가리켜야 하므로 두 연출이 공유
        [SerializeField] private float _lightningHeight          = 9f;   // 강타 세로 길이 - 카메라 orthoSize 5 기준 화면 상단을 넘어서도록 설정
        [SerializeField] private float _lightningGroundEmbed     = 0.4f;  // 강타 스프라이트 하단 여백/발광 감쇠를 가리기 위해 지면 아래로 밀어넣는 깊이(유닛)
        [SerializeField] private float _lightningPlayerRadius    = 3f;    // 낙하 지점을 플레이어 x 위치 기준 이 반경 안에서 랜덤 결정 TODO: 밸런싱
        [SerializeField] private float _lightningRatePinkMult    = 2f;    // 핑크 감정: 발생 비율 x2
        [SerializeField] private float _lightningRateBlueMult    = 0.5f;  // 파랑 감정: 발생 비율 /2

        [SerializeField] private Color _lightningColor          = new Color(1f, 0.95f, 0.4f);      // 강타 표시색 - 폴백 사각형용 기본값. 실제 스프라이트 연결 시 흰색을 권장(원색 유지)
        [SerializeField] private Color _lightningTelegraphColor = new Color(1f, 0.9f, 0.2f, 0.35f); // 예고 장판 색 (노란 반투명)

        [SerializeField] private Sprite[] _lightningStrikeSprites;         // 강타 중 순환할 크랙클 프레임 (비우면 단색 사각형 폴백)
        [SerializeField] private float    _lightningFrameInterval = 0.02f; // 크랙클 프레임 전환 간격(초)

        [SerializeField] private float _lightningParticleSize = 0.08f; // 낙뢰 지점 스파크 입자 크기
        [SerializeField] private Color[] _lightningParticleColors = new Color[] // 스파크 입자 색 - 보라 계열 4색 중 랜덤
        {
            new Color(0.88f, 0.67f, 1f),    // 연보라
            new Color(0.78f, 0.49f, 1f),    // 밝은 보라
            new Color(0.61f, 0.31f, 0.87f), // 중간 보라
            new Color(0.48f, 0.17f, 0.75f), // 진보라
        };

        [Header("사망 연출 (DeathBody+DeathLightFx+DeathCircleFx 순차 재생)")]
        [SerializeField] private float _deathLightDelay = 0.4f; // DeathBody 트리거 후 DeathLightFx를 켜기까지 대기(초) - DeathBody.anim의 patches_sprites-sheet0_21 프레임 등장 시점(0.4초)과 맞춤

        [SerializeField] private float   _deathCircleDelay          = 1.275f; // DeathBody 트리거 후 DeathCircleFx를 켜기까지 대기(초) - DeathLightFx의 8번째 프레임(0.875초) 등장 시점과 맞춤(DeathLightDelay + 0.875)
        [SerializeField] private Vector2 _deathCircleLaunchDirection = new Vector2(-1f, 1f); // 사출 방향(정규화해서 사용) - 2사분면(좌상단)
        [SerializeField] private float   _deathCircleLaunchSpeed    = 8f;   // 사출 단계 이동 속도(유닛/초) TODO: 밸런싱
        [SerializeField] private float   _deathCircleLaunchDuration = 0.3f; // 사출 단계 지속시간(초) TODO: 밸런싱
        [SerializeField] private float   _deathCircleFloatDuration  = 2f;   // 부유 단계 지속시간(초) TODO: 밸런싱
        [SerializeField] private float   _deathCircleFloatAmplitude = 0.15f; // 부유 단계 상하 흔들림 폭(유닛)
        [SerializeField] private float   _deathCircleFloatFrequency = 1.5f;  // 부유 단계 흔들림 주기(라디안/초)
        [SerializeField] private float   _deathCircleReturnSpeed    = 3f;   // 귀환 단계(CircleAim으로) 이동 속도(유닛/초) TODO: 밸런싱

        [Header("감정 - 화남(혼란) / 파랑(하트 픽업)")]
        [SerializeField] private float _confusionInterval = 10f;  // 키반전 발동 주기(초)
        [SerializeField] private float _confusionDuration = 1f;   // 키반전 지속 시간(초)
        [SerializeField] private float _heartPickupHeight = 0.5f; // 픽업 배치 높이(지면 기준)
        [SerializeField] private float _emotionInterval = 8f;     // 자동 감정 전환 주기(초) - 2페이즈부터, 3페이즈는 화남 고정으로 정지

        [Header("1페이즈 즉사 기믹 (레이저 색 순서 암기)")]
        [SerializeField] private int   _gimmickLaserCount      = 3;     // 발사 횟수(색 시퀀스 길이)
        [SerializeField] private float _gimmickSafeZoneAlpha   = 0.35f; // 안전구역 표시 반투명도
        [SerializeField] private float _gimmickTelegraphTime   = 3f;    // 안전구역 표시~레이저 발사까지(초)
        [SerializeField] private float _gimmickLaserActiveTime = 3f;  // 레이저 연출 지속(초)
        [SerializeField] private float _gimmickLaserHeight     = 6f;    // 전장 레이저/안전구역 세로 크기
        [SerializeField] private float _gimmickRefireDelay     = 5f;    // 예고 종료 후 실제 발사까지 대기(초)
        [SerializeField] private float _gimmickJudgeInterval   = 2f;    // 실전 발사 사이 대기(초) - 다음 색 안전구역으로 이동할 시간

        [Header("2페이즈 장풍 (예고 파티클 후 즉발 폭발 강타 - 낙뢰와 동일 구조)")]
        [SerializeField] private float _phase2WaveInterval      = 3f;    // 발사 간격(초)
        [SerializeField] private float _phase2WaveWidth         = 2.5f;  // 강타/예고 공통 판정 폭 - 폭발 비주얼에 맞춰 확대
        [SerializeField] private float _phase2WaveHeight        = 2.5f;  // 강타/예고 공통 판정 높이 - 폭발 비주얼에 맞춰 확대
        [SerializeField] private float _phase2WaveGroundEmbed   = 0.75f;  // 폭발 스프라이트 발광 여백 보정 - 강타 y좌표를 지면 아래로 밀어넣는 값 (낙뢰의 LightningGroundEmbed와 동일 용도)
        [SerializeField] private float _phase2WaveTelegraphTime = 1f;    // 예고 파티클 표시 시간(초)
        [SerializeField] private float _phase2WaveActiveTime    = 0.3f;  // 강타 연출 유지 시간(초) - 폭발 9프레임이 이 시간 안에 전부 순환
        [SerializeField] private float _phase2WaveFrameInterval = 0.0333f; // 폭발 프레임 전환 간격(초) - 9프레임 x 이 값 = ActiveTime
        [SerializeField] private int   _phase2WaveActiveFrameCount = 5;  // 앞 N프레임만 피해 판정 (나머지는 종료 연출, 무판정)

        [SerializeField] private Color _phase2WaveColor = new Color(1f, 1f, 1f); // 강타 표시색 - 폭발 스프라이트 원색 유지를 위해 흰색 권장

        [SerializeField] private Sprite[] _phase2WaveStrikeSprites; // 강타 중 순환할 폭발 프레임 (0~8, 비우면 단색 사각형 폴백)

        [SerializeField] private float   _phase2WaveParticleSize = 0.1f; // 예고 파티클 크기
        [SerializeField] private Color[] _phase2WaveParticleColors = new Color[] // 예고 파티클 색 - 보라 계열 4색 중 랜덤
        {
            new Color(0.88f, 0.67f, 1f),    // 연보라
            new Color(0.78f, 0.49f, 1f),    // 밝은 보라
            new Color(0.61f, 0.31f, 0.87f), // 중간 보라
            new Color(0.48f, 0.17f, 0.75f), // 진보라
        };

        [Header("3페이즈 가로지르는 레이저")]
        [SerializeField] private float _phase3LaserInterval         = 5f;    // 발사 간격(초)
        [SerializeField] private float _phase3LaserWarningTime      = 1.5f;  // 경고(빨간 깜빡임) 시간(초)
        [SerializeField] private float _phase3LaserBlinkInterval    = 0.1f;  // 깜빡임 주기(초)
        [SerializeField] private float _phase3LaserActiveTime       = 1f;    // 레이저 지속(초)
        [SerializeField] private float _phase3LaserRetractTime      = 0.25f; // 회수 연출 시간(초) - 두께가 이 시간 동안 0으로 좁아진다
        [SerializeField] private float _phase3LaserThickness        = 0.5f;  // 레이저 두께
        [SerializeField] private float _phase3LaserWarningThickness = 0.05f; // 경고 실선 두께 - 본 레이저보다 얇게
        [SerializeField] private float _phase3LaserMaxHeight        = 6f;    // 시작/도착 지점 y 랜덤 상한(지면 기준)

        [SerializeField] private Color _phase3LaserWarningColor = new Color(1f, 0.05f, 0.05f, 0.6f); // 경고 깜빡임 실선 색 - 더 빨갛게
        [SerializeField] private Color _phase3LaserColor        = new Color(0.95f, 0f, 0.02f);       // 레이저 발사색 - 더 빨갛게

        [SerializeField] private Material _phase3LaserMaterial; // 에너지빔 쉐이더 머테리얼 (Assets/12.Materials/Phase3LaserBeamMat) - 인스펙터에 직접 드래그해서 연결

        [SerializeField] private float   _phase3LaserFlowParticleSize = 0.12f; // 진행방향으로 흐르는 파티클 크기
        [SerializeField] private float   _phase3LaserFlowSpeed        = 14f;   // 파티클 흐름 속도(로컬 X, 유닛/초)
        [SerializeField] private float   _phase3LaserFlowRate         = 80f;   // 초당 방출 개수(밀도)
        [SerializeField] private Color[] _phase3LaserFlowColors = new Color[] // 흐르는 파티클 색 - 레이저 발사색과 같은 빨강 계열 4색
        {
            new Color(1f, 0.15f, 0.15f),  // 밝은 빨강
            new Color(0.85f, 0f, 0.05f),  // 진한 빨강
            new Color(1f, 0.35f, 0.05f),  // 주황 빛 빨강
            new Color(0.55f, 0f, 0.12f),  // 어두운 크림슨
        };

        /****************************************
        *              Properties
        ****************************************/

        public float TotalHealth => _totalHealth;
        public float TimeLimit   => _timeLimit;

        public int AttackHalves      => _attackHalves;
        public int CloneAttackHalves => _cloneAttackHalves;
        public int ReflectHalves     => _reflectHalves;

        public float MoveSpeed        => _moveSpeed;
        public float AttackRange      => _attackRange;
        public float AttackCooldown   => _attackCooldown;
        public float CastCooldown     => _castCooldown;
        public float AttackActiveTime => _attackActiveTime;
        public Vector2 CombatHitboxSize   => _combatHitboxSize;
        public Vector2 CombatHitboxCenter => _combatHitboxCenter;

        public float JumpCooldown      => _jumpCooldown;
        public float JumpArcHeight     => _jumpArcHeight;
        public float JumpLandActiveTime => _jumpLandActiveTime;
        public float StuckEscapeDelay  => _stuckEscapeDelay;

        public float DodgeCooldown     => _dodgeCooldown;
        public float DodgeTriggerRange => _dodgeTriggerRange;
        public float DodgeBackDistance => _dodgeBackDistance;
        public float DodgeDuration     => _dodgeDuration;

        public float CloneHealth           => _cloneHealth;
        public int   CloneCount            => _cloneCount;
        public float CloneAttackRange      => _cloneAttackRange;
        public float CloneAttackCooldown   => _cloneAttackCooldown;
        public float CloneActionOffset     => _cloneActionOffset;
        public float CloneAttackActiveTime => _cloneAttackActiveTime;
        public float CloneMoveSpeed        => _cloneMoveSpeed;
        public float CloneEntranceLeapHeight => _cloneEntranceLeapHeight;
        public float CloneCrowdAvoidHorizontalRange => _cloneCrowdAvoidHorizontalRange;
        public float CloneCrowdAvoidVerticalRange   => _cloneCrowdAvoidVerticalRange;

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
        public float LightningRatePinkMult    => _lightningRatePinkMult;
        public float LightningRateBlueMult    => _lightningRateBlueMult;
        public Color LightningColor           => _lightningColor;
        public Color LightningTelegraphColor  => _lightningTelegraphColor;

        public Sprite[] LightningStrikeSprites => _lightningStrikeSprites;
        public float    LightningFrameInterval => _lightningFrameInterval;

        public float   LightningParticleSize   => _lightningParticleSize;
        public Color[] LightningParticleColors => _lightningParticleColors;

        public float DeathLightDelay => _deathLightDelay;

        public float   DeathCircleDelay          => _deathCircleDelay;
        public Vector2 DeathCircleLaunchDirection => _deathCircleLaunchDirection;
        public float   DeathCircleLaunchSpeed    => _deathCircleLaunchSpeed;
        public float   DeathCircleLaunchDuration => _deathCircleLaunchDuration;
        public float   DeathCircleFloatDuration  => _deathCircleFloatDuration;
        public float   DeathCircleFloatAmplitude => _deathCircleFloatAmplitude;
        public float   DeathCircleFloatFrequency => _deathCircleFloatFrequency;
        public float   DeathCircleReturnSpeed    => _deathCircleReturnSpeed;

        public float ConfusionInterval => _confusionInterval;
        public float ConfusionDuration => _confusionDuration;
        public float HeartPickupHeight => _heartPickupHeight;
        public float EmotionInterval   => _emotionInterval;

        public int   GimmickLaserCount      => _gimmickLaserCount;
        public float GimmickSafeZoneAlpha   => _gimmickSafeZoneAlpha;
        public float GimmickTelegraphTime   => _gimmickTelegraphTime;
        public float GimmickLaserActiveTime => _gimmickLaserActiveTime;
        public float GimmickLaserHeight     => _gimmickLaserHeight;
        public float GimmickRefireDelay     => _gimmickRefireDelay;
        public float GimmickJudgeInterval   => _gimmickJudgeInterval;

        public float Phase2WaveInterval         => _phase2WaveInterval;
        public float Phase2WaveWidth            => _phase2WaveWidth;
        public float Phase2WaveHeight           => _phase2WaveHeight;
        public float Phase2WaveGroundEmbed      => _phase2WaveGroundEmbed;
        public float Phase2WaveTelegraphTime    => _phase2WaveTelegraphTime;
        public float Phase2WaveActiveTime       => _phase2WaveActiveTime;
        public float Phase2WaveFrameInterval    => _phase2WaveFrameInterval;
        public int   Phase2WaveActiveFrameCount => _phase2WaveActiveFrameCount;
        public Color Phase2WaveColor            => _phase2WaveColor;

        public Sprite[] Phase2WaveStrikeSprites   => _phase2WaveStrikeSprites;
        public float     Phase2WaveParticleSize   => _phase2WaveParticleSize;
        public Color[]   Phase2WaveParticleColors => _phase2WaveParticleColors;

        public float Phase3LaserInterval         => _phase3LaserInterval;
        public float Phase3LaserWarningTime      => _phase3LaserWarningTime;
        public float Phase3LaserBlinkInterval    => _phase3LaserBlinkInterval;
        public float Phase3LaserActiveTime       => _phase3LaserActiveTime;
        public float Phase3LaserRetractTime      => _phase3LaserRetractTime;
        public float Phase3LaserThickness        => _phase3LaserThickness;
        public float Phase3LaserWarningThickness => _phase3LaserWarningThickness;
        public float Phase3LaserMaxHeight        => _phase3LaserMaxHeight;
        public Color Phase3LaserWarningColor     => _phase3LaserWarningColor;
        public Color Phase3LaserColor            => _phase3LaserColor;
        public Material Phase3LaserMaterial      => _phase3LaserMaterial;

        public float   Phase3LaserFlowParticleSize => _phase3LaserFlowParticleSize;
        public float   Phase3LaserFlowSpeed        => _phase3LaserFlowSpeed;
        public float   Phase3LaserFlowRate         => _phase3LaserFlowRate;
        public Color[] Phase3LaserFlowColors       => _phase3LaserFlowColors;
    }
}
