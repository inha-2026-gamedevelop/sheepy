// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Boss;

// 부유 보스(Boss2) 낙뢰 패턴 - Minsung.Boss.BossLightningPattern 구조를 참고해 이식.
// BossHazardPool/DamageHazard(Minsung.Boss)는 BossController에 묶여 있지 않아 그대로 재사용한다(수정 없음)
// 기본 간격마다 플레이어 주변 x에 노란 장판 예고 후 즉발로 내리친다
// TODO: 리와인드 타임라인 미연동 - 결정 로그(x값) 기록/재현 미구현
public class Boss2LightningPattern
{
    /****************************************
    *                Fields
    ****************************************/

    private const int POOL_SIZE = 4; // 동시 사용 최대 슬롯 수 (예고 + 강타 + 여유분)

    private readonly MonoBehaviour _owner;
    private readonly Transform     _target;
    private readonly Boss2DataSO   _dataSo;
    private readonly float         _arenaMinX;
    private readonly float         _arenaMaxX;
    private readonly System.Func<float, float> _getGroundY; // x -> 가장 가까운 지면 마커 Y (Boss2AttackPatterns.GetGroundY)
    private readonly System.Func<float> _getRateMultiplier; // 감정(Pink/Blue)에 따른 발생 간격 배율 - 미연결 시 항상 1배

    private readonly BossHazardPool _pool;
    private Coroutine _loop;

    // 감정별 배율은 Pink(x2)/Blue(x0.5)/그 외(x1) 3가지뿐이라 WaitForSeconds를 미리 캐싱해 GC를 피한다
    private readonly WaitForSeconds _waitIntervalNormal;
    private readonly WaitForSeconds _waitIntervalPink;
    private readonly WaitForSeconds _waitIntervalBlue;
    private readonly WaitForSeconds _waitTelegraph;
    private readonly WaitForSeconds _waitActive;
    private readonly WaitForSeconds _waitFrame;

    private readonly Sprite[] _strikeSprites;
    private readonly float    _frameInterval;
    private readonly bool     _cycleFrames;
    private readonly int      _strikeSpritesCount;

    /****************************************
    *              Constructor
    ****************************************/

    public Boss2LightningPattern(MonoBehaviour owner, Transform target, Boss2DataSO dataSo,
        float arenaMinX, float arenaMaxX, System.Func<float, float> getGroundY, System.Func<float> getRateMultiplier = null)
    {
        _owner             = owner;
        _target            = target;
        _dataSo            = dataSo;
        _arenaMinX         = arenaMinX;
        _arenaMaxX         = arenaMaxX;
        _getGroundY        = getGroundY;
        _getRateMultiplier = getRateMultiplier;

        _strikeSprites       = _dataSo.LightningStrikeSprites;
        _frameInterval       = _dataSo.LightningFrameInterval;
        _strikeSpritesCount  = (_strikeSprites != null) ? _strikeSprites.Length : 0;
        _cycleFrames         = (_strikeSprites != null) && (_strikeSprites.Length > 1) && (_frameInterval > 0f);

        Sprite firstStrikeSprite = null;
        if (_strikeSpritesCount > 0)
        {
            firstStrikeSprite = _strikeSprites[0];
        }
        _pool = new BossHazardPool(POOL_SIZE, "Boss2_LightningBolt", firstStrikeSprite, null, true,
            _dataSo.LightningParticleSize, _dataSo.LightningParticleColors);

        _waitIntervalNormal = new WaitForSeconds(_dataSo.LightningInterval);
        _waitIntervalPink   = new WaitForSeconds(_dataSo.LightningInterval / _dataSo.LightningRatePinkMult);
        _waitIntervalBlue   = new WaitForSeconds(_dataSo.LightningInterval / _dataSo.LightningRateBlueMult);
        _waitTelegraph = new WaitForSeconds(_dataSo.LightningTelegraphTime);
        _waitActive    = new WaitForSeconds(_dataSo.LightningActiveTime);
        _waitFrame     = new WaitForSeconds(_frameInterval);
    }

