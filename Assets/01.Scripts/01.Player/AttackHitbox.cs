// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.TimeSystem;

namespace Minsung.Player
{
    // 근접 공격 판정. 존재하는 동안 닿은 IDamageable(몬스터/보스/보스 분신)에게 피해를 준다.
    public class AttackHitbox : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const int PLAYER_ATTACKER_COUNT = 1;
        private const string POOL_OBJECT_NAME   = "AttackHitboxPool";
        private const string SLOT_OBJECT_PREFIX = "AttackHitbox_";

        private static AttackHitbox _poolOwner;

        private AttackHitbox[] _slots;

        private CircleCollider2D _collider;

        private float _damage;
        private float _expireAt;
        private DamageSource _source;   // 본체/분신 구분 (보스 감정 반사 판정용)
        private PlayerHealth _attacker; // 반사 피해를 받을 공격자 체력

        private bool _isPoolOwner;

        /****************************************
        *              Unity Event
        ****************************************/

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _poolOwner = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePoolOwner()
        {
            if (_poolOwner == null)
            {
                new GameObject(POOL_OBJECT_NAME).AddComponent<AttackHitbox>();
            }
        }

        private void Awake()
        {
            _collider    = GetComponent<CircleCollider2D>();
            _isPoolOwner = (_collider == null);

            if (!_isPoolOwner)
            {
                return;
            }
            if ((_poolOwner != null) && (_poolOwner != this))
            {
                Destroy(gameObject);
                return;
            }

            _poolOwner = this;
            CreateSlots();
        }

        private void Update()
        {
            if (!_isPoolOwner)
            {
                return;
            }

            float currentTime = Time.time;
            for (int i = 0; i < _slots.Length; ++i)
            {
                AttackHitbox slot = _slots[i];
                if ((slot.gameObject.activeSelf) && (currentTime >= slot._expireAt))
                {
                    slot.Deactivate();
                }
            }
        }

        private void OnDestroy()
        {
            if ((!_isPoolOwner) || (_poolOwner != this))
            {
                return;
            }

            if (_slots != null)
            {
                for (int i = 0; i < _slots.Length; ++i)
                {
                    if (_slots[i] != null)
                    {
                        _slots[i].Deactivate();
                    }
                }
            }

            _slots     = null;
            _poolOwner = null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out IDamageable damageable))
            {
                // 실제로 피해가 들어갔을 때만 히트스톱 (반사/동결/사망 무효는 제외)
                if (damageable.TakeDamage(_damage, _source, _attacker))
                {
                    HitStopController.Request();
                }
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 공격자 위치에 풀링된 히트박스를 즉시 활성화한다 </summary>
        public static void Spawn(Vector2 position, float damage,
            DamageSource source = DamageSource.Player, PlayerHealth attacker = null)
        {
            EnsurePoolOwner();
            _poolOwner.ActivateSlot(position, damage, source, attacker);
        }

        private void CreateSlots()
        {
            int slotCount = GameDB.Time.MaxCloneCount + PLAYER_ATTACKER_COUNT;
            _slots = new AttackHitbox[slotCount];

            for (int i = 0; i < slotCount; ++i)
            {
                GameObject slotObject = new GameObject(SLOT_OBJECT_PREFIX + i);
                slotObject.transform.SetParent(transform, false);

                CircleCollider2D slotCollider = slotObject.AddComponent<CircleCollider2D>();
                slotCollider.isTrigger = true;

                AttackHitbox slot = slotObject.AddComponent<AttackHitbox>();
                slotObject.SetActive(false);
                _slots[i] = slot;
            }
        }

        private void ActivateSlot(Vector2 position, float damage, DamageSource source, PlayerHealth attacker)
        {
            AttackHitbox slot = FindAvailableSlot();
            slot.transform.position = position;
            slot._damage            = damage;
            slot._source            = source;
            slot._attacker          = attacker;
            slot._expireAt          = Time.time + GameDB.Player.AttackHitLifetime;
            slot._collider.radius   = GameDB.Player.AttackHitRadius;
            slot.gameObject.SetActive(true);
        }

        private AttackHitbox FindAvailableSlot()
        {
            AttackHitbox oldestSlot = _slots[0];

            for (int i = 0; i < _slots.Length; ++i)
            {
                AttackHitbox slot = _slots[i];
                if (!slot.gameObject.activeSelf)
                {
                    return slot;
                }
                if (slot._expireAt < oldestSlot._expireAt)
                {
                    oldestSlot = slot;
                }
            }

            oldestSlot.Deactivate();
            return oldestSlot;
        }

        private void Deactivate()
        {
            _attacker = null;
            gameObject.SetActive(false);
        }
    }
}
