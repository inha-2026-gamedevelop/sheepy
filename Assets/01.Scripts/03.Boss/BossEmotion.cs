using Minsung.Common;
using Minsung.Common.Data;

namespace Minsung.Boss
{
    // 보스 감정 상태. 페이즈와 무관하게 공통 패턴(반사/낙뢰/혼란)을 변조한다
    public enum BossEmotion
    {
        None,  // 기본 - 변조 없음
        Black, // 모든 공격 반사
        White, // 본체 공격만 반사
        Navy,  // 분신 공격만 반사
        Pink,  // 낙뢰 비율 x2
        Blue,  // 낙뢰 비율 /2 + 맵에 하트 회복 픽업 제공
        Angry, // 3페이즈 고정 - 10초마다 1초 키반전(혼란 아이콘 표시)
    }

    // 감정별 변조 규칙 판정 헬퍼
    public static class BossEmotionExtensions
    {
        /// <summary> 반사 계열 감정(Black/White/Navy)인지 </summary>
        public static bool IsReflect(this BossEmotion emotion)
        {
            return (emotion == BossEmotion.Black)
                || (emotion == BossEmotion.White)
                || (emotion == BossEmotion.Navy);
        }

        /// <summary> 이 감정 상태에서 해당 출처의 공격이 반사되는지 </summary>
        public static bool ShouldReflect(this BossEmotion emotion, DamageSource source)
        {
            switch (emotion)
            {
                case BossEmotion.Black:
                    return true;
                case BossEmotion.White:
                    return source == DamageSource.Player;
                case BossEmotion.Navy:
                    return source == DamageSource.PlayerClone;
                default:
                    return false;
            }
        }

        /// <summary> 낙뢰 낙하 비율 배율. 낙하 간격 = 기본 간격 / 배율 </summary>
        public static float LightningRateMultiplier(this BossEmotion emotion)
        {
            switch (emotion)
            {
                case BossEmotion.Pink:
                    return GameDB.Boss.LightningRatePinkMult;
                case BossEmotion.Blue:
                    return GameDB.Boss.LightningRateBlueMult;
                default:
                    return 1f;
            }
        }
    }
}
