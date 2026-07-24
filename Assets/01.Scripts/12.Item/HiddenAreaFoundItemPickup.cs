// Unity
using UnityEngine;

using Minsung.Achievement;
using Minsung.Player;

namespace Minsung.Item
{
    // 맵의 숨겨진 공간 등 특정 위치에 배치하는 1회성 수집 아이템 - 플랫포밍 실력을 요구하는 히든 업적용.
    // 본체가 닿으면 "당신은 점프킹" 업적을 해제하고 비활성화된다. 레벨 디자이너가 목표 지점에 배치.
    public class HiddenAreaFoundItemPickup : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 분신도 판정에 걸릴 수 있어 본체(PlayerController)만 대상으로 거른다
            if (!other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            AchievementTrigger.HiddenAreaFound();
            gameObject.SetActive(false);
        }
    }
}