    /****************************************
    *                Methods
    ****************************************/

    public void Play()
    {
        if (_loop == null)
        {
            _loop = _owner.StartCoroutine(CoStrikeLoop());
        }
    }

    public void Stop()
    {
        if (_loop != null)
        {
            _owner.StopCoroutine(_loop);
            _loop = null;
        }
        _pool.FreeAll();
    }

    public void Dispose()
    {
        Stop();
        _pool.Dispose();
    }

    /****************************************
    *              Coroutine
    ****************************************/

    private IEnumerator CoStrikeLoop()
    {
        while (true)
        {
            yield return DecideWaitInterval();

            float x = DecideStrikeX();
            _owner.StartCoroutine(CoStrikeBolt(x));
        }
    }

    // 감정(Pink/Blue)에 따른 발생 간격 배율 반영 - 3가지 배율만 존재하므로 미리 캐싱한 WaitForSeconds 중 선택
    private WaitForSeconds DecideWaitInterval()
    {
        float multiplier = (_getRateMultiplier != null) ? _getRateMultiplier.Invoke() : 1f;

        if (Mathf.Approximately(multiplier, _dataSo.LightningRatePinkMult))
        {
            return _waitIntervalPink;
        }
        if (Mathf.Approximately(multiplier, _dataSo.LightningRateBlueMult))
        {
            return _waitIntervalBlue;
        }
        return _waitIntervalNormal;
    }

    // 낙하 지점 결정: 플레이어가 있으면 플레이어 x 위치 기준 반경 안에서, 없으면 아레나 전체에서 랜덤 선택
    private float DecideStrikeX()
    {
        if (_target != null)
        {
            float playerX      = _target.position.x;
            float randomOffset = Random.Range(-_dataSo.LightningPlayerRadius, _dataSo.LightningPlayerRadius);
            return Mathf.Clamp(playerX + randomOffset, _arenaMinX, _arenaMaxX);
        }

        return Random.Range(_arenaMinX, _arenaMaxX);
    }

    // 한 발: 예고(노란 장판, 판정 없음) -> 강타(즉시 배치, 판정 있음, 크랙클 프레임 순환) -> 회수
    private IEnumerator CoStrikeBolt(float x)
    {
        float groundY = _getGroundY(x);

        Vector2 telegraphScale = new Vector2(_dataSo.LightningWidth, _dataSo.LightningTelegraphHeight);
        Vector2 telegraphPos   = new Vector2(x, groundY + (_dataSo.LightningTelegraphHeight * 0.5f));

        int telegraphSlot = _pool.Alloc(telegraphPos, telegraphScale, _dataSo.LightningTelegraphColor, false);
        if (telegraphSlot < 0)
        {
            yield break; // 풀 고갈 - 이번 낙뢰는 생략
        }
        yield return _waitTelegraph;
        _pool.Free(telegraphSlot);

        Vector2 strikeScale = new Vector2(_dataSo.LightningWidth, _dataSo.LightningHeight);
        Vector2 strikePos   = new Vector2(x, groundY + (_dataSo.LightningHeight * 0.5f) - _dataSo.LightningGroundEmbed);

        int strikeSlot = _pool.Alloc(strikePos, strikeScale, _dataSo.LightningColor, true,
            _dataSo.LightningDamageHalves, _dataSo.LightningStunDuration);
        if (strikeSlot < 0)
        {
            yield break;
        }

        if (_cycleFrames)
        {
            float elapsed    = 0f;
            int   frameIndex = 0;
            while ((elapsed < _dataSo.LightningActiveTime) && (_pool.IsActive(strikeSlot)))
            {
                _pool.SetSprite(strikeSlot, _strikeSprites[frameIndex]);
                yield return _waitFrame;
                elapsed += _frameInterval;

                ++frameIndex;
                if (frameIndex >= _strikeSpritesCount)
                {
                    frameIndex = 0;
                }
            }
        }
        else if (_pool.IsActive(strikeSlot))
        {
            yield return _waitActive;
        }
        _pool.Free(strikeSlot);
    }
}
