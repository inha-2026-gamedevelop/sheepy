// Unity
using UnityEngine;

namespace Minsung.Player
{
    // 게임 종료(또는 모바일 백그라운드 전환) 시점에 플레이어 진행 상태를 저장/미러한다.
    // 실제 저장 로직(보스 인지 + 로컬 저장 + 서버 미러)은 PlayerController.PersistProgress가 담당.
    // 플레이어 루트 오브젝트에 부착.
    [RequireComponent(typeof(PlayerController))]
    [AddComponentMenu("Minsung/Player Save On Exit")]
    public class PlayerSaveOnExit : MonoBehaviour
    {
        private PlayerController _controller;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
        }

        // 스탠드얼론/에디터 종료 시
        private void OnApplicationQuit()
        {
            _controller?.PersistProgress();
        }

        // 모바일 등에서 백그라운드로 전환될 때(사실상 종료로 이어질 수 있음)에도 안전하게 저장
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _controller?.PersistProgress();
            }
        }
    }
}
