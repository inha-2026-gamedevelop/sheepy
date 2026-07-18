// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.Monster
{
    // 몬스터 Animator 파라미터 래퍼. 되감기 역재생까지 지원한다.
    [RequireComponent(typeof(Animator))]
    public class MonsterAnimator : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private static readonly int PARAM_SPEED      = Animator.StringToHash("Speed");
        private static readonly int PARAM_ANIM_SPEED = Animator.StringToHash("AnimSpeedMultiplier");
        private static readonly int STATE_IDLE       = Animator.StringToHash("Idle");
        private static readonly int STATE_ATTACK     = Animator.StringToHash("Attack");
        private static readonly int STATE_HIT        = Animator.StringToHash("Hit");
        private static readonly int STATE_DEATH      = Animator.StringToHash("Death");

        private Animator _animator;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 이동 속도 반영. 0이면 Idle, 크면 Move 상태로 전환된다. </summary>
        public void SetLocomotion(float speed)
        {
            _animator.SetFloat(PARAM_SPEED, speed);
        }

        /// <summary> 공격 모션 재생. </summary>
        public void TriggerAttack()
        {
            _animator.Play(STATE_ATTACK, Constants.Player.ANIM_LAYER_BASE, 0f);
        }

        /// <summary> 피격 모션 재생. </summary>
        public void TriggerHit()
        {
            _animator.Play(STATE_HIT, Constants.Player.ANIM_LAYER_BASE, 0f);
        }

        /// <summary> 사망 모션 재생. </summary>
        public void TriggerDeath()
        {
            _animator.Play(STATE_DEATH, Constants.Player.ANIM_LAYER_BASE, 0f);
        }

        /// <summary> true면 모든 모션을 역재생(되감기), false면 정상 재생. </summary>
        public void SetReversed(bool reversed)
        {
            float dir = reversed ? Constants.Player.ANIM_DIR_REVERSE : Constants.Player.ANIM_DIR_FORWARD;
            _animator.SetFloat(PARAM_ANIM_SPEED, dir);
        }

        /// <summary> 되감기 부활 시 Idle로 즉시 복귀. Death처럼 되돌아오는 전이가 없는 상태에 갇히는 것을 막는다 </summary>
        public void ResetToIdle()
        {
            _animator.Play(STATE_IDLE, Constants.Player.ANIM_LAYER_BASE, 0f);
        }
    }
}
