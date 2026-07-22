// System
using System.Collections;

// Unity
using UnityEngine;

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

        private const int POOL_SIZE = 4;  // 동시 사용 최대: 안전구역 3(색별) + 레이저 1
        private const int SLOT_NONE = -1; // 풀 미할당 슬롯 표시 (BossHazardPool.Alloc 실패 반환값과 동일)

        private readonly WaitForSeconds _waitRefireDelay   = new WaitForSeconds(GameDB.Boss.GimmickRefireDelay);
        private readonly WaitForSeconds _waitJudgeInterval = new WaitForSeconds(GameDB.Boss.GimmickJudgeInterval);

        private BossHazardPool _pool;
        private LaserColor[] _sequence;    // 즉사 기믹 색 순서 (실전 발사 때 같은 순서 재사용)
        private Vector2[] _safeZoneRanges; // 색(enum 인덱스)별 안전구역 x 범위(min,max) - 지형 3섹터 배정 결과
        private int[]   _safeZoneSlots;    // 색(enum 인덱스)별 안전구역 풀 슬롯 (예고 단계에서만 할당)
        private bool _gimmickFailed;      // 판정 실패로 보스전 재시작이 이미 트리거됐는지 - 남은 판정 스킵 + 중복 트리거 방지

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
            Material fogMat = Resources.Load<Material>("BossHazardFogMat");
            _pool = new BossHazardPool(POOL_SIZE, "Phase1_Gimmick", null, fogMat);

            // 분신 2체 등장. 전멸 감지는 각 분신의 OnCloneDied 이벤트로 직접 추적한다
            BossCloneController[] clones = Boss.Phase1Clones;
            if (clones != null)
            {
                for (int i = 0; i < clones.Length; ++i)
                {
                    BossCloneController clone = clones[i];
                    if (clone != null)
                    {
                        clone.Activate(i * GameDB.Boss.CloneActionOffset);
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
                    return;
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
            _gimmickFailed = false;

            // 1) 예고: 색별 안전구역 3개를 전부 켜둔 채 색 순서대로 발사. 안전구역은 슬로우 중에만 보인다
            AllocAllSafeZones();
            for (int i = 0; i < _sequence.Length; ++i)
            {
                yield return CoTelegraphLaser(_sequence[i]);
            }
            FreeAllSafeZones();

            // 2) 5초 후 실전: 같은 순서로 재발사. 색이 맞지 않는 위치면 보스전 재시작
            yield return _waitRefireDelay;
            for (int i = 0; i < _sequence.Length; ++i)
            {
                if (i > 0)
                {
                    yield return _waitJudgeInterval; // 발사 사이 이동 시간 - 즉시 판정 연발로 인한 이동 중 즉사 방지
                }
                yield return CoJudgeLaser(_sequence[i]);
                if (_gimmickFailed)
                {
                    yield break; // 재시작이 이미 트리거됨 - 남은 판정/연출은 진행하지 않는다
                }
            }

            // 파훼 성공 - BossController가 2페이즈로 전환한다
        }

        // 색 순서(랜덤 3회, 중복 허용)와 색별 안전구역 중심 x를 결정한다
        private void BuildSequence()
        {
            _sequence = new LaserColor[GameDB.Boss.GimmickLaserCount];
            for (int i = 0; i < _sequence.Length; ++i)
            {
                _sequence[i] = (LaserColor)Random.Range(0, Constants.Combat.GIMMICK_LASER_COLOR_COUNT);
            }

            BuildSafeZoneRanges();

            _safeZoneSlots = new int[Constants.Combat.GIMMICK_LASER_COLOR_COUNT];
            for (int i = 0; i < _safeZoneSlots.Length; ++i)
            {
                _safeZoneSlots[i] = SLOT_NONE;
            }
        }

        // 색별 안전구역을 지형 3섹터(좌측 구덩이/중앙 단상/우측 구덩이)에 배정한다 - 색 -> 섹터 배정만 셔플, 섹터 자체는 지형 그대로
        private void BuildSafeZoneRanges()
        {
            Vector2[] sectors    = Boss.GimmickSectors; // 인덱스 고정: 0=좌측 구덩이, 1=중앙 단상, 2=우측 구덩이
            int       colorCount = Constants.Combat.GIMMICK_LASER_COLOR_COUNT;

            // 색 -> 섹터 배정 셔플 (Fisher-Yates) - 어느 색이 어느 섹터에 올지는 매번 달라진다
            int[] segmentOrder = new int[colorCount];
            for (int i = 0; i < colorCount; ++i)
            {
                segmentOrder[i] = i;
            }
            for (int i = colorCount - 1; i > 0; --i)
            {
                int j    = Random.Range(0, i + 1);
                int temp = segmentOrder[i];
                segmentOrder[i] = segmentOrder[j];
                segmentOrder[j] = temp;
            }

            _safeZoneRanges = new Vector2[colorCount];
            for (int i = 0; i < colorCount; ++i)
            {
                _safeZoneRanges[i] = sectors[segmentOrder[i]];
            }
        }

        // 예고 1발: 텔레그래프 대기 -> 전장 레이저 연출(판정 없음). 대기 중 매 프레임 안전구역 표시를 갱신한다
        private IEnumerator CoTelegraphLaser(LaserColor color)
        {
            float elapsed = 0f;
            while (elapsed < GameDB.Boss.GimmickTelegraphTime)
            {
                elapsed += Time.deltaTime;
                UpdateSafeZoneVisibility();
                yield return null;
            }

            yield return CoFireLaser(color);
        }

        // 실전 1발: 발사 순간 해당 색 안전구역 밖이면 보스전 재시작 -> 아니면 전장 레이저 연출
        private IEnumerator CoJudgeLaser(LaserColor color)
        {
            if (!IsPlayerInSafeZone(color))
            {
                _gimmickFailed = true;
                Boss.RestartBossFight(); // 특정 씬/위치 리스폰은 기획 확정 후 RestartBossFight 내부만 교체하면 됨
                yield break;
            }
            yield return CoFireLaser(color);
        }

        // 3섹터(좌측 구덩이~우측 구덩이) 전체를 덮는 레이저 연출 - 일반 아레나(ArenaMinX/MaxX, 중앙 단상)보다 넓다
        // 즉사 판정은 위치 검사로 별도 처리하므로 콜라이더는 없다
        private IEnumerator CoFireLaser(LaserColor color)
        {
            Vector2[] sectors = Boss.GimmickSectors;
            float   width   = sectors[2].y - sectors[0].x;
            float   centerX = (sectors[0].x + sectors[2].y) * 0.5f;
            Vector2 pos     = GimmickHazardPosition(centerX);
            Vector2 scale   = GimmickHazardScale(width);

            int slot = _pool.Alloc(pos, scale, ColorOf(color), false);

            // 레이저는 3섹터 전체 폭이라 안전구역(색별 1섹터)과 겹쳐 보이면 색이 섞여 보인다
            // 안전구역은 발사 전까지만 표시하고 발사 중엔 숨긴다 (실전 단계는 이미 미할당이라 무시됨)
            HideAllSafeZones();

            float elapsed = 0f;
            while (elapsed < GameDB.Boss.GimmickLaserActiveTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            _pool.Free(slot);

            // TODO: 레이저 발사 이펙트/사운드/화면 흔들림 (Constants.Audio / ParticlePresets)
        }

        // 색별 안전구역 3개를 일괄 할당. 예고 단계 시작 시 호출한다
        private void AllocAllSafeZones()
        {
            for (int i = 0; i < _safeZoneSlots.Length; ++i)
            {
                _safeZoneSlots[i] = AllocSafeZone((LaserColor)i);
            }
        }

        // 예고 단계 종료 시 안전구역 일괄 반환
        private void FreeAllSafeZones()
        {
            for (int i = 0; i < _safeZoneSlots.Length; ++i)
            {
                _pool.Free(_safeZoneSlots[i]);
                _safeZoneSlots[i] = SLOT_NONE;
            }
        }

        // 슬로우 상태(IsSlow)에 따라 안전구역 렌더러를 일괄 갱신. 입력이 hold든 토글이든 상태만 따른다
        private void UpdateSafeZoneVisibility()
        {
            bool visible = SlowMotionController.IsSlow;
            for (int i = 0; i < _safeZoneSlots.Length; ++i)
            {
                _pool.SetVisible(_safeZoneSlots[i], visible);
            }
        }

        // 안전구역을 슬로우 여부와 무관하게 강제로 숨긴다
        // 레이저 발사 중 색 겹침 방지용
        private void HideAllSafeZones()
        {
            for (int i = 0; i < _safeZoneSlots.Length; ++i)
            {
                _pool.SetVisible(_safeZoneSlots[i], false);
            }
        }

        private int AllocSafeZone(LaserColor color)
        {
            Vector2 range   = _safeZoneRanges[(int)color];
            float   centerX = (range.x + range.y) * 0.5f;
            float   width   = range.y - range.x;
            Vector2 pos     = GimmickHazardPosition(centerX);
            Vector2 scale   = GimmickHazardScale(width);

            return _pool.Alloc(pos, scale, SafeZoneColorOf(color), false);
        }

        private Vector2 GimmickHazardPosition(float centerX)
        {
            float height = GimmickHazardHeight();
            return new Vector2(centerX, Boss.GimmickHazardBottomY + (height * 0.5f));
        }

        private Vector2 GimmickHazardScale(float width)
        {
            return new Vector2(width, GimmickHazardHeight());
        }

        private float GimmickHazardHeight()
        {
            float topY = Boss.ArenaGroundY + GameDB.Boss.GimmickLaserHeight;
            return topY - Boss.GimmickHazardBottomY;
        }

        // 판정은 x축만 사용한다 (안전구역은 세로 전체를 덮는 기둥) - 섹터 범위 안이면 생존
        private bool IsPlayerInSafeZone(LaserColor color)
        {
            if (Boss.Player == null)
            {
                return true;
            }
            float   px    = Boss.Player.transform.position.x;
            Vector2 range = _safeZoneRanges[(int)color];
            return (px >= range.x) && (px <= range.y);
        }

        /****************************************
        *             색상 유틸
        ****************************************/

        private static Color ColorOf(LaserColor color)
        {
            Color pure;
            switch (color)
            {
                case LaserColor.Red:
                    pure = Color.red;
                    break;
                case LaserColor.Blue:
                    pure = Color.blue;
                    break;
                default:
                    pure = Color.green;
                    break;
            }
            return Color.Lerp(pure, Color.white, 0.35f);
        }

        private static Color SafeZoneColorOf(LaserColor color)
        {
            Color c = ColorOf(color);
            c.a = GameDB.Boss.GimmickSafeZoneAlpha;
            return c;
        }
    }
}
