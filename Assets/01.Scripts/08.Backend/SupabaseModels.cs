// System
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Minsung.Backend
{
    // scores 테이블 INSERT 페이로드 (SubmitScore 요청 본문).
    // boss_enter_at/boss_end_at은 scores 테이블에 timestamptz 컬럼 추가가 선행되어야 한다 (미추가 시 Supabase가 알 수 없는 컬럼으로 거부).
    public class ScoreSubmit
    {
        [JsonProperty("username")]      public string   Username;
        [JsonProperty("score")]         public int      Score;
        [JsonProperty("duration_ms")]   public int      DurationMs; // 보스 클리어 타임(ms) = GameManager.BossClearTimeMs
        [JsonProperty("ghost")]         public List<GhostFrame> Ghost;
        [JsonProperty("boss_enter_at", NullValueHandling = NullValueHandling.Ignore)] public DateTime? BossEnterAt;
        [JsonProperty("boss_end_at", NullValueHandling = NullValueHandling.Ignore)]   public DateTime? BossEndAt;
    }

    // scores 테이블 SELECT 결과 한 행 (리더보드/고스트 조회 응답).
    public class ScoreEntry
    {
        [JsonProperty("username")]   public string Username;
        [JsonProperty("score")]      public int    Score;
        [JsonProperty("created_at")] public string CreatedAt;
        [JsonProperty("ghost")]      public List<GhostFrame> Ghost; // top-ghost 조회 때만 채워짐
    }

    // players 테이블 진행 상태 PATCH 페이로드 (UpdatePlayerProgress / SetBossCleared).
    // null 필드는 전송에서 제외 → 부분 갱신(위치만/보스클리어만) 가능.
    public class PlayerProgressUpdate
    {
        [JsonProperty("last_scene",   NullValueHandling = NullValueHandling.Ignore)] public string LastScene;
        [JsonProperty("pos_x",        NullValueHandling = NullValueHandling.Ignore)] public float?  PosX;
        [JsonProperty("pos_y",        NullValueHandling = NullValueHandling.Ignore)] public float?  PosY;
        [JsonProperty("pos_z",        NullValueHandling = NullValueHandling.Ignore)] public float?  PosZ;
        [JsonProperty("facing_dir",        NullValueHandling = NullValueHandling.Ignore)] public int?  FacingDir;
        [JsonProperty("boss_cleared",      NullValueHandling = NullValueHandling.Ignore)] public bool? BossCleared;
        [JsonProperty("use_default_spawn", NullValueHandling = NullValueHandling.Ignore)] public bool? UseDefaultSpawn; // true면 이어하기 때 씬 기본 스폰 사용
    }

    // player_achievements 테이블 upsert 한 행.
    public class AchievementRow
    {
        [JsonProperty("username")]       public string Username;
        [JsonProperty("achievement_id")] public string AchievementId;
    }

    // players 진행 상태 SELECT 결과 (GetPlayerProgress - 서버 기준 로드가 필요할 때).
    public class PlayerProgressRow
    {
        [JsonProperty("username")]     public string Username;
        [JsonProperty("last_scene")]   public string LastScene;
        [JsonProperty("pos_x")]        public float  PosX;
        [JsonProperty("pos_y")]        public float  PosY;
        [JsonProperty("pos_z")]        public float  PosZ;
        [JsonProperty("facing_dir")]        public int  FacingDir;
        [JsonProperty("boss_cleared")]      public bool BossCleared;
        [JsonProperty("use_default_spawn")] public bool UseDefaultSpawn;
    }
}
