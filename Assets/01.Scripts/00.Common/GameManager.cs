// System
using System;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

using Minsung.TimeSystem;
using Minsung.Utility;
using Minsung.Visual;

namespace Minsung.Common
{
    // 씬 전환 / 체크포인트 복귀 / 보스 클리어 타이머를 담당하는 전역 매니저.
    [AddComponentMenu("Minsung/Game Manager")]
    public class GameManager : PersistentSingleton<GameManager>, IRewindable
    {
        /****************************************
        *                Fields
        ****************************************/

        private Vector3 _checkpointPosition;
        private bool    _hasCheckpoint;

        private static string _pendingSceneName; // 로딩씬이 진입 시 소비할 다음 씬 이름

        // ---- 보스 클리어 타이머 ----
        private bool  _bossRunActive;          // 보스방 입장 ~ 격파/포기까지 진행 중 여부
        private bool  _bossTransitionPaused;   // 보스 페이즈 전환/아웃트로 컷신 중 정지 (BossController가 설정)
        private bool  _bossGamePaused;         // 일시정지 메뉴 중 정지 (PauseController가 설정)
        private int   _bossElapsedMs;          // 누적 경과 시간(ms) - 정지/리와인드가 반영된 값
        private DateTime _bossEnterAt;         // 보스방 최초 입장 시각 (UTC)
        private DateTime _bossEndAt;           // 보스 격파 확정 시각 (UTC) = 입장 시각 + 클리어 타임
        private RingBuffer<int> _bossTimerBuffer; // 리와인드 복원용 - 씬마다 RewindManager와 함께 새로 만든다

        public bool     IsBossRunActive => _bossRunActive;
        public DateTime BossEnterAt     => _bossEnterAt;
        public DateTime BossEndAt       => _bossEndAt;
        public int      BossClearTimeMs => _bossElapsedMs; // 서버 제출 시 duration_ms로 사용

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 깨끗하게 초기화되도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance           = null;
            _pendingSceneName  = null;
        }

