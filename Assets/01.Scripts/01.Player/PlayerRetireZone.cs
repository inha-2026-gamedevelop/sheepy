// Unity
using UnityEngine;

// 떨어지면 플레이어를 특정 구역으로 이동시키는 구역
public class PlayerRetireZone : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/
    [Header("이동시킬 목적지 위치 (Transform)")]
    public Transform spawnPoint;

    /****************************************
    *              Unity Event
    ****************************************/
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 오브젝트의 레이어가 3번(Player)인지 확인
        if (collision.gameObject.layer == 3)
        {
            // 플레이어의 위치를 지정된 스폰 포인트 위치로 이동
            collision.transform.position = spawnPoint.position;

            // 물리 효과(속도) 초기화 (추락하던 가속도 삭제)
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
