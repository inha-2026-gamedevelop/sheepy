// Unity
using UnityEngine;

using Minsung.Achievement;
using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Utility;

namespace Minsung.TimeSystem
{
    // 키를 누르는 동안 전체 시간을 느리게 만드는 컨트롤러.
    public class SlowMotionController : SceneSingleton<SlowMotionController>
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("테스트 입력 (나중에 스킬 시스템이 대체)")]
        [SerializeField] private KeyCode _slowKey = KeyCode.LeftShift;

        private float _slowScale; // 슬로우 배율 - TimeDB(GameDB.Time)에서 Awake 때 로드

        private float _defaultFixedDelta; // 원본 fixedDeltaTime (복원용)

        private static float _targetScale = 1f; // 슬로우 상태 기준 목표 배율

        public static bool IsSlow { get; private set; } // 다른 시스템이 슬로우 상태를 참조할 때 사용

        // 히트스톱이 끝날 때 복원할 배율 - 슬로우 중이면 슬로우 배율, 아니면 1
        public static bool IsAbilityUnlocked { get; private set; }
        public static float TargetTimeScale => _targetScale;

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 시간 배율이 깨끗하게 초기화되도록.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetStatic();
            IsAbilityUnlocked = false;
            IsSlow       = false;
            _targetScale = 1f;
            Time.timeScale = 1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            EnsureCreated("SlowMotionController");
        }

        protected override void OnSingletonAwake()
        {
            _slowScale         = GameDB.Time.SlowTimeScale;
            _defaultFixedDelta = Time.fixedDeltaTime;
            RestoreAbilityUnlock();
        }

        private void Start()
        {
            RestoreAbilityUnlock();
        }

        private void Update()
        {
            if (!IsAbilityUnlocked)
            {
                return;
            }
            if (Input.GetKeyDown(_slowKey))
            {
                SetSlow(true);
            }
            else if (Input.GetKeyUp(_slowKey))
            {
                SetSlow(false);
            }
        }

        private void OnDisable()
        {
            if (Instance != this)
            {
                return;
            }
            // 비활성/씬 전환 시 시간 원복 보장
            if (IsSlow)
            {
                SetSlow(false);
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 슬로우모션 켜기/끄기. timeScale에 비례해 fixedDeltaTime도 함께 보정한다. </summary>
        public static void UnlockAbility()
        {
            IsAbilityUnlocked = true;
            SaveManager.Instance?.SetSlowAbilityUnlocked(true);
        }

        private static void RestoreAbilityUnlock()
        {
            IsAbilityUnlocked = (SaveManager.Instance != null) && SaveManager.Instance.IsSlowAbilityUnlocked();
        }

        public void SetSlow(bool on)
        {
            if (on)
            {
                AchievementTrigger.SlowMotionUsed();
            }

            IsSlow       = on;
            _targetScale = on ? _slowScale : 1f;
            Time.fixedDeltaTime = _defaultFixedDelta * _targetScale;

            // 히트스톱 진행 중이면 timeScale은 히트스톱 종료 시 TargetTimeScale로 복원된다
            if (!HitStopController.IsActive)
            {
                Time.timeScale = _targetScale;
            }
        }
    }
}
