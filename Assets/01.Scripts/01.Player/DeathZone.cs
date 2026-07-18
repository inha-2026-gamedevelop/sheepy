// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.Player
{
    // 낙사/즉사 구역. Trigger Collider2D와 함께 배치한다.
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone : MonoBehaviour
    {
        [SerializeField] private bool _returnToBossExit;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PlayerHealth health))
            {
                return;
            }

            if (_returnToBossExit)
            {
                RespawnManager.RequestBossReturn();
            }

            health.Kill();
        }
    }
}
