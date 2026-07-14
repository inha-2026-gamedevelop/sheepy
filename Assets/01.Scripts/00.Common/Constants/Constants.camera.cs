namespace Minsung.Common
{
    public static partial class Constants
    {
        public static class Camera
        {
            // Cinemachine 카메라 우선순위 (숫자가 높을수록 우선 적용)
            // 플레이어 카메라는 항상 PRIORITY_DEFAULT로 고정 - 포커스 카메라만 IDLE/FOCUS 사이를 오가며 전환한다.
            // (플레이어와 포커스가 같은 값으로 동률이 되면 Brain이 전환하지 않는 경우가 있어 반드시 값을 벌려둔다)
            public const int PRIORITY_FOCUS_IDLE = 0;  // 포커스 카메라
            public const int PRIORITY_DEFAULT    = 10; // 플레이어 카메라 우선도
            public const int PRIORITY_FOCUS      = 20; // 포커스 카메라

            public const float DEFAULT_BLEND_TIME = 0.5f; // 포커스 전환 기본 블렌드 시간(초)

            // Lens Orthographic Size (카메라 줌 정도)
            public const float PLAYER_ORTHOGRAPHIC_SIZE = 1.3f;   // 플레이어 카메라 줌
            public const float FOCUS_ORTHOGRAPHIC_SIZE  = 1.0f; // 포커스 카메라 기본 줌
            public const float BOSS_ORTHOGRAPHIC_SIZE   = 5.2f; // 보스전 줌아웃 - 아레나가 한 화면에 들어오는 값
        }
    }
}
