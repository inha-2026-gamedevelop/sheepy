// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

using Minsung.Sound;
using Minsung.Utility;

namespace Minsung.Common
{
    // 전역 일시정지 흐름 - ESC 토글, Pause씬 additive 로드/언로드, 시간/사운드 정지-재개
    [AddComponentMenu("Minsung/Pause Controller")]
    public class PauseController : PersistentSingleton<PauseController>
    {
        /****************************************
        *                Fields
        ****************************************/

        private bool _isPaused;

        public bool IsPaused => _isPaused;

        // 다운샘플 배율 - 낮을수록 블러가 강해진다 (저해상도 RenderTexture를 확대 표시하는 방식)
        private const int BLUR_DOWNSAMPLE = 16;

        /// <summary> 일시정지 진입 시점의 게임 화면을 저해상도로 담은 블러용 텍스처 (Pause씬 배경에서 사용) </summary>
        public RenderTexture CapturedBackground { get; private set; }

        /// <summary> 설정 패널 진입 시점의 화면(Pause 메뉴 포함)을 담은 블러용 텍스처 (SettingsBackdrop에서 사용) </summary>
        public RenderTexture CapturedSettingsBackdrop { get; private set; }

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 깨끗하게 초기화되도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        // 씬에 배치하지 않아도 동작하도록 씬 로드 후 자동 생성
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject("PauseController").AddComponent<PauseController>();
            }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(Constants.System.KEY_PAUSE))
            {
                return;
            }

            if (_isPaused)
            {
                Resume();
                return;
            }

            if (CanPause())
            {
                Pause();
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 일시정지 - Pause씬을 additive 로드하고 시간을 멈춘다 </summary>
        public void Pause()
        {
            if (_isPaused)
            {
                return;
            }
            _isPaused = true;

            Time.timeScale = 0f;
            GameManager.Instance?.SetBossTimerGamePaused(true);
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PauseBGM();
                SoundManager.Instance.PauseAllSFX();
            }

            StartCoroutine(CoShowPauseMenu());
        }

        // ScreenCapture.CaptureScreenshotAsTexture()는 프레임 렌더링이 끝난 시점(WaitForEndOfFrame)에만
        // 호출할 수 있다 - Update 도중 곧바로 호출하면 실패해서 일시정지 자체가 진행되지 않는 버그가 있었다
        private IEnumerator CoShowPauseMenu()
        {
            yield return new WaitForEndOfFrame();

            CaptureBlurredBackground();
            SceneManager.LoadScene(Constants.Scene.PAUSE, LoadSceneMode.Additive);
        }

        /// <summary> 재개 - Pause씬을 언로드하고 시간을 되돌린다 </summary>
        public void Resume()
        {
            if (!_isPaused)
            {
                return;
            }
            _isPaused = false;

            Time.timeScale = 1f;
            GameManager.Instance?.SetBossTimerGamePaused(false);
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.UnPauseBGM();
                SoundManager.Instance.ResumeAllSFX();
            }

            SceneManager.UnloadSceneAsync(Constants.Scene.PAUSE);
            ReleaseCapturedBackground();
            ReleaseCapturedSettingsBackdrop();
        }

        /// <summary> 로비로 나가기 - 시간/사운드 복구 후 로딩씬 경유로 메인메뉴 전환 (Pause씬은 단일 로드 전환 시 자동 해제) </summary>
        public void ReturnToMainMenu()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            GameManager.Instance?.ResetBossTimer(); // 로비로 나가면 진행 중이던 보스전 기록 폐기
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.UnPauseBGM();
                SoundManager.Instance.ResumeAllSFX();
            }

            GameManager.Instance.LoadSceneWithLoading(Constants.Scene.MAIN_MENU);
            ReleaseCapturedBackground();
            ReleaseCapturedSettingsBackdrop();
        }

        /// <summary> 게임 종료 </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // MainMenu/Loading/Pause 씬에서는 일시정지를 허용하지 않는다
        private bool CanPause()
        {
            string activeName = SceneManager.GetActiveScene().name;
            return (activeName != Constants.Scene.MAIN_MENU)
                && (activeName != Constants.Scene.LOADING)
                && (activeName != Constants.Scene.PAUSE);
        }

        // 이미 그려진 화면을 그대로 읽어와 저해상도로 축소한다 (블러 효과) - 카메라 속성은 건드리지 않아
        // Cinemachine 렌즈/줌 계산에 영향을 주지 않는다 (Camera.targetTexture를 직접 바꾸면 줌이 풀리는 부작용이 있었음)
        private void CaptureBlurredBackground()
        {
            ReleaseCapturedBackground();
            CapturedBackground = CaptureDownsampledScreen(BLUR_DOWNSAMPLE);
        }

        // 사용이 끝난 블러 텍스처를 풀에 반납
        private void ReleaseCapturedBackground()
        {
            if (CapturedBackground != null)
            {
                RenderTexture.ReleaseTemporary(CapturedBackground);
                CapturedBackground = null;
            }
        }

        /// <summary> 설정 패널이 열리는 시점(Pause 메뉴가 그려진 화면)을 캡처해 강한 블러 텍스처로 저장 </summary>
        public IEnumerator CoCaptureSettingsBackdrop()
        {
            yield return new WaitForEndOfFrame();

            ReleaseCapturedSettingsBackdrop();
            CapturedSettingsBackdrop = CaptureDownsampledScreen(Constants.UI.SETTINGS_BACKDROP_DOWNSAMPLE);
        }

        /// <summary> 사용이 끝난 설정 패널 배경 텍스처를 풀에 반납 </summary>
        public void ReleaseCapturedSettingsBackdrop()
        {
            if (CapturedSettingsBackdrop != null)
            {
                RenderTexture.ReleaseTemporary(CapturedSettingsBackdrop);
                CapturedSettingsBackdrop = null;
            }
        }

        // screenshot을 downsample 배율만큼 축소해 블러 텍스처로 만든다 (공용 헬퍼)
        private RenderTexture CaptureDownsampledScreen(int downsample)
        {
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot == null)
            {
                // 캡처 실패 시 블러 배경 없이 진행 (호출부는 정상 동작해야 한다)
                return null;
            }

            int width  = Mathf.Max(2, screenshot.width  / downsample);
            int height = Mathf.Max(2, screenshot.height / downsample);

            RenderTexture result = RenderTexture.GetTemporary(width, height, 0);
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode   = TextureWrapMode.Clamp;

            Graphics.Blit(screenshot, result);
            Destroy(screenshot);

            return result;
        }
    }
}
