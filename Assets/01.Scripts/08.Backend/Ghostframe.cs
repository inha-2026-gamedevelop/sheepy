// System
using Newtonsoft.Json;

namespace Minsung.Backend
{
    // 고스트 리플레이 한 프레임. 25fps로 다운샘플해서 저장.
    public class GhostFrame
    {
        [JsonProperty("t")] public float Time;   // 시작 후 경과 시간(초)
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("s")] public int   State;  // 애니메이션 상태 (0=달리기, 1=점프, 2=슬라이드 ...)
    }
}
