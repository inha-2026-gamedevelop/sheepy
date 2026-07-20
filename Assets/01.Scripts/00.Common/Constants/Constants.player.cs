// Unity
using UnityEngine;

namespace Minsung.Common
{
    public static partial class Constants
    {
        // 코드 계약값 전용 - 이동/공격/체력/오브 등 밸런싱 수치는 PlayerDataSO(GameDB.Player)에서 관리한다
        // 에셋: 08.Data/Player/PlayerDB.asset
        public static class Player
        {
            // 입력
            public const KeyCode KEY_JUMP          = KeyCode.Space;
            public const KeyCode KEY_ATTACK        = KeyCode.X;
            public const KeyCode KEY_REWIND        = KeyCode.R;
            public const KeyCode KEY_CLEAR_CLONES  = KeyCode.T;
            public const KeyCode KEY_DODGE_INVINCIBLE = KeyCode.LeftControl; // 전용 무적키(보스 즉사기 회피용) - E 상호작용/Shift 슬로우와 분리(임시값, Input 충돌 검사 후 확정)

            public const string  AXIS_HORIZONTAL   = "Horizontal";

            // 판정 epsilon (물리/표시 판정용 고정값)
            public const float GROUND_CHECK_EXTRA = 0.05f; // 접지 레이 여유 거리
            public const float FACING_MIN_SPEED   = 0.01f; // 이 속도 미만이면 바라보는 방향 유지

            // HP 구조 (내부 관리는 반칸 단위 - 보스 분신 공격이 하트 반 칸을 깎는다)
            public const int HALVES_PER_HEART = 2; // 하트 1칸 = 반칸 2개 (반칸 단위 환산용)

            // 애니메이션 재생 방향 (Animator의 State Speed를 AnimSpeedMultiplier 파라미터에 묶어서 사용)
            public const float ANIM_DIR_FORWARD = 1f;
            public const float ANIM_DIR_REVERSE = -1f;

            // 애니메이터 구조 상수
            public const int ANIM_LAYER_BASE = 0; // 기본 레이어 인덱스 - 되감기 스냅샷(캡처/스크럽) 대상 레이어
        }
    }
}
