// System
using System;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Utility;

namespace Minsung.Achievement
{
    // 업적 해제 상태 관리 싱글톤.
    public class AchievementManager : PersistentSingleton<AchievementManager>
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string SAVE_KEY = "Achievements_UnlockedIds";

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
            OnAchievementUnlocked?.Invoke(data);
        }

        // PlayerPrefs에 JSON으로 넣기 위한 직렬화 래퍼 (JsonUtility는 컬렉션 루트를 지원하지 않음).
        [Serializable]
        private class SaveData
        {
            public List<string> UnlockedIds;
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
