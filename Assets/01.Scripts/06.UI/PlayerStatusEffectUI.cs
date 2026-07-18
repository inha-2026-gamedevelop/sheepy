// System
using System;

// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Player;

namespace Minsung.UI
{
    // 활성 상태이상을 왼쪽 슬롯부터 순서대로 표시한다.
    public class PlayerStatusEffectUI : MonoBehaviour
    {
#pragma warning disable CS0649
        [Serializable]
        private struct EffectIconEntry
        {
            public StatusEffectType Type;
            public Sprite Icon;
        }
#pragma warning restore CS0649

        [SerializeField] private PlayerStatusEffectController _statusEffects;
        [SerializeField] private Image[] _iconSlots = Array.Empty<Image>();
        [SerializeField] private EffectIconEntry[] _iconMap = Array.Empty<EffectIconEntry>();

        private void OnEnable()
        {
            if (_statusEffects == null)
            {
                Redraw();
                return;
            }

            _statusEffects.OnEffectChanged += HandleEffectChanged;
            Redraw();
        }

        private void OnDisable()
        {
            if (_statusEffects != null)
            {
                _statusEffects.OnEffectChanged -= HandleEffectChanged;
            }
        }

        // 인스펙터 미지정이면 씬의 본체에서 자동 연결
        private void Start()
        {
            if (_statusEffects == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    _statusEffects = player.StatusEffects;
                    if (_statusEffects != null)
                    {
                        _statusEffects.OnEffectChanged += HandleEffectChanged;
                    }
                }
            }
            Redraw();
        }

        private void HandleEffectChanged(StatusEffectType type, bool active, float remainingDuration)
        {
            Redraw();
        }

        private void Redraw()
        {
            int slotIndex = 0;

            if (_statusEffects != null)
            {
                for (int i = 0; i < _iconMap.Length && slotIndex < _iconSlots.Length; ++i)
                {
                    if (!_statusEffects.IsActive(_iconMap[i].Type) || (_iconMap[i].Icon == null))
                    {
                        continue;
                    }

                    Image slot = _iconSlots[slotIndex];
                    ++slotIndex;
                    if (slot != null)
                    {
                        slot.sprite  = _iconMap[i].Icon;
                        slot.enabled = true;
                    }
                }
            }

            for (int i = slotIndex; i < _iconSlots.Length; ++i)
            {
                if (_iconSlots[i] != null)
                {
                    _iconSlots[i].sprite  = null;
                    _iconSlots[i].enabled = false;
                }
            }
        }
    }
}
