// Unity
using UnityEngine;
using UnityEngine.UI;

// 서드파티
using TMPro;

namespace Minsung.Achievement
{
    // 업적 목록 패널의 항목 1개 - 잠긴 업적은 제목만 보이고 설명은 ???로 가려진다
    [AddComponentMenu("Minsung/UI/Achievement List Item UI")]
    public class AchievementListItemUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string LOCKED_DESCRIPTION = "???";

        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Image    _lockedIcon;   // 잠김 표시(자물쇠 등) - 미지정 시 생략
        [SerializeField] private Image    _unlockedIcon; // 해제 표시(체크 등) - 미지정 시 생략

        [Header("잠김 상태 톤다운")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float       _lockedAlpha = 0.5f;

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 업적 데이터를 항목에 반영. 잠긴 업적은 설명을 ???로 가린다 </summary>
        public void Bind(AchievementData data, bool unlocked)
        {
            if (_titleText != null)
            {
                _titleText.text = data.Title;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = unlocked ? data.Description : LOCKED_DESCRIPTION;
            }

            if (_lockedIcon != null)
            {
                _lockedIcon.enabled = !unlocked;
            }

            if (_unlockedIcon != null)
            {
                _unlockedIcon.enabled = unlocked;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = unlocked ? 1f : _lockedAlpha;
            }
        }
    }
}
