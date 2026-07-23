// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.TimeSystem;
using Minsung.Visual;
using Minsung.UI;
using Minsung.Boss;
using Minsung.Player;

namespace Minsung.Boss2
{
    // 공간찢기(4페이즈 즉사기) 오케스트레이터 - Boss2Health가 체력 임계(기본 10%)를 처음 통과할 때 1회 발동
    // 순서: 배너 예고 -> 창 침식 시네마틱(옵션, SpaceTearWindowPresentation) -> 고정 4라인 예고(랜덤 순서) ->
    //   고정 4회 돌진 -> 대기(반응 시간) -> 보스->플레이어 돌진(무적키로 회피) -> 정리 + 동결 해제
    // 과거의 화면 균열(순차 절단) 연출은 폐기됐다 - 화면 연출은 창 침식 시네마틱이 전담한다
    // 파훼: 각 돌진 판정(Boss2DodgeableKillHazard)에 맞기 직전 전용 무적키로 회피. 절대 즉사(DamageHazard/Kill)는 건드리지 않는다
    public class Boss2SpaceTearPattern : MonoBehaviour
    {
        /****************************************
        *                Types
        ****************************************/

        [System.Serializable]
        private struct LineAnchor
        {
            public Transform Start;
            public Transform End;
        }

        private struct DashLine
        {
            public Vector2 Start;
            public Vector2 End;

            public DashLine(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }
        }

        /****************************************
        *                Fields
        ****************************************/

        [Header("참조")]
        [SerializeField] private Boss2Health         _health;
        [SerializeField] private BossFloatMovement   _movement;
        [SerializeField] private Boss2AttackPatterns _patterns;   // 시퀀스 동안 일반 패턴 정지/재개
        [SerializeField] private BossBannerUI        _banner;
        [SerializeField] private Transform           _player;     // 마지막 돌진의 조준 대상
        [SerializeField] private Boss2DataSO         _dataSo;

        [Header("고정 돌진 라인 (권장 4개, 순서는 매번 랜덤)")]
        [SerializeField] private LineAnchor[] _presetLines;

        [Header("마지막 돌진(플레이어 조준) 시작점 - 종료점은 실행 순간 플레이어 위치에서 더 연장한 지점")]
        [SerializeField] private Transform _playerLineStart;
        [SerializeField] private float _playerOvershootDistance = 3f; // 시작점->플레이어 직선을 이 거리만큼 관통해서 더 연장(플레이어 지점에서 딱 멈추지 않는다)

        [Header("예고 문구")]
        [SerializeField, TextArea] private string _bannerText = "보스보다 시간을 느리게 하여 보스의 패턴을 막아보세요!";

        [Header("창 절단 시네마틱 (옵션) - 켜면 돌진 패턴 앞에 화면이 잘리는 연출을 먼저 재생한다")]
        [SerializeField] private bool _useWindowCinematic;
        [SerializeField] private SpaceTearWindowPresentation _windowPresentation;
        [SerializeField] private float _cinematicMoveSpeed = 14f; // 중앙 이동/퇴장 속도
        [Tooltip("절단선을 가르는 돌진 속도 - 절단선 길이(약 17유닛) 기준 7이면 약 2.4초 동안 화면이 갈라진다")]
        [SerializeField] private float _cinematicDashSpeed = 7f;
        [Tooltip("화면을 부순 뒤 보스가 커지는 배율 - 되돌리지 않고 그대로 유지된다")]
        [SerializeField] private float _bossGrowMultiplier = 2f;

