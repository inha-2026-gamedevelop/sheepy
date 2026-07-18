// System
using System;

// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.Boss
{
    // 보스 감정을 두 곳에 표시 - 감정 아이콘(체력바 좌하단, 항상 표시) / 반사 아이콘(머리 위, 반사 감정일 때만)
    public class BossEmotionHUD : MonoBehaviour
    {
#pragma warning disable CS0649 // Unity serializes these Inspector mapping fields.
        [Serializable]
        private struct EmotionIconEntry
        {
            public BossEmotion Emotion;
            public Sprite Icon;
        }
#pragma warning restore CS0649

        [SerializeField] private BossEmotionController _emotionController;
        [SerializeField] private BossController _boss;
        [SerializeField] private Image _emotionIcon;          // 체력바 - 현재 감정 항상 표시
        [SerializeField] private SpriteRenderer _reflectIcon; // 머리 위 - 반사 3종만 표시

        // 감정 -> 체력바 아이콘 스프라이트 (전체 감정)
        [SerializeField] private EmotionIconEntry[] _icons = Array.Empty<EmotionIconEntry>();

        // 반사 감정 -> 머리 위 아이콘 스프라이트 (Black/White/Navy)
        [SerializeField] private EmotionIconEntry[] _reflectIcons = Array.Empty<EmotionIconEntry>();

        private BossEmotionController _subscribedEmotionController;

        private void OnEnable()
        {
            _subscribedEmotionController = ResolveEmotionController();
            if (_subscribedEmotionController == null)
            {
                HideAll();
                return;
            }

            _subscribedEmotionController.OnEmotionChanged += Redraw;
            Redraw(_subscribedEmotionController.CurrentEmotion);
        }

        private void OnDisable()
        {
            if (_subscribedEmotionController != null)
            {
                _subscribedEmotionController.OnEmotionChanged -= Redraw;
                _subscribedEmotionController = null;
            }
        }

        private BossEmotionController ResolveEmotionController()
        {
            if (_emotionController != null)
            {
                return _emotionController;
            }

            return _boss != null ? _boss.EmotionController : null;
        }

        private void Redraw(BossEmotion emotion)
        {
            DrawEmotion(emotion);
            DrawReflect(emotion);
        }

        // 체력바 감정 아이콘 - 매핑된 감정이면 표시, 아니면 숨김
        private void DrawEmotion(BossEmotion emotion)
        {
            if (_emotionIcon == null)
            {
                return;
            }

            if (TryFindIcon(_icons, emotion, out Sprite icon))
            {
                _emotionIcon.sprite  = icon;
                _emotionIcon.enabled = true;
                return;
            }

            _emotionIcon.sprite  = null;
            _emotionIcon.enabled = false;
        }

        // 머리 위 반사 아이콘 - 반사 감정일 때만 표시
        private void DrawReflect(BossEmotion emotion)
        {
            if (_reflectIcon == null)
            {
                return;
            }

            if (emotion.IsReflect() && TryFindIcon(_reflectIcons, emotion, out Sprite icon))
            {
                _reflectIcon.sprite  = icon;
                _reflectIcon.enabled = true;
                return;
            }

            _reflectIcon.sprite  = null;
            _reflectIcon.enabled = false;
        }

        private static bool TryFindIcon(EmotionIconEntry[] icons, BossEmotion emotion, out Sprite icon)
        {
            for (int i = 0; i < icons.Length; ++i)
            {
                if ((icons[i].Emotion == emotion) && (icons[i].Icon != null))
                {
                    icon = icons[i].Icon;
                    return true;
                }
            }

            icon = null;
            return false;
        }

        private void HideAll()
        {
            if (_emotionIcon != null)
            {
                _emotionIcon.sprite  = null;
                _emotionIcon.enabled = false;
            }

            if (_reflectIcon != null)
            {
                _reflectIcon.sprite  = null;
                _reflectIcon.enabled = false;
            }
        }
    }
}
