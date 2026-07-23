// System
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.TimeSystem;

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

        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        // 컨트롤러에 실존하는 파라미터 해시 - 아직 추가 안 된 파라미터(Attack 등) 호출로 인한 콘솔 에러 방지
        private readonly HashSet<int> _availableParams = new HashSet<int>();

        /// <summary> 바라보는 방향. 오른쪽 +1, 왼쪽 -1. 매달림 코너 판정 레이 방향에 사용 </summary>
        public int FacingDir => _spriteRenderer.flipX ? -1 : 1;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _animator       = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // Awake 1회만 순회해 캐싱 (parameters 접근은 배열 할당이 있어 매 호출 조회 금지)
            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                _availableParams.Add(param.nameHash);
            }

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
            SetFloatSafe(PARAM_SPEED, speed);
            SetBoolSafe(PARAM_GROUNDED, grounded);
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
            SetTriggerSafe(PARAM_JUMP);        // -> 1depthJump
        }

        public void TriggerDoubleJump()
        {
            SetTriggerSafe(PARAM_DOUBLE_JUMP); // -> 2depthJump
        }

        public void TriggerAttack()
        {
            SetTriggerSafe(PARAM_ATTACK);
        }

        public void TriggerLever()
        {
            SetTriggerSafe(PARAM_DO_LEVER);
        }

        /// <summary> GetSlow 획득 연출 동안 플레이어 스프라이트를 숨긴다 - 트리거 쪽 전용 Animator가 그 자리에서 별도로 연출을 재생한다. </summary>
        public void SetVisible(bool visible)
        {
            _spriteRenderer.enabled = visible;
        }

        // 실존 파라미터만 통과시키는 가드 - 컨트롤러에 없는 파라미터를 Set하면 콘솔 에러가 나므로
        private void SetFloatSafe(int paramHash, float value)
        {
            if (_availableParams.Contains(paramHash))
            {
                _animator.SetFloat(paramHash, value);
            }
        }

        private void SetBoolSafe(int paramHash, bool value)
        {
            if (_availableParams.Contains(paramHash))
            {
                _animator.SetBool(paramHash, value);
            }
        }

        private void SetTriggerSafe(int paramHash)
        {
            if (_availableParams.Contains(paramHash))
            {
                _animator.SetTrigger(paramHash);
            }
        }

        /****************************************
        *        Rewind Snapshot (되감기)
        ****************************************/

        /// <summary> 현재 애니메이터 상태 스냅샷. 매 물리 틱 기록해 되감기 재생의 원본이 된다 </summary>
        public AnimCommand CaptureAnimState()
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(Constants.Player.ANIM_LAYER_BASE);
            return new AnimCommand(info.shortNameHash, info.normalizedTime, _spriteRenderer.flipX);
        }

        /// <summary> 기록된 스냅샷 프레임으로 강제 스크럽 (되감기 재생 전용) </summary>
        public void ApplyAnimState(AnimCommand anim)
        {
            if (anim.StateHash == 0)
            {
                return; // 스냅샷 없는 틱(애니메이터 미주입 등) 방어
            }
            _spriteRenderer.flipX = anim.FlipX;
            _animator.Play(anim.StateHash, Constants.Player.ANIM_LAYER_BASE, anim.NormalizedTime);
        }

        /// <summary> true면 스크럽 모드 - 애니메이터 자체 시간 진행을 멈춰 스냅 사이(렌더 프레임)에 모션이 앞으로 흐르는 크리프를 막는다 </summary>
        public void SetScrubbing(bool scrubbing)
        {
            _animator.speed = scrubbing ? 0f : 1f;
        }
    }
}
