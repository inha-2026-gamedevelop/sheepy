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
        [SerializeField] private float _moveSpeed    = 2f; // 이동 속도(유닛/초)
        [SerializeField] private float _jumpForce    = 6f; // 점프 힘(임펄스)
        [SerializeField] private int   _maxJumps     = 2;  // 지상 점프 1회 + 공중(더블) 점프 1회
        [SerializeField] private float _gravityScale = 2f; // 중력 배율

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

        [Header("피격 리액션 (넉백/플래시)")]
        [SerializeField] private float _knockbackForceX   = 6f;    // 넉백 수평 속도 (피해 지점 반대 방향)
        [SerializeField] private float _knockbackForceY   = 4f;    // 넉백 수직 속도 (살짝 띄움)
        [SerializeField] private float _knockbackStunTime = 0.15f; // 넉백 중 이동 잠금 시간(초)
        [SerializeField] private float _hitFlashDuration  = 0.15f; // 피격 글로우 플래시 시간(초)

        [SerializeField] private Color _hitFlashColor = new Color(1f, 0.25f, 0.25f, 0.6f); // 피격 글로우 색

        [Header("오브")]
        [SerializeField] private float _orbAttackRange  = 2.5f;  // 공격 대상 탐지 반경
        [SerializeField] private float _orbFollowSmooth = 0.15f; // 따라다니기 스무딩 시간(초)
        [SerializeField] private float _orbBobAmplitude = 0.12f; // 둥실거림 진폭
        [SerializeField] private float _orbBobSpeed     = 2.5f;  // 둥실거림 속도(라디안/초 배율)
        [SerializeField] private float _orbDashSpeed    = 16f;   // 공격 돌진 속도
        [SerializeField] private float _orbHitDistance  = 0.25f; // 대상 도달 판정 거리
        [SerializeField] private float _orbDashTimeout  = 0.6f;  // 돌진 최대 시간(초) - 대상 소실 대비
        [SerializeField] private float _orbSize         = 0.25f; // 기본 오브 크기(스케일)

        // 오브 대기 위치 (플레이어 기준 오프셋) - 개수 = 오브 개수
        [SerializeField] private Vector2[] _orbOffsets =
        {
            new Vector2(-0.7f, 1.1f),
            new Vector2(-0.7f, 0.4f),
        };

        [SerializeField] private Color _orbColor = new Color(1f, 0.9f, 0.5f, 0.9f); // 기본 오브 색

        [Header("시각 효과")]
        [SerializeField] private Color _rewindTintColor = new Color(0.6f, 0.6f, 1f); // 되감기 중 색상

        // 차지공격 시각 피드백 (몸통 머티리얼 색)
        [SerializeField] private Color _chargingColor    = new Color(1f, 0.75f, 0.4f); // 차지 진행 중 (주황)
        [SerializeField] private Color _chargeReadyColor = new Color(1f, 0.9f, 0.2f);  // 풀차지 완료 (골드)

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

        public float KnockbackForceX   => _knockbackForceX;
        public float KnockbackForceY   => _knockbackForceY;
        public float KnockbackStunTime => _knockbackStunTime;
        public float HitFlashDuration  => _hitFlashDuration;
        public Color HitFlashColor     => _hitFlashColor;

        public float OrbAttackRange  => _orbAttackRange;
        public float OrbFollowSmooth => _orbFollowSmooth;
        public float OrbBobAmplitude => _orbBobAmplitude;
        public float OrbBobSpeed     => _orbBobSpeed;
        public float OrbDashSpeed    => _orbDashSpeed;
        public float OrbHitDistance  => _orbHitDistance;
        public float OrbDashTimeout  => _orbDashTimeout;
        public float OrbSize         => _orbSize;

        public Vector2[] OrbOffsets => _orbOffsets;
        public Color     OrbColor   => _orbColor;

        public Color RewindTintColor  => _rewindTintColor;
        public Color ChargingColor    => _chargingColor;
        public Color ChargeReadyColor => _chargeReadyColor;

        // 반칸 단위 최대 체력 (하트 1칸 = 반칸 2개, 환산 상수는 코드 계약값)
        public int MaxHeartHalves => _maxHearts * Constants.Player.HALVES_PER_HEART;
    }
}
