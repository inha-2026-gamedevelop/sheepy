// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Player;

namespace Minsung.CameraSystem
{
    // 플레이어가 이 zone(Collider2D, IsTrigger)에 들어오면 콜라이더 크기에 맞춰 포커스 카메라를 활성화하고, 나가면 해제한다
    [RequireComponent(typeof(Collider2D))]
    public class CameraBottomZoneTrigger : MonoBehaviour
    {
        [SerializeField] private float _blendTime = Constants.Camera.DEFAULT_BLEND_TIME;

        private Collider2D _zoneCollider;

        private void Awake()
        {
            TryGetComponent(out _zoneCollider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            CameraManager.Instance?.FocusBottomZone(_zoneCollider, _blendTime);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            CameraManager.Instance?.UnFocus();
        }
    }
}
