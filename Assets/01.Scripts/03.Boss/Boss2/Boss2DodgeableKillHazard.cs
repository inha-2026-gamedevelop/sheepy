// Unity
using UnityEngine;

using Minsung.Achievement;

// 공간찢기(4페이즈) 돌진 전용 '회피 가능 즉사' 판정
// 기존 Minsung.Boss.DamageHazard._instantKill은 절대 즉사(타이머 초과/원혼방출)라 그대로 두고, 이 컴포넌트만 IsDodgeInvincible을 존중한다
// 전용 무적키(PlayerHealth.RequestDodgeInvincible)로 무적인 순간에 맞으면 무시, 아니면 Kill()
[RequireComponent(typeof(Collider2D))]
public class Boss2DodgeableKillHazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Minsung.Player.PlayerHealth health))
        {
            return;
        }
        if (health.IsDodgeInvincible)
        {
            return; // 전용 무적키로 회피 성공
        }
        AchievementTrigger.DomainExpansionHit(); // 아자토스 공간찢기에 실제로 피격
        health.Kill(); // Kill()의 사망 가드가 중복 호출을 막는다
    }
}
