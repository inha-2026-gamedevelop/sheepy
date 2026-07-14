namespace Minsung.Common
{
    public static partial class Constants
    {
        public static class Audio
        {
            // 볼륨 기본값 (0~1)
            public const float DEFAULT_BGM_VOLUME = 0.7f;
            public const float DEFAULT_SFX_VOLUME = 1.0f;

            // SoundManager 풀 크기 (개)
            public const int ONESHOT_POOL_SIZE  = 8;  // 단발 SFX 풀 초기 크기
            public const int DURATION_POOL_SIZE = 10; // 지속형 SFX 풀 초기 크기

            // 동일 클립 중복 재생 방지 간격 (프레임)
            public const int DEDUPE_FRAME_GAP = 3;
        }
    }
}
