# The Last Re:wind — 민성 구현 목록

> 기준: 노션 기능 투두리스트 기준 / 챕터 1 완성
> GameManager / AudioManager / SaveManager 는 별도 작업 (이 목록 제외 — 단, 씬전환/체크포인트/런타이머용 경량 GameManager는 구현됨)
> 순서는 의존성 기준 — 앞 항목 완료 후 다음 항목 가능
> 최종 갱신: 2026-07-11 (Player 컴포넌트화 / 상태이상·Sound·Camera·Interactive 시스템 / BossHealthBar Slider 반영)

---

## 범위 제외 (2026-07-11 결정)

> 아래 항목은 미채용으로 확정. 아이템은 LP 수집 하나로 단순화한다.

- **MP 게이지 시스템** - 리와인드/슬로우는 자원 소모 없이 무제한 유지 (게이트 없음)
- **기공파 발사체** - 오브/근접 공격으로 충분
- **장비 시스템 전체** - EquipmentData/6등급/드랍/장착/강화(1~15강)/장인의 기운/계승 모두 제외
- **소비 아이템** - HP·MP 포션/강화석/골드/깨진 회중시계 제외
- **이스터에그** - 숨겨진 방 해금 + 회중시계 엔딩 연출 제외

---

## 완료된 것

### TimeSystem (리와인드 코어)

| 기능 | 파일 |
|---|---|
| 틱 커맨드 구조체 (구 Snapshot 대체) | TickCommand.cs / MoveCommand.cs / AttackCommand.cs / InteractCommand.cs / AnimCommand.cs |
| 커맨드 적용 인터페이스 | ICommandActor.cs |
| 링 버퍼 (무할당 복사 `CopyOrderedTo` 포함) | RingBuffer.cs |
| 전역 리와인드 코디네이터 (타임라인 오너, 자동 생성) | RewindManager.cs |
| 리와인드 참여 인터페이스 (플레이어/몬스터/보스 공통) | IRewindable.cs |
| 역재생 연출 + 공격 Undo | PlayerController.cs |
| 분신 클립 재생 | CloneController.cs |
| 분신 공격 재현 (분신도 PlayerOrbs 장착 - 오브 연출 포함 본체와 동일, _orbTint로 반투명) | CloneController.cs / PlayerOrbs.cs / clonePrefab.prefab |
| 분신 리와인드 참여 (IRewindable - 클립 위치/하트 기록, 사망 시 부활, 기록 창 만료 시 풀 반환) | CloneController.cs / ClonePool.cs |
| 플레이어 하트 리와인드 복원 (틱마다 하트 기록) | PlayerController.cs / PlayerHealth.cs / TickCommand.cs |
| 되감기 중 피해 차단 가드 (플레이어/몬스터 공통) | PlayerHealth.cs / MonsterHealth.cs |
| 분신 오브젝트 풀 (프리 스택 O(1)) | ClonePool.cs |
| 슬로우모션 (fixedDeltaTime 보정 포함) | SlowMotionController.cs |

### Player / Combat

| 기능 | 파일 |
|---|---|
| 플레이어 코디네이터 (컴포넌트 조율 파사드, ICommandActor) | PlayerController.cs |
| 입력 계층 (하드웨어 입력 -> 각 컴포넌트 전달, 혼란 키반전 처리) | PlayerInput.cs |
| 이동/점프/더블점프/접지/경직/매달림 | PlayerMovement.cs |
| 공격 (오브 우선/근접 폴백, 플래시, 역재생 모션) | PlayerCombat.cs |
| 되감기 (IRewindable - 틱 기록/재생, 종료 시 분신 소환) | PlayerRewind.cs |
| 상호작용 잠금/기록 (분신 재연용) | PlayerInteraction.cs |
| 상태이상 컨트롤러 (Bind/InputInvert/RewindSeal) | PlayerStatusEffectController.cs |
| 하트 체력 (6개, 무적시간) — 본체/분신 공유 | PlayerHealth.cs |
| 하트 HUD / 상태이상 HUD | PlayerHeartUI.cs / PlayerStatusEffectUI.cs |
| 근접 공격 히트박스 (본체/분신 공통) | AttackHitbox.cs |
| 오브 공격 (Sheepy 스타일, 근접 폴백) | PlayerOrbs.cs / OrbController.cs |
| Animator 파라미터 래퍼 (역재생 지원) | PlayerAnimator.cs |
| 상호작용 감지 센서 (E키) | PlayerInteractionSensor.cs |

### Monster / Boss

