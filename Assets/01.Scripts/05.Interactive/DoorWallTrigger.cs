// System
using System.Collections;

// Unity
using UnityEngine;

public class DoorWallTrigger : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/
    [Header("이동시킬 오브젝트 설정")]
    [SerializeField] private Transform doorTransform; // 이동할 문의 Transform
    [SerializeField] private Vector2 moveDirection = Vector2.up; // 이동할 방향 (X, Y)
    [SerializeField] private float moveDistance = 3f; // 이동할 거리
    [SerializeField] private float moveDuration = 1.5f; // 이동에 걸리는 시간(초)

    private bool isOpened = false; // 문이 이미 열렸는지 체크

    /****************************************
    *              Unity Event
    ****************************************/
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 들어온 오브젝트의 레이어가 지정된 플레이어 레이어(3번)인지 확인
        if (isOpened || other.gameObject.layer != 3) return;

        isOpened = true;
        StartCoroutine(MoveDoorRoutine());
    }

    /****************************************
    *                Methods
    ****************************************/
    private IEnumerator MoveDoorRoutine()
    {
        Vector3 startPosition = doorTransform.position;
        // Vector2 방향을 Vector3로 변환하여 목표 위치 계산
        Vector3 targetPosition = startPosition + new Vector3(moveDirection.normalized.x, moveDirection.normalized.y, 0) * moveDistance;

        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / moveDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress); // 부드러운 감속 효과

            doorTransform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
            yield return null;
        }

        doorTransform.position = targetPosition;
    }
}
