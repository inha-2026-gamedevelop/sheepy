// Unity
using UnityEngine;

namespace Minsung.CameraSystem
{
    // 씬 진입 시 플레이어 카메라 줌을 이 씬 전용 값으로 고정한다.
    // CameraManager는 PersistentSingleton이라 기본값(Constants.Camera.PLAYER_ORTHOGRAPHIC_SIZE)으로
    // 매번 리셋하므로, 맵마다 카메라 각도를 다르게 하고 싶으면 이 컴포넌트를 배치한다
    public class SceneCameraZoom : MonoBehaviour
    {
        [SerializeField] private float _orthographicSize = 2.4f;

        private void Start()
        {
            CameraManager.Instance?.SetPlayerZoom(_orthographicSize);
        }
    }
}
