namespace Minsung.Common
{
    // 피해 출처. 보스 감정 반사 판정(하양 = 본체 공격만, 남색 = 분신 공격만, 검정 = 모두)에 사용한다.
    public enum DamageSource
    {
        Player,      // 플레이어 본체
        PlayerClone, // 플레이어 분신
    }
}
