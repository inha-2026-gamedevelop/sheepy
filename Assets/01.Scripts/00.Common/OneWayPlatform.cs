// Unity
using UnityEngine;

namespace Minsung.Common
{
    // 아래에서 위로는 통과하고 위에서 착지할 때만 충돌하는 발판. Collider2D가 있는 오브젝트에 붙이면 PlatformEffector2D가 자동 추가/설정된다
    [RequireComponent(typeof(Collider2D), typeof(PlatformEffector2D))]
    public class OneWayPlatform : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("일방통행 설정")]
        [SerializeField, Range(0f, 360f)] private float _surfaceArc = 180f; // 충돌 판정 각도 범위 (기본 180 = 윗면 절반)
        [SerializeField] private float _rotationalOffset = 0f; // 판정 중심 각도. 0 = 오브젝트 기준 정위쪽

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            Collider2D collider = GetComponent<Collider2D>();
            collider.usedByEffector = true;

            PlatformEffector2D effector = GetComponent<PlatformEffector2D>();
            effector.useOneWay        = true;
            effector.surfaceArc       = _surfaceArc;
            effector.rotationalOffset = _rotationalOffset;
        }
    }
}
