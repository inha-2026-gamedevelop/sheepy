// Unity
using UnityEngine;

using Minsung.Player;

namespace Minsung.CameraSystem
{
    // 플레이어가 이 zone(Collider2D, IsTrigger)에 들어오면 플레이어 카메라 Composition의 Screen Position Y를 바꾼다. 존을 벗어나도 되돌리지 않고 그 값이 계속 유지된다
    [RequireComponent(typeof(Collider2D))]
    public class CameraScreenPositionZone : MonoBehaviour
    {
        [SerializeField, Range(-1.5f, 1.5f)] private float _screenPositionY;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            CameraManager.Instance?.SetPlayerScreenPositionY(_screenPositionY);
        }
    }
}
