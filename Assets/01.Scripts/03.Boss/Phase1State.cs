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

        private const int POOL_SIZE = 4;  // 동시 사용 최대: 안전구역 3(색별) + 레이저 1 //26 07 16 kjw
        private const int SLOT_NONE = -1; // 풀 미할당 슬롯 표시 (BossHazardPool.Alloc 실패 반환값과 동일) //26 07 16 kjw

        private readonly WaitForSeconds _waitRefireDelay = new WaitForSeconds(GameDB.Boss.GimmickRefireDelay);

        private BossHazardPool _pool;
        private LaserColor[] _sequence;   // 즉사 기믹 색 순서 (실전 발사 때 같은 순서 재사용)
        private float[] _safeZoneCenters; // 색(enum 인덱스)별 안전구역 중심 x
        private int[]   _safeZoneSlots;   // 색(enum 인덱스)별 안전구역 풀 슬롯 (예고 단계에서만 할당) //26 07 16 kjw

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
            Material wavyMat = Resources.Load<Material>("WavyGimmickMat");
            _pool = new BossHazardPool(POOL_SIZE, "Phase1_Gimmick", null, wavyMat);

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

            // 1) 예고: 색별 안전구역 3개를 전부 켜둔 채 색 순서대로 발사. 안전구역은 슬로우 중에만 보인다
            AllocAllSafeZones(); //26 07 16 kjw
            for (int i = 0; i < _sequence.Length; ++i)
            {
                yield return CoTelegraphLaser(_sequence[i]);
            }
            FreeAllSafeZones(); //26 07 16 kjw

            // 2) 5초 후 실전: 같은 순서로 재발사. 색이 맞지 않는 위치면 즉사
            yield return _waitRefireDelay;
            for (int i = 0; i < _sequence.Length; ++i)
            {
                yield return CoJudgeLaser(_sequence[i]);
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

            BuildSafeZoneCenters(); //26 07 16 kjw

            _safeZoneSlots = new int[Constants.Combat.GIMMICK_LASER_COLOR_COUNT]; //26 07 16 kjw
            for (int i = 0; i < _safeZoneSlots.Length; ++i)
            {
                _safeZoneSlots[i] = SLOT_NONE; //26 07 16 kjw
            }
        }

        // 색별 안전구역 중심 x를 결정한다 - 아레나를 색 수만큼 균등 분할해 구역 겹침을 원천 차단하고, 색-구간 배정은 셔플한다 //26 07 16 kjw
        // TODO: 레벨 디자인(고정 지점) 배치 여부 기획 확정
        private void BuildSafeZoneCenters()
        {
            int   colorCount   = Constants.Combat.GIMMICK_LASER_COLOR_COUNT;
            float halfWidth    = GameDB.Boss.GimmickSafeZoneWidth * 0.5f;
            float segmentWidth = (Boss.ArenaMaxX - Boss.ArenaMinX) / colorCount;

            // 색 -> 구간 배정 셔플 (특정 색이 항상 같은 쪽에 오지 않도록)
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

            _safeZoneCenters = new float[colorCount];
            for (int i = 0; i < colorCount; ++i)
            {
                float segMinX = Boss.ArenaMinX + (segmentOrder[i] * segmentWidth);
                float minX    = segMinX + halfWidth;
                float maxX    = (segMinX + segmentWidth) - halfWidth;

                if (maxX < minX)
                {
                    // 아레나 폭 < 구역 폭 x 색 수 - 구간을 넘어 겹칠 수 있어 구간 중앙에 고정한다
                    Debug.LogWarning("Phase1 안전구역 폭이 아레나에 비해 넓습니다 - 구간 중앙 고정 배치");
                    _safeZoneCenters[i] = segMinX + (segmentWidth * 0.5f);
                }
                else
                {
                    _safeZoneCenters[i] = Random.Range(minX, maxX);
                }
            }
        }

        // 예고 1발: 텔레그래프 대기 -> 전장 레이저 연출(판정 없음). 대기 중 매 프레임 안전구역 표시를 갱신한다 //26 07 16 kjw
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

        // 실전 1발: 발사 순간 해당 색 안전구역 밖이면 즉사 -> 전장 레이저 연출
        private IEnumerator CoJudgeLaser(LaserColor color)
        {
            if (!IsPlayerInSafeZone(color))
            {
                // TODO: 기획 확정 시 즉사 대신 1페이즈 처음으로 되돌리는 페이즈 리셋으로 교체 //26 07 16 kjw
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

            // 예고 단계에선 레이저 연출 중에도 안전구역 표시가 끊기지 않도록 프레임 루프로 대기한다 //26 07 16 kjw
            // 실전 단계에선 안전구역이 미할당(SLOT_NONE)이라 표시 갱신이 무시된다
            float elapsed = 0f;
            while (elapsed < GameDB.Boss.GimmickLaserActiveTime)
            {
                elapsed += Time.deltaTime;
                UpdateSafeZoneVisibility();
                yield return null;
            }
            _pool.Free(slot);

            // TODO: 레이저 발사 이펙트/사운드/화면 흔들림 (Constants.Audio / ParticlePresets)
        }

        // 색별 안전구역 3개를 일괄 할당. 예고 단계 시작 시 호출한다 //26 07 16 kjw
        private void AllocAllSafeZones()
        {
            for (int i = 0; i < _safeZoneSlots.Length; ++i)
            {
                _safeZoneSlots[i] = AllocSafeZone((LaserColor)i);
            }
        }

        // 예고 단계 종료 시 안전구역 일괄 반환 //26 07 16 kjw
        private void FreeAllSafeZones()
        {
            for (int i = 0; i < _safeZoneSlots.Length; ++i)
            {
                _pool.Free(_safeZoneSlots[i]);
                _safeZoneSlots[i] = SLOT_NONE;
            }
        }

        // 슬로우 상태(IsSlow)에 따라 안전구역 렌더러를 일괄 갱신. 입력이 hold든 토글이든 상태만 따른다 //26 07 16 kjw
        private void UpdateSafeZoneVisibility()
        {
            bool visible = SlowMotionController.IsSlow;
            for (int i = 0; i < _safeZoneSlots.Length; ++i)
            {
                _pool.SetVisible(_safeZoneSlots[i], visible);
            }
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
