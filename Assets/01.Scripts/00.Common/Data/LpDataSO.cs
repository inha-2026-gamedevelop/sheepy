// Unity
using UnityEngine;

namespace Minsung.Common.Data
{
    // LP(수집 재화) 밸런싱 데이터 DB - 에셋: 08.Data/Lp/LpDB.asset (GameDB.Lp로 접근)
    [CreateAssetMenu(fileName = "LpDB", menuName = "TheLastRewind/GameDB/LpDB")]
    public class LpDataSO : ScriptableObject
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("드랍")]
        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.5f; // 몬스터 처치 시 LP 드랍 확률

        [Header("자석 픽업")]
        [SerializeField] private float _magnetRadius  = 2.5f; // 이 거리 안으로 들어오면 플레이어 쪽으로 끌려가기 시작
        [SerializeField] private float _magnetSpeed   = 8f;   // 자석 이동 속도(유닛/초)
        [SerializeField] private float _collectRadius = 0.3f; // 이 거리 이하면 실제 획득 처리

        [Header("풀")]
        [SerializeField] private int _poolSize = 16; // 동시에 존재 가능한 LP 오브젝트 수

        /****************************************
        *              Properties
        ****************************************/

        public float DropChance    => _dropChance;
        public float MagnetRadius  => _magnetRadius;
        public float MagnetSpeed   => _magnetSpeed;
        public float CollectRadius => _collectRadius;
        public int   PoolSize      => _poolSize;
    }
}
