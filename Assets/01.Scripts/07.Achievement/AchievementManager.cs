// System
using System;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Backend;
using Minsung.Utility;

namespace Minsung.Achievement
{
    // 업적 해제 상태 관리 싱글톤.
    public class AchievementManager : PersistentSingleton<AchievementManager>
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string SAVE_KEY           = "Achievements_UnlockedIds";
        private const string COUNTER_KEY_PREFIX = "Achievements_Counter_"; // 누적 카운트 업적(사망 100회 등) 저장용
        private const string UNIQUE_KEY_PREFIX  = "Achievements_Unique_";  // 고유 항목 집합 업적(라디오 5개 등) 저장용

        // 씬에 직접 배치할 때만 인스펙터로 지정. 비어 있으면 Resources에서 자동 로드.
        [SerializeField] private AchievementDatabase _database;

        private readonly HashSet<string> _unlocked = new HashSet<string>();

        public event Action<AchievementData> OnAchievementUnlocked;

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 깨끗하게 초기화되도록.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        // 씬에 배치하지 않아도 동작하도록 씬 로드 후(Awake 이후, Start 이전) 자동 생성.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject("AchievementManager").AddComponent<AchievementManager>();
            }
        }

        protected override void OnSingletonAwake()
        {
            if (_database == null)
            {
                _database = Resources.Load<AchievementDatabase>(AchievementDatabase.RESOURCES_PATH);
                if (_database == null)
                {
                    Debug.LogError($"[AchievementManager] AchievementDatabase를 찾을 수 없습니다 (Resources/{AchievementDatabase.RESOURCES_PATH})");
                }
            }

            Load();
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 해당 id의 업적이 이미 해제되었는지 확인. </summary>
        public bool IsUnlocked(string id)
        {
            return _unlocked.Contains(id);
        }

        /// <summary> 업적 해제. 이미 해제됐거나 데이터베이스에 없는 id면 무시. </summary>
        public void Unlock(string id)
        {
            if (_unlocked.Contains(id))
            {
                return; // 이미 해제됨 - 중복 토스트/저장 방지
            }

            if ((_database == null) || !_database.TryGet(id, out AchievementData data))
            {
                Debug.LogWarning($"[AchievementManager] 등록되지 않은 업적 id: {id}");
                return;
            }

            _unlocked.Add(id);
            Save();
            BackendMirror.Instance?.MirrorAchievement(id); // 로컬 저장 후 서버 미러(닉네임 없으면 자동 스킵)
            OnAchievementUnlocked?.Invoke(data);
        }

        /// <summary>
        /// 누적 횟수 기반 업적 진행. counterKey(예: "death_count")의 누적 횟수를 PlayerPrefs에 저장하고,
        /// target에 도달하면 achievementId를 해제한다 (사망 100회/되감기 100회 등). 이미 해제된 업적이면 카운트하지 않는다.
        /// </summary>
        public void IncrementProgress(string counterKey, int target, string achievementId)
        {
            if (IsUnlocked(achievementId))
            {
                return; // 이미 해제됨 - 더 셀 필요 없음
            }

            int count = IncrementCounter(counterKey);
            if (count >= target)
            {
                Unlock(achievementId);
            }
        }

        /// <summary>
        /// counterKey의 누적 횟수를 1 증가시키고 PlayerPrefs에 저장한다. 목표 도달 판정 없이 순수 증가만 필요할 때
        /// (예: 클리어 시점에야 판정하는 "보스에게 500번 죽고도 클리어" 업적) 사용한다.
        /// </summary>
        public int IncrementCounter(string counterKey)
        {
            string prefKey = COUNTER_KEY_PREFIX + counterKey;
            int count = PlayerPrefs.GetInt(prefKey, 0) + 1;
            PlayerPrefs.SetInt(prefKey, count);
            PlayerPrefs.Save();
            return count;
        }

        /// <summary> counterKey의 현재 누적 횟수를 증가 없이 조회한다. </summary>
        public int GetCounter(string counterKey)
        {
            return PlayerPrefs.GetInt(COUNTER_KEY_PREFIX + counterKey, 0);
        }

        /// <summary>
        /// 고유 항목 누적 기반 업적 진행 (예: 라디오 5개를 각각 들어야 하는 경우). groupKey(예: "radio")의
        /// 집합에 itemId를 추가하고, 서로 다른 항목이 target개에 도달하면 achievementId를 해제한다.
        /// 같은 itemId를 여러 번 넣어도 한 번만 카운트된다. 이미 해제된 업적이면 무시.
        /// </summary>
        public void MarkUniqueProgress(string groupKey, string itemId, int target, string achievementId)
        {
            if (IsUnlocked(achievementId) || string.IsNullOrEmpty(itemId))
            {
                return;
            }

            string prefKey = UNIQUE_KEY_PREFIX + groupKey;
            string json = PlayerPrefs.GetString(prefKey, "");
            UniqueSaveData save = string.IsNullOrEmpty(json)
                ? new UniqueSaveData { Ids = new List<string>() }
                : JsonUtility.FromJson<UniqueSaveData>(json);

            if (!save.Ids.Contains(itemId))
            {
                save.Ids.Add(itemId);
                PlayerPrefs.SetString(prefKey, JsonUtility.ToJson(save));
                PlayerPrefs.Save();
            }

            if (save.Ids.Count >= target)
            {
                Unlock(achievementId);
            }
        }

        /// <summary> 해제된 업적 기록을 전부 제거 (설정 - 데이터 초기화). 로컬만 지운다 - 서버 삭제는 BackendMirror.MirrorClearAchievements가 담당. </summary>
        public void ClearAll()
        {
            _unlocked.Clear();
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
        }

        // PlayerPrefs에 JSON으로 넣기 위한 직렬화 래퍼 (JsonUtility는 컬렉션 루트를 지원하지 않음).
        [Serializable]
        private class SaveData
        {
            public List<string> UnlockedIds;
        }

        // MarkUniqueProgress가 groupKey별 고유 항목 집합을 저장하는 래퍼.
        [Serializable]
        private class UniqueSaveData
        {
            public List<string> Ids;
        }

        // 해제 목록 전체를 PlayerPrefs에 저장. 해제는 드문 이벤트라 매번 전체 저장해도 부담 없음.
        private void Save()
        {
            SaveData save = new SaveData { UnlockedIds = new List<string>(_unlocked) };
            PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        // PlayerPrefs에서 해제 목록 복원. 저장 기록이 없으면 아무것도 하지 않는다.
        private void Load()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            SaveData save = JsonUtility.FromJson<SaveData>(json);
            if (save?.UnlockedIds != null)
            {
                _unlocked.UnionWith(save.UnlockedIds);
            }
        }
    }
}