        private GameObject _hitbox;   // 돌진 즉사 판정(보스 몸통에 붙여 스윕)
        private BossHazardPool _telegraphPool; // 고정 라인 예고선(판정 없음) - Minsung.Boss 공용 인프라 재사용
        private RewindManager.RewindLockHandle? _rewindLock; // 시퀀스 동안 임시 리와인드 잠금
        private PlayerHealth _playerHealth;
        private bool _running;
        private bool _bossEnlarged; // 보스 확대는 한 전투에 한 번만

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnSpaceTearTriggered += HandleTrigger;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnSpaceTearTriggered -= HandleTrigger;
            }
            if (_running)
            {
                Cleanup(); // 씬 언로드/비활성에서도 동결/연출/잠금이 잔류하지 않도록
                _running = false;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 디버그 - 체력 임계와 상관없이 숫자 6으로 공간찢기를 즉시 실행한다(연출 확인용)
        // 체력 동결은 걸지 않으므로 시퀀스 도중에도 보스는 평소처럼 피해를 받는다
        private void Update()
        {
            if (Input.GetKeyDown(Constants.Combat.KEY_DEBUG_SPACE_TEAR)
                || Input.GetKeyDown(Constants.Combat.KEY_DEBUG_SPACE_TEAR_KEYPAD))
            {
                HandleTrigger(); // 이미 실행 중이면 내부 _running 가드가 무시한다
            }
        }
#endif

        /****************************************
        *                Methods
        ****************************************/

        private void HandleTrigger()
        {
            if (_running)
            {
                return;
            }
            _running = true;
            StartCoroutine(CoSpaceTear());
        }

        private IEnumerator CoSpaceTear()
        {
            if (_dataSo == null)
            {
                _health?.EndSpaceTearFreeze();
                _running = false;
                yield break;
            }

            _playerHealth ??= _player != null
                ? _player.GetComponentInParent<PlayerHealth>()
                : null;
            _playerHealth?.SetDodgeInvincibleCooldownOverride(0f);

            // 시퀀스 동안 리와인드 잠금 + 일반 패턴 정지(낙뢰/강타/레이저) + 보스 이동 독점(스크립트 이동 모드로 그 자리에 정지)
            // - 화면이 갈라지는 연출 내내 보스는 가만히 있어야 하므로 트리거 즉시(배너보다도 먼저) 멈춘다
            _rewindLock = RewindManager.Instance?.AcquireRewindLock(this);
            _patterns?.SuspendNormalPatterns();
            bool began = (_movement != null) && _movement.TryBeginScriptedMovement();

            // 배너 예고
            if (_banner != null)
            {
                _banner.Show(_bannerText, _dataSo.SpaceTearBannerTime);
            }
            yield return new WaitForSeconds(_dataSo.SpaceTearBannerTime);

            // 창 절단 시네마틱 - 보스 이동과 화면 연출을 번갈아 진행한다
            if (_useWindowCinematic && (_windowPresentation != null) && began)
            {
                yield return CoWindowCutCinematic();
            }

            // 고정 라인은 매번 랜덤 순서로 예고 후 보스가 지나간다
            List<DashLine> fixedLines = BuildFixedLines();
            Shuffle(fixedLines);
            yield return CoShowTelegraph(fixedLines);

            EnsureHitbox();
            if (_hitbox != null)
            {
                _hitbox.SetActive(true);
            }

            WaitForSeconds waitInterval = new WaitForSeconds(_dataSo.SpaceTearDashInterval);

            // 고정 라인들 - 예고선을 따라 보스가 차례로 지나간다(연출용, 결정타 아님)
            for (int i = 0; i < fixedLines.Count; ++i)
            {
                if (began)
                {
                    yield return _movement.CoScriptedDash(fixedLines[i].Start, fixedLines[i].End, _dataSo.SpaceTearDashSpeed);
                }
                yield return waitInterval;
            }

            // 마지막 - 진짜 위협: 보스가 플레이어를 향해 돌진한다. 그 전에 반응할 시간을 준다
            yield return new WaitForSeconds(_dataSo.SpaceTearPlayerWarningTime);

            Vector2 startPos = (_playerLineStart != null) ? (Vector2)_playerLineStart.position
                : (_movement != null ? (Vector2)_movement.transform.position : (Vector2)transform.position);

            // 종료점은 플레이어 위치가 아니라 시작점->플레이어 직선을 관통해서 더 연장한 지점(_playerOvershootDistance)
            // - 보스가 플레이어 지점에서 딱 멈추지 않고 뚫고 지나가는 느낌을 준다. 실행 순간 플레이어 위치 기준 - 사전 스냅샷 아님
            Vector2 endPos;
            if (_player != null)
            {
                Vector2 playerPos = _player.position;
                Vector2 dir = playerPos - startPos;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                }
                else
                {
                    dir = Vector2.right;
                }
                endPos = playerPos + (dir * _playerOvershootDistance);
            }
            else
            {
                endPos = startPos + (Vector2.right * 6f);
            }

            if (began)
            {
                // 고정 라인보다 느린 전용 속도 - 실제 파훼 대상이라 플레이어가 반응할 시간을 준다
                yield return _movement.CoScriptedDash(startPos, endPos, _dataSo.SpaceTearPlayerDashSpeed);
            }

            Cleanup();
            _running = false;
        }

