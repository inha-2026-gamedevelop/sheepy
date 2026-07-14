// System
using System.Collections;

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

        private float _damage;
        private DamageSource _source;   // 본체/분신 구분 (보스 감정 반사 판정용)
        private PlayerHealth _attacker; // 반사 피해를 받을 공격자 체력

        /****************************************
        *              Unity Event
        ****************************************/

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

        /// <summary> 공격자 위치에 히트박스를 즉시 생성. 수명이 다하면 스스로 파괴된다. </summary>
        public static void Spawn(Vector2 position, float damage,
                                DamageSource source = DamageSource.Player, PlayerHealth attacker = null)
        {
            GameObject go = new GameObject("AttackHitbox");
            go.transform.position = position;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = GameDB.Player.AttackHitRadius;

            AttackHitbox hitbox = go.AddComponent<AttackHitbox>();
            hitbox._damage   = damage;
            hitbox._source   = source;
            hitbox._attacker = attacker;
            hitbox.StartCoroutine(hitbox.CoDespawn());
        }

        private IEnumerator CoDespawn()
        {
            yield return new WaitForSeconds(GameDB.Player.AttackHitLifetime);
            Destroy(gameObject);
        }
    }
}