| 기능 | 파일 |
|---|---|
| 몬스터 몸통 + 위치/체력 리와인드 | MonsterController.cs |
| 몬스터 체력 (사망 시 파괴 대신 비활성화, OnDeath 이벤트) | MonsterHealth.cs |
| 몬스터 사망 리와인드 부활 (기록 창 안에서 죽었으면 되살아남) | MonsterController.cs / MonsterHealth.cs |
| 몬스터 BT 노드 (순찰/추격/공격 + 감지/사거리 조건) | Monster/BT/*.cs |
| 보스 4페이즈 프레임 (칼로스식 피통 분리, 전환 컷신) | BossController.cs / BossState.cs |
| 보스 1페이즈 "흑과 백" (권능/폭발, 결정 로그로 리와인드 재현) | Phase1State.cs |
| 해저드 피해 판정 (하트 1개 차감) | DamageHazard.cs |

### Item (LP 수집, 신규 2026-07-11)

| 기능 | 파일 |
|---|---|
| LP 드랍/자석 픽업 풀 (코드 생성 오브젝트, 프리팹 불필요) | LpPickupPool.cs |
| LP 전역 매니저 (드랍 확률/자석 이동/획득/카운트, RewindManager 자동 생성 패턴) | LpManager.cs |
| LP 리와인드 복원 (개수 + 풀 슬롯별 위치/활성상태) | LpManager.cs (IRewindable) |
| LP 카운터 HUD | LpCounterUI.cs |
| LP 밸런싱 DB (DropChance/MagnetRadius/MagnetSpeed/CollectRadius/PoolSize) | LpDataSO.cs (GameDB.Lp) |

### Interactive / Sound / Camera / UI (신규)

| 기능 | 파일 |
|---|---|
| 상호작용 계약 + 정적 레지스트리 (Collider2D 조회) | IInteractable.cs / InteractableRegistry.cs / BaseInteractive.cs |
| 레버 (되감기 재연 지원, 색 전환) | LeverInteractive.cs |
| 라디오 (사운드 토글 + 카메라 포커스, 커스텀 인스펙터) | RadioInteractive.cs / Editor/RadioInteractiveEditor.cs |
| 사운드 매니저 (SFX/BGM, 지속형 SFX 채널) | SoundManager.cs |
| 맵 BGM 자동 재생 / 사운드 데이터(SO) | MapBgmPlayer.cs / SoundData.cs (+Editor) |
| 카메라 매니저 (Cinemachine 우선순위 포커스 전환) | CameraManager.cs |
| 보스 HP 바 (Slider) / 감정 HUD | BossHealthBarUI.cs / BossEmotionHUD.cs |
| 자막 매니저 / 키 가이드 HUD / 공용 스프라이트 | CaptionManager.cs / KeyGuideManager.cs / SpriteReference.cs |

### Achievement / Backend / Common / Visual

| 기능 | 파일 |
|---|---|
| 업적 매니저 (자동 생성, PlayerPrefs 저장) | AchievementManager.cs |
| 업적 카탈로그 SO + 데이터 | AchievementDatabase.cs / AchievementData.cs / AchievementIds.cs |
| 업적 토스트 UI (큐 순차 표시) | AchievementToastUI.cs |
| Supabase 클라이언트 (등록/점수/리더보드/고스트) | SupabaseClient.cs |
| Supabase 모델 / 고스트 프레임 | SupabaseModels.cs / Ghostframe.cs |
| Supabase 스키마 / RLS | supabase_schema.sql |
| KEY.txt 키 로딩 (`URL=`, `ANON_KEY=` 형식) | SupabaseClient.LoadKeys |
| 씬전환 / 체크포인트 / 런타이머 | GameManager.cs |
| Constants 10개 파일 (camera/interactive 추가) | Constants.*.cs |
| DontDestroyOnLoad 싱글톤 베이스 | Utility/PersistentSingleton.cs |
| 코루틴 교체/정지 유틸 | Utility/UtilCoroutine.cs |
| URP Post Processing | SheepyVisualSetup.cs |
| 캐릭터 글로우 / 그림자 / 스피드라인 | CharaGlow.cs / ShadowLayer.cs / SpeedlineEffect.cs |
| 화면 전환 페이드 (코루틴 겹침 방지) | ScreenFade.cs |
| 되감기 VHS 오버레이 | VhsRewindOverlay.cs |
| 파티클 프리셋 | ParticlePresets.cs |
| Sheepy 스프라이트 임포터 | SheepySpriteImporter.cs |

---

## 🔴 1순위 — Week 1

> 팀원 작업과 무관하게 혼자 진행 가능. 이게 없으면 전투/보스가 막힘.

### Combat — 공격 (완료)

- [x] ~~일반공격~~ → **오브 공격으로 구현됨** (PlayerOrbs: 범위 내 적에게 오브 돌진, 없으면 근접 히트박스 폴백)
- [x] **차지공격** (2026-07-11)
  - X 누름 = 일반 공격 즉시(반응성 유지) + 차지 시작, `ChargeTime`(1.2초) 이상 홀드 후 릴리즈 = 강화 공격
  - 배율 `ChargeDamageMult`(2.5) - GameDB.Player에서 관리, 실측 검증(일반 20 / 차지 50)
  - 차지 중 시각 피드백: 몸통 색 주황(차지 중) -> 골드(풀차지)
  - 리와인드/분신 호환: `AttackCommand.Charged` 필드 신설 - 분신이 같은 배율로 재연, 역재생도 동일 시그니처
  - 잠복 버그 수정: `ContactFilter2D.NoFilter()`가 필터 쿼리에서 트리거를 제외해 **트리거 판정 대상은 오브로 못 때리던 문제** - `useTriggers = true` 명시 (PlayerOrbs)
- [x] **히트박스 및 충돌 처리** — AttackHitbox.cs (보스 우선 판정, TryGetComponent)

### Combat — 플레이어 생존 (5개)

- [x] **플레이어 체력 시스템** — HP 대신 **하트 방식**으로 구현 (PlayerHealth: `MAX_HEARTS`=6, 무적 코루틴)
- [x] **피격 처리 및 무적** — 무적시간 + 하트 UI 갱신 구현
  - [x] 넉백 / 피격 플래시 추가 (2026-07-11)
    - 넉백: `PlayerMovement.ApplyKnockback`(피해 지점 반대 방향 속도 임펄스 + 짧은 경직), DamageHazard/MonsterController가 호출
    - 플래시: `PlayerHealth.OnDamaged` 이벤트(신규) -> `CharaGlow.Flash`(실시간 대기, 펄스 일시정지 후 원색 복귀)
    - `TakeDamage/TakeDamageHalves`가 bool 반환(신규) - 무적/되감기 무효 시 넉백/경직도 안 걸림 (무적 시간이 실제로 몸을 지킴)
- [ ] **방어력 데미지 계산** — 하트 방식 채택으로 보류 (장비 시스템 붙일 때 재검토)
- [x] **플레이어 사망 처리** — PlayerController가 `PlayerHealth.OnDeath` 구독 (2026-07-11)
  - 하트 0 -> 입력/물리 잠금 -> `DEATH_RESPAWN_DELAY` 대기 -> `GameManager.RequestCheckpointRespawn`(onRespawn 콜백 추가) -> 하트 리셋 + 분신 정리
  - 시작 지점을 기본 체크포인트로 자동 등록 (체크포인트 오브젝트 전에 죽어도 복귀)
  - 매니저 없는 씬은 제자리 부활 안전망
  - [ ] 사망 애니메이션 - Animator에 Death 트리거 추가되면 `CoDeathRespawn`에 연결 (훅 주석 있음)
- [x] **히트스톱 (타격감)** — HitStopController 신설 (2026-07-11)
  - 공격이 실제로 꽂힌 순간(`TakeDamage`가 true)만 `HIT_STOP_DURATION`(0.05초) 동안 `timeScale = 0` - 반사/동결 무효는 제외
  - 트리거: AttackHitbox(근접) + PlayerOrbs(오브) 양쪽, 되감기 중엔 무시
  - 슬로우모션과 별개 플래그(IsActive) - 종료 시 `SlowMotionController.TargetTimeScale`(슬로우 중 0.4, 평시 1)로 복원, SetSlow도 히트스톱 중엔 timeScale을 건드리지 않음
  - 씬 배치 불필요 (RewindManager처럼 자동 생성)

---

## 🔴 2순위 — Week 2

> 장비/강화/소비아이템 시스템은 제외(2026-07-11). 아이템은 LP 수집 하나로 단순화.

### Item — LP 수집 (2026-07-11 구현 완료)

> 단순 수집 카운터 (사용처 미정). 되감기 시 개수/오브젝트 모두 복원. 회중시계/이스터에그는 제외.

- [x] **LP 드랍** - 일반 몬스터 처치 시 확률적으로 LP 오브젝트 드랍
  - `MonsterController`가 `MonsterHealth.OnDeath` 수신 -> `LpManager.TryDropLp(position)` 호출 (GameDB.Lp.DropChance 확률)
  - 리와인드 규칙 준수: 생성/파괴 대신 풀 활성/비활성 (`LpPickupPool`, `BossHazardPool`과 동일 패턴, 코드 생성 오브젝트라 프리팹/씬 배치 불필요)
- [x] **LP 자석 픽업 + UI 카운트** - 플레이어가 일정 범위 접근 시 자석처럼 끌려가 획득, HUD 개수 증가
  - `LpManager.FixedUpdate`가 매 틱 거리 판정으로 자석 이동/획득 처리 (콜라이더 불필요)
  - GameDB.Lp(LpDataSO, `08.Data/Lp/LpDB.asset`): DropChance / MagnetRadius / MagnetSpeed / CollectRadius / PoolSize
  - HUD: `UICanvas/PlayerHUD/LpCounter`(Icon + TMP Count), `LpCounterUI`가 `LpManager.OnLpChanged` 구독
- [x] **LP 리와인드 복원** - `LpManager`가 IRewindable로 등록(RewindManager와 동일한 자동 생성 싱글톤), 개수(RingBuffer\<int\>) + 풀 슬롯별 위치/활성상태(RingBuffer\<LpSlotTick\>, 슬롯당 1개) 기록. 되감기 시 개수 감소 + 오브젝트 재등장 모두 복원 - Play 모드에서 격리 단위 테스트로 양방향(비활성화/재등장) 검증 완료
  - 진행바(Slider) 아님 - 숫자 카운터로 표시
  - 사용처는 추후 결정 (현재는 단순 수집 카운터)

---

## 🟡 3순위 — Week 3

> 전투 + 장비 완료 후. 보스가 이 게임의 핵심.

### Boss

- [x] **1페이즈 분신 상하 겹침 회피** (2026-07-18) — `BossCloneController` 두 체만 대상. 같은 X축에서 위아래로 1.25m 이내 겹치면 반대 방향으로 이동해 분리한다. 판정 범위는 `BossDataSO`/`BossDB.asset`의 `CloneCrowdAvoidHorizontalRange`(0.75), `CloneCrowdAvoidVerticalRange`(1.25)로 조절.
- [x] **1페이즈 해저드 하단 확장** (2026-07-18) — `ArenaGroundY`는 낙뢰·장풍 등 기존 기준으로 유지하고, 기믹 레이저/안전구역만 씬별 `GimmickHazardBottomY`까지 확장한다. Map2는 보스 등장 움푹한 바닥까지 포함하도록 `-36.5`로 설정 (좌/우 구덩이 전경 장식물 최저점 약 -35.98까지 커버, 최초 설정값 -33.5는 부족해 구덩이 하단이 안 보이는 버그로 재수정).

> 기준 스펙: `claude/README.md`의 "보스 - Azathoth" 섹션 (2026-07-11 상세 기획 반영)
> 아래 남은 작업은 스펙-코드 전수 대조 감사(2026-07-11, 6개 영역)로 도출 - 근거 라인은 각 항목 참고

**구현 완료 (스펙 대조 통과)**

- [x] **보스 체력 및 페이즈 전환** — 단일 피통 64,000 / 페이즈당 16,000 / 하한 동결 -> 종료 기믹 -> 전환. 전투 타이머 10분 즉사(리와인드 무관 실시간)
- [x] **낙뢰 공통 패턴** — 4초 간격 / 하트 1칸 + 0.5초 이동 불가(Bind) / Pink x2·Blue /2 배율까지 스펙과 수치 일치 (GameDB.Boss)
- [x] **1페이즈** — 분신 2체(각 8,000) 근접전 + 즉사 레이저 기믹 (빨/파/초 랜덤 3회, 안전구역 슬로우 중에만 표시 + 발사 시점까지, 5초 후 재발사, 색 불일치 즉사)
- [x] **2페이즈 골격** — 본체 등장 + 장풍(아래->위, 결정 로그 리와인드 재현)
- [x] **3페이즈** — Angry 고정(주기 혼란) + 맵 가로지르는 레이저(경고 1.5초 깜빡임 + 발사 1초 + 높이 랜덤, 풀 스냅샷+결정 로그 리와인드)
- [x] **4페이즈 골격** — 리와인드 전역 잠금(SetRewindEnabled) + 2페이즈 패턴 임시 유지

**남은 작업 - HIGH (기능 부재)**

- [x] **감정 전환 구동부** — 주기적 랜덤 전환 구현 (BossController.CoEmotionLoop, GameDB.Boss.EmotionInterval 8초). 후보 Black/White/Navy/Pink/Blue(화남/기본 제외), 결정 로그(_emotionLog+커서)로 되감기 재현, BossFrame에 감정+커서 스냅샷. 3페이즈는 SetAutoEmotionSuspended로 화남 고정 유지
- [ ] **2페이즈: 1페이즈 분신 근접 패턴 유지** — Phase1.Exit가 분신을 전부 비활성화하고 Phase2는 본체만 활성화. 스펙 문구대로 분신 2체를 유지할지, "본체가 같은 근접 패턴 수행"으로 충족인지 기획 해석 확인 후 구현
- [~] **2페이즈 종료: 컷신 + 씬 변경 맵 전환** — 이관 로직 구현 완료: `BossHandoff`(정적 캐리어) + `BossController.SaveHandoffToNextPhase`/Start 복원 + `Phase2State.CoPhaseEndGimmick`에서 페이드 암전 시 저장 후 `SceneManager.LoadScene(_phase3SceneName)`. **남은 것: 3페이즈 전용 맵 씬 에셋 제작 후 BossController `_phase3SceneName`을 교체(현재 임시 "Boss" 자기 재로드), 대상 씬을 Build Settings에 등록.** 컷신 연출(대사/카메라)은 리소스 확정 후 onMidpoint 앞에 배치

**남은 작업 - MED (스펙 불일치 / 기획 확인)**

- [x] **1페이즈 기믹 트리거 정합** (2026-07-12) — 스펙("분신 2체를 모두 잡으면")대로 정렬. `BossState.UsesHealthFloorTrigger`(기본 true)를 `Phase1State`가 false로 오버라이드해 피통 하한 도달만으로는 기믹이 시작되지 않게 하고, 분신 `OnCloneDied` 이벤트로 둘 다 죽었는지 직접 확인해 `BossController.TriggerPhaseEnd()`를 호출하도록 변경(오버킬해도 생존 분신이 있으면 대기)
- [ ] **1페이즈 레이저 랜덤의 리와인드 처리** — 스펙 "결정 로그 재현(1/2/3P 공통)" vs 코드 "기믹 중 리와인드 전역 잠금으로 대체" (의도적 설계로 보임 - 스펙 문구 또는 코드 중 한쪽 정렬)
- [ ] **낙뢰 리와인드 스냅샷** — 되감기 시 볼트 정리 후 루프 재시작만 지원, 역재생 미지원 (BossLightningPattern TODO. 스펙도 "예정"으로 명시)
- [ ] **혼란 주기 보정** — "10초마다 1초"가 직렬 대기라 실발동 주기 11초 + 슬로우 중 실시간 기준 주기 늘어남. 기준(발동 간격/스케일 시간) 확정
- [ ] **4페이즈 리와인드 봉인 방식 결정** — RewindSeal 상태이상은 완전 구현돼 있으나 미사용(휴면). 현재는 전역 잠금 방식 - 디버프 아이콘이 안 뜨므로 플레이어 피드백 관점에서 택일

**남은 작업 - LOW (연출/에셋/정리)**

- [ ] 본체 등장 연출 (사운드 - Phase2State TODO, 카메라 줌아웃은 2026-07-12에 전투 시작 시점으로 구현 완료)
- [x] 보스 애니메이터 상태/트랜지션 + 코드 훅 연결 (2026-07-12) — `Boss.controller`에 11개 클립(Idle/Idle2/Run/Intro/Combat/Casting/Damaged/Death/Roar/Jump/BackTumbling) 전부 배치, 파라미터 `Speed`(Float) + `Attack`/`Cast`/`Hit`/`Death`/`Roar`(Trigger, AnyState 진입)는 실제 코드에서 호출까지 연결 완료
  - `Speed`/`Attack`: `BossMeleeUnitBase.ChasePlayer/CoAttackLoop` (근접 유닛 본체·분신 공통)
  - `Hit`: `BossBodyController`/`BossCloneController.TakeDamage` (실제 피해가 들어간 순간만)
  - `Cast`: `BossBodyController.PlayCastTrigger()` - `Phase2State.SpawnWave`(장풍) / `Phase3State.CoFireCrossLaser`(레이저) 발사 시점에 호출
  - `Roar`/`Death`: `BossController.CoPhaseEnd` - Roar는 기믹 시작 시그널 + 페이즈 진입 시 2회, Death는 최종 처치 시 + 3페이즈 진입 시(스펙 반영, 재생 후 Idle로 복귀하도록 애니메이터도 수정)
  - 트리거 파라미터명은 `Constants.Combat.BOSS_ANIM_*`로 계약값 관리
- [x] 근접 유닛 점프(도약 슬램)/회피(무적 백스텝) 로직 구현 (2026-07-12, `BossMeleeUnitBase` 본체·분신 공용)
  - 점프: `JumpTriggerRange`(기본 6)보다 멀면 `CoJumpLoop`가 `JumpCooldown`마다 플레이어 방향으로 도약(`JumpForce`/`JumpForwardSpeed`) - 착지 순간 기존 `_attackHitbox`를 `JumpLandActiveTime`만큼 켜서 슬램 판정(각 유닛 AttackHalves 재사용, 새 데미지 값 없음). 착지 판정은 `Collider2D` 기반 지면 레이캐스트(`_groundLayer`, Player의 `CheckGrounded`와 동일 방식)
  - 회피: `DodgeTriggerRange`(기본 1.5)보다 가까우면 `CoDodgeLoop`가 `DodgeCooldown`마다 반대 방향으로 `DodgeDuration` 동안 백스텝 - 그동안 `IsInvulnerable`(`TakeDamage`가 참조)로 피해 완전 무시
  - 밸런싱 값은 `BossDataSO`에 본체·분신 공용 필드로 추가(개별 스탯 아님) - 전용 애니메이터 파라미터는 `Constants.Combat.BOSS_ANIM_JUMP`/`BOSS_ANIM_DODGE`
  - 공격/점프/회피 세 루프가 서로를 배타적으로 막음(`_isAttacking`/`_isJumping`/`_isDodging`), 전부 `BeginCombat`/`StopCombatLoops`(구 `StopAttackLoop`, 되감기 훅 포함)로 일괄 시작·정지
- [x] `BossBody`/`BossClone1`/`BossClone2` GameObject `Boss.unity` 씬 배치 (2026-07-12, 임시 플레이스홀더)
  - 각각 Rigidbody2D/BoxCollider2D(비트리거)/SpriteRenderer(Boss1 스프라이트시트 임시 사용)/Animator(`Boss.controller`)/`BossBodyController`·`BossCloneController` + 자식 `AttackHitbox`(BoxCollider2D 트리거 + `DamageHazard`)
  - `_groundLayer`는 전부 Ground 레이어로 지정 완료, `_boss`/`_animator`/`_attackHitbox` 상호 참조 연결 완료, 시작 시 비활성(`SetActive(false)`)
  - `BossController._body`/`_phase1Clones`도 새 오브젝트로 연결. `BossController._animator`는 `BossBody`의 Animator를 재사용(1페이즈 기믹-시작 Roar 1회만 Body 비활성 상태라 무음 처리, 이후 전환/사망 신호는 정상 동작)
  - 배치 좌표는 아레나 기준 대략치(Body x=0, Clone x=-4/+4) - 정식 아트/밸런싱 배치 시 조정 필요
  - [ ] 히트박스 타이밍 애니메이션 이벤트로 교체 (기존 TODO 유지 - `BossMeleeUnitBase.CoAttackLoop`)
- [x] `BossBody`/`BossClone1`/`BossClone2` 콜라이더를 실제 애니메이션 프레임 크기에 맞게 축소 (2026-07-12)
  - 기존 기본값(1x1유닛)이 실제 스프라이트(Idle 기준 약 0.30x0.32유닛, PPU 100)보다 훨씬 컸음 - `BoxCollider2D` 크기를 `0.45x0.55`로 축소, offset `(0, 0.275)` + Transform Y를 `0`으로 내려 콜라이더 하단이 지면(y=0)에 닿도록 정리
  - 자식 `AttackHitbox`는 로컬 Y `0.3`(상체 높이)으로 재배치 - 히트박스 자체 크기(사거리 표현)는 유지
  - 참고: `Run`/`Roar`/`Intro` 클립은 `Boss2` 시트가 아니라 `Assets/03.Images/NPC/patches_sprites-sheet0.png`(NPC 임시 스프라이트)를 참조 중 - 추후 정식 교체 필요
- [x] 보스전 카메라 줌아웃 구현 (2026-07-12) — 플레이어 카메라가 평소 타이트 줌(`PLAYER_ORTHOGRAPHIC_SIZE`=1.3)을 유지한 채라 아레나 전체(폭 20유닛)가 화면에 안 들어와 보스/분신이 항상 프레임 밖에 있는 것처럼 보이던 문제(기존에 "보스 입장 연출 - 카메라 줌아웃" LOW 항목으로 미구현 상태였음)
  - `CameraManager.SetPlayerZoom(float)`/`ResetPlayerZoom()` 추가, `BossController.BeginBattle()`에서 `Constants.Camera.BOSS_ORTHOGRAPHIC_SIZE`(5.2, 2026-07-12 사용자 지정값)로 줌아웃 -> `CoPhaseEnd`의 보스 처치 분기에서 `ResetPlayerZoom()`으로 복귀
  - Play 모드 실측 확인(런타임 PlayerCamera OrthographicSize 5.2 적용)
- [x] 분신 추격 속도를 플레이어보다 느리게 조정 (2026-07-12) — `CloneMoveSpeed`(3 -> 1.8)가 플레이어 `MoveSpeed`(2)보다 빨라 항상 따라잡혀 붙어있던 문제. 카메라가 데드존 없이 플레이어를 딱 붙어 따라가다 보니 "분신이 화면 밖을 못 나간다"는 인상으로 보고됨 - 실제 원인은 카메라가 아니라 추격 속도였음. `BossDB.asset` 값도 함께 갱신(SO 에셋이 실제 값 소스)
  - 참고: 본체 `MoveSpeed`(2.5)도 플레이어보다 빠름 - 동일한 이유로 따라잡힐 수 있음, 분신과 별개로 조정 필요하면 요청 바람
- [x] `_totalHealth` 400으로 테스트용 유지 (2026-07-12) — 기획서/코드 주석 기준 64,000과 다르지만 사용자가 테스트 편의를 위해 의도적으로 설정. 정식 배포 전 64,000으로 복원 필요
- [x] 1페이즈 전용 분신 개별 체력바 UI (2026-07-12) — 1페이즈에는 보스 총 체력바 대신 분신 2체 개별 체력바를 표시, 2페이즈부터 보스 체력바로 전환
  - `BossCloneController`에 `CurrentHealth`/`OnHealthChanged`(현재, 최대) 이벤트 추가 (Activate/TakeDamage/되감기 복원 시점마다 발행)
  - `BossCloneHealthBarUI.cs`(06.UI) — 기존 `BossHealthBarUI`와 동일 패턴으로 분신 전용 Slider 바인딩
  - `BossHealthDisplayRouter.cs`(06.UI) — `BossController.OnPhaseChanged` + 초기 `PhaseIndex` 확인으로 1페이즈(`phaseIndex==0`)면 보스바 숨김+분신바 표시, 그 외엔 반대로 전환. `UICanvas/BossUI`에 배치
  - 씬: 기존 `BossHealthBar`를 복제해 `Clone1HealthBar`/`Clone2HealthBar` 생성(가로 절반씩 분할 배치), `_boss`/`_clone`/`_slider` 상호 참조 연결. Play 모드로 실측 확인(1페이즈 시작 시 분신바 2개만 표시됨)
- [x] 분신 피통을 보스 본체 총 피통과 완전 분리 + 보스 바 2페이즈 75% 시작 (2026-07-12)
  - 기존: 분신 피해가 `_boss.TakeDamage`로 공용 피통(64k/400)을 깎으면서 자기 `_health`도 깎는 이중 추적 -> 변경: 분신은 자기 독립 피통만 사용, 감정 반사 규칙만 `BossController.ReflectIfNeeded(source, attacker)`(신규 추출 public 메서드)로 공유
  - `BossController.CoPhaseEnd`에서 페이즈 진입 시 `_currentHealth = PhaseCeilHealth`로 스냅 -> 1페이즈(분신 별도 피통)를 지나 2페이즈 진입 시 본체 바가 75%(= Total - PhaseHealth)부터 표시. 2->3, 3->4는 이미 하한=새 상한이라 값 변화 없음
  - 분신 체력바에서 `PhaseNotch_1/2/3`(25% 페이즈 구분선) 6개 삭제 - 분신 바는 단일 독립 피통이라 페이즈 분할 불필요. 보스 본체 바의 notch는 2/3/4페이즈 경계 표시로 유지
  - Play 모드 런타임 검증: 분신에 1000 피해 -> 보스 총 피통 400 유지(디커플링 확인), 분신 자기 피통만 소진. `PhaseCeilHealth`(400 - 100 = 300 = 75%) 확인
- [x] 패턴 경고 배너 (화면 중앙 대사, 메이플 유피테르식) 2026-07-12
  - `BossBannerUI.cs`(06.UI) — 화면 중앙에 문구를 페이드 인 -> 유지 -> 페이드 아웃으로 표시. CanvasGroup alpha 보간(unscaled - 슬로우/일시정지 무관). **컴포넌트는 항상 활성인 루트(`PatternBanner`)에, 토글/페이드 대상은 자식(`Content`)에 둔다** - 컴포넌트 GameObject를 SetActive(false)하면 StartCoroutine이 죽으므로 CaptionManager와 동일하게 매니저/토글대상 분리
  - `BossController`: `[SerializeField] BossBannerUI _patternBanner` + `_bannerDuration`(2.5s) + `[TextArea] _phase1GimmickMessage`(기본 "균형을 유지하지 못한다면 대가를 치르게 될 것이다") + 공개 `ShowBanner(string)` 메서드. `CoPhaseEnd`에서 1페이즈 즉사 기믹 진입 시 배너 표시
  - 씬: `UICanvas/BossUI/PatternBanner`(+Content: 반투명 검정 BG + DungGeunMo SDF TMP 텍스트, 중앙 상단 y+150). Play 모드로 `ShowBanner` 코루틴 경로 실측 확인
  - **다른 패턴 확장**: 각 페이즈 상태/코루틴에서 `Boss.ShowBanner("문구")` 호출만 하면 됨. 문구는 현재 코드/BossController 필드에 있음 - 대량이면 DB/SO로 분리 검토
- [x] 페이즈별 등장 오브젝트 정리 (2026-07-12) — 1페이즈=분신만, 2페이즈~=본체만
  - 본체(Boss 오브젝트) 스프라이트+콜라이더를 `BossController.SetBodyPresence(bool)`로 토글: `Start`에서 숨김(1페이즈), `CoPhaseEnd`의 2페이즈 진입 시 표시. GameObject 전체가 아니라 컴포넌트만 토글(BossController가 이 오브젝트에 있어 비활성화 불가)
  - 1페이즈 후 분신 숨김은 기존 `Phase1State.Exit -> clone.Deactivate()`가 이미 처리(체력바도 `BossHealthDisplayRouter`가 페이즈 전환 시 전환)
  - Play 모드 실측: 1페이즈 시작 시 본체 renderer/collider 비활성 + 분신 2체 활성 확인
  - 주의: 별도 `BossBody`(-37020, 근접 유닛)와 본체 Boss 오브젝트(91018) 스프라이트가 2페이즈에 둘 다 보일 수 있음 - 시각 중복이면 어느 쪽을 쓸지 정리 필요
- [ ] 장풍 애니메이션 — 현재 임시 사각 스프라이트 (스펙 "애니메이션 포함")
- [ ] 3페이즈 레이저 시작 방향 랜덤 — 현재 좌->우 고정(높이만 랜덤). 정지 빔이라 게임플레이 차이는 없음 - 스펙 문구 유지 여부 확인
- [ ] 4페이즈 전용 전투 패턴 / 리와인드 삭제 연출(회중시계 파괴) — 기획 확정 후
- [ ] 수치 기획 미확정분 확정 — 감정 반사 피해(현 하트 1칸), 장풍 피해(현 1칸), 3P 레이저 피해(현 1칸), 분신 이동/사거리/쿨다운 (전부 BossDataSO TODO 표기)
- [ ] 하트 픽업 리와인드 처리 — 되감으면 획득 취소/부활 여부 기획 확정 (HeartPickup TODO)
- [ ] BossDataSO.CloneCount 미참조 정리 — 실분신 수는 씬 배치 배열이 결정. SO 값과 어긋날 수 있어 제거 또는 검증 로직 추가
- [ ] BeginBattle 시점 이동 — 현재 Start 즉시 타이머 시작. 보스 입장 연출 완성 후 그 시점으로 (BossController TODO)
- [ ] **보스 입장 연출** — ScreenFade + BGM 전환, 팀원 AudioManager 연동 (카메라 줌아웃은 2026-07-12에 구현 완료 - `BossController.BeginBattle`/`CameraManager.SetPlayerZoom`)
- [ ] **보스 입장 재료 시스템** — 특정 아이템 보유 확인, 없으면 보스방 진입 차단

**기획 의도 확인 필요 (코드가 이렇게 동작 - 맞는지 확인)**

- 보스 리와인드는 **피통만** 되돌리고 페이즈/기믹 진행은 되돌리지 않음 (현재 페이즈 구간으로 클램프) - 스펙 "보스 HP도 함께 되돌아간다"와 모순은 아니나 명시 확인
- 1페이즈 레이저 색 시퀀스는 중복 허용 (같은 색 3연속 가능), 안전구역 3개 겹침 방지 없음
- 즉사 판정은 x축 기둥 기준 (세로 전체), "5초 후"는 시퀀스 단위 단일 대기

### Backend — 클리어 연동 (4개)

- [ ] **닉네임 등록 플로우**
  - 첫 실행 시 팝업 → 중복체크(`Register`의 409 처리 활용) → PlayerPrefs 저장
- [ ] **클리어 타임 제출**
  - 보스 사망 이벤트(`BossController.OnBossDefeated`) → `GameManager.StopRunTimerMs` → `SubmitScore`
- [ ] **랭킹 조회**
  - `GetLeaderboard` Top 10 → 명진 랭킹 UI로 데이터 전달
- [ ] **네트워크 Fallback**
  - 타임아웃 2~3초, 실패 시 캐시 표시, 게임 계속 진행

---

## 🟡 4순위 — Week 3~4

> 시스템 완성 후 디테일 추가.

### CheatMode (4개)

- [ ] **콘솔 치트키** (`#if UNITY_EDITOR` 전용)
  - `god`: 무적 토글
  - `skip`: 다음 씬 이동
  - `item [id]`: 아이템 지급
  - `mp`: MP 즉시 맥스
- [ ] **타임스케일 조작**
  - F1: 0.1x / F2: 정상(1x) / F3: 2x
- [ ] **Ctrl+클릭 순간이동**
  - 마우스 클릭 위치로 플레이어 텔레포트
- [ ] **F9 엔딩 강제 트리거**
  - 숨겨진 방 진입 없이 바로 이스터에그 연출 (QA용)

### Visual (1개)

- [x] **분신 반투명 머티리얼** → Sheepy 감성 커스텀 셰이더로 구현
  - `09.Shaders/SheepySprite.shader` — HDR Glow(블룸 연동) + 실루엣 림 라이트, 버텍스/머티리얼 틴트 호환
  - `SheepyCharacter.mat`(본체) / `SheepyClone.mat`(분신, 파란빛 반투명) 생성 및 연결
- [x] **Sheepy 배경 셰이더 스택** (`09.Shaders/`)
  - `SheepyLit.shader` — 2D Light + 노멀맵 + HDR 에미션 (배경/벽면, Sprite-Lit-Default 확장. **씬에 Global Light 2D 필수**)
  - `SheepyFog.shader` — 배경 텍스처를 보라/파랑 안개톤으로 물들이는 틴트형
  - `SheepyFogOverlay.shader` — 레이어 사이에 끼우는 Screen 블렌드 흐르는 안개층 (월드좌표 FBM)
  - `SheepyGodRay.shader` — 창문 빛줄기 (Additive + HDR, 줄기 노이즈 일렁임)
  - `SheepyVisualSetup.cs` — Film Grain 포스트 추가 (Bloom/ColorGrading/Vignette/Grain 4종 세트)

---

## 🟢 5순위 — Week 4 (시간 남으면)

### Boss 추가 연출 (1개)

- [ ] **보스 페이즈 전환 연출**
  - 화면 흔들림 + ScreenFade + BGM 페이즈2 전환
  - (전환 무적/상태 교체 골격은 `TransitionRoutine`에 이미 있음 — 연출만 끼우면 됨)

---

## 📊 전체 요약

| 순위 | 카테고리 | 남은 개수 | 주차 |
|---|---|---|---|
| ✅ 완료 | TimeSystem 코어 / Player 컴포넌트화 / Monster·Boss(1~3P) / BT / 상태이상 / 상호작용 / Sound / Camera / UI / 업적 / Visual / Backend 클라 | 50개+ | — |
| 🔴 1순위 | (완료 - 방어력 계산·사망 애니는 보류/에셋 대기) | **0개** | Week 1 |
| 🔴 2순위 | (완료 - LP 드랍/자석픽업/카운터/리와인드) - 장비/강화/소비아이템 제외 | **0개** | Week 2 |
| 🟡 3순위 | Boss 페이즈 2~4·연출(5) + Backend 클리어(4) | **9개** | Week 3 |
| 🟡 4순위 | CheatMode(4) - 이스터에그 제외 | **4개** | Week 3~4 |
| 🟢 5순위 | 보스연출(1) - 장비 계승 제외 | **1개** | Week 4 |
| **합계 (남은 작업)** | | **14개** | |

---

## 🧹 리팩토링 이력 — 2026-07-12

**구조 — 보스 BT 제거, FSM(BossState/Phase1~4State)으로 완전 일원화**
- `Assets/01.Scripts/03.Boss/BT/`(`IsBossPhaseCondition.cs`, `IsBossTransitioningCondition.cs`) 삭제. `BehaviorGraphAgent`/Behavior Graph 에셋이 어디에도 없어 실제로는 호출되지 않는 미사용 코드였음(Player BT 제거와 동일한 정리)
- 보스 판단은 처음부터 지금까지 순수 C# FSM(`BossState` 추상 클래스 + `Phase1State~Phase4State`)이 전담 - 코드상 실질 변경 없음, 죽은 코드 제거 + 문서 정정(`03.Boss/ (+BT)` -> `03.Boss/`)

**기능 — 1페이즈 즉사 기믹 트리거를 분신 전멸 기준으로 정렬**
- `BossState.UsesHealthFloorTrigger`(기본 true) 추가 - 페이즈 하한 도달 시 `BossController`가 자동으로 `TriggerPhaseEnd()`를 호출할지 여부
- `Phase1State`는 이를 false로 오버라이드하고 `BossCloneController.OnCloneDied` 이벤트로 분신 2체가 모두 죽었는지 직접 확인해 `TriggerPhaseEnd()`를 호출 - 오버킬로 총 피통이 먼저 하한에 닿아도 생존 분신이 있으면 기믹이 시작되지 않음

## 🧹 리팩토링 이력 — 2026-07-11

**구조 — 데이터 관리: Constants 상수 -> ScriptableObject DB (GameDB)**
- 밸런싱 수치를 SO 기반 DB로 이관: `PlayerDataSO` / `BossDataSO` / `TimeDataSO` + 루트 `GameDatabaseSO`(도메인 SO 묶음) + 정적 접근자 `GameDB` (`00.Common/Data/`, `Minsung.Common.Data`).
- 에셋: `08.Data/Resources/GameDB.asset`(자동 로드 루트), `08.Data/Player|Boss|Time/*DB.asset`. 사용처는 `GameDB.Player.MoveSpeed` 형태로 읽는다. SO 변수명은 `So` 어미 (`_playerSo`, `bossSo`).
- `Constants.Player/Combat`은 코드 계약값(입력 키, 판정 epsilon, `HALVES_PER_HEART`, `ANIM_DIR_*`, 몬스터 SerializeField 기본값, `GIMMICK_LASER_COLOR_COUNT`)만 유지. `Constants.time.cs`는 전부 이관되어 삭제.
- 컴포넌트의 밸런싱 SerializeField 미러 제거(PlayerMovement/PlayerHealth/ClonePool/SlowMotionController/RewindManager/CloneController) - DB가 단일 소스, Awake에서 로드. Player.prefab 튜닝값(이동 2 / 점프 6)은 PlayerDB 기본값으로 베이크.
- Phase 상태들의 `static readonly WaitForSeconds` -> 인스턴스 필드 (DB 값 기반 생성 + 도메인 리로드 OFF 대비).
- Monster(`ENEMY_*`)는 BT 그래프 에셋 호환/배치별 인스펙터 튜닝 전제로 Constants 유지 - 몬스터 종류가 늘면 `MonsterDataSO` 분리 예정.
- `Ex/SO` 샘플은 `ExBossDataSO`로 개명해 프로덕션 `BossDataSO`와 이름 충돌 해소.

**구조 — Player 컴포넌트화 (단일 PlayerController -> 코디네이터 패턴)**
- `PlayerController`를 파사드/조율자로 전환. 세부 기능을 `PlayerInput`(입력) / `PlayerMovement`(물리) / `PlayerCombat`(공격) / `PlayerInteraction`(상호작용) / `PlayerRewind`(되감기) / `PlayerStatusEffectController`(상태이상)로 분리.
- `IRewindable` 구현이 `PlayerController` -> `PlayerRewind`로 이동. `ICommandActor`는 `PlayerController` 유지 (해저드/픽업이 "본체"를 루트 컴포넌트로 식별하기 때문).

**기능 — 신규 시스템**
- **상태이상**: `PlayerStatusEffectController`(Bind/InputInvert/RewindSeal) + `PlayerStatusEffectUI`. 리와인드에 포함하지 않고 실시간 진행.
- **상호작용**: `IInteractable`/`InteractableRegistry`/`BaseInteractive` + `LeverInteractive`(되감기 재연) / `RadioInteractive`. 분신 재연용 `InteractCommand`, 애니 스크럽용 `AnimCommand` 추가.
- **사운드**: `SoundManager`/`MapBgmPlayer`/`SoundData(SO)` + 에디터 툴.
- **카메라**: `CameraManager`(Cinemachine 우선순위 포커스 전환).
- **UI**: `CaptionManager`(자막) / `KeyGuideManager` / `SpriteReference`.
- **보스 감정**: `BossEmotion`(반사/낙뢰/혼란 변조) + `BossEmotionHUD`, `DamageSource`로 반사 판정.

**UI 컨벤션**
- 진행바를 `Image.fillAmount` -> `Slider.value`로 통일 (`BossHealthBarUI`). `Ex/`는 샘플 코드로 명시하고 프로덕션(`BossController`)에서 `Ex/BossUIManager` 참조(`public` 필드) 제거.

---

## 🧹 리팩토링 이력 — 2026-07-06

**구조**
- 플레이어 이동/점프 입력을 BT에서 `PlayerController.Update()`로 이관 — `MoveAction`/`JumpAction` 노드 삭제, 키는 `Constants.Player.KEY_JUMP`/`AXIS_HORIZONTAL`로 중앙화. `PlayerBT.asset` 그래프에서 두 노드 수동 제거 필요.

**기능**
- 더블점프 구현 (`MAX_JUMPS = 2`, 낙하 중 첫 점프는 1단 소모로 취급). 2단 점프는 `DoubleJump` 트리거로 별도 모션(`2depthJump`) 재생 — Animator에서 Any State → 1depth/2depthJump 트랜지션 연결 필요.
- 플레이어 입력 전체(공격 X/되감기 R/분신삭제 T)를 `PlayerController.ReadInput()`으로 이관, Player/BT 폴더 완전 삭제 — 프리팹에 BehaviorGraphAgent가 없어 BT 입력이 전부 죽어 있던 문제 해결. 키는 `Constants.Player.KEY_*`로 중앙화.
- 스프라이트 좌우 반전(`flipX`) 구현 — 수평 속도 방향 기준, 되감기 역재생(`SetPose`)에도 적용. 멈추면 마지막 방향 유지.

---

## 🧹 리팩토링 이력 — 2026-07-02

> 동작 변경 없음 (버그 수정 2건 제외). 상세는 커밋/코드 주석 참고.

**버그 수정**
- `AchievementManager`: DB 로드 성공 시에도 LogError가 찍히던 버그 수정
- `VhsRewindOverlay`: 의미가 반전된 `IsInVaildPlay()` → `ShouldKeepPlaying()` 정리

**구조**
- `Utility/PersistentSingleton<T>` 신설 → GameManager / ScreenFade / ParticlePresets / AchievementManager 싱글톤 보일러플레이트 통합
- 리와인드 버퍼 용량 계산 4곳 중복 → `RewindManager.TickCapacity`로 일원화
- `SupabaseClient` 에러 처리 4곳 중복 → `HasError()` 헬퍼로 통합
- Player BT 노드의 하드코딩 키 → `[SerializeField] KeyCode` (기존 그래프 호환, 기본값 동일)

**성능 / GC**
- `RingBuffer.ToOrderedList`(리와인드마다 List 할당) 제거 → `CopyOrderedTo`(무할당), 분신이 클립 리스트 재사용
- `PlayerOrbs`: OverlapCircleAll 배열 할당 + 후보별 클로저 → 재사용 버퍼 + 대상 확정 후 클로저 1개
- `WaitForSeconds` / 머티리얼 / `PlayerHealth` 참조 캐싱, `TryGetComponent` 전환
- `ClonePool`: O(n²) Contains 탐색 → 프리 스택

**API 시그니처 변경 (호출부 함께 수정 완료)**
- `ClonePool.Spawn(List<TickCommand>)` → `Spawn(RingBuffer<TickCommand>)`
- `CloneController.Init(List<TickCommand>)` → `Init(RingBuffer<TickCommand>)`
- 삭제: `RingBuffer.TryPopNewest` / `ToOrderedList`, `Constants.TimeSystem.SLOW_FIXED_DELTA_MULT`

---

## ⚠️ 팀원 연동 포인트

```
명진 작업 완료 후 내가 붙이는 것
├── 플레이어 이동 완료       → 공격 히트박스 연동 테스트
├── 체크포인트 오브젝트 배치 → GameManager 체크포인트 복귀 연동
├── 카메라 줌아웃 완성(2026-07-12) → 남은 건 ScreenFade/BGM 보스 입장 연출 연동
├── 맵 배치 완료             → 회중시계 고정 위치 배치
└── 랭킹 UI 완성             → GetLeaderboard 데이터 전달

진욱 작업 완료 후 내가 붙이는 것
├── 몬스터 사망 이벤트       → 장비/아이템 드랍 로직 호출 + KILL_MP_RESTORE 충전
├── 강화 UI 버튼 이벤트      → 강화 로직 연동
├── AudioManager 완성        → 보스 BGM 전환 연동
└── GameManager 씬전환 완성  → ScreenFade 연동

에디터 셋업 완료 (2026-07-11) - Boss.unity
├── UICanvas (canvas-convention.md 준수: Overlay / 1920x1080 / Match 0.5)
│     ├── BossUI/BossHealthBar: Slider(비상호작용 0~1) + BossHealthBarUI 와이어링
│     │     └── 페이즈 눈금 3개(0.25/0.5/0.75) - 페이즈당 피통 25% 시각화
│     │     └── Play 검증 통과: TakeDamage(16000) -> Slider 1 -> 0.75
│     └── PlayerHUD: Hearts(6칸, HeartFull/Empty) + PlayerHeartUI / StatusEffects(3슬롯) + PlayerStatusEffectUI
│           └── Play 검증 통과: TakeDamage(1) -> 반칸 12 -> 10, HUD 5칸 표시
├── Boss GO(placeholder BossController) + BossEmotionHUD 와이어링 - 감정 아이콘(UICanvas/BossUI/BossHealthBar/EmotionIcon, UI Image, 체력바 좌하단) / 반사 아이콘(Boss/ReflectIcon, SpriteRenderer, 머리 위)
│     └── 주의: 실보스 셋업 시 이 Boss GO를 삭제하지 말고 여기에 살을 붙일 것 (UI가 이 GO의 BossController를 참조)
├── Player: 프리팹 인스턴스 배치 + PlayerHealth/PlayerStatusEffectController 인스턴스에 추가
│     └── 주의: Player.prefab 원본에는 PlayerHealth가 없음 - 프리팹 편입 여부는 진욱과 협의
├── KeyGuideManager / SpriteReference 싱글톤 프리팹 배치
├── AchievementToast (CanvasGroup + Icon/Title/Description, 우하단) + AchievementToastUI 와이어링
│     └── Play 검증 통과: Unlock(first_rewind) -> "시간을 거스른 자" 토스트 페이드 인 확인
└── 남은 것
      ├── BossEmotionHUD._icons(감정6)/_reflectIcons(반사3) / PlayerStatusEffectUI._iconMap / 업적 _icon - 아이콘 에셋 확보 후
      ├── HP바/하트 스타일 스킨 (스타일된 BossHealthBar가 저장소에 없음 - Sample_UI는 빈 씬)
      └── EventSystem (현재 표시 전용 HUD라 불필요, 버튼 등 상호작용 UI 추가 시 필요)

버그 수정 (2026-07-11)
└── PlayerHeartUI: 씬 로드 시 UI가 플레이어보다 먼저 깨어나면 MaxHalves=0으로 그려져
      하트가 전부 숨겨지던 초기화 순서 버그 - Start()에서 재드로우 추가로 수정

프리팹 정비 (2026-07-11)
├── Player.prefab: PlayerHealth 편입 (씬 인스턴스 추가분 Apply - 기존엔 프리팹에 없어서 씬마다 수동 추가해야 했음)
├── clonePrefab: PlayerHealth 추가 (분신 하트 규칙 공유인데 빠져 있었음)
├── GameHUD.prefab 신설 (02.Prefabs/UI) - UICanvas 전체(BossUI/PlayerHUD/AchievementToast) 프리팹화
│     └── 드롭인 자동 와이어링: HeartUI/StatusEffectUI/BossHealthBarUI가 인스펙터 미지정 시
│           씬의 PlayerController/BossController를 Start에서 자동 탐색 - Map1/Map2에 끌어다 놓기만 하면 동작
│     └── 보스 없는 맵에서는 BossHealthBar가 자동 숨김 (SetActive false)
│     └── Play 드롭인 테스트 통과: _boss/_playerHealth/_statusEffects 전부 자동 연결 확인

에디터 셋업 필요 (코드는 완료, 씬 연결만 남음)
├── Player: BehaviorGraphAgent + Behavior Graph 에셋 (PlayerController.cs 상단 주석 참고)
├── Monster: BehaviorGraphAgent + 순찰/추격/공격 그래프 (MonsterController.cs 상단 주석 참고)
├── Player: Animator Controller 5파라미터 구성 (PlayerAnimator.cs 상단 주석 참고)
├── Clone: clonePrefab에 본체와 같은 Animator + PlayerAnimator 추가 시 자동 연결 (코드 준비 완료)
└── ~~Resources/AchievementDatabase.asset + 업적 4종 에셋 생성~~ -> 이미 존재 확인 (08.Data/Resources + 08.Data/Achievements, 2026-07-11) - 업적 _icon 스프라이트만 미지정
```
