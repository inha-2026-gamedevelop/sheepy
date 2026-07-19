using Minsung.Player;

namespace Minsung.Common
{
    // 플레이어 공격이 꽂히는 대상 공통 인터페이스 (몬스터 / 보스 / 보스 분신).
    public interface IDamageable
    {
        /// <summary> 피해 적용 - 실제로 들어갔으면 true, 반사/피통 동결/사망 등으로 무효화되면 false(반사 시 attacker가 대신 피해). </summary>
        bool TakeDamage(float dmg, DamageSource source = DamageSource.Player, PlayerHealth attacker = null);
    }
}
