// System
using System;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

using Minsung.Utility;
using Minsung.Visual;

namespace Minsung.Common
{
    // 씬 전환 / 체크포인트 복귀 / 클리어 타임 측정을 담당하는 전역 매니저.
    [AddComponentMenu("Minsung/Game Manager")]
    public class GameManager : PersistentSingleton<GameManager>
    {
        /****************************************
        *                Fields
        ****************************************/

        private Vector3 _checkpointPosition; // 마지막으로 저장된 체크포인트 위치
        private bool    _hasCheckpoint;      // 체크포인트를 한 번이라도 지났는지

        private float _runStartTime;   // 클리어 타임 측정 시작 시각 (Time.time)
        private bool  _runTimerActive; // 측정 진행 중 여부

        private static string _pendingSceneName; // 로딩씬이 진입 시 소비할 다음 씬 이름

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

        /// <summary>
        /// 사망 처리에서 호출 -> 페이드 아웃 후 체크포인트로 복귀, 페이드 인.
        /// onRespawn은 위치 복귀 직후(화면이 어두운 시점)에 호출된다 - 체력 리셋 등 상태 복원용.
        /// 체크포인트가 없으면 아무것도 하지 않고 false를 반환한다.
        /// </summary>
        public bool RequestCheckpointRespawn(Transform target, Action onRespawn = null)
        {
            if (!_hasCheckpoint)
            {
                return false;
            }

            if (ScreenFade.Instance != null)
            {
                ScreenFade.Instance.FadeOutIn(() =>
                {
                    target.position = _checkpointPosition;
                    onRespawn?.Invoke();
                });
            }
            else
            {
                target.position = _checkpointPosition;
                onRespawn?.Invoke();
            }
            return true;
        }

        /// <summary> 클리어 타임 측정 시작 (챕터/보스 입장 시 호출). </summary>
        public void StartRunTimer()
        {
            _runStartTime   = Time.time;
            _runTimerActive = true;
        }

        /// <summary> 클리어 타임 측정 종료. SupabaseClient.SubmitScore의 durationMs로 사용. </summary>
        public int StopRunTimerMs()
        {
            if (!_runTimerActive)
            {
                return 0;
            }

            _runTimerActive = false;
            return Mathf.RoundToInt((Time.time - _runStartTime) * 1000f);
        }
    }
}