        // 창 절단 시네마틱 - 보스가 화면 중앙으로 이동 -> 오른쪽 밖으로 퇴장 -> 화면 축소 ->
        //   절단선을 따라 왼쪽으로 돌진하며 화면을 가르고 -> 잘린 위쪽 조각이 바닥으로 떨어져 부서진다
        private IEnumerator CoWindowCutCinematic()
        {
            Vector2 bossPos = _movement.transform.position;

            // 1) 화면 정중앙으로
            Vector2 center = _windowPresentation.GetScreenCenterWorld();
            yield return _movement.CoScriptedDash(bossPos, center, _cinematicMoveSpeed);

            // 2) 화면 오른쪽 바깥으로 퇴장
            Vector2 offscreen = _windowPresentation.GetOffscreenRightWorld();
            yield return _movement.CoScriptedDash(center, offscreen, _cinematicMoveSpeed);

            // 3) 창이 만들어지고 화면이 줄어든다(보스는 화면 밖이라 보이지 않는다)
            yield return _windowPresentation.CoBeginWindow();
            yield return _windowPresentation.CoShrinkScreen();

            // 4) 절단선을 따라 돌진 - 자국이 보스를 따라 그어지고, 다 지나가면 화면이 갈라진다
            if (_windowPresentation.TryGetCutLineWorld(out Vector3 cutStart, out Vector3 cutEnd))
            {
                float distance = Vector3.Distance(cutStart, cutEnd);
                float dashTime = distance / Mathf.Max(0.01f, _cinematicDashSpeed);

                StartCoroutine(_windowPresentation.CoCutAlongLine(dashTime));
                yield return _movement.CoScriptedDash(cutStart, cutEnd, _cinematicDashSpeed);
            }

            // 5) 위쪽 조각이 작업표시줄까지 떨어져 산산조각 나고, 남은 화면에 HUD가 돌아온다
            yield return _windowPresentation.CoDropUpperFragment();

            // 6) 화면을 부순 보스는 두 배로 커진 채 남는다(되돌리지 않는다)
            EnlargeBossOnce();

            _windowPresentation.EndWindow();
        }

        // 보스를 지정 배율로 한 번만 키운다 - 디버그 키로 여러 번 발동해도 계속 커지지 않게 막는다
        private void EnlargeBossOnce()
        {
            if (_bossEnlarged || (_movement == null) || (_bossGrowMultiplier <= 0f))
            {
                return;
            }
            _bossEnlarged = true;
            _movement.transform.localScale *= _bossGrowMultiplier;
        }

        // 고정 프리셋 라인(앵커)만 수집한다. 프리셋이 모자라면 보스 위치 중심 방사형으로 채운다(플레이어 조준 라인은 별도 처리)
        private List<DashLine> BuildFixedLines()
        {
            List<DashLine> list = new List<DashLine>();

            if (_presetLines != null)
            {
                for (int i = 0; i < _presetLines.Length; ++i)
                {
                    LineAnchor a = _presetLines[i];
                    if ((a.Start != null) && (a.End != null))
                    {
                        list.Add(new DashLine(a.Start.position, a.End.position));
                    }
                }
            }

            // 프리셋 미배치 대비 - 목표 개수(전체 돌진 수 - 플레이어 조준 1개)까지 방사형 라인으로 채운다
            int target = Mathf.Max(1, _dataSo.SpaceTearDashCount - 1);
            Vector2 bossPos = (_movement != null) ? (Vector2)_movement.transform.position : (Vector2)transform.position;
            const float halfLen = 12f;
            int guard = 0;
            while ((list.Count < target) && (guard < target))
            {
                float ang = Mathf.Deg2Rad * (list.Count * (360f / target));
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                list.Add(new DashLine(bossPos - (dir * halfLen), bossPos + (dir * halfLen)));
                ++guard;
            }
            return list;
        }

