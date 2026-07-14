// System
using System;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Player;
using Minsung.TimeSystem;

namespace Minsung.Monster
{
    // 일반 몬스터 체력. 죽어도 파괴하지 않고 비활성화만 한다 - 되감기로 부활해야 하기 때문.
    public class MonsterHealth : MonoBehaviour, IDamageable
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private float _maxHealth = Constants.Combat.ENEMY_BASE_HEALTH;

        private float _currentHealth; // 남은 체력. 0 이하가 되면 비활성화 (리와인드로 부활 가능)

        public float CurrentHealth => _currentHealth;
        public bool  IsDead        => _currentHealth <= 0f;

        public event Action OnDeath;   // 사망 순간 1회 (아이템 드랍/MP 충전 등 후속 연동 지점)
        public event Action OnDamaged; // 피해가 실제로 들어갔지만 생존한 순간 (피격 모션/플래시 연동 지점)

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 피격 (IDamageable). 이미 사망했거나 되감기 중이면 무시(false). 체력 0 이하 -> 비활성화. </summary>
        /// 몬스터는 감정 반사가 없어 source/attacker는 사용하지 않는다.
        public bool TakeDamage(float dmg, DamageSource source = DamageSource.Player, PlayerHealth attacker = null)
        {
            if (IsDead)
            {
                return false;
            }
            // 되감기 중에는 세상이 역재생 중이므로 새 피해가 끼어들면 기록과 어긋난다.
            if ((RewindManager.Instance != null) && RewindManager.Instance.IsRewinding)
            {
                return false;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - dmg);
            if (_currentHealth <= 0f)
            {
                OnDeath?.Invoke();
                gameObject.SetActive(false); // 파괴 대신 비활성화 - 리와인드 부활 대상
            }
            else
            {
                OnDamaged?.Invoke();
            }
            return true;
        }

        /// <summary> 리와인드 복원용 - 기록된 시점의 체력으로 되돌린다. </summary>
        public void RestoreHealth(float health)
        {
            _currentHealth = Mathf.Clamp(health, 0f, _maxHealth);
        }
    }
}
