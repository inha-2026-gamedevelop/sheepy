namespace Minsung.Achievement
{
    // 코드에서 AchievementManager.Unlock(id)로 호출할 id 모음.
    public static class AchievementIds
    {
        public const string FIRST_REWIND      = "first_rewind";      // 시간을 거스른 자 - 첫 되감기
        public const string CLONE_FULL_SQUAD  = "clone_full_squad";  // 나, 나, 그리고 나 - 분신 최대치 동시 유지
        public const string BOSS_PHASE1_CLEAR = "boss_phase1_clear"; // 흑과 백 - 보스 1페이즈 돌파
        public const string BOSS_DEFEATED     = "boss_defeated";     // 마지막 되감기 - 보스 처치
    }
}
