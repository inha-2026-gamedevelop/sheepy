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