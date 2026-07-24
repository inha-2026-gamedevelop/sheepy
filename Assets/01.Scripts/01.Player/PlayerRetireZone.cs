// Unity
using UnityEngine;
using UnityEngine.Serialization;

using Minsung.Achievement;
using Minsung.Player;

// 떨어지면 플레이어를 특정 구역으로 이동시키는 구역
public class PlayerRetireZone : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/
    [Header("이동시킬 목적지 위치 (Transform)")]
    [FormerlySerializedAs("spawnPoint")]
    [SerializeField] private Transform _spawnPoint;

    /****************************************
    *              Unity Event
    ****************************************/
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.TryGetComponent(out PlayerController playerController))
        {
            return;
        }

        AchievementTrigger.PlayerFellIntoRetireZone(); // "이걸 떨어져?" - 처음으로 낙하

        // 리타이어 존 추락은 하트 반 칸 피해
        if (collision.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamageHalves(1);
        }

        // 빛 분해 연출 + 페이드 후 스폰 지점으로 이동 (PlayerController가 시퀀스 전담)
        playerController.PlayRetireRespawn(_spawnPoint);
    }
}
