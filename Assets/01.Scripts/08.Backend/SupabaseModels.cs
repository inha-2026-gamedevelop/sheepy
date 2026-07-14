// System
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Minsung.Backend
{
    // scores 테이블 INSERT 페이로드 (SubmitScore 요청 본문).
    public class ScoreSubmit
    {
        [JsonProperty("username")]    public string Username;
        [JsonProperty("score")]       public int    Score;
        [JsonProperty("duration_ms")] public int    DurationMs;
        [JsonProperty("ghost")]       public List<GhostFrame> Ghost;
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
