namespace Minsung.Common
{
    public static partial class Constants
    {
        public static class Audio
        {
            // 볼륨 기본값 (0~1)
            public const float DEFAULT_BGM_VOLUME = 1.0f;
            public const float DEFAULT_SFX_VOLUME = 1.0f;
            public const float BASE_BGM_VOLUME    = 0.5f;
            public const float BASE_SFX_VOLUME    = 0.2f;

            // SoundManager 풀 크기 (개)
            public const int ONESHOT_POOL_SIZE  = 8;
            public const int DURATION_POOL_SIZE = 10;

            // 동일 클립 중복 재생 방지 간격 (프레임)
            public const int DEDUPE_FRAME_GAP = 3;
        }
    }
}
