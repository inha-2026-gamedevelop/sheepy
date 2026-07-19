// Unity
using UnityEngine;

namespace Minsung.Boss
{
    // 씬 전환(2->3페이즈 맵 변경) 시 보스 상태를 넘기는 정적 캐리어. 새 씬 BossController.Start가 소비해 이관값으로 복원한다
    public static class BossHandoff
    {
        /****************************************
        *                Fields
        ****************************************/

        public static bool        HasPending { get; private set; } // 복원할 이관 상태가 있는지
        public static float       Health;        // 총 피통 기준 현재 값
        public static int         PhaseIndex;    // 복원 후 진입할 페이즈 인덱스
        public static float       BattleElapsed; // 전투 경과(초) - 10분 즉사 타이머 이어가기
        public static BossEmotion Emotion;       // 이관 시점 감정

        /****************************************
        *                Methods
        ****************************************/

        // 도메인 리로드를 꺼도 정적 상태가 깨끗하게 초기화되도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            HasPending    = false;
            Health        = 0f;
            PhaseIndex    = 0;
            BattleElapsed = 0f;
            Emotion       = BossEmotion.None;
        }

        /// <summary> 다음 페이즈로 넘길 보스 상태 저장 (씬 전환 직전 호출) </summary>
        public static void Save(float health, int phaseIndex, float battleElapsed, BossEmotion emotion)
        {
            Health        = health;
            PhaseIndex    = phaseIndex;
            BattleElapsed = battleElapsed;
            Emotion       = emotion;
            HasPending    = true;
        }

        /// <summary> 복원 후 소비 - 이후 씬 재진입 시 중복 복원을 막는다 </summary>
        public static void Consume()
        {
            HasPending = false;
        }
    }
}
