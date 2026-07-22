using Minsung.Common;

namespace Minsung.Boss2
{
    // 부유 보스(Boss2) 감정 상태 - Minsung.Boss.BossEmotion과 동일한 규칙을 이식
    // 이름 앞에 Boss2를 붙인 이유: 네임스페이스 없는 전역 타입이라 이름이 같으면 Minsung.UI.BossEmotionIconTooltip처럼
    // "using Minsung.Boss;" + 무네임스페이스 참조를 함께 쓰는 기존 코드와 실제로 충돌한다(컴파일 에러로 확인됨) - Boss2Health 등 다른 파일과도 명명 일관성 유지
    // 페이즈와 무관하게 공통 패턴(반사/낙뢰 비율)을 변조한다
    public enum Boss2Emotion
    {
        None,  // 기본 - 변조 없음
        Black, // 모든 공격 반사
        White, // 본체 공격만 반사
        Navy,  // 분신 공격만 반사
        Pink,  // 낙뢰 비율 x2
        Blue,  // 낙뢰 비율 /2 + 맵에 하트 회복 픽업 제공
        Angry, // 고정 시 10초마다 1초 키반전(혼란 아이콘 표시)
    }

    // 감정별 변조 규칙 판정 헬퍼
    public static class Boss2EmotionExtensions
    {
        /// <summary> 반사 계열 감정(Black/White/Navy)인지 </summary>
        public static bool IsReflect(this Boss2Emotion emotion)
        {
            return (emotion == Boss2Emotion.Black)
                || (emotion == Boss2Emotion.White)
                || (emotion == Boss2Emotion.Navy);
        }

        /// <summary> 이 감정 상태에서 해당 출처의 공격이 반사되는지 </summary>
        public static bool ShouldReflect(this Boss2Emotion emotion, DamageSource source)
        {
            switch (emotion)
            {
                case Boss2Emotion.Black:
                    return true;
                case Boss2Emotion.White:
                    return source == DamageSource.Player;
                case Boss2Emotion.Navy:
                    return source == DamageSource.PlayerClone;
                default:
                    return false;
            }
        }

        // 낙뢰 낙하 비율 배율. 낙하 간격 = 기본 간격 / 배율
        // GameDB(Minsung.Common.Data)에 연결하지 않는 Boss2DataSO 값을 직접 받는다(원본은 GameDB.Boss를 읽음)
        public static float LightningRateMultiplier(this Boss2Emotion emotion, Boss2DataSO dataSo)
        {
            switch (emotion)
            {
                case Boss2Emotion.Pink:
                    return dataSo.LightningRatePinkMult;
                case Boss2Emotion.Blue:
                    return dataSo.LightningRateBlueMult;
                default:
                    return 1f;
            }
        }
    }
}
