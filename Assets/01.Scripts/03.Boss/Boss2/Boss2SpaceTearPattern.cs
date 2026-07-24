// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.TimeSystem;
using Minsung.Visual;
using Minsung.UI;
using Minsung.Player;

namespace Minsung.Boss2
{
    // 공간찢기(4페이즈 즉사기) 오케스트레이터 - Boss2Health가 체력 임계(기본 10%)를 처음 통과할 때 1회 발동
    // 순서: 배너 예고 -> 창 절단 시네마틱(가로지르며 공간을 찢음) -> 보스 5배 확대 -> 플레이어를 삼키듯 돌진(잡아먹기)
    //   -> 삼키는 순간 전용 무적키(Ctrl)로 회피 못하면 즉사 -> 정리 + 동결 해제
    // 과거의 고정 4라인 예고 + 5회 돌진 연출은 폐기됐다 - 이제 절단 후 '보스가 커져 삼키는' 단일 파훼로 대체된다
    // 파훼: 삼키기 판정(Boss2DodgeableKillHazard)에 맞기 직전 전용 무적키로 회피. 절대 즉사(DamageHazard/Kill)는 건드리지 않는다
    public class Boss2SpaceTearPattern : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("참조")]
        [SerializeField] private Boss2Health         _health;
        [SerializeField] private BossFloatMovement   _movement;
        [SerializeField] private Boss2AttackPatterns _patterns;   // 시퀀스 동안 일반 패턴 정지/재개
        [SerializeField] private BossBannerUI        _banner;
        [SerializeField] private Transform           _player;     // 삼키기 돌진의 조준 대상
        [SerializeField] private Boss2DataSO         _dataSo;

        [Header("삼키기 돌진 시작점(비우면 보스 현재 위치) - 종료점은 실행 순간 플레이어 위치에서 더 연장한 지점")]
        [SerializeField] private Transform _playerLineStart;
        [SerializeField] private float _playerOvershootDistance = 3f; // 시작점->플레이어 직선을 이 거리만큼 관통해서 더 연장(플레이어 지점에서 딱 멈추지 않는다)

        [Header("예고 문구")]
        [SerializeField, TextArea] private string _bannerText = "보스보다 시간을 느리게 하여 보스의 패턴을 막아보세요!";
        [SerializeField, TextArea] private string _swallowWarningText = "잡아먹힌다! Ctrl로 회피하라!";

        [Header("창 절단 시네마틱 - 가로지르며 공간을 찢는 연출")]
        [SerializeField] private SpaceTearWindowPresentation _windowPresentation;
        [SerializeField] private float _cinematicMoveSpeed = 14f; // 중앙 이동/퇴장 속도
        [Tooltip("절단선을 가르는 돌진 속도 - 절단선 길이(약 17유닛) 기준 7이면 약 2.4초 동안 화면이 갈라진다")]
        [SerializeField] private float _cinematicDashSpeed = 7f;

        [Header("삼키기 - 보스 확대")]
        [Tooltip("공간을 찢은 뒤 보스가 커지는 배율 - 되돌리지 않고 그대로 유지된다")]
        [SerializeField] private float _bossGrowMultiplier = 5f;
        [Tooltip("확대된 보스가 커지는 데 걸리는 시간(초)")]
        [SerializeField] private float _bossGrowTime = 0.6f;
        [Tooltip("'잡아먹힌다' 문구를 화면에 유지하는 시간(초) - 이 문구가 완전히 닫힌 뒤 다시 _killDelayAfterSwallowBanner 후에 즉사 판정")]
        [SerializeField] private float _swallowDelay = 1.5f;
        [Tooltip("'잡아먹힌다' 문구가 닫히고 나서 먹히는 판정(즉사)까지의 대기 시간(초) - 이 사이 Ctrl로 회피")]
        [SerializeField] private float _killDelayAfterSwallowBanner = 1f;

        private GameObject _hitbox;   // 삼키기 즉사 판정(보스 몸통에 붙여 스윕)
        private RewindManager.RewindLockHandle? _rewindLock; // 시퀀스 동안 임시 리와인드 잠금
        private PlayerHealth _playerHealth;
        private bool _running;
        private bool _bossEnlarged; // 보스 확대는 시퀀스 중 한 번만
        private Vector3 _bossOriginalScale; // 확대 전 원래 스케일 - 패턴 종료 시 복원
        private bool _bossScaleSaved;

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

            // 창 절단 시네마틱 - 보스가 화면을 가로지르며 공간을 찢는다
            if ((_windowPresentation != null) && began)
            {
                yield return CoWindowCutCinematic();

                // 절단 후 보스는 화면 밖(왼쪽)이라 그 자리서 커지면 안 보인다 - 확대가 보이도록 화면 중앙으로 데려온다
                Vector2 center = _windowPresentation.GetScreenCenterWorld();
                yield return _movement.CoScriptedDash(_movement.transform.position, center, _cinematicMoveSpeed);
            }

            // 공간을 찢은 뒤 보스가 5배로 커진다(잡아먹기 예고)
            yield return CoEnlargeBoss();

            // 삼키기 - '잡아먹힌다' 문구를 띄우고, 문구가 완전히 닫힌 뒤 다시 _killDelayAfterSwallowBanner(1초) 후에 먹히는 판정(즉사)을 한다
            if (_banner != null)
            {
                _banner.Show(_swallowWarningText, _swallowDelay);

                // 문구가 페이드아웃까지 끝나 완전히 닫힐 때까지 대기
                while (_banner.IsShowing)
                {
                    yield return null;
                }
            }

            // 문구가 닫히고 나서 즉사 판정까지의 반응 시간 - 이 사이 Ctrl로 회피
            yield return new WaitForSeconds(_killDelayAfterSwallowBanner);

            EnsureHitbox();
            if (_hitbox != null)
            {
                _hitbox.SetActive(true);
            }

            if (began)
            {
                yield return _movement.CoScriptedDash(GetSwallowStart(), GetSwallowEnd(), _dataSo.SpaceTearPlayerDashSpeed);
            }

            Cleanup();
            _running = false;
        }

        // 삼키기 돌진 시작점 - 지정 앵커가 있으면 그곳, 없으면 보스 현재 위치
        private Vector2 GetSwallowStart()
        {
            if (_playerLineStart != null)
            {
                return _playerLineStart.position;
            }
            return (_movement != null) ? (Vector2)_movement.transform.position : (Vector2)transform.position;
        }

        // 삼키기 돌진 종료점 - 플레이어 위치가 아니라 시작점->플레이어 직선을 관통해서 더 연장한 지점(_playerOvershootDistance)
        // 보스가 플레이어 지점에서 딱 멈추지 않고 뚫고 지나가며 삼키는 느낌을 준다. 실행 순간 플레이어 위치 기준 - 사전 스냅샷 아님
        private Vector2 GetSwallowEnd()
        {
            Vector2 startPos = GetSwallowStart();
            if (_player == null)
            {
                return startPos + (Vector2.right * 6f);
            }

            Vector2 playerPos = _player.position;
            Vector2 dir = playerPos - startPos;
            dir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;
            return playerPos + (dir * _playerOvershootDistance);
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
            // 창은 여기서 닫지 않는다 - 잡아먹힐 때까지 잘린 창을 유지하고, 삼키기 판정 후 Cleanup에서 닫는다
            yield return _windowPresentation.CoDropUpperFragment();
        }

        // 보스를 지정 배율까지 부드럽게 한 번만 키운다 - 디버그 키로 여러 번 발동해도 계속 커지지 않게 막는다
        private IEnumerator CoEnlargeBoss()
        {
            if (_bossEnlarged || (_movement == null) || (_bossGrowMultiplier <= 0f))
            {
                yield break;
            }
            _bossEnlarged = true;

            Transform bossTf   = _movement.transform;
            Vector3   startScl = bossTf.localScale;
            _bossOriginalScale = startScl; // 종료 시 이 크기로 되돌린다
            _bossScaleSaved    = true;
            Vector3   endScl   = startScl * _bossGrowMultiplier;
            float     dur      = Mathf.Max(0.01f, _bossGrowTime);
            float     elapsed  = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dur));
                bossTf.localScale = Vector3.Lerp(startScl, endScl, t);
                yield return null;
            }
            bossTf.localScale = endScl;
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

            // 확대했던 보스 크기를 원래대로 되돌린다
            if (_bossScaleSaved && (_movement != null))
            {
                _movement.transform.localScale = _bossOriginalScale;
            }
            _bossScaleSaved = false;
            _bossEnlarged   = false;

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

    #if UNITY_EDITOR
        // 삼키기 돌진 시작점 배치 실수를 바로 알 수 있도록 경고
        private void OnValidate()
        {
            if (_playerLineStart == null)
            {
                Debug.LogWarning("[Boss2SpaceTearPattern] _playerLineStart 미배치 - 실행 시 보스의 현재 위치를 시작점으로 대체합니다.", this);
            }
        }

        // 삼키기 돌진 시작점을 Scene View에 표시 - 앵커 배치 확인용
        private void OnDrawGizmosSelected()
        {
            if (_playerLineStart != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_playerLineStart.position, 0.3f);
                UnityEditor.Handles.Label(_playerLineStart.position, "Swallow Dash Start (End=플레이어 실시간 위치)");
            }
        }
    #endif
    }
}
