// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.TimeSystem
{
    // 타격감용 히트스톱 - 공격이 실제로 꽂힌 순간 아주 짧게 시간을 멈춘다.
    // 슬로우모션과 별개 플래그(IsActive)로 관리하고, 종료 시 슬로우모션의 목표 배율로 복원한다.
    public class HitStopController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        public static HitStopController Instance { get; private set; }

        // 히트스톱 진행 중 여부 - SlowMotionController가 timeScale 쓰기 충돌 방지에 참조
        public static bool IsActive { get; private set; }

        private float _stopUntil; // 히트스톱 종료 목표 시각(실시간) - 연속 적중 시 이 값만 뒤로 밀린다

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 초기화되도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            IsActive = false;
        }

        // 씬에 배치하지 않아도 동작하도록 씬 로드 후 자동 생성
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject("HitStopController").AddComponent<HitStopController>();
            }
        }

        private void Awake()
        {
            if ((Instance != null) && (Instance != this))
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }
            // 진행 중이던 히트스톱 복원 보장 (씬 전환 안전망)
            if (IsActive)
            {
                Time.timeScale = SlowMotionController.TargetTimeScale;
                IsActive       = false;
            }
            Instance = null;
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 히트스톱 요청. 되감기 중에는 무시, 연속 적중 시 지속시간이 새로 시작된다. </summary>
        public static void Request()
        {
            if (Instance == null)
            {
                return;
            }
            if ((RewindManager.Instance != null) && RewindManager.Instance.IsRewinding)
            {
                return;
            }
            Instance.Play();
        }

        // 코루틴을 정지 후 재시작하면 이전 코루틴이 복원 코드를 못 밟고 끊긴다
        // (연속 적중 시 IsActive/timeScale이 눌러앉는 버그) - 종료 목표 시각만 뒤로 미루고
        // 코루틴 자체는 하나만 계속 돌려 마지막 적중 이후 반드시 복원까지 도달하게 한다
        private void Play()
        {
            _stopUntil = Time.realtimeSinceStartup + Constants.Combat.HIT_STOP_DURATION;

            if (!IsActive)
            {
                IsActive       = true;
                Time.timeScale = 0f;
                StartCoroutine(CoHitStop());
            }
        }

        // timeScale 0 동안에도 흐르도록 실시간 기준으로 대기 (무할당)
        private IEnumerator CoHitStop()
        {
            while (Time.realtimeSinceStartup < _stopUntil)
            {
                yield return null;
            }

            Time.timeScale = SlowMotionController.TargetTimeScale;
            IsActive = false;
        }
    }
}
