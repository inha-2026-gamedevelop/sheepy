#if UNITY_EDITOR || DEVELOPMENT_BUILD
// System
using System;
using System.IO;

// Unity
using UnityEngine;

using Minsung.Boss;
using Minsung.Boss2;
using Minsung.Player;
using Minsung.TimeSystem;
using Minsung.Utility;

namespace Minsung.Common
{

    [DefaultExecutionOrder(10000)]
    public class DevelopmentBuildTools : PersistentSingleton<DevelopmentBuildTools>
    {
        /****************************************
        *                Fields
        ****************************************/

        private const KeyCode FREEZE_CAPTURE_KEY = KeyCode.F12;
        private const KeyCode PLAYER_INVINCIBILITY_KEY = KeyCode.F11;
        private const KeyCode BOSS_PHASE_2_KEY = KeyCode.Alpha2;
        private const KeyCode BOSS2_BATTLE_START_KEY = KeyCode.Alpha3;
        private const KeyCode BOSS2_HALF_HEALTH_KEY = KeyCode.Alpha4;
        private const KeyCode BOSS_DEATH_KEY = KeyCode.Alpha5;
        private const KeyCode BOSS2_SPACE_TEAR_HEALTH_KEY = KeyCode.Alpha6;
        private const KeyCode BOSS2_SPACE_TEAR_HEALTH_KEYPAD = KeyCode.Keypad6;
        private const string CAPTURE_FOLDER_NAME = "DevelopmentCaptures";

        private bool  _isFrozen;
        private bool  _isPlayerInvincible;
        private bool  _previousAudioListenerPause;
        private float _previousTimeScale;
        private int   _captureSequence;
        private string _captureDirectory;

        public static bool IsFrozen => (Instance != null) && Instance._isFrozen;
        public static bool IsPlayerInvincible => (Instance != null) && Instance._isPlayerInvincible;

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
            EnsureCreated("DevelopmentBuildTools");
        }

        protected override void OnSingletonAwake()
        {
            _captureDirectory = Path.Combine(Application.persistentDataPath, CAPTURE_FOLDER_NAME);
        }

        private void Update()
        {
            if (Input.GetKeyDown(PLAYER_INVINCIBILITY_KEY))
            {
                TogglePlayerInvincibility();
                return;
            }

            if (Input.GetKeyDown(FREEZE_CAPTURE_KEY))
            {
                HandleFreezeCaptureInput();
                return;
            }

            if (_isFrozen || ((PauseController.Instance != null) && PauseController.Instance.IsPaused))
            {
                return;
            }

            HandleBossQaInput();
        }

        private void HandleFreezeCaptureInput()
        {
            if (_isFrozen)
            {
                ResumeGame();
                return;
            }

            if ((PauseController.Instance != null) && PauseController.Instance.IsPaused)
            {
                CaptureScreenshot();
                return;
            }

            FreezeAndCapture();
        }

        private void LateUpdate()
        {
            if (!_isFrozen)
            {
                return;
            }

            // 히트스톱이나 슬로우가 같은 프레임에 timeScale을 복원해도 QA 정지가 우선한다.
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        protected override void OnDestroy()
        {
            if (_isFrozen)
            {
                ResumeGame();
            }

            base.OnDestroy();
        }

        /****************************************
        *                Methods
        ****************************************/

        private void FreezeAndCapture()
        {
            _previousTimeScale          = Time.timeScale;
            _previousAudioListenerPause = AudioListener.pause;
            _isFrozen                   = true;

            Time.timeScale      = 0f;
            AudioListener.pause = true;

            CaptureScreenshot();
            Debug.Log($"[DevelopmentBuildTools] F12 freeze enabled. Screenshot requested under {_captureDirectory}");
        }

        private void ResumeGame()
        {
            _isFrozen = false;

            Time.timeScale = (_previousTimeScale > 0f)
                ? _previousTimeScale
                : SlowMotionController.TargetTimeScale;
            AudioListener.pause = _previousAudioListenerPause;

            Debug.Log("[DevelopmentBuildTools] F12 freeze released.");
        }

        private void CaptureScreenshot()
        {
            Directory.CreateDirectory(_captureDirectory);

            ++_captureSequence;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string fileName = $"Capture_{timestamp}_{_captureSequence:D3}.png";
            string filePath = Path.Combine(_captureDirectory, fileName);

            ScreenCapture.CaptureScreenshot(filePath);
            Debug.Log($"[DevelopmentBuildTools] Screenshot requested: {filePath}");
        }

        private void TogglePlayerInvincibility()
        {
            _isPlayerInvincible = !_isPlayerInvincible;
            FindAnyObjectByType<PlayerHealth>()?.QaSetDevelopmentInvincible(_isPlayerInvincible);
            Debug.Log($"[DevelopmentBuildTools] F11 player invincibility: {_isPlayerInvincible}");
        }

        private static void HandleBossQaInput()
        {
            if (Input.GetKeyDown(BOSS_PHASE_2_KEY))
            {
                FindAnyObjectByType<BossController>()?.QaJumpToPhase(1);
            }
            else if (Input.GetKeyDown(BOSS2_BATTLE_START_KEY))
            {
                FindAnyObjectByType<Boss2Health>()?.QaResetToBattleStart();
            }
            else if (Input.GetKeyDown(BOSS2_HALF_HEALTH_KEY))
            {
                FindAnyObjectByType<Boss2Health>()?.QaSetToHalfHealth();
            }
            else if (Input.GetKeyDown(BOSS_DEATH_KEY))
            {
                FindAnyObjectByType<BossController>()?.QaForceDeath();
            }
            else if (Input.GetKeyDown(BOSS2_SPACE_TEAR_HEALTH_KEY) || Input.GetKeyDown(BOSS2_SPACE_TEAR_HEALTH_KEYPAD))
            {
                FindAnyObjectByType<Boss2Health>()?.QaSetToSpaceTearReadyHealth();
            }
        }

            
    }
}
#endif
