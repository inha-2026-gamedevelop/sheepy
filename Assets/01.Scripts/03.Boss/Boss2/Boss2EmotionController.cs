// System
using System;
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Player;
using Minsung.Boss;

// 부유 보스(Boss2)가 공유하는 감정 상태/결정 로그/부가 효과 - Minsung.Boss.BossEmotionController를 그대로 본떴다
// Configure가 BossDataSO 대신 Boss2DataSO를 받는다는 점만 다름(GameDB에 연결되지 않은 독립 SO라서 원본을 그대로 재사용할 수 없었음)
// HeartPickup(Minsung.Boss)/PlayerController/PlayerStatusEffectController는 BossController에 묶여 있지 않아 그대로 재사용
public class Boss2EmotionController : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/

    private PlayerController _player;
    private HeartPickup      _heartPickup;
    private Boss2DataSO      _dataSo;
    private Func<bool>       _isTransitioning;

    private float _arenaMinX;
    private float _arenaMaxX;
    private float _arenaGroundY;

    private Boss2Emotion _currentEmotion = Boss2Emotion.None;
    private bool _autoEmotionSuspended;
    private Coroutine _emotionLoop;
    private Coroutine _confusionRoutine;
    private WaitForSeconds _waitEmotionInterval;
    private WaitForSeconds _waitConfusionInterval;
    private WaitForSeconds _waitConfusionDuration;
    private readonly List<Boss2Emotion> _emotionLog = new List<Boss2Emotion>();
    private int _emotionCursor;

    public Boss2Emotion CurrentEmotion => _currentEmotion;
    public event Action<Boss2Emotion> OnEmotionChanged;

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

    // Boss2AttackPatterns.Start()가 자신의 플레이어/픽업/밸런싱 DB/아레나 문맥을 주입한다
    public void Configure(PlayerController player, HeartPickup heartPickup, Boss2DataSO dataSo,
        float arenaMinX, float arenaMaxX, float arenaGroundY, Func<bool> isTransitioning)
    {
        _player          = player;
        _heartPickup     = heartPickup;
        _dataSo          = dataSo;
        _arenaMinX       = arenaMinX;
        _arenaMaxX       = arenaMaxX;
        _arenaGroundY    = arenaGroundY;
        _isTransitioning = isTransitioning;

        if (_dataSo == null)
        {
            return;
        }

        _waitEmotionInterval   = new WaitForSeconds(_dataSo.EmotionInterval);
        _waitConfusionInterval = new WaitForSeconds(_dataSo.ConfusionInterval);
        _waitConfusionDuration = new WaitForSeconds(_dataSo.ConfusionDuration);
    }

    /****************************************
    *              Public API
    ****************************************/

    public void StartEmotionLoop(bool applyImmediately = false)
    {
        if ((_dataSo == null) || (_emotionLoop != null))
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

    public void SetEmotion(Boss2Emotion emotion)
    {
        if (_currentEmotion == emotion)
        {
            return;
        }

        Boss2Emotion previous = _currentEmotion;
        _currentEmotion = emotion;

        if (previous == Boss2Emotion.Angry)
        {
            StopConfusion();
        }

        if (emotion == Boss2Emotion.Angry)
        {
            _confusionRoutine = StartCoroutine(CoConfusionLoop());
        }

        if (emotion == Boss2Emotion.Blue)
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

        if ((attacker != null) && (_dataSo != null))
        {
            attacker.TakeDamageHalves(_dataSo.ReflectHalves);
        }

        return true;
    }

    /// <summary> 낙뢰 패턴이 매 사이클 호출 - 현재 감정에 따른 발생 간격 배율 </summary>
    public float LightningRateMultiplier()
    {
        return (_dataSo != null) ? _currentEmotion.LightningRateMultiplier(_dataSo) : 1f;
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

    private Boss2Emotion GetOrMakeAutoEmotion()
    {
        if (_emotionCursor < _emotionLog.Count)
        {
            Boss2Emotion logged = _emotionLog[_emotionCursor];
            ++_emotionCursor;
            return logged;
        }

        Boss2Emotion emotion = RandomAutoEmotion();
        _emotionLog.Add(emotion);
        ++_emotionCursor;
        return emotion;
    }

    private static Boss2Emotion RandomAutoEmotion()
    {
        return (Boss2Emotion)UnityEngine.Random.Range(1, (int)Boss2Emotion.Angry);
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
                _player.StatusEffects.Apply(StatusEffectType.InputInvert, _dataSo.ConfusionDuration);
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
        if ((_heartPickup == null) || (_dataSo == null))
        {
            return;
        }

        float x = UnityEngine.Random.Range(_arenaMinX, _arenaMaxX);
        _heartPickup.transform.position = new Vector3(x, _arenaGroundY + _dataSo.HeartPickupHeight, 0f);
        _heartPickup.gameObject.SetActive(true);
    }

    private bool IsTransitioning()
    {
        return (_isTransitioning != null) && _isTransitioning.Invoke();
    }
}
