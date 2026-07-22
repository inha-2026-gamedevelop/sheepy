// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.TimeSystem;
using Minsung.CameraSystem;
using Minsung.Boss2;

// 4페이즈 진입 연출 오케스트레이터 - Boss2Health가 최종 페이즈(4페이즈)로 전환(OnPhaseChanged)될 때 1회 발동한다.
// 순서: 리와인드 잠금 + 일반 패턴 정지 + 피격 동결 -> Phase4Aim 지점으로 보스 이동(+카메라 포커스) ->
//   뒤 오라 파티클 ON + Scream 클립을 정/역 순환(입 벌림/다물기)으로 지정 시간만큼 재생 ->
//   Idle 복귀 + 오라 OFF + 카메라 복귀 + 동결/잠금 해제(보스는 Phase4Aim에 남아 4페이즈 전투 재개)
// 골격은 Boss2SpaceTearPattern(리와인드 잠금 + 패턴 정지 + 스크립트 이동 독점)과 동일한 관례를 따른다.
public class Boss2Phase4Intro : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("참조")]
    [SerializeField] private Boss2Health         _health;      // OnPhaseChanged 구독 + 피격 동결 제어
    [SerializeField] private BossFloatMovement   _movement;    // 연출 동안 이동 독점(Phase4Aim으로 대시)
    [SerializeField] private Boss2AttackPatterns _patterns;    // 연출 동안 일반 패턴 정지/재개
    [SerializeField] private Boss2DataSO         _dataSo;

    [Header("연출 배치")]
    [SerializeField] private Transform  _phase4Aim;    // 보스가 이동할 목표 지점(씬의 Phase4Aim)
    [SerializeField] private Transform  _cameraTip;    // 카메라 포커스 지점 - 미배치 시 _phase4Aim을 사용
    [SerializeField] private Animator[] _screamAnimators; // Scream 상태를 재생할 애니메이터(보스 Visual/Body)
    [SerializeField] private GameObject _aura;         // 보스 뒤 오라 파티클(비활성 시작, 연출 동안만 ON)

    [Header("Scream 순환")]
    [Tooltip("입 벌림(0→1) 또는 다물기(1→0) 한 방향에 걸리는 시간(초). 작을수록 빠르게 벌렸다 다문다.")]
    [SerializeField] private float _screamOpenCloseTime = 0.4f;

    private static readonly int SCREAM_STATE = Animator.StringToHash("Scream");
    private static readonly int IDLE_STATE   = Animator.StringToHash("Idle");

    private bool _played;  // 한 전투 1회만 - 최종 페이즈 진입은 한 번뿐이지만 안전용
    private bool _running;
    private RewindManager.RewindLockHandle? _rewindLock;

    /****************************************
    *              Unity Event
    ****************************************/

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnPhaseChanged -= HandlePhaseChanged;
        }
        if (_running)
        {
            Cleanup(); // 씬 언로드/비활성에서도 동결/포커스/잠금이 잔류하지 않도록
            _running = false;
        }
        if (_aura != null)
        {
            _aura.SetActive(false); // 오라는 전투 내내 켜져 있으므로 컴포넌트가 꺼질 때(씬 언로드 등)만 최종적으로 정리
        }
    }

    /****************************************
    *                Methods
    ****************************************/

    private void HandlePhaseChanged(int phaseIndex)
    {
        // 최종 페이즈(4페이즈) 진입 시 1회 발동. 3페이즈(index 0)에는 반응하지 않는다.
        if (_played || (_health == null) || !_health.IsFinalPhase)
        {
            return;
        }
        _played  = true;
        _running = true;
        StartCoroutine(CoIntro());
    }

    private IEnumerator CoIntro()
    {
        // 연출 동안 리와인드 잠금 + 일반 패턴 정지 + 피격 동결 + 이동 독점(배회/돌진 정지)
        _rewindLock = RewindManager.Instance?.AcquireRewindLock(this);
        _patterns?.SuspendNormalPatterns();
        _health?.BeginPhase4IntroFreeze();
        bool began = (_movement != null) && _movement.TryBeginScriptedMovement();

        // 카메라 포커스(Phase4Aim 지점 클로즈업)
        Transform tip = (_cameraTip != null) ? _cameraTip : _phase4Aim;
        if ((tip != null) && (_dataSo != null))
        {
            CameraManager.Instance?.Focus(tip, _dataSo.Phase4IntroCameraSize, _dataSo.Phase4IntroCameraBlend);
        }

        // 보스를 Phase4Aim 지점으로 이동(카메라와 함께)
        if (began && (_phase4Aim != null) && (_dataSo != null))
        {
            Vector2 start = _movement.transform.position;
            yield return _movement.CoScriptedDash(start, _phase4Aim.position, _dataSo.Phase4IntroMoveSpeed);
        }

        // 뒤 오라 ON
        if (_aura != null)
        {
            _aura.SetActive(true);
        }

        // Scream 정/역 순환(입 벌림/다물기)을 지정 시간만큼 재생
        float duration = (_dataSo != null) ? _dataSo.Phase4IntroScreamDuration : 3f;
        yield return CoScreamCycle(duration);

        // 정리 - 보스는 Phase4Aim에 남고(EndScriptedMovement가 그 자리를 새 배회 중심으로 삼음), 카메라만 플레이어로 복귀
        Cleanup();
        _running = false;
    }

    // Scream 클립을 speed 0으로 고정해 두고 매 프레임 normalizedTime을 핑퐁(0→1→0)으로 직접 스크럽한다.
    // 컨트롤러에 트리거 파라미터가 없어 Animator.Play로 상태를 직접 재생/스크럽한다.
    private IEnumerator CoScreamCycle(float duration)
    {
        for (int i = 0; i < _screamAnimators.Length; ++i)
        {
            Animator a = _screamAnimators[i];
            if (a != null)
            {
                a.speed = 0f;
                a.Play(SCREAM_STATE, 0, 0f);
                a.Update(0f);
            }
        }

        float half    = Mathf.Max(0.05f, _screamOpenCloseTime);
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            float phase = Mathf.PingPong(elapsed / half, 1f); // 0→1→0 반복 = 입 벌림→다물기 순환
            for (int i = 0; i < _screamAnimators.Length; ++i)
            {
                Animator a = _screamAnimators[i];
                if (a != null)
                {
                    a.Play(SCREAM_STATE, 0, phase);
                    a.Update(0f);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // 정상 종료/비활성/예외 어디서 불려도 동일하게 원상복구(두 번 호출해도 안전)
    // 오라는 여기서 끄지 않는다 - 4페이즈 진입 후에는 전투 내내 계속 켜져 있어야 한다(OnDisable에서만 최종 정리)
    private void Cleanup()
    {
        for (int i = 0; i < _screamAnimators.Length; ++i)
        {
            Animator a = _screamAnimators[i];
            if (a != null)
            {
                a.speed = 1f;
                a.Play(IDLE_STATE, 0, 0f); // Scream 스크럽을 풀고 Idle로 복귀
            }
        }

        CameraManager.Instance?.UnFocus();
        _movement?.EndScriptedMovement(); // 현재 위치(Phase4Aim)를 새 배회 중심으로 삼고 배회/돌진 재개
        _patterns?.ResumeNormalPatterns();
        _health?.EndPhase4IntroFreeze();

        if (_rewindLock.HasValue)
        {
            _rewindLock.Value.Dispose();
            _rewindLock = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_screamAnimators == null || _screamAnimators.Length == 0)
        {
            Debug.LogWarning("[Boss2Phase4Intro] _screamAnimators 미배치 - 보스 Visual/Body의 Animator를 연결해야 Scream 연출이 재생됩니다.", this);
        }
        if (_phase4Aim == null)
        {
            Debug.LogWarning("[Boss2Phase4Intro] _phase4Aim 미배치 - 보스 이동 목표(Phase4Aim)를 연결해야 합니다.", this);
        }
    }
#endif
}
