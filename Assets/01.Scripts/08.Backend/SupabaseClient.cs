// System
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

// Unity
using UnityEngine;
using UnityEngine.Networking;

namespace Minsung.Backend
{
    // Supabase REST API 래퍼. 닉네임 등록 / 점수 제출 / 리더보드 / 1등 고스트 조회를 담당한다.
    public class SupabaseClient : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string KEY_FILE = "KEY.txt"; // StreamingAssets 안의 키 파일 이름

        private string _url;     // Supabase 프로젝트 URL
        private string _anonKey; // anon public 키

        private string Rest => $"{_url}/rest/v1"; // REST 엔드포인트 베이스

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            LoadKeys();
        }

        /****************************************
        *                Methods
        ****************************************/

        // KEY.txt를 줄 단위로 파싱해 URL/ANON_KEY를 채운다.
        private void LoadKeys()
        {
            string path = Path.Combine(Application.streamingAssetsPath, KEY_FILE);
            if (!File.Exists(path))
            {
                Debug.LogError($"[Supabase] 키 파일 없음: {path}");
                return;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();
                if ((trimmed.Length == 0) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                int eq = trimmed.IndexOf('=');
                if (eq < 0)
                {
                    continue;
                }

                string key   = trimmed.Substring(0, eq).Trim();
                string value = trimmed.Substring(eq + 1).Trim();

                switch (key)
                {
                    case "URL":
                        _url = value;
                        break;
                    case "ANON_KEY":
                        _anonKey = value;
                        break;
                }
            }
        }

        /// <summary> 닉네임 등록. 중복 닉네임(409)이면 onError로 안내 문구를 돌려준다. </summary>
        public void Register(string username, Action onSuccess, Action<string> onError)
        {
            StartCoroutine(RegisterRoutine(username, onSuccess, onError));
        }

        /// <summary> 점수 + 클리어 타임 + 고스트 리플레이 제출. bossEnterAt/bossEndAt은 보스 클리어 기록일 때만 채워 전달(GameManager.BossEnterAt/BossEndAt). </summary>
        public void SubmitScore(string username, int score, int durationMs, List<GhostFrame> ghost,
            Action onSuccess, Action<string> onError, DateTime? bossEnterAt = null, DateTime? bossEndAt = null)
        {
            StartCoroutine(SubmitScoreRoutine(username, score, durationMs, ghost, bossEnterAt, bossEndAt, onSuccess, onError));
        }

        /// <summary> 점수 내림차순 상위 limit개 조회 (고스트 제외). </summary>
        public void GetLeaderboard(int limit, Action<List<ScoreEntry>> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetLeaderboardRoutine(limit, onSuccess, onError));
        }

        /// <summary> 1등 기록의 고스트 리플레이 조회. 기록이 없으면 null. </summary>
        public void GetTopGhost(Action<ScoreEntry> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetTopGhostRoutine(onSuccess, onError));
        }

        /// <summary> players 행의 진행 상태(위치/방향/씬/보스클리어 등)를 부분 갱신. 해당 username 행이 있어야 반영됨. </summary>
        public void UpdatePlayerProgress(string username, PlayerProgressUpdate update, Action onSuccess, Action<string> onError)
        {
            StartCoroutine(PatchPlayerRoutine(username, update, onSuccess, onError));
        }

        /// <summary> 보스 클리어 여부만 갱신하는 편의 메서드. </summary>
        public void SetBossCleared(string username, bool cleared, Action onSuccess, Action<string> onError)
        {
            StartCoroutine(PatchPlayerRoutine(username, new PlayerProgressUpdate { BossCleared = cleared }, onSuccess, onError));
        }

        /// <summary> 업적 1개를 player_achievements에 upsert (중복이면 병합). </summary>
        public void UpsertAchievement(string username, string achievementId, Action onSuccess, Action<string> onError)
        {
            UpsertAchievements(username, new[] { achievementId }, onSuccess, onError);
        }

        /// <summary> 업적 여러 개를 한 번에 upsert (로컬 목록 전체 동기화 등). </summary>
        public void UpsertAchievements(string username, IEnumerable<string> achievementIds, Action onSuccess, Action<string> onError)
        {
            StartCoroutine(UpsertAchievementsRoutine(username, achievementIds, onSuccess, onError));
        }

        /// <summary> players 진행 상태 1행 조회. 없으면 null. </summary>
        public void GetPlayerProgress(string username, Action<PlayerProgressRow> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetPlayerProgressRoutine(username, onSuccess, onError));
        }

        private IEnumerator RegisterRoutine(string username, Action onSuccess, Action<string> onError)
        {
            string body = JsonConvert.SerializeObject(new { username });
            using UnityWebRequest req = MakePost($"{Rest}/players", body);
            yield return req.SendWebRequest();

            if (req.responseCode == 409)
            {
                onError?.Invoke("이미 사용 중인 닉네임입니다."); // unique 제약 위반
                yield break;
            }
            if (HasError(req, onError))
            {
                yield break;
            }
            onSuccess?.Invoke();
        }

        private IEnumerator SubmitScoreRoutine(string username, int score, int durationMs, List<GhostFrame> ghost,
            DateTime? bossEnterAt, DateTime? bossEndAt, Action onSuccess, Action<string> onError)
        {
            ScoreSubmit payload = new ScoreSubmit
            {
                Username = username, Score = score, DurationMs = durationMs, Ghost = ghost,
                BossEnterAt = bossEnterAt, BossEndAt = bossEndAt
            };
            string body = JsonConvert.SerializeObject(payload);
            using UnityWebRequest req = MakePost($"{Rest}/scores", body);
            yield return req.SendWebRequest();

            if (HasError(req, onError))
            {
                yield break;
            }
            onSuccess?.Invoke();
        }

        private IEnumerator GetLeaderboardRoutine(int limit, Action<List<ScoreEntry>> onSuccess, Action<string> onError)
        {
            string url = $"{Rest}/scores?select=username,score,created_at&order=score.desc&limit={limit}";
            using UnityWebRequest req = MakeGet(url);
            yield return req.SendWebRequest();

            if (HasError(req, onError))
            {
                yield break;
            }
            List<ScoreEntry> list = JsonConvert.DeserializeObject<List<ScoreEntry>>(req.downloadHandler.text);
            onSuccess?.Invoke(list ?? new List<ScoreEntry>());
        }

        private IEnumerator GetTopGhostRoutine(Action<ScoreEntry> onSuccess, Action<string> onError)
        {
            string url = $"{Rest}/scores?select=username,score,ghost&order=score.desc&limit=1";
            using UnityWebRequest req = MakeGet(url);
            yield return req.SendWebRequest();

            if (HasError(req, onError))
            {
                yield break;
            }
            List<ScoreEntry> list = JsonConvert.DeserializeObject<List<ScoreEntry>>(req.downloadHandler.text);
            onSuccess?.Invoke(((list != null) && (list.Count > 0)) ? list[0] : null);
        }

        private IEnumerator PatchPlayerRoutine(string username, PlayerProgressUpdate update, Action onSuccess, Action<string> onError)
        {
            string body = JsonConvert.SerializeObject(update);
            string url  = $"{Rest}/players?username=eq.{UnityWebRequest.EscapeURL(username)}";
            using UnityWebRequest req = MakePatch(url, body);
            yield return req.SendWebRequest();

            if (HasError(req, onError))
            {
                yield break;
            }
            onSuccess?.Invoke();
        }

        private IEnumerator UpsertAchievementsRoutine(string username, IEnumerable<string> achievementIds, Action onSuccess, Action<string> onError)
        {
            List<AchievementRow> rows = new List<AchievementRow>();
            if (achievementIds != null)
            {
                foreach (string id in achievementIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        rows.Add(new AchievementRow { Username = username, AchievementId = id });
                    }
                }
            }
            if (rows.Count == 0)
            {
                onSuccess?.Invoke();
                yield break;
            }

            string body = JsonConvert.SerializeObject(rows);
            string url  = $"{Rest}/player_achievements?on_conflict=username,achievement_id";
            using UnityWebRequest req = MakePost(url, body);
            req.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=minimal"); // 중복 PK는 병합(무해)
            yield return req.SendWebRequest();

            if (HasError(req, onError))
            {
                yield break;
            }
            onSuccess?.Invoke();
        }

        private IEnumerator GetPlayerProgressRoutine(string username, Action<PlayerProgressRow> onSuccess, Action<string> onError)
        {
            string url = $"{Rest}/players?username=eq.{UnityWebRequest.EscapeURL(username)}" +
                         "&select=username,last_scene,pos_x,pos_y,pos_z,facing_dir,boss_cleared&limit=1";
            using UnityWebRequest req = MakeGet(url);
            yield return req.SendWebRequest();

            if (HasError(req, onError))
            {
                yield break;
            }
            List<PlayerProgressRow> list = JsonConvert.DeserializeObject<List<PlayerProgressRow>>(req.downloadHandler.text);
            onSuccess?.Invoke(((list != null) && (list.Count > 0)) ? list[0] : null);
        }

        /****************************************
        *                Helper
        ****************************************/

        // 요청 실패 시 onError에 "에러: 응답 본문"을 전달하고 true를 반환.
        private static bool HasError(UnityWebRequest req, Action<string> onError)
        {
            if (req.result == UnityWebRequest.Result.Success)
            {
                return false;
            }
            onError?.Invoke($"{req.error}: {req.downloadHandler?.text}");
            return true;
        }

        // JSON 본문을 담은 POST 요청 생성 (헤더/핸들러 포함).
        private UnityWebRequest MakePost(string url, string jsonBody)
        {
            UnityWebRequest req = new UnityWebRequest(url, "POST")
            {
                uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            ApplyHeaders(req);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Prefer", "return=representation");
            return req;
        }

        // JSON 본문을 담은 PATCH 요청 생성 (부분 갱신용).
        private UnityWebRequest MakePatch(string url, string jsonBody)
        {
            UnityWebRequest req = new UnityWebRequest(url, "PATCH")
            {
                uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            ApplyHeaders(req);
            req.SetRequestHeader("Content-Type", "application/json");
            return req;
        }

        // GET 요청 생성 (인증 헤더 포함).
        private UnityWebRequest MakeGet(string url)
        {
            UnityWebRequest req = UnityWebRequest.Get(url);
            ApplyHeaders(req);
            return req;
        }

        // Supabase 공통 인증 헤더 부착.
        private void ApplyHeaders(UnityWebRequest req)
        {
            req.SetRequestHeader("apikey", _anonKey);
            req.SetRequestHeader("Authorization", $"Bearer {_anonKey}");
        }
    }
}