namespace Minsung.Common
{
    public static partial class Constants
    {
        // PlayerPrefs 키 - SaveManager 전용
        public static class Save
        {
            public const string KEY_LAST_SCENE   = "Save_LastScene";
            public const string KEY_PLAYER_STATE = "Save_PlayerState";  // JSON(SaveData) 보관 - 위치 기반 이어하기
            public const string KEY_USERNAME     = "Save_Username";     // 서버 식별용 닉네임(등록 후 영구 보관)
            public const string KEY_BOSS_CLEARED = "Save_BossCleared";  // 보스 클리어 여부 (0/1)
        }
    }
}
