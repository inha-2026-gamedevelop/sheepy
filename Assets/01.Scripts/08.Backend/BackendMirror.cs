// System
using System;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Utility;

namespace Minsung.Backend
{
    // 로컬 저장(PlayerPrefs)을 Supabase 서버로 "미러링"하는 서비스.
    // 로컬 우선 원칙: 닉네임이 등록되지 않았거나(오프라인/미가입) 네트워크가 실패해도
    // 조용히 건너뛰며 게임 진행에는 전혀 영향을 주지 않는다. 서버는 백업/집계용.
    [AddComponentMenu("Minsung/Backend Mirror")]
    public class BackendMirror : PersistentSingleton<BackendMirror>
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private SupabaseClient _client; // 미지정 시 런타임에 자동 확보

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
            EnsureCreated(nameof(BackendMirror));
        }

        protected override void OnSingletonAwake()
        {
            if (_client == null)
            {
                _client = GetComponent<SupabaseClient>();
                if (_client == null)
                {
                    _client = gameObject.AddComponent<SupabaseClient>();
                }
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 닉네임 등록 + 성공 시 로컬 영구 저장. 등록 UI가 호출하면 이후 미러가 활성화된다. </summary>
        public void RegisterAndRemember(string username, Action onSuccess, Action<string> onError)
        {
            if (_client == null)
            {
                onError?.Invoke("SupabaseClient 없음");
                return;
            }

            _client.Register(username,
                onSuccess: () =>
                {
                    SaveManager.Instance?.SaveUsername(username);
                    onSuccess?.Invoke();
                },
                onError: onError);
        }

        /// <summary> 플레이어 위치/방향/씬을 서버 players 행에 미러. useDefaultSpawn=true면 이어하기 때 씬 기본 스폰 사용. </summary>
        public void MirrorPlayerProgress(string sceneName, Vector3 position, int facingDir, bool useDefaultSpawn = false)
        {
            if (!TryGetUser(out string username))
            {
                return;
            }

            _client.UpdatePlayerProgress(username, new PlayerProgressUpdate
            {
                LastScene       = sceneName,
                PosX            = position.x,
                PosY            = position.y,
                PosZ            = position.z,
                FacingDir       = facingDir,
                UseDefaultSpawn = useDefaultSpawn
            }, null, LogError);
        }

        /// <summary> 보스 클리어 여부를 서버에 미러 (보스 격파 확정 시점). </summary>
        public void MirrorBossCleared()
        {
            if (!TryGetUser(out string username))
            {
                return;
            }

            _client.SetBossCleared(username, true, null, LogError);
        }

        /// <summary> 업적 1개 해제를 서버에 미러 (해제 순간). </summary>
        public void MirrorAchievement(string achievementId)
        {
            if (!TryGetUser(out string username))
            {
                return;
            }

            _client.UpsertAchievement(username, achievementId, null, LogError);
        }

        /// <summary> 로컬에 쌓인 업적 목록 전체를 서버로 밀어넣기 (최초 로그인/일괄 동기화). </summary>
        public void MirrorAchievements(IEnumerable<string> achievementIds)
        {
            if (!TryGetUser(out string username))
            {
                return;
            }

            _client.UpsertAchievements(username, achievementIds, null, LogError);
        }

        /****************************************
        *                Helper
        ****************************************/

        // 미러 가능 여부: 클라이언트가 있고, 로컬에 닉네임이 저장되어 있어야 한다.
        private bool TryGetUser(out string username)
        {
            username = (SaveManager.Instance != null) ? SaveManager.Instance.GetUsername() : string.Empty;
            return (_client != null) && !string.IsNullOrEmpty(username);
        }

        private static void LogError(string error)
        {
            Debug.LogWarning($"[BackendMirror] 서버 미러 실패(로컬 저장은 정상): {error}");
        }
    }
}
