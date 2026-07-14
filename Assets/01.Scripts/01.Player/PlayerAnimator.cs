// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.Player
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private static readonly int PARAM_SPEED       = Animator.StringToHash("Speed");
        private static readonly int PARAM_GROUNDED    = Animator.StringToHash("IsGrounded"); // 컨트롤러 이름에 맞춤 (구 Grounded)
        private static readonly int PARAM_JUMP        = Animator.StringToHash("Jump1");      // 구 Jump
        private static readonly int PARAM_DOUBLE_JUMP = Animator.StringToHash("Jump2");      // 구 DoubleJump
        private static readonly int PARAM_ATTACK      = Animator.StringToHash("Attack");         // 컨트롤러에 파라미터 추가 전까지 무반응
        private static readonly int PARAM_DO_LEVER    = Animator.StringToHash("DoLever");        // 〃
        private static readonly int PARAM_ANIM_SPEED  = Animator.StringToHash("AnimSpeedMultiplier"); // 〃

        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        /// <summary> 바라보는 방향. 오른쪽 +1, 왼쪽 -1. 매달림 코너 판정 레이 방향에 사용 </summary>
        public int FacingDir => _spriteRenderer.flipX ? -1 : 1;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _animator       = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // 이동/점프는 전부 코드(Rigidbody2D)로 처리한다. 루트 모션이 켜져 있으면
            // 점프/더블점프 클립이 Transform(=Rigidbody)을 조금씩 밀어 우측으로 드리프트하거나
            // 착지 후에도 위치 잔상이 남는다. 인스펙터 체크 상태와 무관하게 항상 끈다.
            _animator.applyRootMotion = false;
        }

        // 클립의 Z회전 보정(눕혀 그린 프레임용 +90°)은 오른쪽 바라볼 때 기준이다.
        // flipX는 그림만 미러링하고 회전 방향은 못 뒤집으므로, 왼쪽을 볼 땐 회전을 반대로 뒤집어야
        // 양쪽 모두 바로 선 모습이 된다. (Animator가 쓴 값을 매 프레임 후처리)
        private void LateUpdate()
        {
            if (!_spriteRenderer.flipX)
            {
                return;
            }

            float z = transform.localEulerAngles.z;
            if (Mathf.Approximately(z, 0f) || Mathf.Approximately(z, 360f))
            {
                return;
            }
            transform.localEulerAngles = new Vector3(0f, 0f, 360f - z);
        }

        /****************************************
        *                Methods
        ****************************************/

        // Speed로 Idle ↔ Move, IsGrounded로 착지 복귀를 구동한다
        public void SetLocomotion(float speed, bool grounded)
        {
            _animator.SetFloat(PARAM_SPEED, speed);
            _animator.SetBool(PARAM_GROUNDED, grounded);
        }

        /// <summary> 이동 입력 방향으로 스프라이트 좌우 반전. 입력 0이면 방향 유지 </summary>
        public void SetFacing(float horizontal)
        {
            if (horizontal > 0f)
            {
                _spriteRenderer.flipX = false;
            }
            else if (horizontal < 0f)
            {
                _spriteRenderer.flipX = true;
            }
        }

        /// <summary> 수평 속도 방향으로 스프라이트 반전. 아주 느리면(정지 판정) 방향 유지. </summary>
        public void SetFacingByVelocity(float horizontalVelocity)
        {
            if (Mathf.Abs(horizontalVelocity) < Constants.Player.FACING_MIN_SPEED)
            {
                return;
            }
            _spriteRenderer.flipX = horizontalVelocity < 0f;
        }

        public void TriggerJump()
        {
            _animator.SetTrigger(PARAM_JUMP);        // → 1depthJump
        }

        public void TriggerDoubleJump()
        {
            _animator.SetTrigger(PARAM_DOUBLE_JUMP); // → 2depthJump
        }

        public void TriggerAttack()
        {
            _animator.SetTrigger(PARAM_ATTACK);
        }

        public void TriggerLever()
        {
            _animator.SetTrigger(PARAM_DO_LEVER);
        }

        /// <summary> true면 모든 모션을 역재생(되감기), false면 정상 재생. </summary>
        public void SetReversed(bool reversed)
        {
            float dir = reversed ? Constants.Player.ANIM_DIR_REVERSE : Constants.Player.ANIM_DIR_FORWARD;
            _animator.SetFloat(PARAM_ANIM_SPEED, dir);
        }
    }
}
