namespace Minsung.Achievement
{
    // 코드에서 AchievementManager.Unlock(id)로 호출할 id 모음.
    public static class AchievementIds
    {
        public const string FIRST_REWIND      = "first_rewind";      // 시간을 거스른 자 - 첫 되감기
        public const string CLONE_FULL_SQUAD  = "clone_full_squad";  // 나, 나, 그리고 나 - 분신 최대치 동시 유지
        public const string BOSS_PHASE1_CLEAR = "boss_phase1_clear"; // 흑과 백 - 보스 1페이즈 돌파
        public const string BOSS_DEFEATED     = "boss_defeated";     // 마지막 되감기 - 보스 처치

        public const string DEATH_100         = "death_100";         // 죽음 100회 누적
        public const string REWIND_100        = "rewind_100";        // 되감기 100회 누적
        public const string FIRST_SLOW        = "first_slow";        // 슬로우모션 최초 사용
        public const string NO_REWIND         = "no_rewind";         // 되감기 없이 보스 클리어
        public const string DOMAIN_EXPANSION  = "domain_expansion";  // 아자토스 공간찢기(즉사기)에 피격
        public const string MAP_TOP_ITEM      = "jumpking";          // 맵 최고점 아이템 획득
    }
}
