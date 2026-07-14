// Unity
using UnityEngine;

namespace Minsung.Achievement
{
    /// <summary> 업적 정보 </summary>
    [CreateAssetMenu(fileName = "Achievement_", menuName = "Achievement System/Achievement Data", order = 1)]
    public class AchievementData : ScriptableObject
    {
        public string Id => _id;
        public string Title => _title;
        public string Description => _description;
        public Sprite Icon => _icon;

        [SerializeField] private string _id; // AchievementManager.Unlock(id)/AchievementIds와 일치해야 함
        [SerializeField] private string _title;
        [Multiline]
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
    }
}
