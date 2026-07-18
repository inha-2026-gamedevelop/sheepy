// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common.Data;
using Minsung.Visual;

namespace Minsung.Boss
{
    // 2페이즈 (48,000 ~ 32,000) - 보스 본체 근접 공격 + 장풍 패턴을 관리하는 상태
    public class Phase2State : BossState
    {
        /****************************************
        *                Fields
        ****************************************/

        private const int WAVE_POOL_SIZE = 4; // 동시 진행 최대 수 (예고+강타 겹침 대비 여유분)

        private WaitForSeconds _waitWaveInterval;
        private WaitForSeconds _waitTelegraph;
        private WaitForSeconds _waitActive;
        private WaitForSeconds _waitFrame;

        private BossHazardPool _wavePool;
        private Coroutine      _waveLoop;

        private BossDataSO _bossSo;
        private Sprite[]   _strikeSprites;
        private float      _frameInterval;
        private int        _strikeSpritesCount;
        private bool       _cycleFrames;

        // 결정 로그 - 장풍 x 랜덤을 보존해 리와인드 후 동일 순서를 재현한다
        private List<float> _waveXLog;
        private int         _waveCursor;

        /****************************************
        *              Constructor
        ****************************************/

        public Phase2State(BossController boss) : base(boss) { }

        /****************************************
        *            Enter / Exit
        ****************************************/

        public override void Enter()
        {
            // 카메라 줌아웃은 BossController.BeginBattle에서 전투 시작 시점에 이미 적용됨
            // TODO: 본체 등장 전용 연출(사운드/카메라 팬 등) - 현재는 즉시 활성화
            if (Boss.Body != null)
            {
                Boss.Body.Activate(); // 이미 등장해 있으면(3·4페이즈 재진입) 내부에서 무시된다
            }

            _bossSo = GameDB.Boss;

            _strikeSprites      = _bossSo.Phase2WaveStrikeSprites;
            _frameInterval      = _bossSo.Phase2WaveFrameInterval;
            _strikeSpritesCount = (_strikeSprites != null) ? _strikeSprites.Length : 0;
            _cycleFrames        = (_strikeSprites != null) && (_strikeSprites.Length > 1) && (_frameInterval > 0f);

            _waitWaveInterval = new WaitForSeconds(_bossSo.Phase2WaveInterval);
            _waitTelegraph    = new WaitForSeconds(_bossSo.Phase2WaveTelegraphTime);
            _waitActive       = new WaitForSeconds(_bossSo.Phase2WaveActiveTime);
            _waitFrame        = new WaitForSeconds(_frameInterval);

            // 초기 슬롯 표시용 스프라이트만 지정하고, 낙뢰와 달리 프레임별 원본 크기를 그대로 쓰도록 Sliced 정규화는 끈다(sliceToScale: false)
            Sprite firstStrikeSprite = (_strikeSpritesCount > 0) ? _strikeSprites[0] : null;
            _wavePool = new BossHazardPool(WAVE_POOL_SIZE, "Phase2_Wave", firstStrikeSprite, null, true,
                                            _bossSo.Phase2WaveParticleSize, _bossSo.Phase2WaveParticleColors, false);

            _waveXLog   = new List<float>();
            _waveCursor = 0;

            _waveLoop = Boss.StartCoroutine(CoWaveLoop());
        }

        public override void Exit()
        {
            StopWaveLoop();
            if (_wavePool != null)
            {
                _wavePool.Dispose();
                _wavePool = null;
            }
            if (Boss.Body != null)
            {
                Boss.Body.Deactivate();
            }
        }

        /****************************************
        *           IRewindable 훅
        ****************************************/

        // 되감기 시작 - 진행 중인 예고/강타를 정지하고 회수한다
        // (지속시간이 짧아 낙뢰와 동일하게 정밀 스크럽 대신 정지+재시작으로 처리)
        // TODO: 프레임 단위 스냅샷/복원이 필요해지면 BossHazardPool.Capture/Apply로 확장
        public override void OnRewindStart()
        {
            StopWaveLoop();
            _wavePool.FreeAll();
        }

        public override void OnRewindEnd(int orderedIndex)
        {
            _waveLoop = Boss.StartCoroutine(CoWaveLoop());
        }

        /****************************************
        *            종료 기믹
        ****************************************/

