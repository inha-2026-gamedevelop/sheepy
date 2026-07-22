// Unity
using UnityEngine;

namespace Minsung.Common.Data
{
    // 플레이어 밸런싱 데이터 DB - 에셋: 08.Data/Player/PlayerDB.asset (GameDB.Player로 접근)
    // 입력 키/축, 판정 epsilon, 하트 반칸 환산 등 코드 계약값은 Constants.Player에 남는다
    [CreateAssetMenu(fileName = "PlayerDB", menuName = "TheLastRewind/GameDB/PlayerDB")]
    public class PlayerDataSO : ScriptableObject
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("이동")]
        [SerializeField] private float _moveSpeed    = 1.3f; // 이동 속도(유닛/초)
        [SerializeField] private float _jumpForce    = 4.5f; // 점프 힘(임펄스)
        [SerializeField] private int   _maxJumps     = 2;    // 지상 점프 1회 + 공중(더블) 점프 1회
        [SerializeField] private float _gravityScale = 1.4f; // 중력 배율

        [Header("공격")]
        [SerializeField] private float _attackDamage      = 20f;
        [SerializeField] private float _chargeDamageMult  = 2.5f;  // 차지공격 배율
        [SerializeField] private float _chargeTime        = 1.2f;  // 풀차지 소요 시간(초)
        [SerializeField] private float _attackCooldown    = 0.3f;  // 공격 간격(초)
        [SerializeField] private float _projectileSpeed   = 10f;   // 투사체 속도(유닛/초)
        [SerializeField] private float _attackFlashTime   = 0.1f;  // 공격 플래시 지속시간(초)
        [SerializeField] private float _attackHitRadius   = 0.6f;  // 공격 히트박스 반경
        [SerializeField] private float _attackHitLifetime = 0.15f; // 공격 히트박스 존재 시간(초)

        [Header("체력 / 사망")]
        [SerializeField] private int   _maxHearts          = 6;    // 하트(체력) 총 개수
        [SerializeField] private float _invincibleDuration = 1f;   // 피격 후 무적 시간(초)
        [SerializeField] private float _deathRespawnDelay  = 0.6f; // 하트 0 이후 페이드 시작까지 대기(초) - 사망 연출 여지

        [Header("전용 무적키 (보스 즉사기 회피)")]
        [SerializeField] private float _dodgeInvincibleDuration = 1f;  // 무적 지속시간(초)
        [SerializeField] private float _dodgeInvincibleCooldown = 30f; // 재사용 대기시간(초) - 발동 즉시부터 카운트

        [Header("피격 리액션 (넉백/플래시)")]
        [SerializeField] private float _knockbackForceX   = 6f;    // 넉백 수평 속도 (피해 지점 반대 방향)
        [SerializeField] private float _knockbackForceY   = 4f;    // 넉백 수직 속도 (살짝 띄움)
        [SerializeField] private float _knockbackStunTime = 0.15f; // 넉백 중 이동 잠금 시간(초)
        [SerializeField] private float _hitFlashDuration  = 0.15f; // 피격 글로우 플래시 시간(초)

        [SerializeField] private Color _hitFlashColor = new Color(1f, 0.25f, 0.25f, 0.6f); // 피격 글로우 색

        [Header("피격 화면 연출")]
        [SerializeField] private float _hitShakeForce          = 0.5f;  // 카메라 임펄스 세기
        [SerializeField] private float _hitShakeDuration       = 0.25f; // 카메라 임펄스 감쇠 시간(초)
        [SerializeField] private float _hitVignetteAlpha       = 0.35f; // 피격 빨간 화면 최대 알파(0~1)
        [SerializeField] private float _hitVignetteDuration    = 0.4f;  // 피격 빨간 화면 감쇠 시간(실시간, 초)
        [SerializeField] private float _hitStopOnDamagedDuration = 0.08f; // 피격 히트스톱 시간(실시간, 초)

        [Header("오브")]
        [SerializeField] private float _orbAttackRange  = 2.5f;  // 공격 대상 탐지 반경
        [SerializeField] private float _orbFollowSmooth = 0.15f; // 따라다니기 스무딩 시간(초)
        [SerializeField] private Vector2 _orbAnchorOffset = new Vector2(-0.05f, 0.15f); // 몸통 기준 대기 지점(왼쪽 위)
        [SerializeField] private float _orbWanderRadius = 0.06f; // 대기 지점 주변을 떠다니는 반경(유닛)
        [SerializeField] private float _orbWanderSpeed  = 0.5f;  // 떠다니는 펄린 노이즈 샘플링 속도
        [SerializeField] private float _orbSpacing      = 0.12f; // 오브끼리 벌려놓는 간격(유닛)
        [SerializeField] private int   _orbCount        = 2;     // 오브 개수
        [SerializeField] private float _orbDashSpeed    = 16f;   // 공격 돌진 속도
        [SerializeField] private float _orbHitDistance  = 0.25f; // 대상 도달 판정 거리
        [SerializeField] private float _orbDashTimeout  = 0.6f;  // 돌진 최대 시간(초) - 대상 소실 대비
        [SerializeField] private float _orbSize         = 0.05f; // 기본 오브 크기(스케일)

        [SerializeField] private Color _orbColor = new Color(1f, 0.9f, 0.5f, 0.9f); // 기본 오브 색

        [Header("시각 효과")]
        [SerializeField] private Color _rewindTintColor = new Color(0.6f, 0.6f, 1f); // 되감기 중 색상

        // 차지공격 시각 피드백 (몸통 머티리얼 색)
        [SerializeField] private Color _chargingColor    = new Color(1f, 0.75f, 0.4f); // 차지 진행 중 (주황)
        [SerializeField] private Color _chargeReadyColor = new Color(1f, 0.9f, 0.2f);  // 풀차지 완료 (골드)

        [Header("오브 비주얼")]
        [SerializeField] private Color _orbGlowColor       = new Color(0.08f, 0.48f, 1f, 1f); // 오브 발광 색상
        [SerializeField] private float _orbGlowIntensity   = 3.5f;   // 오브 HDR 발광 강도
        [SerializeField] private float _orbPulseSpeed       = 2f;    // 오브 발광 맥동 속도
        [SerializeField] private float _orbPulseAmount      = 0.15f; // 오브 발광 맥동 폭

        [Header("오브 궤적")]
        [SerializeField] private Color _orbTrailColor              = new Color(0.08f, 0.55f, 1f, 0.85f); // 궤적 색상
        [SerializeField] private float _orbTrailDuration           = 0.3f;   // 궤적이 남아있는 시간(초)
        [SerializeField] private float _orbTrailWidth              = 0.045f; // 궤적 기본 너비(유닛)
        [SerializeField] private float _orbTrailMinVertexDistance  = 0.015f; // 궤적 꼭짓점 추가 최소 거리(유닛)

        /****************************************
        *              Properties
        ****************************************/

        public float MoveSpeed    => _moveSpeed;
        public float JumpForce    => _jumpForce;
        public int   MaxJumps     => _maxJumps;
        public float GravityScale => _gravityScale;

        public float AttackDamage      => _attackDamage;
        public float ChargeDamageMult  => _chargeDamageMult;
        public float ChargeTime        => _chargeTime;
        public float AttackCooldown    => _attackCooldown;
        public float ProjectileSpeed   => _projectileSpeed;
        public float AttackFlashTime   => _attackFlashTime;
        public float AttackHitRadius   => _attackHitRadius;
        public float AttackHitLifetime => _attackHitLifetime;

        public int   MaxHearts          => _maxHearts;
        public float InvincibleDuration => _invincibleDuration;
        public float DeathRespawnDelay  => _deathRespawnDelay;

        public float DodgeInvincibleDuration => _dodgeInvincibleDuration;
        public float DodgeInvincibleCooldown => _dodgeInvincibleCooldown;

        public float KnockbackForceX   => _knockbackForceX;
        public float KnockbackForceY   => _knockbackForceY;
        public float KnockbackStunTime => _knockbackStunTime;
        public float HitFlashDuration  => _hitFlashDuration;
        public Color HitFlashColor     => _hitFlashColor;
        public float HitShakeForce           => _hitShakeForce;
        public float HitShakeDuration        => _hitShakeDuration;
        public float HitVignetteAlpha        => _hitVignetteAlpha;
        public float HitVignetteDuration     => _hitVignetteDuration;
        public float HitStopOnDamagedDuration => _hitStopOnDamagedDuration;

        public float   OrbAttackRange   => _orbAttackRange;
        public float   OrbFollowSmooth  => _orbFollowSmooth;
        public Vector2 OrbAnchorOffset  => _orbAnchorOffset;
        public float   OrbWanderRadius  => _orbWanderRadius;
        public float   OrbWanderSpeed   => _orbWanderSpeed;
        public float   OrbSpacing       => _orbSpacing;
        public int     OrbCount         => _orbCount;
        public float OrbDashSpeed    => _orbDashSpeed;
        public float OrbHitDistance  => _orbHitDistance;
        public float OrbDashTimeout  => _orbDashTimeout;
        public float OrbSize         => _orbSize;

        public Color OrbColor => _orbColor;

        public Color OrbGlowColor              => _orbGlowColor;
        public float OrbGlowIntensity          => _orbGlowIntensity;
        public float OrbPulseSpeed              => _orbPulseSpeed;
        public float OrbPulseAmount             => _orbPulseAmount;
        public Color OrbTrailColor              => _orbTrailColor;
        public float OrbTrailDuration           => _orbTrailDuration;
        public float OrbTrailWidth              => _orbTrailWidth;
        public float OrbTrailMinVertexDistance  => _orbTrailMinVertexDistance;

        public Color RewindTintColor  => _rewindTintColor;
        public Color ChargingColor    => _chargingColor;
        public Color ChargeReadyColor => _chargeReadyColor;

        // 반칸 단위 최대 체력 (하트 1칸 = 반칸 2개, 환산 상수는 코드 계약값)
        public int MaxHeartHalves => _maxHearts * Constants.Player.HALVES_PER_HEART;
    }
}
