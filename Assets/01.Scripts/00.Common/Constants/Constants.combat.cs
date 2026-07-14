namespace Minsung.Common
{
    public static partial class Constants
    {
        // 코드 계약값 전용 - 보스 밸런싱 수치는 BossDataSO(GameDB.Boss)에서 관리한다
        // 에셋: 08.Data/Boss/BossDB.asset
        public static class Combat
        {
            // 데미지 계산
            public const float MIN_DAMAGE          = 1f;     // 방어력 계산 후 최소 피해
            public const float CRITICAL_MULTIPLIER = 1.5f;

            // 히트스톱 (타격감을 위한거)
            public const float HIT_STOP_DURATION   = 0.05f;

            // 몬스터 기본값 (BT 노드/컴포넌트 SerializeField 초기값 - 배치별 인스펙터 튜닝 전제)
            public const float ENEMY_BASE_HEALTH      = 30f;
            public const float ENEMY_PATROL_SPEED     = 2f;
            public const float ENEMY_DETECT_RANGE     = 5f;
            public const float ENEMY_ATTACK_RANGE     = 1.2f;
            public const float ENEMY_ATTACK_COOLDOWN  = 1.5f;
            public const float ENEMY_CHASE_SPEED_MULT = 1.5f;
            public const float ENEMY_PATROL_DISTANCE  = 3f;   // 스폰 지점 기준 좌우 순찰 거리

            // 1페이즈 즉사 기믹 - LaserColor enum 개수와 일치해야 하는 구조 상수
            public const int GIMMICK_LASER_COLOR_COUNT = 3;

            // 보스 애니메이터 파라미터명 - Boss.controller의 파라미터명과 반드시 일치해야 한다
            public const string BOSS_ANIM_SPEED  = "Speed";
            public const string BOSS_ANIM_ATTACK = "Attack";
            public const string BOSS_ANIM_CAST   = "Cast";
            public const string BOSS_ANIM_HIT    = "Hit";
            public const string BOSS_ANIM_DEATH  = "Death";
            public const string BOSS_ANIM_ROAR   = "Roar";
            public const string BOSS_ANIM_JUMP   = "Jump";
            public const string BOSS_ANIM_DODGE  = "Dodge";

            // 근접 유닛 착지 판정 - Player.GROUND_CHECK_EXTRA와 같은 값이지만 계약 대상이 달라 독립 유지
            public const float GROUND_CHECK_EXTRA = 0.05f;

            // 보스 계열 스프라이트 원본 아트 응시 방향 (-1 = 왼쪽) - 시트 교체로 기본 방향이 바뀌면 이 값만 수정
            public const float BOSS_ART_FACING_SIGN = -1f;
        }
    }
}
