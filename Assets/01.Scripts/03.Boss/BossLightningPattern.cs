// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;

namespace Minsung.Boss
{
    // 전 페이즈 공통 낙뢰 패턴. 기본 4초 간격으로 플레이어 주변 지점에 노란 장판 예고 후 즉발로 내리친다
    public class BossLightningPattern : IBossPattern
    {
        /****************************************
        *                Fields
        ****************************************/

        private const int POOL_SIZE = 4; // 동시 사용 최대 슬롯 수 (예고 + 강타 + 여유분)

        private readonly BossController _boss;
        private readonly BossHazardPool _pool;
        private Coroutine _loop;

        // 감정 배율별 대기 캐시 (코루틴 회전마다 할당 방지)
        private readonly WaitForSeconds _waitNormal;
        private readonly WaitForSeconds _waitPink;
        private readonly WaitForSeconds _waitBlue;

        private readonly WaitForSeconds _waitTelegraph;
        private readonly WaitForSeconds _waitActive;
        private readonly WaitForSeconds _waitFrame;

        private readonly Sprite[] _strikeSprites;
        private readonly float    _frameInterval;
        private readonly BossDataSO _bossSo;
        private readonly bool     _cycleFrames;
        private readonly int      _strikeSpritesCount;

        /****************************************
        *              Constructor
        ****************************************/

        public BossLightningPattern(BossController boss)
        {
            _boss = boss;
            _bossSo = GameDB.Boss;

            _strikeSprites = _bossSo.LightningStrikeSprites;
            _frameInterval = _bossSo.LightningFrameInterval;
            _strikeSpritesCount = (_strikeSprites != null) ? _strikeSprites.Length : 0;
            _cycleFrames = (_strikeSprites != null) && (_strikeSprites.Length > 1) && (_frameInterval > 0f);

            Sprite firstStrikeSprite = null;
            if (_strikeSpritesCount > 0)
            {
                firstStrikeSprite = _strikeSprites[0];
            }
            _pool = new BossHazardPool(POOL_SIZE, "LightningBolt", firstStrikeSprite);

            _waitNormal    = new WaitForSeconds(_bossSo.LightningInterval);
            _waitPink      = new WaitForSeconds(_bossSo.LightningInterval / _bossSo.LightningRatePinkMult);
            _waitBlue      = new WaitForSeconds(_bossSo.LightningInterval / _bossSo.LightningRateBlueMult);
            _waitTelegraph = new WaitForSeconds(_bossSo.LightningTelegraphTime);
            _waitActive    = new WaitForSeconds(_bossSo.LightningActiveTime);
            _waitFrame     = new WaitForSeconds(_frameInterval);
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 낙뢰 루프 시작 </summary>
        public void Play()
        {
            if (_loop == null)
            {
                _loop = _boss.StartCoroutine(CoStrikeLoop());
            }
        }

        /// <summary> 낙뢰 루프 정지 + 떠 있는 예고/강타 정리 </summary>
        public void Stop()
        {
            if (_loop != null)
            {
                _boss.StopCoroutine(_loop);
                _loop = null;
            }
            _pool.FreeAll(); 
        }

        /// <summary> 풀까지 파괴. 보스 파괴 시 호출 </summary>
        public void Dispose()
        {
            Stop();
            _pool.Dispose();
        }

        // TODO: 리와인드 스냅샷 - 예고/강타 상태를 틱마다 기록해 역재생 (BossHazardPool.Capture/Apply 사용,
        //       Phase3State의 2슬롯 스냅샷 방식 참고). 현재는 되감기 시 정리하고 루프만 재시작한다
        public void OnRewindStart()
        {
            Stop();
        }

        public void OnRewindEnd()
        {
            Play();
        }

        /****************************************
        *              Coroutine
        ****************************************/

        // 발생 루프: 감정 배율이 적용된 간격마다 플레이어 주변 x에 한 발 예고+강타를 실행한다
        private IEnumerator CoStrikeLoop()
        {
            while (true)
            {
                yield return CurrentWait();

                if (_boss.IsTransitioning)
                {
                    continue; // 기믹/컷신 중에는 발생 정지
                }

                float x = DecideStrikeX();
                _boss.StartCoroutine(CoStrikeBolt(x));
            }
        }

        private WaitForSeconds CurrentWait()
        {
            switch (_boss.CurrentEmotion)
            {
                case BossEmotion.Pink:
                    return _waitPink;
                case BossEmotion.Blue:
                    return _waitBlue;
                default:
                    return _waitNormal;
            }
        }

        // 낙하 지점 결정: 플레이어가 있으면 플레이어 x 위치 기준 반경 안에서, 없으면 아레나 전체에서 랜덤 선택
        private float DecideStrikeX()
        {
            if (_boss.Player != null)
            {
                float playerX      = _boss.Player.transform.position.x;
                float randomOffset = Random.Range(-_bossSo.LightningPlayerRadius, _bossSo.LightningPlayerRadius);
                return Mathf.Clamp(playerX + randomOffset, _boss.ArenaMinX, _boss.ArenaMaxX);
            }

            return Random.Range(_boss.ArenaMinX, _boss.ArenaMaxX);
        }

        // 한 발: 예고(노란 장판, 판정 없음) -> 강타(즉시 배치, 판정 있음, 크랙클 프레임 순환) -> 회수
        private IEnumerator CoStrikeBolt(float x)
        {
            // 예고 - 지면에 얇은 노란 장판, 판정 없음. 강타와 같은 x 열을 가리키므로 폭은 LightningWidth를 공유
            Vector2 telegraphScale = new Vector2(_bossSo.LightningWidth, _bossSo.LightningTelegraphHeight);
            Vector2 telegraphPos   = new Vector2(x, _boss.ArenaGroundY + (_bossSo.LightningTelegraphHeight * 0.5f));

            int telegraphSlot = _pool.Alloc(telegraphPos, telegraphScale, _bossSo.LightningTelegraphColor, false);
            if (telegraphSlot < 0)
            {
                yield break; // 풀 고갈 - 이번 낙뢰는 생략
            }
            yield return _waitTelegraph;
            _pool.Free(telegraphSlot);

            if (_boss.IsTransitioning)
            {
                yield break; // 예고 중 컷신/기믹 진입 시 발사 취소
            }

            // 강타 - 같은 x에 즉시 배치(보간 없음), 짧게 유지하며 크랙클 프레임 순환
            Vector2 strikeScale = new Vector2(_bossSo.LightningWidth, _bossSo.LightningHeight);
            Vector2 strikePos   = new Vector2(x, _boss.ArenaGroundY + (_bossSo.LightningHeight * 0.5f));

            int strikeSlot = _pool.Alloc(strikePos, strikeScale, _bossSo.LightningColor, true,
                                         _bossSo.LightningDamageHalves, _bossSo.LightningStunDuration);
            if (strikeSlot < 0)
            {
                yield break;
            }

            if (_cycleFrames)
            {
                float elapsed    = 0f;
                int   frameIndex = 0;
                while ((elapsed < _bossSo.LightningActiveTime) && (_pool.IsActive(strikeSlot)))
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

            // TODO: 착탄 사운드 (Constants.Audio 연동)
            // TODO: 예고 장판 세부 비주얼(그라데이션/펄스 등) - 이번엔 골격만, 추후 조정
        }
    }
}
