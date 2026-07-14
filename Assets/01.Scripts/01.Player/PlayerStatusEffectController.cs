// System
using System;

// Unity
using UnityEngine;

namespace Minsung.Player
{
    public enum StatusEffectType
    {
        Bind,
        InputInvert,
        RewindSeal,
    }

    // 플레이어에게 적용되는 지속시간형 디버프를 한 곳에서 관리한다.
    // 상태이상 시간은 리와인드 스냅샷에 포함하지 않고 현재 게임 시간 기준으로 계속 흐른다.
    public class PlayerStatusEffectController : MonoBehaviour
    {
        private PlayerController _playerController;
        private PlayerMovement   _movement;

        private float[] _remainingDurations;
        private bool[]  _activeEffects;

        public bool IsRewindSealed => IsActive(StatusEffectType.RewindSeal);

        // type, 활성 여부, 남은 시간
        public event Action<StatusEffectType, bool, float> OnEffectChanged;

        private void Awake()
        {
            int count = Enum.GetValues(typeof(StatusEffectType)).Length;
            _remainingDurations = new float[count];
            _activeEffects      = new bool[count];
        }

        public void Init(PlayerController playerController, PlayerMovement movement)
        {
            _playerController = playerController;
            _movement         = movement;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _activeEffects.Length; ++i)
            {
                if (!_activeEffects[i])
                {
                    continue;
                }

                _remainingDurations[i] -= deltaTime;
                if (_remainingDurations[i] <= 0f)
                {
                    Remove((StatusEffectType)i);
                }
            }
        }

        private void OnDisable()
        {
            ClearAll();
        }

        public void Apply(StatusEffectType type, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            int index = (int)type;
            float previousRemaining = _remainingDurations[index];
            float nextRemaining = Mathf.Max(previousRemaining, duration);
            bool wasActive = _activeEffects[index];

            _remainingDurations[index] = nextRemaining;
            _activeEffects[index]      = true;

            switch (type)
            {
                case StatusEffectType.Bind:
                    _movement?.ApplyStun(nextRemaining);
                    break;
                case StatusEffectType.InputInvert:
                    _playerController?.SetInputInverted(true);
                    break;
                case StatusEffectType.RewindSeal:
                    break;
            }

            if (!wasActive || !Mathf.Approximately(previousRemaining, nextRemaining))
            {
                OnEffectChanged?.Invoke(type, true, nextRemaining);
            }
        }

        public void Remove(StatusEffectType type)
        {
            int index = (int)type;
            if (!_activeEffects[index])
            {
                return;
            }

            _activeEffects[index]      = false;
            _remainingDurations[index] = 0f;

            if (type == StatusEffectType.InputInvert)
            {
                _playerController?.SetInputInverted(false);
            }

            OnEffectChanged?.Invoke(type, false, 0f);
        }

        public void ClearAll()
        {
            if (_activeEffects == null)
            {
                return;
            }

            for (int i = 0; i < _activeEffects.Length; ++i)
            {
                if (_activeEffects[i])
                {
                    Remove((StatusEffectType)i);
                }
            }
        }

        public bool IsActive(StatusEffectType type)
        {
            return (_activeEffects != null) && _activeEffects[(int)type];
        }

        public float GetRemainingDuration(StatusEffectType type)
        {
            return (_remainingDurations != null) ? _remainingDurations[(int)type] : 0f;
        }
    }
}
