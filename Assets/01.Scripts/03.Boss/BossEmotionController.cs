// System
using System;
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Player;

namespace Minsung.Boss
{
    // 보스 리와인드 프레임에 포함할 감정의 최소 상태
    public struct BossEmotionSnapshot
    {
        public BossEmotion Emotion;
        public int EmotionCursor;
    }

    // 보스 간에 공유하는 감정 상태, 결정 로그, 부가 효과 컴포넌트
    public class BossEmotionController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private PlayerController _player;
        private HeartPickup _heartPickup;
        private BossDataSO _bossSo;
        private Func<bool> _isTransitioning;

        private float _arenaMinX;
        private float _arenaMaxX;
        private float _arenaGroundY;

        private BossEmotion _currentEmotion = BossEmotion.None;
        private bool _autoEmotionSuspended;
        private Coroutine _emotionLoop;
        private Coroutine _confusionRoutine;
        private WaitForSeconds _waitEmotionInterval;
        private WaitForSeconds _waitConfusionInterval;
        private WaitForSeconds _waitConfusionDuration;
        private readonly List<BossEmotion> _emotionLog = new List<BossEmotion>();
        private int _emotionCursor;

        public BossEmotion CurrentEmotion => _currentEmotion;
        public event Action<BossEmotion> OnEmotionChanged;

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnDisable()
        {
            StopEmotionLoop();
            StopConfusion();
        }

        /****************************************
        *             Configuration
        ****************************************/

        // 각 보스가 자신의 플레이어, 픽업, 밸런싱 DB, 아레나 문맥을 주입한다
        public void Configure(PlayerController player, HeartPickup heartPickup, BossDataSO bossSo,
            float arenaMinX, float arenaMaxX, float arenaGroundY, Func<bool> isTransitioning)
        {
            _player          = player;
            _heartPickup     = heartPickup;
            _bossSo          = bossSo;
            _arenaMinX       = arenaMinX;
            _arenaMaxX       = arenaMaxX;
            _arenaGroundY    = arenaGroundY;
            _isTransitioning = isTransitioning;

            if (_bossSo == null)
            {
                return;
            }

            _waitEmotionInterval   = new WaitForSeconds(_bossSo.EmotionInterval);
            _waitConfusionInterval = new WaitForSeconds(_bossSo.ConfusionInterval);
            _waitConfusionDuration = new WaitForSeconds(_bossSo.ConfusionDuration);
        }

        /****************************************
        *              Public API
        ****************************************/

        public void StartEmotionLoop(bool applyImmediately = false)
        {
            if ((_bossSo == null) || (_emotionLoop != null))
            {
                return;
            }

            _emotionLoop = StartCoroutine(CoEmotionLoop(applyImmediately));
        }

        public void StopEmotionLoop()
        {
            if (_emotionLoop != null)
            {
                StopCoroutine(_emotionLoop);
                _emotionLoop = null;
            }
        }

        public void SetAutoEmotionSuspended(bool suspended)
        {
            _autoEmotionSuspended = suspended;
        }

        public void SetEmotion(BossEmotion emotion)
        {
            if (_currentEmotion == emotion)
            {
                return;
            }

            BossEmotion previous = _currentEmotion;
            _currentEmotion = emotion;

            if (previous == BossEmotion.Angry)
            {
                StopConfusion();
            }

            if (emotion == BossEmotion.Angry)
            {
                _confusionRoutine = StartCoroutine(CoConfusionLoop());
            }

            if (emotion == BossEmotion.Blue)
            {
                SpawnHeartPickup();
            }

            OnEmotionChanged?.Invoke(emotion);
        }

        public bool ReflectIfNeeded(DamageSource source, PlayerHealth attacker)
        {
            if (!_currentEmotion.ShouldReflect(source))
            {
                return false;
            }

            if ((attacker != null) && (_bossSo != null))
            {
                attacker.TakeDamageHalves(_bossSo.ReflectHalves);
            }

            return true;
        }

        public BossEmotionSnapshot Capture()
        {
            return new BossEmotionSnapshot
            {
                Emotion = _currentEmotion,
                EmotionCursor = _emotionCursor,
            };
        }

        // 리와인드 복원은 혼란, 하트 생성 같은 부가 효과 없이 표시 상태만 되돌린다
        public void Restore(BossEmotionSnapshot snapshot)
        {
            _emotionCursor = snapshot.EmotionCursor;
            if (_currentEmotion == snapshot.Emotion)
            {
                return;
            }

            _currentEmotion = snapshot.Emotion;
            OnEmotionChanged?.Invoke(_currentEmotion);
        }

        public void ResetState()
        {
            StopEmotionLoop();
            StopConfusion();
            _autoEmotionSuspended = false;
            _emotionLog.Clear();
            _emotionCursor = 0;
            SetEmotion(BossEmotion.None);

            if (_heartPickup != null)
            {
                _heartPickup.gameObject.SetActive(false);
            }
        }

        /****************************************
        *             Emotion Loop
        ****************************************/

        private IEnumerator CoEmotionLoop(bool applyImmediately)
        {
            if (applyImmediately && !_autoEmotionSuspended)
            {
                SetEmotion(GetOrMakeAutoEmotion());
            }

            while (true)
            {
                yield return _waitEmotionInterval;
                if (IsTransitioning() || _autoEmotionSuspended)
                {
                    continue;
                }

                SetEmotion(GetOrMakeAutoEmotion());
            }
        }

        private BossEmotion GetOrMakeAutoEmotion()
        {
            if (_emotionCursor < _emotionLog.Count)
            {
                BossEmotion logged = _emotionLog[_emotionCursor];
                ++_emotionCursor;
                return logged;
            }

            BossEmotion emotion = RandomAutoEmotion();
            _emotionLog.Add(emotion);
            ++_emotionCursor;
            return emotion;
        }

        private static BossEmotion RandomAutoEmotion()
        {
            return (BossEmotion)UnityEngine.Random.Range(1, (int)BossEmotion.Angry);
        }

        /****************************************
        *             Side Effects
        ****************************************/

        private void StopConfusion()
        {
            if (_confusionRoutine != null)
            {
                StopCoroutine(_confusionRoutine);
                _confusionRoutine = null;
            }

            if (_player == null)
            {
                return;
            }

            if (_player.StatusEffects != null)
            {
                _player.StatusEffects.Remove(StatusEffectType.InputInvert);
                return;
            }

            _player.SetInputInverted(false);
        }

        private IEnumerator CoConfusionLoop()
        {
            while (true)
            {
                yield return _waitConfusionInterval;
                if (_player == null)
                {
                    continue;
                }

                if (_player.StatusEffects != null)
                {
                    _player.StatusEffects.Apply(StatusEffectType.InputInvert, _bossSo.ConfusionDuration);
                }
                else
                {
                    _player.SetInputInverted(true);
                }

                yield return _waitConfusionDuration;
                if ((_player != null) && (_player.StatusEffects == null))
                {
                    _player.SetInputInverted(false);
                }
            }
        }

        private void SpawnHeartPickup()
        {
            if ((_heartPickup == null) || (_bossSo == null))
            {
                return;
            }

            float x = UnityEngine.Random.Range(_arenaMinX, _arenaMaxX);
            _heartPickup.transform.position = new Vector3(x, _arenaGroundY + _bossSo.HeartPickupHeight, 0f);
            _heartPickup.gameObject.SetActive(true);
        }

        private bool IsTransitioning()
        {
            return (_isTransitioning != null) && _isTransitioning.Invoke();
        }
    }
}
