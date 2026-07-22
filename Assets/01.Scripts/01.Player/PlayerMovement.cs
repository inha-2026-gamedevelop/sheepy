// System
using System;
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;

namespace Minsung.Player
{
    // 플레이어 물리 이동 담당 - 걷기/점프/더블점프/접지 판정/경직(스턴). 입력은 PlayerInput이 넘긴 값을 저장만 하고, 실제 Rigidbody 속도 변경은 물리 틱(Tick)에서 한 번만 한다.
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerMovement : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("판정")]
        [SerializeField] private LayerMask _groundLayer;

        // 이동 밸런싱 - PlayerDB(GameDB.Player)에서 Awake 때 로드
        private float _moveSpeed;
        private float _jumpForce;

        private PlayerController _coordinator; // 공유 잠금 상태(되감기/상호작용) 조회용
        private PlayerAnimator   _animator;

        private Rigidbody2D _rb;
        private Collider2D  _col;

        private float _moveInput;   // 수평 입력값 (Update에서 저장 -> Tick에서 1회 반영)
        private bool  _wantJump;    // 점프 예약 (다음 Tick에 소비)
        private int   _jumpCount;   // 착지 후 사용한 점프 횟수 (MAX_JUMPS까지, 2회째부터 더블점프 모션)
        private bool  _grounded;

        // 경직(낙뢰 피격)
        private float _stunEndTime;

        private Coroutine _coRestorePhysics; // 상호작용 종료 후 회전이 풀릴 때까지 물리 복구를 미루는 코루틴

        public bool IsGrounded => _grounded;
        public bool IsStunned  => Time.time < _stunEndTime;
        public int  FacingDir  => (_animator != null) ? _animator.FacingDir : 1;

        public Vector2 Position => _rb.position;
        public Vector2 Velocity => _rb.linearVelocity;

        /// <summary> 점프 입력이 실제로 반영된 순간 (더블점프 포함, SFX 훅용) </summary>
        public event Action OnJumped;

        /// <summary> 공중 -> 접지 전이 순간 1회 (SFX 훅용) </summary>
        public event Action OnLanded;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _rb  = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();

            PlayerDataSO playerSo = GameDB.Player;
            _moveSpeed       = playerSo.MoveSpeed;
            _jumpForce       = playerSo.JumpForce;
            _rb.gravityScale = playerSo.GravityScale;
            _rb.constraints  = RigidbodyConstraints2D.FreezeRotation; // 캐릭터가 넘어지지 않게 회전 고정
        }

        public void Init(PlayerController coordinator, PlayerAnimator animator)
        {
            _coordinator = coordinator;
            _animator    = animator;
        }

        /****************************************
        *            Input Requests
        ****************************************/

        public void SetMoveInput(float horizontal)
        {
            if (_coordinator.IsRewinding || IsStunned || _coordinator.IsInteracting)
            {
                _moveInput = 0f; // 잠금 중엔 입력을 버려 다음 틱에 잔여 가속이 없게 한다
                return;
            }

            if (_animator != null)
            {
                _animator.SetFacing(horizontal); // Move/Idle 좌우 반전
            }
            _moveInput = horizontal; // 저장만. 실제 속도 반영은 Tick()에서.
        }

        public void RequestJump()
        {
            if (_coordinator.IsRewinding || IsStunned || _coordinator.IsInteracting)
            {
                return;
            }
            if (_jumpCount >= GameDB.Player.MaxJumps)
            {
                return; // 지상 점프 1회 + 공중 점프 1회 한도
            }
            _wantJump = true;
        }

        /// <summary> 일정 시간 이동/점프 불가 (낙뢰 피격 경직). </summary>
        public void ApplyStun(float duration)
        {
            _stunEndTime = Mathf.Max(_stunEndTime, Time.time + duration);

            Vector2 v = _rb.linearVelocity;
            v.x = 0f;
            _rb.linearVelocity = v;
        }

        /// <summary> 지정한 속도로 즉시 튕겨낸다 (보스 손아귀 투척 등). Kinematic 잠금 상태였어도 Dynamic으로 되돌리고 속도를 준다. </summary>
        public void Launch(Vector2 velocity)
        {
            if (_coRestorePhysics != null)
            {
                StopCoroutine(_coRestorePhysics); // 상호작용 물리 복구 대기와 경합하지 않도록 여기서 Dynamic 확정
                _coRestorePhysics = null;
            }
            _rb.bodyType       = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = velocity;
        }

        /// <summary> 피격 넉백 - 피해 지점 반대 방향으로 밀려나며 짧은 경직. 경직 중엔 Move가 속도를 덮지 않는다. </summary>
        public void ApplyKnockback(Vector2 sourcePosition)
        {
            PlayerDataSO playerSo = GameDB.Player;
            ApplyStun(playerSo.KnockbackStunTime);

            float dirX = (_rb.position.x >= sourcePosition.x) ? 1f : -1f;
            _rb.linearVelocity = new Vector2(dirX * playerSo.KnockbackForceX,
                                            playerSo.KnockbackForceY);
        }

        /// <summary> 상호작용 진입 시 물리 정지 (PlayerInteraction이 호출).
        /// 연출 클립이 루트를 z회전시키면 콜라이더도 함께 돌아 바닥과 겹치는데,
        /// Dynamic 상태면 밀어내기 보정 때문에 몸이 떠오르므로 연출 동안은 물리를 끈다. </summary>
        public void OnInteractingBegan()
        {
            if (_coRestorePhysics != null)
            {
                StopCoroutine(_coRestorePhysics);
                _coRestorePhysics = null;
            }
            _rb.bodyType       = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
        }

        /// <summary> 상호작용 종료 시 물리 복구. 되감기 중 해제되면 복구는 되감기 종료(OnRewindEnd)가 맡는다.
        /// 클립이 끝나도 Idle로 되돌아가는 전이 동안 회전이 서서히 0으로 풀리는데, 그 전에 Dynamic으로
        /// 돌리면 아직 돌아간 콜라이더가 바닥에 박힌 채로 밀려나 몸이 위로 튄다. 고정 시간 대기 대신
        /// 회전이 실제로 0에 가까워질 때까지 기다린다 - 애니메이션/전이 길이가 바뀌어도 안전하다. </summary>
        public void OnInteractingEnded()
        {
            if ((_coordinator != null) && _coordinator.IsRewinding)
            {
                return;
            }
            if (_coRestorePhysics != null)
            {
                StopCoroutine(_coRestorePhysics);
            }
            _coRestorePhysics = StartCoroutine(CoRestorePhysicsAfterRotationSettles());
        }

        private IEnumerator CoRestorePhysicsAfterRotationSettles()
        {
            while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, 0f)) > 0.5f)
            {
                yield return null;
            }
            if ((_coordinator == null) || !_coordinator.IsRewinding)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
            }
            _coRestorePhysics = null;
        }

        /****************************************
        *              Physics Tick
        ****************************************/

        // 코디네이터의 FixedUpdate가 (되감기 중이 아닐 때) 호출한다.
        public void Tick()
        {
            bool wasGrounded = _grounded;
            _grounded = CheckGrounded();
            if (_grounded && !wasGrounded)
            {
                OnLanded?.Invoke();
            }
            if (_grounded && _rb.linearVelocity.y <= 0f)
            {
                _jumpCount = 0; // 상승 중 접지 오탐으로 점프 횟수가 풀리지 않게 하강/정지 시에만 리셋
            }

            Move();
        }

        private void Move()
        {
            Vector2 v = _rb.linearVelocity;

            // 수평 이동은 저장된 입력을 이번 물리 틱에 한 번만 반영한다.
            // 상호작용/경직 잠금 중에는 손대지 않아(0으로 잡아둔 속도 유지) 잔여 이동이 생기지 않는다.
            if (!_coordinator.IsInteracting && !IsStunned)
            {
                v.x = _moveInput * _moveSpeed;
            }

            if (_wantJump)
            {
                v.y = _jumpForce;
                _wantJump = false;

                ++_jumpCount;

                if (_animator != null)
                {
                    if (_jumpCount > 1)
                    {
                        _animator.TriggerDoubleJump();
                    }
                    else
                    {
                        _animator.TriggerJump();
                    }
                }

                // 이륙 틱에 접지가 true로 남아 있으면 착지 전이(IsGrounded 조건)가 점프 상태를 즉시 끊어버린다
                _grounded = false;
                OnJumped?.Invoke();
            }
            _rb.linearVelocity = v;

            if (_animator != null)
            {
                _animator.SetFacingByVelocity(v.x);
                _animator.SetLocomotion(Mathf.Abs(v.x), _grounded);
            }
        }

        // 콜라이더 중심에서 아래로 짧은 레이를 쏴 접지 여부 판정.
        private bool CheckGrounded()
        {
            float dist = _col.bounds.extents.y + Constants.Player.GROUND_CHECK_EXTRA;
            return Physics2D.Raycast(_col.bounds.center, Vector2.down, dist, _groundLayer);
        }

        /****************************************
        *          Rewind Support
        ****************************************/

        // 되감기 재생 시 한 틱의 포즈를 강제로 세팅 (ICommandActor.SetPose 경로).
        public void SetPose(Vector2 position, Vector2 velocity, bool grounded)
        {
            _rb.position = position;

            if (_animator != null)
            {
                _animator.SetFacingByVelocity(velocity.x);
                _animator.SetLocomotion(Mathf.Abs(velocity.x), grounded);
            }
        }

        // 되감기 시작: 점프 초기화 후 물리 정지(Kinematic).
        public void OnRewindStart()
        {
            _wantJump  = false;
            _jumpCount = 0; // 되감기 후 위치가 바뀌므로 점프 횟수도 초기화

            if (_coRestorePhysics != null)
            {
                StopCoroutine(_coRestorePhysics); // 상호작용 물리 복구 대기 중 되감기가 끼어들면 취소
                _coRestorePhysics = null;
            }
            _rb.bodyType = RigidbodyType2D.Kinematic; // 되감기 중 물리 끄기
            _rb.linearVelocity = Vector2.zero;
        }

        // 되감기 종료: 물리 복구.
        public void OnRewindEnd()
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;
        }
    }
}
