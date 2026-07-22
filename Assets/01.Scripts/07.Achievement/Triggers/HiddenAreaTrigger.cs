// Unity
using UnityEngine;

using Minsung.Player;

namespace Minsung.Achievement
{
    // Collider2D에 붙여서 사용
    // 진정한 모험가 업적용
    public class HiddenAreaTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 분신도 판정에 걸릴 수 있어 본체(PlayerController)만 대상으로 거른다
            if (!other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            AchievementTrigger.HiddenAreaFound();
        }
    }
}