        // Fisher-Yates - 고정 라인이 몇 번째 화면 조각을 맡을지 매 발동마다 랜덤하게 섞는다
        private static void Shuffle(List<DashLine> list)
        {
            for (int i = list.Count - 1; i > 0; --i)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // 고정 라인들을 동시에 점멸 경고선으로 표시(판정 없음) - 사각형 공식은 기존 Boss2LaserPattern과 동일(center/length/angle/scale)
        private IEnumerator CoShowTelegraph(List<DashLine> lines)
        {
            EnsureTelegraphPool(lines.Count);

            int[] slots = new int[lines.Count];
            for (int i = 0; i < lines.Count; ++i)
            {
                Vector2 delta  = lines[i].End - lines[i].Start;
                Vector2 center = (lines[i].Start + lines[i].End) * 0.5f;
                float   angle  = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                Vector2 scale  = new Vector2(delta.magnitude, _dataSo.SpaceTearTelegraphThickness);

                slots[i] = _telegraphPool.Alloc(center, scale, _dataSo.SpaceTearTelegraphColor, false, rotationDeg: angle);
            }

            bool  visible = true;
            float elapsed = 0f;
            float blink   = Mathf.Max(0.02f, _dataSo.SpaceTearTelegraphBlink);
            while (elapsed < _dataSo.SpaceTearTelegraphTime)
            {
                yield return new WaitForSeconds(blink);
                elapsed += blink;
                visible = !visible;
                for (int i = 0; i < slots.Length; ++i)
                {
                    if (slots[i] >= 0)
                    {
                        _telegraphPool.SetVisible(slots[i], visible);
                    }
                }
            }

            _telegraphPool.FreeAll(); // 실제 돌진이 시작되기 직전 예고선 정리 - 보스 몸의 실제 궤적이 그 자리를 대신한다
        }

        private void EnsureTelegraphPool(int count)
        {
            if ((_telegraphPool != null) && (_telegraphPool.Size >= count))
            {
                return;
            }
            _telegraphPool?.Dispose();
            _telegraphPool = new BossHazardPool(count, "SpaceTearTelegraph");
        }

        private void EnsureHitbox()
        {
            if (_hitbox != null)
            {
                return;
            }

            _hitbox = new GameObject("SpaceTearHitBox");
            Transform parent = (_movement != null) ? _movement.transform : transform;
            _hitbox.transform.SetParent(parent, false);
            _hitbox.transform.localPosition = Vector3.zero;

            BoxCollider2D box = _hitbox.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = _dataSo.SpaceTearHitboxSize;

            _hitbox.AddComponent<Boss2DodgeableKillHazard>();
            _hitbox.SetActive(false);
        }

        // 정상 종료/비활성/예외 어디서 불려도 동일하게 원상복구(두 번 호출해도 안전)
        private void Cleanup()
        {
            if (_hitbox != null)
            {
                _hitbox.SetActive(false);
            }
            _telegraphPool?.FreeAll();
            _windowPresentation?.Stop(); // 시네마틱 도중 중단돼도 창/카메라가 잔류하지 않게 한다

            // 이동 재개/패턴 재개는 코루틴을 시작하므로 오브젝트가 살아있을 때만 호출한다
            // (씬 언로드/종료 중에는 "Coroutine couldn't be started" 에러만 남기고 의미도 없다)
            if (gameObject.activeInHierarchy)
            {
                _movement?.EndScriptedMovement();
                _patterns?.ResumeNormalPatterns();
            }
            _health?.EndSpaceTearFreeze();
            _playerHealth?.ClearDodgeInvincibleCooldownOverride();

            if (_rewindLock.HasValue)
            {
                _rewindLock.Value.Dispose();
                _rewindLock = null;
            }
        }

        private void OnDestroy()
        {
            _telegraphPool?.Dispose();
        }

    #if UNITY_EDITOR
        // 프리셋 라인/플레이어 시작점 배치 실수를 바로 알 수 있도록 경고
        private void OnValidate()
        {
            if ((_presetLines != null) && (_presetLines.Length != 4))
            {
                Debug.LogWarning($"[Boss2SpaceTearPattern] 고정 라인은 4개를 권장합니다(현재 {_presetLines.Length}개) - 나머지 1개는 플레이어 조준으로 자동 추가됩니다.", this);
            }
            if (_playerLineStart == null)
            {
                Debug.LogWarning("[Boss2SpaceTearPattern] _playerLineStart 미배치 - 실행 시 보스의 현재 위치를 시작점으로 대체합니다.", this);
            }
        }

        // 고정 라인 4개 + 플레이어 조준 라인의 시작점을 Scene View에 표시 - 앵커 배치 확인용
        private void OnDrawGizmosSelected()
        {
            if (_presetLines != null)
            {
                for (int i = 0; i < _presetLines.Length; ++i)
                {
                    LineAnchor a = _presetLines[i];
                    if ((a.Start == null) || (a.End == null))
                    {
                        continue;
                    }
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(a.Start.position, a.End.position);
                    UnityEditor.Handles.Label((a.Start.position + a.End.position) * 0.5f, "Line " + i);
                }
            }
            if (_playerLineStart != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_playerLineStart.position, 0.3f);
                UnityEditor.Handles.Label(_playerLineStart.position, "Player Line Start (End=플레이어 실시간 위치)");
            }
        }
    #endif
    }
}
