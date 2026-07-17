// Unity
using UnityEngine;

namespace Minsung.CameraSystem
{
    // 씬 진입 시 플레이어 카메라 줌을 이 씬 전용 값으로 고정한다 (CameraManager는 PersistentSingleton이라 매번 기본값으로 리셋되므로, 맵마다 다른 각도가 필요하면 이 컴포넌트를 배치)
    public class SceneCameraZoom : MonoBehaviour
    {
        [SerializeField] private float _orthographicSize = 2.4f;

        private void Start()
        {
            CameraManager.Instance?.SetPlayerZoom(_orthographicSize);
        }
    }
}
