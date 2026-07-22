// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 3페이즈 (32,000 ~ 16,000) - 화남 감정 고정 상태로, 2페이즈 패턴에 가로지르는 레이저 공격을 추가한다
    public class Phase3State : Phase2State
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 레이저 풀 2슬롯 고정 크기 스냅샷. 힙 할당 없음
        private struct Phase3Frame
        {
            public BossHazardPool.HazardRecord L0, L1;
            public int LaserCursor;
        }

        // 레이저 한 발의 랜덤 결정 (지면 기준 시작/도착 높이)
        private readonly struct LaserDecision
        {
            public readonly float StartY;
            public readonly float EndY;

            public LaserDecision(float startY, float endY)
            {
                StartY = startY;
                EndY   = endY;
            }
        }

        /****************************************
        *                Fields
        ****************************************/

        private const int LASER_POOL_SIZE = 2; // 동시 사용 최대: 경고 또는 발사 1 + 여유 1

        private readonly WaitForSeconds _waitLaserInterval = new WaitForSeconds(GameDB.Boss.Phase3LaserInterval);
        private readonly WaitForSeconds _waitLaserBlink    = new WaitForSeconds(GameDB.Boss.Phase3LaserBlinkInterval);
        private readonly WaitForSeconds _waitLaserActive   = new WaitForSeconds(GameDB.Boss.Phase3LaserActiveTime);

        private BossHazardPool _laserPool;
        private Coroutine      _laserLoop;

        // 리와인드 기록 (2페이즈 장풍 버퍼와 별개로 레이저만 기록)
        private RingBuffer<Phase3Frame> _laserRewindBuffer;

        // 결정 로그 - 레이저 시작/도착 높이를 보존해 리와인드 후 동일 궤적을 재현한다
        private List<LaserDecision> _laserLog;
        private int                 _laserCursor;

        /****************************************
        *              Constructor
        ****************************************/

        public Phase3State(BossController boss) : base(boss) { }

        /****************************************
        *            Enter / Exit
        ****************************************/

        public override void Enter()
        {
            base.Enter(); // 본체 + 장풍 (2페이즈 패턴 유지)

            Boss.SetAutoEmotionSuspended(true); // 자동 전환 정지 - 3페이즈는 화남 고정
            Boss.SetEmotion(BossEmotion.Angry); // 3페이즈 고정 - 10초마다 1초 혼란(키반전)

            Material laserMat  = GameDB.Boss.Phase3LaserMaterial;
            _laserPool         = new BossHazardPool(LASER_POOL_SIZE, "Phase3_Laser", customMaterial: laserMat,
                                                    attachParticle: true,
                                                    particleSize: GameDB.Boss.Phase3LaserFlowParticleSize,
                                                    particleColors: GameDB.Boss.Phase3LaserFlowColors,
                                                    particleOnHitOnly: true,
                                                    particleFlowAlongX: true,
                                                    particleFlowSpeed: GameDB.Boss.Phase3LaserFlowSpeed,
                                                    particleRate: GameDB.Boss.Phase3LaserFlowRate);
            _laserRewindBuffer = new RingBuffer<Phase3Frame>(RewindManager.TickCapacity);
            _laserLog          = new List<LaserDecision>();
            _laserCursor       = 0;

            _laserLoop = Boss.StartCoroutine(CoLaserLoop());
        }

        public override void Exit()
        {
            StopLaserLoop();
            if (_laserPool != null)
            {
                _laserPool.Dispose();
                _laserPool = null;
            }

            Boss.SetEmotion(BossEmotion.None); // 혼란 해제
            Boss.SetAutoEmotionSuspended(false); // 4페이즈부터 자동 전환 재개

            base.Exit();
        }

        /****************************************
        *           IRewindable 훅
        ****************************************/

        public override void RecordTick()
        {
            base.RecordTick();

            Phase3Frame f = new Phase3Frame();
            if (_laserPool != null)
            {
                f.L0 = _laserPool.Capture(0);
                f.L1 = _laserPool.Capture(1);
            }
            f.LaserCursor = _laserCursor;
            _laserRewindBuffer.Push(f);
        }

        public override void OnRewindStart()
        {
            base.OnRewindStart();
            StopLaserLoop();
        }

        public override void ApplyRewindTick(int orderedIndex)
        {
            base.ApplyRewindTick(orderedIndex);

            if (_laserRewindBuffer.TryGetOrdered(orderedIndex, out Phase3Frame f))
            {
                _laserPool.Apply(0, f.L0);
                _laserPool.Apply(1, f.L1);
            }
        }

        // 되감기 종료 - 결정 커서만 복원하고 진행 중이던 경고/레이저는 회수한다
        // (소유 코루틴이 사라졌으므로. 루프가 같은 궤적 순서로 다시 쏜다)
        public override void OnRewindEnd(int orderedIndex)
        {
            base.OnRewindEnd(orderedIndex);

            if (_laserRewindBuffer.TryGetOrdered(orderedIndex, out Phase3Frame f))
            {
                _laserCursor = f.LaserCursor;
            }

            _laserPool.FreeAll();
            _laserRewindBuffer.Clear();
            _laserLoop = Boss.StartCoroutine(CoLaserLoop());
        }

        /****************************************
        *            종료 기믹
        ****************************************/

        // 3->4페이즈 전환은 별도 기믹 없음 - 2페이즈의 컷신/씬 전환을 상속받지 않도록 비워 둔다
        // TODO: 4페이즈 진입 연출(리와인드 삭제 - 회중시계 파괴 등) 기획 확정 후 여기에 배치
        public override IEnumerator CoPhaseEndGimmick()
        {
            yield break;
        }

        /****************************************
        *          Coroutine / 레이저
        ****************************************/

        // 레이저 루프: 간격마다 결정 로그의 궤적(없으면 새 랜덤)으로 한 발씩 가로지른다
        private IEnumerator CoLaserLoop()
        {
            while (true)
            {
                yield return _waitLaserInterval;

                if (Boss.IsTransitioning)
                {
                    continue; // 기믹/컷신 중에는 발사 정지
                }
                yield return CoFireCrossLaser(GetOrMakeLaserDecision());
            }
        }

        // 이미 만들어진 결정이 있으면 재사용(리와인드 후 재현), 없으면 새로 만들어 로그에 추가
        private LaserDecision GetOrMakeLaserDecision()
        {
            if (_laserCursor < _laserLog.Count)
            {
                LaserDecision logged = _laserLog[_laserCursor];
                ++_laserCursor;
                return logged;
            }

            LaserDecision d = new LaserDecision(
                Random.Range(0f, GameDB.Boss.Phase3LaserMaxHeight),
                Random.Range(0f, GameDB.Boss.Phase3LaserMaxHeight));

            _laserLog.Add(d);
            ++_laserCursor;
            return d;
        }

        // 한 발: 경고(빨간 깜빡임 얇은 실선, 판정 없음) -> 발사(회전 사각 판정, 한 칸)
        private IEnumerator CoFireCrossLaser(LaserDecision d)
        {
            Vector2 start  = new Vector2(Boss.ArenaMinX, Boss.ArenaGroundY + d.StartY);
            Vector2 end    = new Vector2(Boss.ArenaMaxX, Boss.ArenaGroundY + d.EndY);
            Vector2 center = (start + end) * 0.5f;
            Vector2 delta  = end - start;
            float   angle  = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Vector2 scale  = new Vector2(delta.magnitude, GameDB.Boss.Phase3LaserThickness);

            // 경고: 본 레이저보다 얇은 실선으로 깜빡임 주기마다 표시를 토글
            Vector2 warnScale = new Vector2(delta.magnitude, GameDB.Boss.Phase3LaserWarningThickness);
            int warnSlot = _laserPool.Alloc(center, warnScale, GameDB.Boss.Phase3LaserWarningColor, false,
                                            rotationDeg: angle);
            bool  visible = true;
            float elapsed = 0f;
            while (elapsed < GameDB.Boss.Phase3LaserWarningTime)
            {
                yield return _waitLaserBlink;
                elapsed += GameDB.Boss.Phase3LaserBlinkInterval;
                visible = !visible;
                _laserPool.SetVisible(warnSlot, visible);
            }
            _laserPool.Free(warnSlot);

            // 발사: 같은 궤적에 판정 있는 레이저 (하트 한 칸)
            int laserSlot = _laserPool.Alloc(center, scale, GameDB.Boss.Phase3LaserColor, true,
                                            GameDB.Boss.AttackHalves, rotationDeg: angle);
            Boss.Body?.PlayCastTrigger();
            yield return _waitLaserActive;

            // 회수: 판정부터 끄고 두께를 서서히 0으로 줄여 점점 좁아지며 사라지는 연출
            _laserPool.SetColliderActive(laserSlot, false);
            float retractElapsed = 0f;
            while (retractElapsed < GameDB.Boss.Phase3LaserRetractTime)
            {
                yield return null;
                retractElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(retractElapsed / GameDB.Boss.Phase3LaserRetractTime);
                _laserPool.SetScale(laserSlot, new Vector2(scale.x, Mathf.Lerp(scale.y, 0f, t)));
            }
            _laserPool.Free(laserSlot);
        }

        private void StopLaserLoop()
        {
            if (_laserLoop != null)
            {
                Boss.StopCoroutine(_laserLoop);
                _laserLoop = null;
            }
        }
    }
}
