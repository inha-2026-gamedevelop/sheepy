// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class VolumeWeightController : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/
    [Header("시간 설정 (초)")]
    [SerializeField] private float increaseDuration = 1.0f; // 높아지는 시간
    [SerializeField] private float decreaseDuration = 1.0f; // 낮아지는 시간
    [SerializeField] private float peakWaitDuration = 0.0f; // 최고점에서 머무는 시간

    [Header("가중치 설정")]
    [SerializeField] private float minWeight = 0.0f;
    [SerializeField] private float maxWeight = 1.0f;

    [Header("루프 설정")]
    [SerializeField] private bool loopEffect = true; // 무한 반복 여부

    private Volume targetVolume;
    private Coroutine weightCoroutine;

    /****************************************
    *              Unity Event
    ****************************************/
    private void Awake()
    {
        targetVolume = GetComponent<Volume>();
    }

    private void Start()
    {
        // 게임 시작 시 자동으로 효과를 실행하고 싶다면 아래 주석을 해제하세요.
        TriggerWeightEffect();
    }

    /****************************************
    *                Methods
    ****************************************/
    // 외부 스크립트나 이벤트에서 효과를 시작할 때 호출하는 함수
    public void TriggerWeightEffect()
    {
        if (weightCoroutine != null)
        {
            StopCoroutine(weightCoroutine);
        }
        weightCoroutine = StartCoroutine(AnimateWeightRoutine());
    }

    private IEnumerator AnimateWeightRoutine()
    {
        do
        {
            // 1. Weight 증가 (min -> max)
            float elapsedTime = 0f;
            while (elapsedTime < increaseDuration)
            {
                elapsedTime += Time.deltaTime;
                targetVolume.weight = Mathf.Lerp(minWeight, maxWeight, elapsedTime / increaseDuration);
                yield return null;
            }
            targetVolume.weight = maxWeight;

            // 2. 최고점에서 대기
            if (peakWaitDuration > 0f)
            {
                yield return new WaitForSeconds(peakWaitDuration);
            }

            // 3. Weight 감소 (max -> min)
            elapsedTime = 0f;
            while (elapsedTime < decreaseDuration)
            {
                elapsedTime += Time.deltaTime;
                targetVolume.weight = Mathf.Lerp(maxWeight, minWeight, elapsedTime / decreaseDuration);
                yield return null;
            }
            targetVolume.weight = minWeight;

        } while (loopEffect); // loopEffect가 true이면 무한 반복
    }
}
