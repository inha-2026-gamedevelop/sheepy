// Unity
using UnityEngine;

namespace Minsung.Common.Data
{
    [CreateAssetMenu(fileName = "PotionDB", menuName = "TheLastRewind/GameDB/PotionDB")]
    public class PotionDataSO : ScriptableObject
    {
        [Header("Drop")]
        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.15f;

        [Header("Magnet")]
        [SerializeField] private float _magnetRadius = 2.5f;
        [SerializeField] private float _magnetSpeed = 8f;
        [SerializeField] private float _collectRadius = 0.3f;

        [Header("Potion")]
        [SerializeField, Min(0)] private int   _initialCarryCount = 100; // 게임 시작 시 지급 수량
        [SerializeField, Min(1)] private int   _maxCarryCount     = 100; // 최대 소지 수량
        [SerializeField, Min(1)] private int   _healHalves        = 2;   // 사용 시 회복량(반칸 단위)
        [SerializeField, Min(0f)] private float _useCooldown      = 15f; // 사용 재사용 대기시간(초)

        [Header("Pool")]
        [SerializeField, Min(1)] private int _poolSize = 16;

        public float DropChance         => _dropChance;
        public float MagnetRadius       => _magnetRadius;
        public float MagnetSpeed        => _magnetSpeed;
        public float CollectRadius      => _collectRadius;
        public int InitialCarryCount    => _initialCarryCount;
        public int MaxCarryCount        => _maxCarryCount;
        public int HealHalves           => _healHalves;
        public float UseCooldown        => _useCooldown;
        public int PoolSize             => _poolSize;
    }
}
