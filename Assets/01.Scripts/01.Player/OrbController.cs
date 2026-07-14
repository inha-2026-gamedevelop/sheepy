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
    // 플레이어를 따라다니는 오브 하나. 평소에는 몸통 왼쪽 위 대기 지점 주변을
    // 펄린 노이즈로 자유롭게 떠다니며 따라오고,
    // Attack()이 호출되면 대상에게 돌진해 도달 시 타격 콜백을 실행한 뒤 다시 따라온다.
    public class OrbController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private Transform _followTarget;
        private float _noiseSeed;   // 오브마다 다른 펄린 노이즈 좌표를 쓰기 위한 시드 (겹침 방지)
        private Vector2 _slotOffset; // 대기 지점 기준 오브간 간격 (겹치지 않도록 벌려놓는 고정 오프셋)
        private Vector3 _velocity;  // SmoothDamp용
        private Coroutine _coAttack;
        private PlayerDataSO _playerSo; // 오브 밸런싱 DB 캐시 (매 프레임 떠다니기 계산에 사용)

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

            Vector3 anchor = _followTarget.position + (Vector3)WanderOffset();
            transform.position = Vector3.SmoothDamp(transform.position, anchor, ref _velocity, _playerSo.OrbFollowSmooth);
        }

        /****************************************
        *                Methods
        ****************************************/

        public void Init(Transform followTarget, float noiseSeed, Vector2 slotOffset)
        {
            _followTarget       = followTarget;
            _noiseSeed          = noiseSeed;
            _slotOffset         = slotOffset;
            transform.position  = followTarget.position + (Vector3)WanderOffset();
        }

        /// <summary> 대기 위치로 즉시 순간이동. 주인(분신)이 풀에서 다시 활성화될 때 호출. </summary>
        public void SnapToFollowTarget()
        {
            if (_followTarget == null)
            {
                return;
            }
            _velocity          = Vector3.zero; // 이전 활성 시점의 관성 제거
            transform.position = _followTarget.position + (Vector3)WanderOffset();
        }

        // 몸통 왼쪽 위 대기 지점(OrbAnchorOffset) 기준, 펄린 노이즈로 자유롭게 떠다니는 오프셋
        private Vector2 WanderOffset()
        {
            float t  = Time.time * _playerSo.OrbWanderSpeed;
            float nx = (Mathf.PerlinNoise(_noiseSeed, t) * 2f) - 1f;
            float ny = (Mathf.PerlinNoise(_noiseSeed + 91.7f, t) * 2f) - 1f;
            Vector2 wander = new Vector2(nx, ny) * _playerSo.OrbWanderRadius;
            return _playerSo.OrbAnchorOffset + _slotOffset + wander;
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
