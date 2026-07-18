// System
using System;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Item;

namespace Minsung.Player
{
    // 하드웨어 입력만 읽어 이동/전투/되감기 컴포넌트로 전달하는 얇은 입력 계층 - 게임 로직 판정은 각 컴포넌트가 담당하고, 혼란(키반전)만 여기서 처리한다.
    public class PlayerInput : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private PlayerMovement _movement;
        private PlayerCombat   _combat;
        private PlayerRewind   _rewind;

        private bool _inputInverted; // 혼란: 좌우 반전

        public bool IsInverted => _inputInverted;

        public event Action<bool> OnInvertedChanged; // 혼란 아이콘 UI 연동용

        /****************************************
        *                Methods
        ****************************************/

        // 코디네이터(PlayerController)가 참조를 주입한다.
        public void Init(PlayerMovement movement, PlayerCombat combat, PlayerRewind rewind)
        {
            _movement = movement;
            _combat   = combat;
            _rewind   = rewind;
        }

        // 코디네이터의 Update가 매 프레임 호출한다.
        // GetKeyDown은 눌린 프레임에만 true이므로 물리 틱이 아니라 Update에서 읽어야 입력이 씹히지 않는다.
        public void HandleInput()
        {
            float horizontal = Input.GetAxisRaw(Constants.Player.AXIS_HORIZONTAL);
            if (_inputInverted)
            {
                horizontal = -horizontal;
            }

            _movement?.SetMoveInput(horizontal);

            if (Input.GetKeyDown(Constants.Player.KEY_JUMP))
            {
                _movement?.RequestJump();
            }
            if (Input.GetKeyDown(Constants.Player.KEY_ATTACK))
            {
                _combat?.RequestAttack(); // 누른 즉시 일반 공격 (반응성 유지)
                _combat?.BeginCharge();   // 동시에 차지 시작 - 홀드 유지 시 강화 공격으로 이어진다
            }
            if (Input.GetKeyUp(Constants.Player.KEY_ATTACK))
            {
                _combat?.ReleaseCharge(); // 풀차지였으면 강화 공격 발동
            }
            if (Input.GetKeyDown(Constants.Player.KEY_REWIND))
            {
                _rewind?.RequestRewind();
            }
            if (Input.GetKeyDown(Constants.Player.KEY_CLEAR_CLONES))
            {
                _rewind?.RequestClearClones();
            }
            if (Input.GetKeyDown(Constants.Player.KEY_USE_POTION))
            {
                RequestUsePotion();
            }
        }

        public void RequestUsePotion()
        {
            PotionManager.Instance?.TryUsePotion();
        }

        /// <summary> 혼란(키반전) 상태 설정. 화남 감정이 10초마다 1초간 건다. </summary>
        public void SetInverted(bool inverted)
        {
            if (_inputInverted == inverted)
            {
                return;
            }
            _inputInverted = inverted;
            OnInvertedChanged?.Invoke(inverted);
        }
    }
}
