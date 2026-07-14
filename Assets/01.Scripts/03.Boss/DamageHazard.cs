// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Player;

namespace Minsung.Boss
{
    // 보스 공격 판정. 닿으면 설정된 반칸만큼 하트 차감 + 옵션(경직/즉사)
    public class DamageHazard : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private int   _damageHalves = Constants.Player.HALVES_PER_HEART; // 하트 차감(반칸 단위)
        [SerializeField] private float _stunDuration = 0f;    // 피격 시 이동 불가 시간(초, 0 = 없음)
        [SerializeField] private bool  _instantKill  = false; // true면 차감 대신 즉사

        public int   DamageHalves => _damageHalves;
        public float StunDuration => _stunDuration;
        public bool  InstantKill  => _instantKill;

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PlayerHealth health))
            {
                return;
            }

            if (_instantKill)
            {
                health.Kill();
                return;
            }

            // 무적/되감기로 무효면 경직/넉백도 없음 - 무적 시간이 실제로 몸을 지켜준다
            if (!health.TakeDamageHalves(_damageHalves))
            {
                return;
            }

            // 리액션은 본체(PlayerController)만 - 분신은 클립 재생이라 이동 제어가 없다
            if (other.TryGetComponent(out PlayerController player))
            {
                player.ApplyKnockback(transform.position);

                if (_stunDuration > 0f)
                {
                    if (player.StatusEffects != null)
                    {
                        player.StatusEffects.Apply(StatusEffectType.Bind, _stunDuration);
                    }
                    else
                    {
                        player.ApplyStun(_stunDuration);
                    }
                }
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 풀 슬롯 재사용 시 런타임에 판정 설정을 바꾼다 (BossHazardPool이 호출) </summary>
        public void Configure(int damageHalves, float stunDuration = 0f, bool instantKill = false)
        {
            _damageHalves = damageHalves;
            _stunDuration = stunDuration;
            _instantKill  = instantKill;
        }
    }
}
