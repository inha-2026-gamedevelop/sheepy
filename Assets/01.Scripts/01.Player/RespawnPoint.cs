// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.Player
{
    // 씬에 배치하는 리스폰 마커. 같은 씬에서 사망한 플레이어는 가장 가까운 마커로 돌아간다.
    public class RespawnPoint : MonoBehaviour
    {
        [SerializeField] private bool _isBossReturnPoint;

        public Vector3 Position => transform.position;
        public bool IsBossReturnPoint => _isBossReturnPoint;

        private void OnEnable()
        {
            RespawnManager.Register(this);
        }

        private void OnDisable()
        {
            RespawnManager.Unregister(this);
        }
    }
}
