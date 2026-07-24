// System
using System.Collections;

// Unity
using UnityEngine;

public class PlayerRetireWall : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/
    [Header("--- 이동 위치 설정 ---")]
    public Vector3 targetOffset = new Vector3(0, -1f, 0);

    [Header("--- 시간 설정 (초 단위) ---")]
    public float dropDuration = 0.2f;
    public float waitDuration = 1.0f;
    public float riseDuration = 2.0f;
    public float resetDuration = 0.5f;

    [Header("--- 플레이어 리스폰 설정 ---")]
    public Transform trapSpawnPoint;
    public int playerLayer = 3;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isDropping = false;

    // 컴포넌트를 제어하기 위한 변수
    private Collider2D trapCollider;

    /****************************************
    *              Unity Event
    ****************************************/
    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + targetOffset;

        // 오브젝트에 붙어있는 Collider2D(BoxCollider2D 등)를 자동으로 가져옴
        trapCollider = GetComponent<Collider2D>();

        StartCoroutine(StompLoop());
    }

    // 플레이어 충돌 감지 (Trigger 모드일 때만 호출됨)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 내려오는 도중 플레이어와 겹치면 리스폰
        if (isDropping && collision.gameObject.layer == playerLayer)
        {
            RespawnPlayer(collision);

            // 리타이어 벽 충돌시 하트 반 칸 피해
            if (collision.TryGetComponent(out Minsung.Player.PlayerHealth playerHealth))
            {
                playerHealth.TakeDamageHalves(1);
            }
        }
    }

    // 플레이어 물리적 충돌 감지 (Solid 벽 모드일 때 내려오다 부딪히는 예외 처리 예방용)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 혹시라도 내려오는 타이밍 전환 순간에 벽 판정으로 부딪힌 경우를 대비한 안전장치
        if (isDropping && collision.gameObject.layer == playerLayer)
        {
            RespawnPlayer(collision.collider);
        }
    }

    /****************************************
    *                Methods
    ****************************************/
    IEnumerator StompLoop()
    {
        while (true)
        {
            // 1. 내려찍기 시작: 통과 가능한 Trigger 모드로 변경
            isDropping = true;
            if (trapCollider != null)
            {
                trapCollider.isTrigger = true;
            }

            yield return StartCoroutine(MovePosition(startPosition, targetPosition, dropDuration));

            // 2. 바닥 대기 시작: 단단한 벽(Solid) 모드로 변경
            isDropping = false;
            if (trapCollider != null)
            {
                trapCollider.isTrigger = false;
            }

            yield return new WaitForSeconds(waitDuration);

            // 3. 천천히 원래 위치로 상승 (벽 상태 유지로 올라탈 수 있음)
            yield return StartCoroutine(MovePosition(targetPosition, startPosition, riseDuration));

            // 4. 위에서 다음 공격까지 대기 (벽 상태 유지)
            yield return new WaitForSeconds(resetDuration);
        }
    }

    IEnumerator MovePosition(Vector3 from, Vector3 to, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, elapsedTime / duration);
            yield return null;
        }
        transform.position = to;
    }

    // 리스폰 공통 로직 추출
    private void RespawnPlayer(Collider2D playerCollider)
    {
        playerCollider.transform.position = trapSpawnPoint.position;

        Rigidbody2D playerRb = playerCollider.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
        }
    }
}
