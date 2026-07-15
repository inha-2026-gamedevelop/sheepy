// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.TimeSystem;
using Minsung.Visual;

namespace Minsung.Boss
{
    // 2페이즈 (48,000 ~ 32,000) - 보스 본체 근접 공격 + 장풍 패턴을 관리하는 상태
    public class Phase2State : BossState
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 장풍 풀 4슬롯 고정 크기 스냅샷. 힙 할당 없음
        private struct Phase2Frame
        {
            public BossHazardPool.HazardRecord W0, W1, W2, W3;
            public int WaveCursor;
        }

        // 상승 중인 장풍 하나의 구동 상태 (이동은 코루틴 대신 FixedTick이 굴린다 -
        // 스냅샷 역재생과 충돌하지 않고, 페이즈 전환 중에는 자동으로 멈추기 위함)
        private struct WaveState
        {
            public int   Slot; // -1 = 비활성
            public float X;
            public float Y;
        }

        /****************************************
        *                Fields
        ****************************************/

        private const int WAVE_POOL_SIZE = 4; // 동시 상승 최대 수 (간격 대비 상승 시간 여유분)

        private readonly WaitForSeconds _waitWaveInterval = new WaitForSeconds(GameDB.Boss.Phase2WaveInterval);

        private BossHazardPool _wavePool;
        private Coroutine      _waveLoop;
        private readonly WaveState[] _waves = new WaveState[WAVE_POOL_SIZE];

        // 리와인드 기록
        private RingBuffer<Phase2Frame> _rewindBuffer;

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

            _wavePool = new BossHazardPool(WAVE_POOL_SIZE, "Phase2_Wave");
            ClearWaveStates();

            // 버퍼 용량은 플레이어/몬스터와 동일한 기준(TickCapacity)을 써야 되감기 인덱스가 일치한다
            _rewindBuffer = new RingBuffer<Phase2Frame>(RewindManager.TickCapacity);
            _waveXLog     = new List<float>();
            _waveCursor   = 0;

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
        *              FixedTick
        ****************************************/

        // 장풍 상승. 페이즈 전환/되감기 중에는 BossController가 호출 자체를 막는다
        public override void FixedTick()
        {
            float topY = Boss.ArenaGroundY + GameDB.Boss.Phase2WaveMaxHeight;

            for (int i = 0; i < _waves.Length; ++i)
            {
                if (_waves[i].Slot < 0)
                {
                    continue;
                }

                _waves[i].Y += GameDB.Boss.Phase2WaveRiseSpeed * Time.fixedDeltaTime;
                if (_waves[i].Y >= topY)
                {
                    _wavePool.Free(_waves[i].Slot);
                    _waves[i].Slot = -1;
                    continue;
                }
                _wavePool.SetPosition(_waves[i].Slot, new Vector2(_waves[i].X, _waves[i].Y));
            }
        }

        /****************************************
        *           IRewindable 훅
        ****************************************/

        // 매 물리 틱마다 장풍 풀 상태 + 결정 커서를 스냅샷으로 기록
        public override void RecordTick()
        {
            Phase2Frame f = new Phase2Frame();
            if (_wavePool != null)
            {
                f.W0 = _wavePool.Capture(0);
                f.W1 = _wavePool.Capture(1);
                f.W2 = _wavePool.Capture(2);
                f.W3 = _wavePool.Capture(3);
            }
            f.WaveCursor = _waveCursor;
            _rewindBuffer.Push(f);
        }

        public override void OnRewindStart()
        {
            StopWaveLoop();
        }

        public override void ApplyRewindTick(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out Phase2Frame f))
            {
                ApplyWaveFrame(f);
            }
        }

        // 되감기 종료 - 결정 커서만 복원하고 떠 있던 장풍은 회수한다
        // (구동 상태(WaveState)가 함께 되돌아갈 수 없으므로 루프가 같은 x 순서로 다시 쏜다)
        public override void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out Phase2Frame f))
            {
                _waveCursor = f.WaveCursor;
            }

            _wavePool.FreeAll();
            ClearWaveStates();
            _rewindBuffer.Clear();
            _waveLoop = Boss.StartCoroutine(CoWaveLoop());
        }

        /****************************************
        *            종료 기믹
        ****************************************/

        // 2페이즈 종료: 컷신(페이드) -> 3페이즈. 기믹 중에는 리와인드가 잠긴다(BossController.CoPhaseEnd)
        public override IEnumerator CoPhaseEndGimmick()
        {
            // 컷신 시작 - 패턴 정지 + 떠 있는 장풍 정리
            StopWaveLoop();
            _wavePool.FreeAll();
            ClearWaveStates();

            // TODO: 컷신 연출(보스 대사/카메라) 기획 확정 후 교체
            // TODO: 씬 전환으로 맵 변경 - FadeOutIn의 onMidpoint에서 GameManager 씬 전환 호출
            //       보스 상태(피통/페이즈/타이머) 이관 구조 설계 후 연결한다
            if (ScreenFade.Instance != null)
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

        // 장풍 루프: 간격마다 결정 로그의 x(없으면 새 랜덤)에 장풍 하나를 쏘아 올린다
        private IEnumerator CoWaveLoop()
        {
            while (true)
            {
                yield return _waitWaveInterval;

                if (Boss.IsTransitioning)
                {
                    continue; // 기믹/컷신 중에는 발사 정지
                }
                SpawnWave(GetOrMakeWaveX());
            }
        }

        // 이미 만들어진 결정이 있으면 재사용(리와인드 후 재현), 없으면 새로 만들어 로그에 추가
        private float GetOrMakeWaveX()
        {
            if (_waveCursor < _waveXLog.Count)
            {
                return _waveXLog[_waveCursor++];
            }

            float x = Random.Range(Boss.ArenaMinX, Boss.ArenaMaxX);
            _waveXLog.Add(x);
            ++_waveCursor;
            return x;
        }

        private void SpawnWave(float x)
        {
            BossDataSO bossSo = GameDB.Boss;

            float   startY = Boss.ArenaGroundY - bossSo.Phase2WaveSpawnDepth;
            Vector2 scale  = new Vector2(bossSo.Phase2WaveWidth, bossSo.Phase2WaveHeight);

            int slot = _wavePool.Alloc(new Vector2(x, startY), scale, bossSo.Phase2WaveColor, true,
                                       bossSo.AttackHalves);
            if (slot < 0)
            {
                return; // 풀 고갈 - 이번 장풍은 생략
            }
            Boss.Body?.PlayCastTrigger();

            for (int i = 0; i < _waves.Length; ++i)
            {
                if (_waves[i].Slot < 0)
                {
                    _waves[i] = new WaveState { Slot = slot, X = x, Y = startY };
                    return;
                }
            }
        }

        private void StopWaveLoop()
        {
            if (_waveLoop != null)
            {
                Boss.StopCoroutine(_waveLoop);
                _waveLoop = null;
            }
        }

        private void ClearWaveStates()
        {
            for (int i = 0; i < _waves.Length; ++i)
            {
                _waves[i].Slot = -1;
            }
        }

        // 스냅샷 내용대로 장풍 풀 4슬롯을 복원
        private void ApplyWaveFrame(Phase2Frame f)
        {
            _wavePool.Apply(0, f.W0);
            _wavePool.Apply(1, f.W1);
            _wavePool.Apply(2, f.W2);
            _wavePool.Apply(3, f.W3);
        }
    }
}
