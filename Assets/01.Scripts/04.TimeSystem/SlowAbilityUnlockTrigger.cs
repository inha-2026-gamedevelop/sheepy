// System
using System.Collections;

// Unity
using UnityEngine;

// Project
using Minsung.Player;

namespace Minsung.TimeSystem
{
    // Map1에서 슬로우 능력을 획득하면 GetSlow 연출을 재생하고, Shift 안내와 Map2 이동 지점을 연다.
    [RequireComponent(typeof(Collider2D))]
    public class SlowAbilityUnlockTrigger : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string SHIFT_IMAGE_NAME = "ShiftImage[OFF]";
        private const string MAP2_GATE_NAME   = "GoToMap2Scene[OFF]";
        private const string GET_SLOW_STATE   = "GetSlow"; // 이 오브젝트의 Animator(GetSlow.controller) 스테이트 이름

        private const float LANDING_DROP_HEIGHT = 0.5f; // 재배치 시 살짝 띄워야 접지->비접지->접지 전이가 생겨 OnLanded가 발생한다

        [Header("연출 타이밍")]
        [SerializeField] private float _getSlowAnimDuration = 4.5f; // GetSlow.anim 길이와 맞춰 조절 - 짧으면 연출이 끝나기 전에 플레이어가 복귀한다
        [SerializeField] private float _shiftImageDisplayDuration = 5f; // 착지 후 ShiftImage를 표출할 시간

        private Collider2D _trigger;
        private Animator   _getSlowAnimator; // 이 오브젝트에 등록된, GetSlow 연출 전용 Animator
        private GameObject _shiftImage;
        private GameObject _map2Gate;
        private bool       _unlocked;

        private PlayerController _pendingPlayer; // 착지 대기 중인 플레이어 (구독 해제용)

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            TryGetComponent(out _trigger);
            if (_trigger != null)
            {
                _trigger.isTrigger = true;
            }
            TryGetComponent(out _getSlowAnimator);

            Transform parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            Transform shiftImage = parent.Find(SHIFT_IMAGE_NAME);
            Transform map2Gate   = parent.Find(MAP2_GATE_NAME);
            if (shiftImage != null)
            {
                _shiftImage = shiftImage.gameObject;
            }
            if (map2Gate != null)
            {
                _map2Gate = map2Gate.gameObject;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_unlocked || !other.TryGetComponent(out PlayerController playerController))
            {
                return;
            }

            _unlocked = true;
            if (_trigger != null)
            {
                _trigger.enabled = false; // 연출이 끝나기 전까지 코루틴이 살아있어야 하므로 오브젝트 자체는 끄지 않는다
            }
            SlowMotionController.UnlockAbility();

            if (_map2Gate != null)
            {
                _map2Gate.SetActive(true);
            }

            other.TryGetComponent(out PlayerAnimator playerAnimator);
            playerAnimator?.SetVisible(false); // 이 오브젝트의 GetSlow 연출이 그 자리를 대신 보여주는 동안 플레이어 스프라이트를 숨긴다
            if (_getSlowAnimator != null)
            {
                _getSlowAnimator.Play(GET_SLOW_STATE, 0, 0f);
            }

            playerController.SetInteracting(true); // 연출 재생 동안 이동/점프/공격 입력과 물리를 잠근다
            StartCoroutine(CoPlayGetSlowSequence(playerController, playerAnimator));
        }

        private void OnDestroy()
        {
            if (_pendingPlayer != null)
            {
                _pendingPlayer.OnLanded -= HandlePlayerLanded;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        // 연출 대기 -> 플레이어를 능력 지점으로 재배치 + 재표시 -> 잠금 해제 -> 착지 대기 순으로 진행한다.
        private IEnumerator CoPlayGetSlowSequence(PlayerController playerController, PlayerAnimator playerAnimator)
        {
            yield return new WaitForSeconds(_getSlowAnimDuration);

            if (playerController == null)
            {
                yield break;
            }

            Vector3 landingStartPos = transform.position + (Vector3.up * LANDING_DROP_HEIGHT);
            playerController.SetPose(landingStartPos, Vector2.zero, false);
            playerAnimator?.SetVisible(true);
            playerController.SetInteracting(false); // 이동/물리 복구 - 이후 중력으로 자연스럽게 착지한다

            _pendingPlayer = playerController;
            playerController.OnLanded += HandlePlayerLanded;
        }

        private void HandlePlayerLanded()
        {
            if (_pendingPlayer != null)
            {
                _pendingPlayer.OnLanded -= HandlePlayerLanded;
                _pendingPlayer = null;
            }
            StartCoroutine(CoShowShiftImage());
        }

        private IEnumerator CoShowShiftImage()
        {
            if (_shiftImage != null)
            {
                _shiftImage.SetActive(true);
            }

            yield return new WaitForSeconds(_shiftImageDisplayDuration);

            if (_shiftImage != null)
            {
                _shiftImage.SetActive(false);
            }
            gameObject.SetActive(false); // 시퀀스가 완전히 끝난 뒤에야 트리거 오브젝트를 정리한다
        }
    }
}
