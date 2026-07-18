// Unity
using UnityEngine;

namespace Minsung.Common.Data
{
    // 포션(회복 소비 아이템) 밸런싱 데이터 DB - 에셋: 08.Data/Potion/PotionDB.asset (GameDB.Potion으로 접근)
    [CreateAssetMenu(fileName = "PotionDB", menuName = "TheLastRewind/GameDB/PotionDB")]
    public class PotionDataSO : ScriptableObject
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("드랍")]
        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.15f; // 몬스터 처치 시 포션 드랍 확률

        [Header("자석 픽업")]
        [SerializeField] private float _magnetRadius  = 2.5f; // 이 거리 안으로 들어오면 플레이어 쪽으로 끌려가기 시작
        [SerializeField] private float _magnetSpeed   = 8f;   // 자석 이동 속도(유닛/초)
        [SerializeField] private float _collectRadius = 0.3f; // 이 거리 이하면 실제 획득 처리

        [Header("풀")]
        [SerializeField] private int _poolSize = 8; // 동시에 존재 가능한 포션 오브젝트 수

        [Header("소지/사용")]
        [SerializeField, Min(1)] private int _maxCarryCount = 3; // 최대 소지 개수 - 가득 차면 추가 드랍을 줍지 못한다
        [SerializeField, Min(1)] private int _healHalves    = 2; // 1개 사용 시 회복량(반칸 단위, 2 = 하트 1칸)

        /****************************************
        *              Properties
        ****************************************/

        public float DropChance    => _dropChance;
        public float MagnetRadius  => _magnetRadius;
        public float MagnetSpeed   => _magnetSpeed;
        public float CollectRadius => _collectRadius;
        public int   PoolSize      => _poolSize;
        public int   MaxCarryCount => _maxCarryCount;
        public int   HealHalves    => _healHalves;
    }
}
