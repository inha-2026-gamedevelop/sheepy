// System
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace Minsung.Backend
{
    // Supabase 연동 스모크 테스트. 릴리스 빌드에는 포함하지 않는다.
    public class SupabaseTester : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private SupabaseClient _client;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            string name = "t" + (System.DateTime.Now.Ticks % 100000); // 16자 제한 대응

            _client.Register(name,
                onSuccess: () =>
                {
                    Debug.Log($"[1] 등록 성공: {name}");
                    SubmitAndFetch(name);
                },
                onError: err => Debug.LogError($"[1] 등록 실패: {err}"));
        }

        /****************************************
        *                Methods
        ****************************************/

        // 더미 고스트 2프레임과 함께 점수를 제출하고, 성공하면 리더보드/고스트 조회까지 이어서 확인.
        private void SubmitAndFetch(string name)
        {
            List<GhostFrame> ghost = new List<GhostFrame>
            {
                new GhostFrame { Time = 0f,    X = 0f,   Y = 0f,   State = 0 },
                new GhostFrame { Time = 0.04f, X = 1.2f, Y = 0.3f, State = 1 },
            };

            _client.SubmitScore(name, 1234, 62000, ghost,
                onSuccess: () =>
                {
                    Debug.Log("[2] 점수 제출 성공");

                    _client.GetLeaderboard(10,
                        list =>
                        {
                            Debug.Log($"[3] 리더보드 {list.Count}개");
                            foreach (ScoreEntry e in list)
                            {
                                Debug.Log($"    {e.Username}: {e.Score}");
                            }
                        },
                        err => Debug.LogError($"[3] 리더보드 실패: {err}"));

                    _client.GetTopGhost(
                        top => Debug.Log($"[4] 1등 고스트: {top?.Username}, 프레임 {top?.Ghost?.Count ?? 0}개"),
                        err => Debug.LogError($"[4] 고스트 실패: {err}"));
                },
                onError: err => Debug.LogError($"[2] 점수 제출 실패: {err}"));
        }
    }
}
