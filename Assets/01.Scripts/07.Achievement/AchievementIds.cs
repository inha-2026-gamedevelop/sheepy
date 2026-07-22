namespace Minsung.Achievement
{
    // 코드에서 AchievementManager.Unlock(id)로 호출할 id 모음.
    // 주석의 "구현:"은 실제 판단 로직 위치 - 전부 Triggers/AchievementTrigger.cs를 거치며,
    // 괄호 안은 그 메서드를 호출하는 게임플레이 코드(원본 이벤트 발생 지점).
    public static class AchievementIds
    {
        // 시간을 거스른 자 - 첫 되감기
        // 구현: AchievementTrigger.RewindStarted() (01.Player/PlayerRewind.cs OnRewindStart)
        public const string FIRST_REWIND      = "first_rewind";

        // 나, 나, 그리고 나 - 분신 최대치 동시 유지
        // 구현: AchievementTrigger.CloneSquadFull() (04.TimeSystem/ClonePool.cs Spawn)
        public const string CLONE_FULL_SQUAD  = "clone_full_squad";

        // 흑과 백 - 보스 1페이즈 돌파
        // 구현: AchievementTrigger.BossPhase1Cleared() (03.Boss/BossController.cs CoPhaseEnd)
        public const string BOSS_PHASE1_CLEAR = "boss_phase1_clear";

        // 마지막 되감기 - 보스 처치
        // 구현: AchievementTrigger.BossDefeated() (03.Boss/BossController.cs CoPhaseEnd)
        public const string BOSS_DEFEATED     = "boss_defeated";

        // 죽음 100회 누적
        // 구현: AchievementTrigger.PlayerDied() (01.Player/PlayerController.cs HandleDeath)
        public const string DEATH_100         = "death_100";

        // 되감기 100회 누적
        // 구현: AchievementTrigger.RewindStarted() (01.Player/PlayerRewind.cs OnRewindStart)
        public const string REWIND_100        = "rewind_100";

        // 슬로우모션 최초 사용
        // 구현: AchievementTrigger.SlowMotionUsed() (04.TimeSystem/SlowMotionController.cs SetSlow)
        public const string FIRST_SLOW        = "first_slow";

        // 되감기 없이 보스 클리어
        // 구현: AchievementTrigger.BossDefeated() (03.Boss/BossController.cs CoPhaseEnd - usedRewind 분기)
        public const string NO_REWIND         = "no_rewind";

        // 아자토스 공간찢기(즉사기)에 피격
        // 구현: AchievementTrigger.DomainExpansionHit() (03.Boss/Boss2/Boss2DodgeableKillHazard.cs OnTriggerEnter2D)
        public const string DOMAIN_EXPANSION  = "domain_expansion";

        // 맵 최고점 아이템 획득
        // 구현: AchievementTrigger.MapTopItemCollected() (12.Item/MapTopItemPickup.cs OnTriggerEnter2D)
        public const string MAP_TOP_ITEM      = "jumpking";

        // 잠만보 - 5분 이상 무입력
        // 구현: AchievementTrigger.IdleTick() (01.Player/PlayerController.cs Update, Input.anyKeyDown 기반)
        public const string AFK_5MIN             = "afk_5min";

        // 어서와, 이런 보스는 처음이지? - 보스에게 첫 죽음
        // 구현: AchievementTrigger.PlayerDiedToBoss() (03.Boss/BossController.cs HandlePlayerDeath)
        public const string BOSS_FIRST_DEATH     = "boss_first_death";

        // 진정한 탐험가 - 숨은 공간 발견
        // 구현: AchievementTrigger.HiddenAreaFound() (07.Achievement/Triggers/HiddenAreaTrigger.cs OnTriggerEnter2D)
        public const string HIDDEN_AREA_FOUND    = "hidden_area_found";

        // 주파수 고정 - 라디오 5개 전부 청취
        // 구현: AchievementTrigger.RadioListened() (05.Interactive/RadioInteractive.cs PlayRadio)
        public const string RADIO_ALL_HEARD      = "radio_all_heard";

        // 이걸 떨어져? - 처음으로 낙하(리타이어존 진입)
        // 구현: AchievementTrigger.PlayerFellIntoRetireZone() (01.Player/PlayerRetireZone.cs OnTriggerEnter2D)
        public const string FIRST_FALL           = "first_fall";

        // 아름다운 이별 - 엔딩 크레딧 끝까지 시청
        // 구현: 없음 (TODO) - 엔딩 크레딧 씬 미구현, id/SO 에셋만 준비됨
        public const string ENDING_CREDITS       = "ending_credits";

        // 싱크로나이즈드 올림픽 - 레버 2개 이상 동시 작동
        // 구현: AchievementTrigger.LeversSynchronized() (05.Interactive/ElevatorController.cs SetLeverPulled)
        public const string LEVER_SYNC           = "lever_sync";

        // 너한테 모든걸 맡긴다 - 분신이 보스 막타
        // 구현: AchievementTrigger.BossDefeated() (03.Boss/BossController.cs TakeDamage에서 _finalHitWasClone 기록 -> CoPhaseEnd에서 전달)
        public const string CLONE_FINISHER       = "clone_finisher";

        // 눈을 뜨세요 - 공격 반사 누적 100회
        // 구현: AchievementTrigger.AttackReflected() (03.Boss/BossController.cs ReflectIfNeeded)
        public const string REFLECT_100          = "reflect_100";

        // 중꺾마 - 보스에게 500번 이상 죽고도 결국 클리어
        // 구현: AchievementTrigger.PlayerDiedToBoss()(카운트 증가, BossController.cs HandlePlayerDeath) + BossDefeated()(판정, CoPhaseEnd)
        public const string BOSS_DEATH_500_CLEAR = "boss_death_500_clear";

        // 어라? 왜 눈물이 나지? - 보스 체력 10% 이하로 남기고 사망
        // 구현: AchievementTrigger.PlayerDiedToBoss() (03.Boss/BossController.cs HandlePlayerDeath - bossHealthRatio 분기)
        public const string BOSS_NEAR_DEATH_LOSS = "boss_near_death_loss";
    }
}