        // 씬에 배치하지 않아도 동작하도록 씬 로드 후 자동 생성
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject("GameManager").AddComponent<GameManager>();
            }
        }

        // GameManager는 씬을 넘나드는 동안 파괴되지 않지만 RewindManager는 씬마다 새로 생기므로,
        // 씬이 바뀔 때마다 새 RewindManager에 다시 등록하고 버퍼도 새로 만든다 (되감기 기록은 씬 경계를 넘지 않는다).
        protected override void OnSingletonAwake()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            RegisterBossTimerRewind();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Pause씬 등 Additive 로드는 RewindManager를 새로 만들지 않으므로 재등록 대상에서 제외
            if (mode == LoadSceneMode.Single)
            {
                RegisterBossTimerRewind();
            }
        }

        private void RegisterBossTimerRewind()
        {
            _bossTimerBuffer = new RingBuffer<int>(RewindManager.TickCapacity);
            RewindManager.Instance?.Register(this);
        }

        // 되감기 등록 여부와 무관하게 항상 정확히 흐르도록 Update에서 직접 누적한다(BossController._battleElapsed와 동일 패턴).
        // RecordTick/ApplyRewindTick은 스냅샷 기록·복원만 담당 - 여기서 또 누적하면 이중 누적이 된다.
        private void Update()
        {
            if (_bossRunActive && !_bossTransitionPaused && !_bossGamePaused && !IsRewindManagerRewinding())
            {
                _bossElapsedMs += Mathf.RoundToInt(Time.unscaledDeltaTime * 1000f);
            }
        }

        private static bool IsRewindManagerRewinding()
        {
            return (RewindManager.Instance != null) && RewindManager.Instance.IsRewinding;
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> ScreenFade와 함께 씬을 전환. ScreenFade가 없으면 즉시 전환. </summary>
        public void LoadScene(string sceneName)
        {
            if (ScreenFade.Instance != null)
            {
                ScreenFade.Instance.FadeOutIn(() => SceneManager.LoadScene(sceneName));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        /// <summary> 이미 화면이 가려진 상태에서 호출 - 페이드아웃 없이 즉시 씬 로드 후 새 씬에서 페이드인만 한다. </summary>
        public void LoadSceneFadeInOnly(string sceneName)
        {
            static void OnLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnLoaded;
                if (ScreenFade.Instance != null)
                {
                    ScreenFade.Instance.FadeIn();
                }
            }
            SceneManager.sceneLoaded += OnLoaded;
            SceneManager.LoadScene(sceneName);
        }

        /// <summary> 로딩씬(Constants.Scene.LOADING)을 경유해 씬 전환 - 무거운 씬 전환에 사용 (프로그레스바는 LoadingController 담당) </summary>
        public void LoadSceneWithLoading(string sceneName)
        {
            _pendingSceneName = sceneName;
            SceneManager.LoadScene(Constants.Scene.LOADING);
        }

        /// <summary> LoadingController가 로딩씬 진입 시 대상 씬 이름을 1회 소비 </summary>
        public static string ConsumePendingScene()
        {
            string name = _pendingSceneName;
            _pendingSceneName = null;
            return name;
        }

        /// <summary> 진행도 저장 후 로딩씬 경유 전환 - 로비 '게임 시작'/'이어하기' 등 게임플레이 진입 지점 전용 </summary>
        public void LoadGameplayScene(string sceneName)
        {
            SaveManager.Instance?.SaveProgress(sceneName);
            LoadSceneWithLoading(sceneName);
        }

        /// <summary> 현재 체크포인트 위치 저장. 체크포인트 오브젝트 진입 시 호출. </summary>
        public void SetCheckpoint(Vector3 position)
        {
            _checkpointPosition = position;
            _hasCheckpoint       = true;
        }

        /// <summary> 사망 처리 시 호출 - 페이드 아웃 후 체크포인트로 복귀, 페이드 인. onRespawn은 위치 복귀 직후(화면이 어두운 시점)에 호출된다. 체크포인트가 없으면 false 반환. </summary>
        public bool RequestCheckpointRespawn(Transform target, Action onRespawn = null)
        {
            if (!_hasCheckpoint)
            {
                return false;
            }

            return RequestRespawnAt(target, _checkpointPosition, onRespawn);
        }

        /// <summary>지정 위치로 페이드 복귀. RespawnManager가 최근접 스폰 지점을 선택한 뒤 호출한다.</summary>
        public bool RequestRespawnAt(Transform target, Vector3 position, Action onRespawn = null)
        {
            if (target == null)
            {
                return false;
            }

            if (ScreenFade.Instance != null)
            {
                ScreenFade.Instance.FadeOutIn(() =>
                {
                    target.position = position;
                    onRespawn?.Invoke();
                });
            }
            else
            {
                target.position = position;
                onRespawn?.Invoke();
            }
            return true;
        }

        /****************************************
        *            보스 클리어 타이머
        ****************************************/

        /// <summary> 보스방 입장 시 호출. 이미 진행 중이면 무시하므로 페이즈 구간 씬을 넘나들어도 이어진다. </summary>
        public void StartBossTimer()
        {
            if (_bossRunActive)
            {
                return;
            }
            _bossRunActive         = true;
            _bossElapsedMs         = 0;
            _bossEnterAt           = DateTime.UtcNow;
            _bossTransitionPaused  = false;
            _bossGamePaused        = false;
        }

        /// <summary> 보스 격파 시 호출 - 종료 시각과 클리어 타임을 확정한다. BossClearTimeMs/BossEnterAt/BossEndAt로 서버에 제출. </summary>
        public void StopBossTimer()
        {
            if (!_bossRunActive)
            {
                return;
            }
            _bossRunActive = false;
            _bossEndAt     = _bossEnterAt.AddMilliseconds(_bossElapsedMs);
        }

        /// <summary> 진행 중이던 기록을 폐기한다 (보스전 중 사망 - 보스방에서 쫓겨남 / 보스전 재시작). </summary>
        public void ResetBossTimer()
        {
            _bossRunActive        = false;
            _bossElapsedMs        = 0;
            _bossTransitionPaused = false;
            _bossGamePaused       = false;
        }

        /// <summary> 보스 페이즈 전환/아웃트로 컷신 동안 타이머 정지. BossController가 컷신 시작/종료 시점에 호출. </summary>
        public void SetBossTimerTransitionPaused(bool paused)
        {
            _bossTransitionPaused = paused;
        }

        /// <summary> 일시정지 메뉴 동안 타이머 정지. PauseController가 Pause/Resume 시점에 호출. </summary>
        public void SetBossTimerGamePaused(bool paused)
        {
            _bossGamePaused = paused;
        }

        /****************************************
        *              IRewindable
        ****************************************/

        // 누적은 Update가 전담 - 여기서는 매 틱 스냅샷만 남긴다 (동결 중엔 값이 안 변하므로 무해)
        // - 되감기 시 ApplyRewindTick이 과거 값을 그대로 복원해 "되감은 시간은 클리어 타임에서 빠진다"
        public void RecordTick()
        {
            _bossTimerBuffer?.Push(_bossElapsedMs);
        }

        public void OnRewindStart()
        {
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            if ((_bossTimerBuffer != null) && _bossTimerBuffer.TryGetOrdered(orderedIndex, out int ms))
            {
                _bossElapsedMs = ms;
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            ApplyRewindTick(orderedIndex);
            _bossTimerBuffer?.Clear();
        }
    }
}
