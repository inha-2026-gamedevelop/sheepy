// Unity
using UnityEngine;

using Minsung.Player;
using Minsung.TimeSystem;

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
            if (_used || !other.TryGetComponent(out PlayerController player))
            {
                return;
            }

            // 되감기로 궤적을 역재생하다 트리거를 스치는 경우는 입장이 아니다 - 입장 연출이 플레이어 조작을
            // 잠근 채로 되감기와 겹치면 연출이 끝나도 조작이 풀리지 않는다. _used는 소비하지 않고 넘긴다
            if ((RewindManager.Instance != null) && RewindManager.Instance.IsRewinding)
            {
                return;
            }

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
