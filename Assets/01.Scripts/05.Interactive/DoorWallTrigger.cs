// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.TimeSystem;

public class DoorWallTrigger : MonoBehaviour, IRewindable
{
    /****************************************
    *                Fields
    ****************************************/
    [Header("이동시킬 오브젝트 설정")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private Vector2 moveDirection = Vector2.up;
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float moveDuration = 1.5f;

    private bool isOpened = false;
    private Coroutine moveCoroutine;

    // 레버와 동일하게 상태를 저장하는 구조체 정의
    private readonly struct DoorTick
    {
        public readonly Vector3 Position;
        public readonly bool IsOpened;

        public DoorTick(Vector3 position, bool isOpened)
        {
            Position = position;
            IsOpened = isOpened;
        }
    }

    // 리스트 대신 RingBuffer 사용 (메모리 효율 및 시스템 일관성)
    private RingBuffer<DoorTick> _rewindBuffer;

    /****************************************
    *              Unity Event
    ****************************************/
    private void Start()
    {
        // 시스템의 TickCapacity를 사용하여 버퍼 생성
        _rewindBuffer = new RingBuffer<DoorTick>(RewindManager.TickCapacity);

        // 매니저에 등록해야 RecordTick() 등이 호출됨
        RewindManager.Instance?.Register(this);
    }

    private void OnDestroy()
    {
        // 파괴 시 등록 해제
        RewindManager.Instance?.Unregister(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpened || other.gameObject.layer != 3) return;

        isOpened = true;
        moveCoroutine = StartCoroutine(MoveDoorRoutine());
    }

    /****************************************
    *                Methods
    ****************************************/
    private IEnumerator MoveDoorRoutine()
    {
        Vector3 startPosition = doorTransform.position;
        Vector3 targetPosition = startPosition + new Vector3(moveDirection.normalized.x, moveDirection.normalized.y, 0) * moveDistance;

        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / moveDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            doorTransform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
            yield return null;
        }

        doorTransform.position = targetPosition;
        moveCoroutine = null;
    }

    public void RecordTick()
    {
        // 현재 위치와 열림 상태를 버퍼에 푸시
        _rewindBuffer.Push(new DoorTick(doorTransform.position, isOpened));
    }

    public void OnRewindStart()
    {
        // 되감기 시작 시 실행 중인 이동 코루틴 중단 (필수)
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    public void ApplyRewindTick(int orderedIndex)
    {
        // RingBuffer의 TryGetOrdered를 사용하여 안전하게 과거 상태 복구
        if (_rewindBuffer.TryGetOrdered(orderedIndex, out DoorTick tick))
        {
            doorTransform.position = tick.Position;
            isOpened = tick.IsOpened;
        }
    }

    public void OnRewindEnd(int orderedIndex)
    {
        // 되감기가 끝난 후 상태 최종 확인
        if (_rewindBuffer.TryGetOrdered(orderedIndex, out DoorTick tick))
        {
            isOpened = tick.IsOpened;
        }

        // 버퍼를 클리어하거나 추가 처리를 할 수 있음
        // _rewindBuffer.Clear(); 
    }
}
