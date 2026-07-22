// Unity
using UnityEngine;

namespace Minsung.Achievement
{
    // 업적 해제 조건 모은곳
    // 어떤 업적 id를 해제할지/누적 카운트 목표/조건 분기 같은 판단은 전부 여기서 담당한다.
    // 새 업적 판단이 생기면 이 클래스에 메서드를 추가한다.
    public static class AchievementTrigger
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string COUNTER_DEATH      = "death_count";      // 사망 누적 카운터 키 (PlayerPrefs 진행 유지용 - 변경 금지)
        private const string COUNTER_REWIND     = "rewind_count";     // 되감기 누적 카운터 키 (PlayerPrefs 진행 유지용 - 변경 금지)
        private const string COUNTER_BOSS_DEATH = "boss_death_count"; // 보스에게 죽은 누적 횟수 카운터 키 (PlayerPrefs 진행 유지용 - 변경 금지)
        private const string COUNTER_REFLECT    = "reflect_count";    // 공격 반사 누적 카운터 키 (PlayerPrefs 진행 유지용 - 변경 금지)
        private const string UNIQUE_GROUP_RADIO = "radio";            // 라디오 고유 청취 집합 키 (PlayerPrefs 진행 유지용 - 변경 금지)

        private const int DEATH_TARGET            = 100; // 사망 100회 달성 시 DEATH_100 해제
        private const int REWIND_TARGET           = 100; // 되감기 100회 달성 시 REWIND_100 해제
        private const int REFLECT_TARGET          = 100; // 반사 100회 달성 시 REFLECT_100 해제
        private const int RADIO_TARGET            = 5;   // 라디오 5개 전부 청취 시 RADIO_ALL_HEARD 해제
        private const int BOSS_DEATH_CLEAR_TARGET = 500; // 보스에게 500회 이상 죽고 클리어 시 BOSS_DEATH_500_CLEAR 해제

        private const float BOSS_NEAR_DEATH_RATIO   = 0.1f; // 보스 체력이 이 비율 이하일 때 죽으면 BOSS_NEAR_DEATH_LOSS 해제
        private const float IDLE_TARGET_SECONDS     = 300f; // 5분 무입력 시 AFK_5MIN 해제

