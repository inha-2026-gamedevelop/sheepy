// System
using System.Collections;

// Unity
using UnityEngine;

// 3페이즈 낙인 정화 제단(Boss2AltarInteractive) 소환 코디네이터
// 보스 조우 시작(3페이즈)부터 바로 AltarSpawnInterval마다 아레나 바닥 랜덤 x에 활성화한다
// 제단 오브젝트 1개를 계속 재사용한다(생성/파괴 대신 활성/비활성) - 이미 활성 상태(아직 보스에게 안 먹힘)면 그 주기는 건너뛴다
// 4페이즈는 별도 기믹으로 대체될 예정이라 3->4페이즈 전환 시점에 소환 루프를 정지하고 제단도 비활성화한다
public class Boss2AltarSpawner : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("참조")]
    [SerializeField] private Boss2Health _health;
    [SerializeField] private GameObject  _altar; // 비활성 상태로 시작하는 제단 오브젝트
    [SerializeField] private Boss2DataSO _dataSo;

    [Header("아레나 경계 (Boss2AttackPatterns와 동일한 값 권장)")]
    [SerializeField] private float _arenaMinX    = -7.4f;
    [SerializeField] private float _arenaMaxX    = 8.6f;
    [SerializeField] private float _arenaGroundY = -3f; // 지면 마커가 비어있을 때의 폴백값
    [SerializeField] private float _altarHeight  = 0f; // 제단 피벗 기준 지면 위 여백

    [Header("지면 마커 (씬의 다리/바닥 표면 위에 배치 - 가장 가까운 마커의 Y를 지면으로 사용)")]
    [SerializeField] private Transform[] _groundMarkers;

    private Coroutine      _spawnLoop;
    private WaitForSeconds _waitSpawnInterval;

    /****************************************
    *              Unity Event
    ****************************************/

    private void Start()
    {
        if (_altar != null)
        {
            _altar.SetActive(false);
        }
        if (_health != null)
        {
            _health.OnPhaseChanged += HandlePhaseChanged;
        }
        if (_dataSo != null)
        {
            _waitSpawnInterval = new WaitForSeconds(_dataSo.AltarSpawnInterval);
            _spawnLoop = StartCoroutine(CoSpawnLoop()); // 3페이즈(보스 조우 시작)부터 바로 소환 루프 시작
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnPhaseChanged -= HandlePhaseChanged;
        }
        StopSpawnLoop();
    }

    /****************************************
    *                Methods
    ****************************************/

    // 3->4페이즈 전환 시점 - 제단 시스템은 3페이즈 전용이라 소환 루프를 정지하고 떠 있던 제단도 정리한다
    private void HandlePhaseChanged(int phaseIndex)
    {
        StopSpawnLoop();
        if (_altar != null)
        {
            _altar.SetActive(false);
        }
    }

    private IEnumerator CoSpawnLoop()
    {
        while (true)
        {
            yield return _waitSpawnInterval;
            TrySpawnAltar();
        }
    }

    private void TrySpawnAltar()
    {
        if ((_altar == null) || _altar.activeSelf)
        {
            return;
        }

        float x = Random.Range(_arenaMinX, _arenaMaxX);
        float groundY = GetGroundY(x);
        _altar.transform.position = new Vector3(x, groundY + _altarHeight, 0f);
        _altar.SetActive(true);
    }

    // 가장 가까운 지면 마커의 Y를 지면 높이로 사용 - 마커가 없으면 _arenaGroundY로 폴백
    private float GetGroundY(float x)
    {
        if ((_groundMarkers == null) || (_groundMarkers.Length == 0))
        {
            return _arenaGroundY;
        }

        Transform nearest = _groundMarkers[0];
        float nearestDist = Mathf.Abs(nearest.position.x - x);
        for (int i = 1; i < _groundMarkers.Length; i++)
        {
            float dist = Mathf.Abs(_groundMarkers[i].position.x - x);
            if (dist < nearestDist)
            {
                nearest = _groundMarkers[i];
                nearestDist = dist;
            }
        }
        return nearest.position.y;
    }

    private void StopSpawnLoop()
    {
        if (_spawnLoop != null)
        {
            StopCoroutine(_spawnLoop);
            _spawnLoop = null;
        }
    }
}
