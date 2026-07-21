// System
using System;
using System.Collections.Generic;
using System.Globalization;

// Unity
using UnityEngine;

using Minsung.Achievement;
using Minsung.Common;
using Minsung.Common.Data;
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

        /// <summary>
        /// 닉네임 등록 또는 본인 로그인. "한 기기당 계정 1개" 정책.
        ///  1) 이 기기(device_id)로 이미 등록된 계정이 있으면
        ///       - 입력한 이름과 같으면 → 본인 로그인(onSuccess)
        ///       - 다르면 → 신규 등록 차단, onBlocked("이 기기는 이미 ...")
        ///  2) 이 기기로 등록된 계정이 없으면
        ///       - 입력한 이름이 서버에 없으면 → 기기값과 함께 신규 등록 후 로컬 저장(onSuccess)
        ///       - 이미 있으면(=다른 기기 소유) → onBlocked("이미 등록된 이름입니다.")
        /// 성공 시 이후 서버 미러가 활성화된다.
        /// </summary>
        public void RegisterOrLogin(string username, Action onSuccess, Action<string> onBlocked, Action<string> onError)
        {
            if (_client == null)
            {
                onError?.Invoke("SupabaseClient 없음");
                return;
            }

            string deviceId = SystemInfo.deviceUniqueIdentifier; // 현재 PC 고유값

            // 1) 이 기기로 이미 등록한 계정이 있는지 먼저 확인 (기기당 1계정)
            _client.GetPlayerByDevice(deviceId,
                onSuccess: deviceRow =>
                {
                    if (deviceRow != null)
                    {
                        if (deviceRow.Username == username)
                        {
                            SaveManager.Instance?.SaveUsername(username); // 같은 기기 + 같은 이름 = 본인 로그인
                            onSuccess?.Invoke();
                        }
                        else
                        {
                            onBlocked?.Invoke($"이 기기는 이미 '{deviceRow.Username}' 이름으로 등록되어 있습니다.");
                        }
                        return;
                    }

                    // 2) 이 기기로 등록된 계정 없음 → 이름 중복 검사 후 신규 등록
                    _client.GetPlayer(username,
                        onSuccess: nameRow =>
                        {
                            if (nameRow == null)
                            {
                                _client.Register(username, deviceId,
                                    onSuccess:  () => { ClearStaleLocalDataForNewAccount(username); onSuccess?.Invoke(); },
                                    onConflict: () => onBlocked?.Invoke("이미 등록된 이름입니다."), // 조회~등록 레이스
                                    onError:    onError);
                            }
                            else
                            {
                                // 이 기기 계정이 없는데 이름이 존재 → 다른 기기가 소유
                                onBlocked?.Invoke("이미 등록된 이름입니다.");
                            }
                        },
                        onError: onError);
                },
                onError: onError);
        }

        // 신규 계정 등록 직후 호출 - 같은 기기에 다른 계정으로 테스트하며 남은 로컬 진행/업적 기록을
        // 새 계정 것으로 오인하지 않도록 제거한 뒤 닉네임을 저장한다 (서버 데이터는 건드리지 않음)
        private void ClearStaleLocalDataForNewAccount(string username)
        {
            SaveManager.Instance?.ClearPlayerState();
            AchievementManager.Instance?.ClearAll();
            SaveManager.Instance?.SaveUsername(username);
        }

        /// <summary>
        /// 자동 로그인. 이 기기(device_id)로 등록된 계정을 서버에서 조회해,
        ///  - 있으면  → 닉네임을 로컬에 반영하고, 서버/로컬 진행상황 중 더 최근 것을 기준으로 동기화(SyncProgress) 후 onLoggedIn
        ///  - 없으면  → onNoAccount (등록 폼을 보여줘야 함)
        /// 등록 씬 진입 시 호출한다.
        /// </summary>
        public void TryAutoLogin(Action onLoggedIn, Action onNoAccount, Action<string> onError)
        {
            if (_client == null)
            {
                onError?.Invoke("SupabaseClient 없음");
                return;
            }

            string deviceId = SystemInfo.deviceUniqueIdentifier;

            _client.GetPlayerByDevice(deviceId,
                onSuccess: row =>
                {
                    if (row == null)
                    {
                        onNoAccount?.Invoke();
                        return;
                    }

                    // 닉네임만 로컬 반영, 실제 진행상황은 최신성 비교 후 반영/역전송한다.
                    SaveManager.Instance?.SaveUsername(row.Username);
                    SyncProgress(row.Username, () => onLoggedIn?.Invoke());
                },
                onError: onError);
        }

        /// <summary>
        /// 서버와 로컬의 진행상황(위치/보스클리어) 중 더 최근에 바뀐 쪽을 기준으로 맞춘다.
        ///  - 서버가 더 최신(또는 로컬에 기록이 없음) → 서버 값을 로컬에 반영
        ///  - 로컬이 더 최신(오프라인 저장 등으로 서버가 못 받은 경우) → 로컬 값을 서버로 밀어넣음
        /// 실패해도 로그인/진행 자체는 막지 않는다.
        /// </summary>
        private void SyncProgress(string username, Action onDone)
        {
            _client.GetPlayerProgress(username,
                onSuccess: prog =>
                {
                    if ((prog == null) || (SaveManager.Instance == null))
                    {
                        onDone?.Invoke();
                        return;
                    }

                    bool serverHasProgress = !string.IsNullOrEmpty(prog.LastScene);
                    bool localHasProgress  = SaveManager.Instance.HasPlayerState();

                    bool useServer = serverHasProgress &&
                        (!localHasProgress || (ParseUtc(prog.UpdatedAt) > SaveManager.Instance.GetLocalUpdatedAtUtc()));

                    if (useServer)
                    {
                        ApplyServerProgress(prog);
                    }
                    else if (localHasProgress)
                    {
                        PushLocalProgress(username);
                    }

                    onDone?.Invoke();
                },
                onError: err =>
                {
                    LogError(err);
                    onDone?.Invoke();
                });
        }

        // 서버 값을 로컬 SaveManager에 그대로 반영 (서버가 더 최신일 때).
        private void ApplyServerProgress(PlayerProgressRow prog)
        {
            // 위치는 저장된 적이 있을 때만 반영 (신규 계정은 컬럼이 null)
            if (!string.IsNullOrEmpty(prog.LastScene) && prog.PosX.HasValue && prog.PosY.HasValue)
            {
                Vector3 pos = new Vector3(prog.PosX.Value, prog.PosY.Value, prog.PosZ ?? 0f);
                SaveManager.Instance.SavePlayerState(prog.LastScene, pos, prog.FacingDir ?? 1, prog.UseDefaultSpawn ?? false);
            }
            SaveManager.Instance.SetBossCleared(prog.BossCleared ?? false);
        }

        // 로컬 값을 서버로 밀어넣는다 (로컬이 더 최신일 때 - 오프라인 저장 등으로 서버가 못 받은 경우 따라잡기).
        private void PushLocalProgress(string username)
        {
            if (!SaveManager.Instance.TryLoadPlayerState(out SaveData data))
            {
                return;
            }

            _client.UpdatePlayerProgress(username, new PlayerProgressUpdate
            {
                LastScene       = data.SceneName,
                PosX            = data.PlayerPosition.x,
                PosY            = data.PlayerPosition.y,
                PosZ            = data.PlayerPosition.z,
                FacingDir       = data.FacingDir,
                UseDefaultSpawn = data.UseDefaultSpawn,
                BossCleared     = SaveManager.Instance.IsBossCleared()
            }, null, LogError);
        }

        // Supabase timestamptz 문자열을 UTC DateTime으로 안전 파싱 (실패/빈 값이면 최솟값 -> 서버가 더 최신이 아닌 것으로 취급).
        private static DateTime ParseUtc(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return DateTime.MinValue;
            }
            return DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime result)
                ? result
                : DateTime.MinValue;
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

        /// <summary>
        /// 새로 시작 시 서버 진행상황을 초기화(Map1 기본 스폰, 보스 미클리어)한다.
        /// 서버 권위 자동 로그인(TryAutoLogin)이 다음 실행 때 옛 위치를 다시 내려받아
        /// "새로 시작"을 덮어써버리는 것을 방지하기 위함. 로컬 초기화(SaveManager.ClearPlayerState)와 짝을 이룬다.
        /// </summary>
        public void ResetProgress(string startScene)
        {
            if (!TryGetUser(out string username))
            {
                return;
            }

            _client.UpdatePlayerProgress(username, new PlayerProgressUpdate
            {
                LastScene       = startScene,
                PosX            = 0f,
                PosY            = 0f,
                PosZ            = 0f,
                FacingDir       = 1,
                UseDefaultSpawn = true,
                BossCleared     = false
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

        /// <summary> 이 유저의 서버 업적 기록을 전부 삭제 (설정 - 업적 기록 제거). 로컬 삭제는 AchievementManager.ClearAll이 담당. </summary>
        public void MirrorClearAchievements()
        {
            if (!TryGetUser(out string username))
            {
                return;
            }

            _client.DeleteAchievements(username, null, LogError);
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