        // 유휴는 세션(플레이 도중) 한정 누적 - PlayerPrefs에 매 프레임 쓰면 디스크 IO 비용이 커서 메모리로만 관리한다
        private static float s_idleSeconds;
        private static bool  s_idleUnlocked;

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 깨끗하게 초기화되도록.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_idleSeconds  = 0f;
            s_idleUnlocked = false;
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 되감기 발동 - 첫 되감기 및 누적 100회 판단. </summary>
        public static void RewindStarted()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.FIRST_REWIND);
            AchievementManager.Instance?.IncrementProgress(COUNTER_REWIND, REWIND_TARGET, AchievementIds.REWIND_100);
        }

        /// <summary> 분신을 최대치까지 동시에 유지 - "나, 나, 그리고 나". </summary>
        public static void CloneSquadFull()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.CLONE_FULL_SQUAD);
        }

        /// <summary> 슬로우모션 최초 사용. </summary>
        public static void SlowMotionUsed()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.FIRST_SLOW);
        }

        /// <summary> 플레이어 사망 - 누적 100회 판단. </summary>
        public static void PlayerDied()
        {
            AchievementManager.Instance?.IncrementProgress(COUNTER_DEATH, DEATH_TARGET, AchievementIds.DEATH_100);
        }

        /// <summary>
        /// 보스 격파. usedRewind가 false면 "되감기 없이 클리어"까지 함께 해제.
        /// finishedByClone이면 분신 막타 업적, 누적 보스 사망 횟수가 500 이상이면 "중꺾마"까지 함께 판단한다.
        /// </summary>
        public static void BossDefeated(bool usedRewind, bool finishedByClone)
        {
            AchievementManager.Instance?.Unlock(AchievementIds.BOSS_DEFEATED);
            if (!usedRewind)
            {
                AchievementManager.Instance?.Unlock(AchievementIds.NO_REWIND);
            }
            if (finishedByClone)
            {
                AchievementManager.Instance?.Unlock(AchievementIds.CLONE_FINISHER);
            }

            int deaths = AchievementManager.Instance != null ? AchievementManager.Instance.GetCounter(COUNTER_BOSS_DEATH) : 0;
            if (deaths >= BOSS_DEATH_CLEAR_TARGET)
            {
                AchievementManager.Instance?.Unlock(AchievementIds.BOSS_DEATH_500_CLEAR);
            }
        }

        /// <summary> 보스 1페이즈 돌파 - "흑과 백". </summary>
        public static void BossPhase1Cleared()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.BOSS_PHASE1_CLEAR);
        }

        /// <summary> 아자토스 공간찢기(즉사기)에 실제로 피격. </summary>
        public static void DomainExpansionHit()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.DOMAIN_EXPANSION);
        }

        /// <summary> 맵 최고점 히든 아이템 획득 - "당신은 점프킹". </summary>
        public static void MapTopItemCollected()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.MAP_TOP_ITEM);
        }

        /// <summary>
        /// 이번 프레임에 입력이 있었는지를 보고받아 무입력 시간을 누적한다. 입력이 있으면 타이머를 리셋하고,
        /// 5분(무입력 지속) 도달 시 1회 "잠만보"를 해제한다.
        /// </summary>
        public static void IdleTick(bool hadInputThisFrame, float deltaSeconds)
        {
            if (s_idleUnlocked)
            {
                return;
            }

            if (hadInputThisFrame)
            {
                s_idleSeconds = 0f;
                return;
            }

            s_idleSeconds += deltaSeconds;
            if (s_idleSeconds >= IDLE_TARGET_SECONDS)
            {
                s_idleUnlocked = true;
                AchievementManager.Instance?.Unlock(AchievementIds.AFK_5MIN);
            }
        }

        /// <summary> 사망 등으로 무입력 누적을 명시적으로 리셋한다. </summary>
        public static void ResetIdle()
        {
            s_idleSeconds = 0f;
        }

        /// <summary>
        /// 보스전 도중 플레이어 사망 - 첫 죽음 판단 + 누적 사망 횟수 증가(500번 죽고 클리어 업적용).
        /// bossHealthRatio(0~1)가 10% 이하면 "어라? 왜 눈물이 나지?"까지 함께 해제.
        /// </summary>
        public static void PlayerDiedToBoss(float bossHealthRatio)
        {
            AchievementManager.Instance?.Unlock(AchievementIds.BOSS_FIRST_DEATH);
            AchievementManager.Instance?.IncrementCounter(COUNTER_BOSS_DEATH);

            if (bossHealthRatio <= BOSS_NEAR_DEATH_RATIO)
            {
                AchievementManager.Instance?.Unlock(AchievementIds.BOSS_NEAR_DEATH_LOSS);
            }
        }

        /// <summary> 숨겨진 공간 발견 - "진정한 탐험가". 레벨에 배치된 트리거가 호출한다. </summary>
        public static void HiddenAreaFound()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.HIDDEN_AREA_FOUND);
        }

        /// <summary> 처음으로 낙하(리타이어존 진입) - "이걸 떨어져?". </summary>
        public static void PlayerFellIntoRetireZone()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.FIRST_FALL);
        }

        /// <summary> 레버 2개 이상 동시 작동 - "싱크로나이즈드 올림픽". </summary>
        public static void LeversSynchronized()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.LEVER_SYNC);
        }

        /// <summary> 라디오 청취 - radioId별로 고유 집계, 5개 전부 들으면 "주파수 고정" 해제. </summary>
        public static void RadioListened(string radioId)
        {
            AchievementManager.Instance?.MarkUniqueProgress(UNIQUE_GROUP_RADIO, radioId, RADIO_TARGET, AchievementIds.RADIO_ALL_HEARD);
        }

        /// <summary> 보스 감정 반사에 공격이 막힘 - 누적 100회 판단("눈을 뜨세요"). </summary>
        public static void AttackReflected()
        {
            AchievementManager.Instance?.IncrementProgress(COUNTER_REFLECT, REFLECT_TARGET, AchievementIds.REFLECT_100);
        }

        /// <summary> 엔딩 크레딧 시청 - "아름다운 이별". </summary>
        public static void EndingCreditsWatched()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.ENDING_CREDITS);
        }
    }
}
