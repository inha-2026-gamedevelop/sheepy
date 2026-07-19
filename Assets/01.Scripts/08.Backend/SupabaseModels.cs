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
}
