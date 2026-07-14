// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Player;

namespace Minsung.Boss
{
    // 파랑 감정: 맵에 제공되는 하트 회복 픽업. 본체가 닿으면 하트 한 칸 회복 후 비활성화
    public class HeartPickup : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private int _healHalves = Constants.Player.HALVES_PER_HEART; // 회복량(반칸 단위, 2 = 한 칸)

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 분신도 PlayerHealth를 갖고 있어 본체(PlayerController)만 회복 대상으로 거른다
            if (!other.TryGetComponent(out PlayerController _))
            {
                return;
            }
            if (!other.TryGetComponent(out PlayerHealth health))
            {
                return;
            }

            health.Heal(_healHalves);
            gameObject.SetActive(false);

            // TODO: 획득 이펙트/사운드 (ParticlePresets / Constants.Audio)
            // TODO: 리와인드 참여 여부 기획 확정 (되감으면 획득이 취소되고 픽업이 되살아나야 하는가)
        }
    }
}
