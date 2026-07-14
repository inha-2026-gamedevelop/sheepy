// Unity
using UnityEngine;

using Minsung.Utility;

namespace Minsung.Visual
{
    // 자주 쓰는 파티클 효과 관리.
    [AddComponentMenu("Minsung/Particle Presets")]
    public class ParticlePresets : PersistentSingleton<ParticlePresets>
    {
        /****************************************
        *                Fields
        ****************************************/

        public enum FxType
        {
            Hit,        // 피격
            Collect,    // 아이템 획득
            Land,       // 착지
            CloneSpawn, // 분신 소환
            CloneDeath  // 분신 사망
        }

        [Header("파티클 프리팹 (각 타입별 할당)")]
        [SerializeField] private ParticleSystem _hitParticle;
        [SerializeField] private ParticleSystem _collectParticle;
        [SerializeField] private ParticleSystem _landParticle;
        [SerializeField] private ParticleSystem _cloneSpawnParticle;
        [SerializeField] private ParticleSystem _cloneDeathParticle;

        /****************************************
        *                Methods
        ****************************************/

        /// <summary>
        /// 지정한 타입의 파티클을 2D 위치에서 재생.
        /// Vector2로 받아서 Z = 0 으로 변환.
        /// </summary>
        public void PlayAt(FxType type, Vector2 position)
        {
            ParticleSystem ps = GetSystem(type);
            if (ps == null) return;

            // 2D: Vector2 -> Vector3 변환 (Z = 0)
            ps.transform.position = new Vector3(position.x, position.y, 0f);
            ps.Play();
        }

        // 타입 -> 인스펙터에 연결된 파티클 시스템 매핑.
        private ParticleSystem GetSystem(FxType type)
        {
            switch (type)
            {
                case FxType.Hit:         return _hitParticle;
                case FxType.Collect:     return _collectParticle;
                case FxType.Land:        return _landParticle;
                case FxType.CloneSpawn:  return _cloneSpawnParticle;
                case FxType.CloneDeath:  return _cloneDeathParticle;
                default:                 return null;
            }
        }
    }
}