        // 2페이즈 종료: 컷신(페이드) -> 3페이즈. 기믹 중에는 리와인드가 잠긴다(BossController.CoPhaseEnd)
        public override IEnumerator CoPhaseEndGimmick()
        {
            // 컷신 시작 - 패턴 정지 + 떠 있는 예고/강타 정리
            StopWaveLoop();
            _wavePool.FreeAll();

            // TODO: 컷신 연출(보스 대사/카메라) 기획 확정 후 교체
            if (ScreenFade.Instance == null)
            {
                yield break;
            }

            if (Boss.IsFinalPhase)
            {
                // 이 씬은 여기서 끝 - 다음은 영상+씬 전환이라 다시 밝아질 필요 없이 어두운 채로 넘긴다
                bool faded = false;
                ScreenFade.Instance.FadeOut(onComplete: () => faded = true);
                while (!faded)
                {
                    yield return null;
                }
            }
            else
            {
                bool midpointReached = false;
                ScreenFade.Instance.FadeOutIn(() => midpointReached = true);
                while (!midpointReached)
                {
                    yield return null;
                }
            }
        }

        /****************************************
        *          Coroutine / 장풍
        ****************************************/

        // 장풍 루프: 간격마다 결정 로그의 x(없으면 새 랜덤)에 예고+강타 한 세트를 실행한다
        private IEnumerator CoWaveLoop()
        {
            while (true)
            {
                yield return _waitWaveInterval;

                if (Boss.IsTransitioning)
                {
                    continue; // 기믹/컷신 중에는 발사 정지
                }
                Boss.StartCoroutine(CoTelegraphAndStrike(GetOrMakeWaveX()));
            }
        }

        // 이미 만들어진 결정이 있으면 재사용(리와인드 후 재현), 없으면 새로 만들어 로그에 추가
        private float GetOrMakeWaveX()
        {
            if (_waveCursor < _waveXLog.Count)
            {
                float logged = _waveXLog[_waveCursor];
                ++_waveCursor;
                return logged;
            }

            float x = Random.Range(Boss.ArenaMinX, Boss.ArenaMaxX);
            _waveXLog.Add(x);
            ++_waveCursor;
            return x;
        }

        // 한 세트: 예고(파티클, 판정 없음) -> 강타(즉시 배치, 폭발 프레임 순환, 앞 N프레임만 판정)
        private IEnumerator CoTelegraphAndStrike(float x)
        {
            Vector2 scale = new Vector2(_bossSo.Phase2WaveWidth, _bossSo.Phase2WaveHeight);
            // 스프라이트 하단의 발광 감쇠(여백)를 가리기 위해 지면 아래로 살짝 밀어넣는다(Phase2WaveGroundEmbed)
            Vector2 pos   = new Vector2(x, Boss.ArenaGroundY + (_bossSo.Phase2WaveHeight * 0.5f) - _bossSo.Phase2WaveGroundEmbed);

            // 예고 - 판정 없음. 스케일이 강타와 동일해 파티클 방출 영역이 폭발 크기만큼 넓어진다(scalingMode=Shape)
            int telegraphSlot = _wavePool.Alloc(pos, scale, _bossSo.Phase2WaveColor, false);
            if (telegraphSlot < 0)
            {
                yield break; // 풀 고갈 - 이번 장풍은 생략
            }
            yield return _waitTelegraph;
            if (_wavePool == null)
            {
                yield break; // 대기 중 페이즈 전환으로 풀이 정리됨 - 더 진행하지 않는다
            }
            _wavePool.Free(telegraphSlot);

            if (Boss.IsTransitioning)
            {
                yield break; // 예고 중 컷신/기믹 진입 시 발사 취소
            }
            Boss.Body?.PlayCastTrigger();

            // 강타 - 같은 위치에 즉시 배치, 폭발 프레임 순환
            int strikeSlot = _wavePool.Alloc(pos, scale, _bossSo.Phase2WaveColor, true, _bossSo.AttackHalves);
            if (strikeSlot < 0)
            {
                yield break;
            }

            if (_cycleFrames)
            {
                float elapsed    = 0f;
                int   frameIndex = 0;
                while ((elapsed < _bossSo.Phase2WaveActiveTime) && (_wavePool.IsActive(strikeSlot)))
                {
                    if (frameIndex == _bossSo.Phase2WaveActiveFrameCount)
                    {
                        _wavePool.SetColliderActive(strikeSlot, false); // 종료 프레임 진입 - 판정 해제, 연출만 유지
                    }
                    _wavePool.SetSprite(strikeSlot, _strikeSprites[frameIndex]);
                    yield return _waitFrame;
                    if (_wavePool == null)
                    {
                        yield break; // 프레임 대기 중 페이즈 전환으로 풀이 정리됨
                    }
                    elapsed += _frameInterval;

                    ++frameIndex;
                    if (frameIndex >= _strikeSpritesCount)
                    {
                        frameIndex = 0;
                    }
                }
            }
            else if (_wavePool.IsActive(strikeSlot))
            {
                yield return _waitActive;
                if (_wavePool == null)
                {
                    yield break;
                }
            }
            _wavePool.Free(strikeSlot);
        }

        private void StopWaveLoop()
        {
            if (_waveLoop != null)
            {
                Boss.StopCoroutine(_waveLoop);
                _waveLoop = null;
            }
        }
    }
}
