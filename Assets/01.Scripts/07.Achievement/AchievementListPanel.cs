// System
using System.Collections.Generic;

// Unity
using UnityEngine;

// 서드파티
using TMPro;

// 프로젝트 내부 (민성 네임스페이스)
using Minsung.Common;

namespace Minsung.Achievement
{
    // 업적 목록 패널 - 전체 업적을 깬 것/안 깬 것 구분해 보여준다, 로비 '업적' 버튼에서 연다
    [AddComponentMenu("Minsung/UI/Achievement List Panel")]
    public class AchievementListPanel : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private Transform              _content;
        [SerializeField] private AchievementListItemUI  _itemPrefab;
        [SerializeField] private TMP_Text               _progressText; // 예: "3 / 10"

        private AchievementDatabase _database;
        private readonly List<AchievementListItemUI> _spawned = new List<AchievementListItemUI>();

        /****************************************
        *              Unity Event
        ****************************************/

        // 패널이 열릴 때마다 최신 해제 상태를 반영해 다시 그린다.
        private void OnEnable()
        {
            Refresh();
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 닫기 버튼 - 설정 패널과 동일하게 자체적으로 닫고 캡처해둔 블러 배경을 반납한다 </summary>
        public void OnClickClose()
        {
            gameObject.SetActive(false);
            PauseController.Instance?.ReleaseCapturedSettingsBackdrop();
        }

        private void Refresh()
        {
            if (_database == null)
            {
                _database = Resources.Load<AchievementDatabase>(AchievementDatabase.RESOURCES_PATH);
            }

            if ((_database == null) || (_content == null) || (_itemPrefab == null))
            {
                Debug.LogWarning("[AchievementListPanel] 데이터베이스/컨텐츠/아이템 프리팹 참조가 비어 있습니다.");
                return;
            }

            ClearSpawned();

            int unlockedCount = 0;
            foreach (AchievementData data in _database.Achievements)
            {
                if (data == null)
                {
                    continue;
                }

                bool unlocked = (AchievementManager.Instance != null) && AchievementManager.Instance.IsUnlocked(data.Id);
                if (unlocked)
                {
                    unlockedCount++;
                }

                AchievementListItemUI item = Instantiate(_itemPrefab, _content);
                item.Bind(data, unlocked);
                _spawned.Add(item);
            }

            if (_progressText != null)
            {
                _progressText.text = $"{unlockedCount} / {_database.Achievements.Count}";
            }
        }

        private void ClearSpawned()
        {
            foreach (AchievementListItemUI item in _spawned)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _spawned.Clear();
        }
    }
}
