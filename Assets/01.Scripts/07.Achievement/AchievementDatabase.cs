// System
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace Minsung.Achievement
{
    // 게임의 모든 업적 목록을 담는 카탈로그 에셋.
    [CreateAssetMenu(fileName = "AchievementDatabase", menuName = "Achievement System/Achievement Database", order = 0)]
    public class AchievementDatabase : ScriptableObject
    {
        /****************************************
        *                Fields
        ****************************************/

        public const string RESOURCES_PATH = "AchievementDatabase";

        [SerializeField] private AchievementData[] _achievements;

        private Dictionary<string, AchievementData> _byId;

        public IReadOnlyList<AchievementData> Achievements => _achievements;

        /****************************************
        *                Methods
        ****************************************/

        public bool TryGet(string id, out AchievementData data)
        {
            if (_byId == null)
            {
                BuildIndex();
            }
            return _byId.TryGetValue(id, out data);
        }

        private void BuildIndex()
        {
            _byId = new Dictionary<string, AchievementData>();
            foreach (AchievementData achievement in _achievements)
            {
                if ((achievement == null) || string.IsNullOrEmpty(achievement.Id))
                {
                    Debug.LogWarning($"[AchievementDatabase] 비어 있는 항목 또는 Id 없는 업적이 있습니다: {name}", this);
                    continue;
                }
                if (_byId.ContainsKey(achievement.Id))
                {
                    Debug.LogWarning($"[AchievementDatabase] 중복된 업적 Id: {achievement.Id}", this);
                    continue;
                }
                _byId.Add(achievement.Id, achievement);
            }
        }
    }
}
