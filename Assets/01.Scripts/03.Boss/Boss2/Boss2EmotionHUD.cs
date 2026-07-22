// System
using System;

// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.Boss2
{
    // 부유 보스(Boss2) 감정을 두 곳에 표시 - Minsung.Boss.BossEmotionHUD를 그대로 본떴다
    // 감정 아이콘(체력바 좌하단, 항상 표시) / 반사 아이콘(머리 위, 반사 감정일 때만)
    // Boss2는 "전투 시작" 개념이 원본처럼 별도 컷신으로 나뉘어 있지 않아(스폰 == 전투 시작) BossController.IsBattleStarted 게이팅은 뺐다
    public class Boss2EmotionHUD : MonoBehaviour
    {
    #pragma warning disable CS0649 // Unity serializes these Inspector mapping fields.
        [Serializable]
        private struct EmotionIconEntry
        {
            public Boss2Emotion Emotion;
            public Sprite Icon;
        }
    #pragma warning restore CS0649

        [SerializeField] private Boss2EmotionController _emotionController;
        [SerializeField] private Image _emotionIcon;          // 체력바 - 현재 감정 항상 표시
        [SerializeField] private SpriteRenderer _reflectIcon; // 머리 위 - 반사 3종만 표시

        // 감정 -> 체력바 아이콘 스프라이트 (전체 감정)
        [SerializeField] private EmotionIconEntry[] _icons = Array.Empty<EmotionIconEntry>();

        // 반사 감정 -> 머리 위 아이콘 스프라이트 (Black/White/Navy)
        [SerializeField] private EmotionIconEntry[] _reflectIcons = Array.Empty<EmotionIconEntry>();

        private Boss2EmotionController _subscribedEmotionController;

        // OnEnable 대신 Start를 쓴다 - Boss2EmotionController는 Boss2AttackPatterns.Awake()에서 동적으로 AddComponent되는데,
        // 같은 오브젝트 안에서도 OnEnable 호출 순서는 보장되지 않아(실제로 컴포넌트가 아직 없을 때 먼저 불린 적이 있었다)
        // 모든 Awake 이후에 실행이 보장되는 Start에서 구독해야 안전하다
        private void Start()
        {
            _subscribedEmotionController = ResolveEmotionController();
            if (_subscribedEmotionController == null)
            {
                HideAll();
                return;
            }

            EnsureTooltip(_subscribedEmotionController);
            _subscribedEmotionController.OnEmotionChanged += Redraw;
            Redraw(_subscribedEmotionController.CurrentEmotion);
        }

        private void OnDestroy()
        {
            if (_subscribedEmotionController != null)
            {
                _subscribedEmotionController.OnEmotionChanged -= Redraw;
                _subscribedEmotionController = null;
            }
        }

        private Boss2EmotionController ResolveEmotionController()
        {
            if (_emotionController != null)
            {
                return _emotionController;
            }

            return GetComponentInParent<Boss2EmotionController>();
        }

        private void EnsureTooltip(Boss2EmotionController emotionController)
        {
            if (_emotionIcon == null)
            {
                return;
            }

            if (!_emotionIcon.TryGetComponent(out Minsung.UI.Boss2EmotionIconTooltip tooltip))
            {
                tooltip = _emotionIcon.gameObject.AddComponent<Minsung.UI.Boss2EmotionIconTooltip>();
            }

            tooltip.Configure(emotionController);
        }

        private void Redraw(Boss2Emotion emotion)
        {
            DrawEmotion(emotion);
            DrawReflect(emotion);
        }

        // 체력바 감정 아이콘 - 매핑된 감정이면 표시, 아니면 숨김
        private void DrawEmotion(Boss2Emotion emotion)
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
        private void DrawReflect(Boss2Emotion emotion)
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

        private static bool TryFindIcon(EmotionIconEntry[] icons, Boss2Emotion emotion, out Sprite icon)
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
