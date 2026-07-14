// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.CameraSystem;
using Minsung.Common;
using Minsung.Common.Data;
using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 1페이즈 (64,000 ~ 48,000) - 보스 분신 2체의 근접 전투와, 하한 도달 시 즉사 레이저 기믹을 관리한다
    public class Phase1State : BossState
    {
        /****************************************
        *                Fields
        ****************************************/

        // 동시 사용 최대: 전체 색 안전구역(GIMMICK_LASER_COLOR_COUNT) + 레이저 1
        private const int POOL_SIZE = Constants.Combat.GIMMICK_LASER_COLOR_COUNT + 1;

        private readonly WaitForSeconds _waitRefireDelay = new WaitForSeconds(GameDB.Boss.GimmickRefireDelay);
        private readonly WaitForSeconds _waitLaserActive = new WaitForSeconds(GameDB.Boss.GimmickLaserActiveTime);

        private BossHazardPool _pool;
        private LaserColor[] _sequence;   // 즉사 기믹 색 순서 (실전 발사 때 같은 순서 재사용)
        private float[] _safeZoneCenters; // 색(enum 인덱스)별 안전구역 중심 x

        /****************************************
        *              Constructor
        ****************************************/

        public Phase1State(BossController boss) : base(boss) { }

        // 총 피통 하한 도달만으로는 기믹을 시작하지 않는다 - 분신 2체 전멸(HandleCloneDied)로만 트리거한다
        // (오버킬 시 피통은 하한에 먼저 닿아도 생존 분신이 있으면 기믹이 시작되지 않아야 한다)
        public override bool UsesHealthFloorTrigger => false;

        /****************************************
        *            Enter / Exit
        ****************************************/

        public override void Enter()
        {
            _pool = new BossHazardPool(POOL_SIZE, "Phase1_Gimmick");

            // 분신 2체 등장. 전멸 감지는 각 분신의 OnCloneDied 이벤트로 직접 추적한다
            BossCloneController[] clones = Boss.Phase1Clones;
            if (clones != null)
            {
                foreach (BossCloneController clone in clones)
                {
                    if (clone != null)
                    {
                        clone.Activate();
                        clone.OnCloneDied += HandleCloneDied;
                    }
                }
            }
        }

        public override void Exit()
        {
            BossCloneController[] clones = Boss.Phase1Clones;
            if (clones != null)
            {
                foreach (BossCloneController clone in clones)
                {
                    if (clone != null)
                    {
                        clone.OnCloneDied -= HandleCloneDied;
                        clone.Deactivate();
                    }
                }
            }

            if (_pool != null)
            {
                _pool.Dispose();
                _pool = null;
            }
        }

        // 분신이 죽을 때마다 나머지도 죽었는지 확인 - 둘 다 죽었을 때만 종료 기믹을 시작한다
        private void HandleCloneDied(BossCloneController _)
        {
            BossCloneController[] clones = Boss.Phase1Clones;
            if (clones == null)
            {
                return;
            }
            foreach (BossCloneController clone in clones)
            {
                if ((clone != null) && (clone.IsAlive))
                {
                    return; // 아직 생존한 분신이 있음
                }
            }
            Boss.TriggerPhaseEnd();
        }

        /****************************************
        *             즉사 기믹
        ****************************************/

        // 기믹은 페이즈 전환 중 1회만 진행되고 그동안 리와인드가 잠기므로(BossController.CoPhaseEnd)
        public override IEnumerator CoPhaseEndGimmick()
        {
            BuildSequence();

            // 레이저가 나오기 시작하는 구간 - 아레나 전체가 보이도록 플레이어 카메라를 줌 아웃
            CameraManager.Instance?.SetPlayerOrthographicSize(GameDB.Boss.GimmickCameraOrthoSize);

            // 1) 예고: 전체 색 안전구역을 동시에 표시(슬로우 중에만)하며 색 순서대로 발사
            yield return CoTelegraphSequence();

            // 2) 실전: 같은 순서로 재발사. 레이저 사이 간격은 5초(GimmickRefireDelay). 색이 맞지 않는 위치면 즉사
            for (int i = 0; i < _sequence.Length; ++i)
            {
                yield return _waitRefireDelay;
                yield return CoJudgeLaser(_sequence[i]);
            }

            // 파훼 성공 - BossController가 2페이즈로 전환한다. 카메라 줌은 원래 크기로 복원
            CameraManager.Instance?.ResetPlayerOrthographicSize();
        }

        // 색 순서(랜덤 3회, 중복 허용)와 색별 안전구역 중심 x를 결정한다
        private void BuildSequence()
        {
            _sequence = new LaserColor[GameDB.Boss.GimmickLaserCount];
            for (int i = 0; i < _sequence.Length; ++i)
            {
                _sequence[i] = (LaserColor)Random.Range(0, Constants.Combat.GIMMICK_LASER_COLOR_COUNT);
            }

            // 색별 안전구역 - 아레나 안 랜덤 배치
            // TODO: 구역 겹침 방지 / 레벨 디자인(고정 지점) 여부 기획 확정
            float halfWidth = GameDB.Boss.GimmickSafeZoneWidth * 0.5f;
            _safeZoneCenters = new float[Constants.Combat.GIMMICK_LASER_COLOR_COUNT];
            for (int i = 0; i < _safeZoneCenters.Length; ++i)
            {
                _safeZoneCenters[i] = Random.Range(Boss.ArenaMinX + halfWidth, Boss.ArenaMaxX - halfWidth);
            }
        }

        // 예고 전체 구간: 색별 안전구역을 전부 동시에 켜 둔 채(슬로우 중에만 렌더) 색 순서대로 예고 레이저를 발사한다
        private IEnumerator CoTelegraphSequence()
        {
            int[] zoneSlots = new int[Constants.Combat.GIMMICK_LASER_COLOR_COUNT];
            for (int c = 0; c < zoneSlots.Length; ++c)
            {
                zoneSlots[c] = AllocSafeZone((LaserColor)c);
            }

            for (int i = 0; i < _sequence.Length; ++i)
            {
                yield return CoTelegraphLaser(zoneSlots, _sequence[i]);
            }

            for (int c = 0; c < zoneSlots.Length; ++c)
            {
                _pool.Free(zoneSlots[c]);
            }
        }

        // 예고 1발: 전체 안전구역 표시(슬로우 중에만 렌더) -> 전장 레이저 연출(판정 없음)
        private IEnumerator CoTelegraphLaser(int[] zoneSlots, LaserColor color)
        {
            // 레이저를 쏠 때까지 매 프레임 슬로우 여부로 전체 안전구역 표시를 토글한다
            float elapsed = 0f;
            while (elapsed < GameDB.Boss.GimmickTelegraphTime)
            {
                elapsed += Time.deltaTime;
                bool visible = SlowMotionController.IsSlow;
                for (int c = 0; c < zoneSlots.Length; ++c)
                {
                    _pool.SetVisible(zoneSlots[c], visible);
                }
                yield return null;
            }

            yield return CoFireLaser(color);
        }

        // 실전 1발: 발사 순간 해당 색 안전구역 밖이면 즉사 -> 전장 레이저 연출
        private IEnumerator CoJudgeLaser(LaserColor color)
        {
            if (!IsPlayerInSafeZone(color))
            {
                Boss.KillPlayer();
            }
            yield return CoFireLaser(color);
        }

        // 아레나 전체를 덮는 레이저 연출. 즉사 판정은 위치 검사로 별도 처리하므로 콜라이더는 없다
        private IEnumerator CoFireLaser(LaserColor color)
        {
            float   width   = Boss.ArenaMaxX - Boss.ArenaMinX;
            float   centerX = (Boss.ArenaMinX + Boss.ArenaMaxX) * 0.5f;
            Vector2 pos     = new Vector2(centerX, Boss.ArenaGroundY + (GameDB.Boss.GimmickLaserHeight * 0.5f));
            Vector2 scale   = new Vector2(width, GameDB.Boss.GimmickLaserHeight);

            int slot = _pool.Alloc(pos, scale, ColorOf(color), false);
            yield return _waitLaserActive;
            _pool.Free(slot);

            // TODO: 레이저 발사 이펙트/사운드/화면 흔들림 (Constants.Audio / ParticlePresets)
        }

        private int AllocSafeZone(LaserColor color)
        {
            float   centerX = _safeZoneCenters[(int)color];
            Vector2 pos     = new Vector2(centerX, Boss.ArenaGroundY + (GameDB.Boss.GimmickLaserHeight * 0.5f));
            Vector2 scale   = new Vector2(GameDB.Boss.GimmickSafeZoneWidth, GameDB.Boss.GimmickLaserHeight);

            return _pool.Alloc(pos, scale, SafeZoneColorOf(color), false);
        }

        // 판정은 x축만 사용한다 (안전구역은 세로 전체를 덮는 기둥)
        private bool IsPlayerInSafeZone(LaserColor color)
        {
            if (Boss.Player == null)
            {
                return true; // 플레이어 참조가 없으면 판정 생략
            }
            float px     = Boss.Player.transform.position.x;
            float center = _safeZoneCenters[(int)color];
            return Mathf.Abs(px - center) <= (GameDB.Boss.GimmickSafeZoneWidth * 0.5f);
        }

        /****************************************
        *             색상 유틸
        ****************************************/

        private static Color ColorOf(LaserColor color)
        {
            switch (color)
            {
                case LaserColor.Red:
                    return Color.red;
                case LaserColor.Blue:
                    return Color.blue;
                default:
                    return Color.green;
            }
        }

        private static Color SafeZoneColorOf(LaserColor color)
        {
            Color c = ColorOf(color);
            c.a = GameDB.Boss.GimmickSafeZoneAlpha;
            return c;
        }
    }
}
