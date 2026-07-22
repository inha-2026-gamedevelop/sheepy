// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Boss;

namespace Minsung.Boss2
{
    // 부유 보스(Boss2) 강타 패턴 - Minsung.Boss.Phase2State의 장풍/강타 로직을 참고해 이식.
    // BossHazardPool(Minsung.Boss)은 BossController에 묶여 있지 않아 그대로 재사용한다(수정 없음)
    // 기본 간격마다 아레나 전체에서 랜덤 x에 예고 파티클 후 즉발 폭발 강타를 낸다
    // TODO: 리와인드 타임라인 미연동 - 결정 로그(x값) 기록/재현 미구현
    public class Boss2WavePattern
    {
        /****************************************
        *                Fields
        ****************************************/

        private const int POOL_SIZE = 4; // 동시 진행 최대 수 (예고+강타 겹침 대비 여유분)

        private readonly MonoBehaviour _owner;
        private readonly Boss2DataSO   _dataSo;
        private readonly float         _arenaMinX;
        private readonly float         _arenaMaxX;
        private readonly float         _arenaGroundY;

        private readonly BossHazardPool _pool;
        private Coroutine _loop;

        private readonly WaitForSeconds _waitInterval;
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

        public Boss2WavePattern(MonoBehaviour owner, Boss2DataSO dataSo,
            float arenaMinX, float arenaMaxX, float arenaGroundY)
        {
            _owner        = owner;
            _dataSo       = dataSo;
            _arenaMinX    = arenaMinX;
            _arenaMaxX    = arenaMaxX;
            _arenaGroundY = arenaGroundY;

            _strikeSprites      = _dataSo.WaveStrikeSprites;
            _frameInterval      = _dataSo.WaveFrameInterval;
            _strikeSpritesCount = (_strikeSprites != null) ? _strikeSprites.Length : 0;
            _cycleFrames        = (_strikeSprites != null) && (_strikeSprites.Length > 1) && (_frameInterval > 0f);

            // 낙뢰와 달리 프레임별 원본 크기를 그대로 쓰도록 Sliced 정규화는 끈다(sliceToScale: false)
            Sprite firstStrikeSprite = (_strikeSpritesCount > 0) ? _strikeSprites[0] : null;
            _pool = new BossHazardPool(POOL_SIZE, "Boss2_Wave", firstStrikeSprite, null, true,
                _dataSo.WaveParticleSize, _dataSo.WaveParticleColors, false);

            _waitInterval  = new WaitForSeconds(_dataSo.WaveInterval);
            _waitTelegraph = new WaitForSeconds(_dataSo.WaveTelegraphTime);
            _waitActive    = new WaitForSeconds(_dataSo.WaveActiveTime);
            _waitFrame     = new WaitForSeconds(_frameInterval);
        }

        /****************************************
        *                Methods
        ****************************************/

        public void Play()
        {
            if (_loop == null)
            {
                _loop = _owner.StartCoroutine(CoWaveLoop());
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

        private IEnumerator CoWaveLoop()
        {
            while (true)
            {
                yield return _waitInterval;

                float x = Random.Range(_arenaMinX, _arenaMaxX);
                _owner.StartCoroutine(CoTelegraphAndStrike(x));
            }
        }

        // 한 세트: 예고(파티클, 판정 없음) -> 강타(즉시 배치, 폭발 프레임 순환, 앞 N프레임만 판정)
        private IEnumerator CoTelegraphAndStrike(float x)
        {
            Vector2 scale = new Vector2(_dataSo.WaveWidth, _dataSo.WaveHeight);
            Vector2 pos   = new Vector2(x, _arenaGroundY + (_dataSo.WaveHeight * 0.5f) - _dataSo.WaveGroundEmbed);

            int telegraphSlot = _pool.Alloc(pos, scale, _dataSo.WaveColor, false);
            if (telegraphSlot < 0)
            {
                yield break; // 풀 고갈 - 이번 강타는 생략
            }
            yield return _waitTelegraph;
            _pool.Free(telegraphSlot);

            int strikeSlot = _pool.Alloc(pos, scale, _dataSo.WaveColor, true, _dataSo.WaveDamageHalves);
            if (strikeSlot < 0)
            {
                yield break;
            }

            if (_cycleFrames)
            {
                float elapsed    = 0f;
                int   frameIndex = 0;
                while ((elapsed < _dataSo.WaveActiveTime) && (_pool.IsActive(strikeSlot)))
                {
                    if (frameIndex == _dataSo.WaveActiveFrameCount)
                    {
                        _pool.SetColliderActive(strikeSlot, false); // 종료 프레임 진입 - 판정 해제, 연출만 유지
                    }
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
}
