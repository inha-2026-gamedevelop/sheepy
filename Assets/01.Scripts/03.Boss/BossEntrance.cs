// Unity
using UnityEngine;

using Minsung.Player;

namespace Minsung.Boss
{
    // 보스방 입구 트리거. 플레이어가 진입할 때 대기 중인 보스전을 초기 상태로 시작한다.
    [RequireComponent(typeof(Collider2D))]
    public class BossEntrance : MonoBehaviour
    {
        [SerializeField] private BossController _boss;
        [SerializeField] private Transform _playerSpawn;
        [SerializeField] private bool _oneShot = true;

        private bool _used;

        private void Awake()
        {
            if (_boss == null)
            {
                _boss = FindAnyObjectByType<BossController>();
            }

            if (_playerSpawn == null)
            {
                GameObject spawnObject = GameObject.Find("PlayerSpawn");
                if (spawnObject != null)
                {
                    _playerSpawn = spawnObject.transform;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used || !other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            PlayerController player = other.GetComponent<PlayerController>();
            _boss?.BeginBossIntro(() =>
            {
                if (_playerSpawn != null)
                {
                    player.SetPose(_playerSpawn.position, Vector2.zero, false);
                }
            });
            _used = _oneShot;
        }
    }
}
