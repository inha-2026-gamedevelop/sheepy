// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common.Data;

namespace Minsung.Boss
{
    // 보스 사망 연출 3단계 담당 - 사출(지정 방향 직선 이동) -> 부유(제자리 상하 흔들림) -> 귀환(목표 지점으로 이동 후 정착)
    // DeathCircle.anim(루프) 재생은 전용 Animator가 계속 담당, 이 스크립트는 위치만 갱신
    [RequireComponent(typeof(SpriteRenderer))]
    public class BossDeathCircleFx : MonoBehaviour
    {
        [SerializeField] private Transform _returnTarget; // 귀환 목적지 (씬 배치, 예: CircleAim)

        private Coroutine _sequenceCoroutine;

        // 사출->부유->귀환 시퀀스 진행 중인지 - 호출자가 완료를 기다려야 할 때 폴링용(BossController.CoPlayTransitionDeathSequence)
        public bool IsPlaying => _sequenceCoroutine != null;

        // 사출 방향을 받아 전체 연출 시퀀스를 처음부터 재생 (중복 호출 시 이전 진행 취소 후 재시작)
        public void PlaySequence(Vector2 launchDirection)
        {
            gameObject.SetActive(true);
            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
            }
            _sequenceCoroutine = StartCoroutine(CoSequence(launchDirection.normalized));
        }

        private IEnumerator CoSequence(Vector2 direction)
        {
            float launchSpeed    = GameDB.Boss.DeathCircleLaunchSpeed;
            float launchDuration = GameDB.Boss.DeathCircleLaunchDuration;
            float floatDuration  = GameDB.Boss.DeathCircleFloatDuration;
            float floatAmplitude = GameDB.Boss.DeathCircleFloatAmplitude;
            float floatFrequency = GameDB.Boss.DeathCircleFloatFrequency;
            float returnSpeed    = GameDB.Boss.DeathCircleReturnSpeed;

            // 1. 사출 - 지정 방향으로 빠르게 직선 이동
            float elapsed = 0f;
            while (elapsed < launchDuration)
            {
                transform.position += (Vector3)(direction * launchSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 2. 부유 - 제자리에서 상하로 천천히 흔들리며 대기
            Vector3 floatOrigin = transform.position;
            elapsed = 0f;
            while (elapsed < floatDuration)
            {
                float offsetY = Mathf.Sin(elapsed * floatFrequency) * floatAmplitude;
                transform.position = floatOrigin + new Vector3(0f, offsetY, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 3. 귀환 - 목적지로 이동 후 정착 (도착 후에도 DeathCircle.anim 루프는 계속 재생)
            if (_returnTarget != null)
            {
                while (Vector3.Distance(transform.position, _returnTarget.position) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, _returnTarget.position, returnSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = _returnTarget.position;
            }

            _sequenceCoroutine = null;
        }
    }
}
