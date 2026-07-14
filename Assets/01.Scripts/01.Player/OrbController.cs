// System
using System;
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Utility;

namespace Minsung.Player
{
    // 플레이어를 따라다니는 오브 하나. 평소에는 지정 오프셋 주변을 둥실거리며 따라오고,
    // Attack()이 호출되면 대상에게 돌진해 도달 시 타격 콜백을 실행한 뒤 다시 따라온다.
    public class OrbController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private Transform _followTarget;
        private Vector2 _offset;
        private float _bobPhase;   // 오브끼리 둥실거림이 겹치지 않게 하는 값
        private Vector3 _velocity; // SmoothDamp용
        private Coroutine _coAttack;
        private PlayerDataSO _playerSo; // 오브 밸런싱 DB 캐시 (매 프레임 둥실거림 계산에 사용)

        public bool IsAttacking => _coAttack != null;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _playerSo = GameDB.Player;
        }

        private void OnDisable()
        {
            _coAttack = null;
        }

        private void Update()
        {
            if (IsAttacking || (_followTarget == null))
            {
                return;
            }

            float bob = Mathf.Sin((Time.time * _playerSo.OrbBobSpeed) + _bobPhase) * _playerSo.OrbBobAmplitude;
            Vector3 anchor = _followTarget.position + (Vector3)_offset + (Vector3.up * bob);
            transform.position = Vector3.SmoothDamp(transform.position, anchor, ref _velocity, _playerSo.OrbFollowSmooth);
        }

        /****************************************
        *                Methods
        ****************************************/

        public void Init(Transform followTarget, Vector2 offset, float bobPhase)
        {
            _followTarget      = followTarget;
            _offset            = offset;
            _bobPhase          = bobPhase;
            transform.position = followTarget.position + (Vector3)offset;
        }

        /// <summary> 대기 위치로 즉시 순간이동. 주인(분신)이 풀에서 다시 활성화될 때 호출. </summary>
        public void SnapToFollowTarget()
        {
            if (_followTarget == null)
            {
                return;
            }
            _velocity          = Vector3.zero; // 이전 활성 시점의 관성 제거
            transform.position = _followTarget.position + (Vector3)_offset;
        }

        /// <summary> 대상에게 돌진해 도달하면 onHit을 1회 실행. 이미 공격 중이면 새 공격으로 교체. </summary>
        public void Attack(Transform target, Action onHit)
        {
            UtilCoroutine.CheckRunCoroutine(ref _coAttack, StartCoroutine(CoAttack(target, onHit)), this);
        }

        private IEnumerator CoAttack(Transform target, Action onHit)
        {
            Vector3 lastKnownPos = (target != null) ? target.position : transform.position;
            float elapsed = 0f;

            while (elapsed < _playerSo.OrbDashTimeout)
            {
                // 움직이는 대상 추적, 죽으면 마지막 위치로
                if (target != null)
                {
                    lastKnownPos = target.position;
                }

                transform.position = Vector3.MoveTowards(transform.position, lastKnownPos, _playerSo.OrbDashSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, lastKnownPos) <= _playerSo.OrbHitDistance)
                {
                    onHit?.Invoke();
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _velocity = Vector3.zero;
            _coAttack = null;
        }
    }
}
