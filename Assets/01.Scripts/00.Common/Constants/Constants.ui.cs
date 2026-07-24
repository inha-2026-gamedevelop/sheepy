namespace Minsung.Common
{
    public static partial class Constants
    {
        public static class UI
        {
            // 팝업
            public const float POPUP_ANIM_DURATION  = 0.2f;

            // HUD
            public const float HP_BAR_LERP_SPEED   = 5f;    // HP바 부드럽게 줄어드는 속도
            public const float MP_BAR_LERP_SPEED   = 8f;

            // 피격 텍스트 팝업
            public const float DAMAGE_TEXT_DURATION = 0.8f;
            public const float DAMAGE_TEXT_RISE     = 1.2f; // 올라가는 거리(유닛)

            // 랭킹
            public const int   RANKING_TOP_COUNT    = 10;

            // 로딩씬
            public const float LOADING_MIN_DISPLAY_SECONDS = 0.5f; // 최소 노출 시간 (즉시 완료 시 화면이 깜빡여 보이는 것 방지)
            public const float SCENE_FADE_DURATION         = 1f;   // 모든 씬 전환 Fade Out / Fade In 시간
            public const float SCENE_ACTIVATION_PROGRESS   = 0.9f; // AsyncOperation.progress가 여기서 멈춰 있다가 activation 시 1로 점프하는 Unity 계약값

            // 메뉴 선택 파티클 버스트
            public const float MENU_BURST_DELAY_SECONDS = 0.35f; // 버스트 재생 후 씬 전환까지 대기 시간

            // 설정 패널 배경 (일시정지 화면 전용)
            public const int   SETTINGS_BACKDROP_DOWNSAMPLE = 40;   // 다운샘플 배율 - Pause 배경(16)보다 훨씬 강한 블러
            public const float SETTINGS_BACKDROP_BRIGHTNESS = 0.15f; // 블러 텍스처에 곱하는 명도 - 낮을수록 어둡다

            // FPS 카운터 (우상단 개발용 오버레이, 나머지 레이아웃 값은 FpsCounterUI.prefab에서 직접 조절)
            public const float FPS_REFRESH_INTERVAL = 0.25f; // 표시 갱신 주기(초, unscaled)
        }
    }
}
