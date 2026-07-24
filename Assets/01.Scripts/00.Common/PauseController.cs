// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

using Minsung.Player;
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

        private const int BLUR_DOWNSAMPLE = 16;

        public RenderTexture CapturedBackground { get; private set; }

        public RenderTexture CapturedSettingsBackdrop { get; private set; }

        /****************************************
        *              Unity Event
        ****************************************/

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetStatic();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            EnsureCreated("PauseController");
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

        private IEnumerator CoShowPauseMenu()
        {
            yield return new WaitForEndOfFrame();

            CaptureBlurredBackground();
            SceneManager.LoadScene(Constants.Scene.PAUSE, LoadSceneMode.Additive);
        }

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

        public void ReturnToMainMenu()
        {
            _isPaused = false;
            Time.timeScale = 1f;


            FindAnyObjectByType<PlayerController>()?.PersistProgress();

            GameManager.Instance?.ResetBossTimer();
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.UnPauseBGM();
                SoundManager.Instance.ResumeAllSFX();
            }

            GameManager.Instance.LoadSceneWithLoading(Constants.Scene.MAIN_MENU);
            ReleaseCapturedBackground();
            ReleaseCapturedSettingsBackdrop();
        }

        /// <summary> 게임 종료 - PlayerSaveOnExit(OnApplicationQuit)에 더해 명시적으로 한 번 더 저장해 확실히 남긴다 </summary>
        public void QuitGame()
        {
            FindAnyObjectByType<PlayerController>()?.PersistProgress();

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

        private void CaptureBlurredBackground()
        {
            ReleaseCapturedBackground();
            CapturedBackground = CaptureDownsampledScreen(BLUR_DOWNSAMPLE);
        }

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
        // ScreenCapture는 검정 레터박스 바까지 포함한 전체 화면을 캡처하므로, 16:9로 잘린 메인 카메라 뷰포트만 잘라내
        // Pause 캔버스(같은 비율로 레터박스된)에 그대로 채웠을 때 비율이 안 맞아 찌그러지는 것을 막는다
        private RenderTexture CaptureDownsampledScreen(int downsample)
        {
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot == null)
            {
                // 캡처 실패 시 블러 배경 없이 진행 (호출부는 정상 동작해야 한다)
                return null;
            }

            Rect viewport = (Camera.main != null) ? Camera.main.rect : new Rect(0f, 0f, 1f, 1f);

            int width  = Mathf.Max(2, Mathf.RoundToInt(screenshot.width  * viewport.width  / downsample));
            int height = Mathf.Max(2, Mathf.RoundToInt(screenshot.height * viewport.height / downsample));

            RenderTexture result = RenderTexture.GetTemporary(width, height, 0);
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode   = TextureWrapMode.Clamp;

            Graphics.Blit(screenshot, result, new Vector2(viewport.width, viewport.height), new Vector2(viewport.x, viewport.y));
            Destroy(screenshot);

            return result;
        }
    }
}
