# Boss2 (Map3, 3~4페이즈 부유 보스) 인수인계

> 작성일: 2026-07-20
> 대상: `Assets/00.Scenes/Jinwook/Map3.unity`의 부유 보스(Boss2) 시스템
> 관련 커밋: `c44a956`(이동) / `2bade9c`(HP UI·히트박스·돌진) / `bd70503`(원거리 패턴)
> **이 문서는 진욱 작업분 전용이다. `claude/PLAN.md`(민성 구현 목록)는 건드리지 않았다.**

---

## 1. 개요

Map3의 보스(3~4페이즈)는 기존 `Minsung.Boss.BossController` 계열과 완전히 별개의 신규 시스템이다.

- **네임스페이스 없음** — 팀 컨벤션(`claude/CLAUDE.md` Critical Rule 2)에 따라 진욱 코드는 네임스페이스를 쓰지 않는다.
- **기존 `Minsung.Boss.*` 코드는 한 줄도 수정하지 않았다.** 재사용 가능한 것(`BossHazardPool`, `DamageHazard`, `IDamageable`, `DamageSource`)은 그대로 참조해서 쓰고, 재사용이 안 되는 것(`BossController` 타입에 고정된 `BossHealthBarUI` 등)만 같은 이름으로 Boss2 폴더에 새로 만들었다.
- 보스는 **부유체** — 중력 없음, `Kinematic Rigidbody2D` + 코드로 직접 위치를 제어한다.

## 2. 스크립트 위치

```
Assets/01.Scripts/03.Boss/Boss2/
├── BossFloatMovement.cs      # 이동 전체 (배회/추적/돌진)
├── Boss2Health.cs            # 체력 (IDamageable) + 감정 반사 판정
├── BossHealthBarUI.cs        # 체력바 UI (Minsung.UI.BossHealthBarUI와 동명, 네임스페이스만 다름)
├── Boss2AttackPatterns.cs    # 원거리 패턴 3종 + 감정 코디네이터
├── Boss2LightningPattern.cs  # 낙뢰 (감정 Pink/Blue 배율 반영)
├── Boss2WavePattern.cs       # 강타(장풍)
├── Boss2LaserPattern.cs      # 레이저
├── Boss2Emotion.cs           # 감정 enum + 판정 헬퍼 (Minsung.Boss.BossEmotion 이식, 이름은 Boss2Emotion)
├── Boss2EmotionController.cs # 감정 상태/결정 로그/부가효과 (Minsung.Boss.BossEmotionController 이식)
└── Boss2EmotionHUD.cs        # 감정 아이콘 UI (Minsung.Boss.BossEmotionHUD 이식)

Assets/01.Scripts/00.Common/Data/
└── Boss2DataSO.cs            # Boss2 전용 밸런싱 DB (GameDB 트리에는 연결 안 함)
```

- `Boss2DataSO`는 `Assets/08.Data/Boss2/Boss2DB.asset`에 저장. `GameDatabaseSO`(민성 소유, `Minsung.Common.Data`)에는 슬롯을 추가하지 않았다 — 구조 변경은 소유자 확인이 필요해서, 완전히 독립된 SO로 분리했다. 컴포넌트 인스펙터에 직접 드래그해서 연결하는 방식.
- 새 `*DataSO`는 앞으로도 전부 이 폴더(`00.Common/Data/`)에 만들기로 함(사용자 지시).
- **명명 예외 — `Boss2Emotion*`는 "동일 이름 + 네임스페이스만 제거" 규칙을 안 따르고 `Boss2` 접두사를 붙였다.** `BossEmotionController`로 그대로 만들었더니 `Minsung.UI.BossEmotionIconTooltip.cs`(`using Minsung.Boss;` + 무네임스페이스 참조)가 실제로 컴파일 에러(`CS0029`)를 냈다 — 네임스페이스 없는 전역 타입은 C# 이름 해석 규칙상 `using` 지시문보다 우선순위가 높아서, 우리가 만든 전역 `BossEmotionController`가 Minsung 파일이 기대하던 `Minsung.Boss.BossEmotionController`를 가려버린 것. Minsung 코드는 수정 불가라 우리 쪽 이름을 바꿔서 해결했다(`Boss2Emotion`/`Boss2EmotionController`/`Boss2EmotionHUD`). **앞으로 Boss2 신규 클래스가 Minsung.Boss 타입과 동명이면, 그 타입을 다른 네임스페이스 파일이 `using Minsung.Boss;`로 참조하는 곳이 있는지 먼저 확인할 것** — 있으면 `Boss2` 접두사를 붙인다.

## 3. 씬 구성 (`Map3.unity`)

```
--------Boss------------------
└── Boss                              (Rigidbody2D Kinematic, BossFloatMovement,
    │                                  Boss2AttackPatterns, Boss2EmotionController(런타임 AddComponent),
    │                                  Boss2EmotionHUD)
    ├── AttackHitBox                  (BoxCollider2D Trigger + Minsung.Boss.DamageHazard,
    │                                  평소 비활성 — 돌진 중에만 켜짐)
    ├── Visual                        (SpriteRenderer + Animator -> Boss2.controller)
    ├── HitCenter                     (BoxCollider2D Trigger + Boss2Health)
    └── ReflectIcon                   (SpriteRenderer, 머리 위 반사 아이콘 - 반사 감정일 때만 표시)

GameHUD (Assets/02.Prefabs/UI/GameHUD.prefab 인스턴스)
├── BossUI/BossHealthBar[ON]          (Slider + BossHealthBarUI, Boss2Health 구독)
│   ├── PhaseNotch_2만 활성(중앙 50% 표시선), 1/3은 비활성 — 2분할용
│   └── EmotionIcon                   (Image, 좌하단 감정 아이콘 - 항상 표시)
├── BossUI/BossTimer[ON]              (Minsung.UI.BossTimerUI, GameManager.IsBossRunActive/BossClearTimeMs 구독)
│   └── TimerFrame / Label("BOSS") / TimerText("00:00") / Label (1)("TIMER") — Map2의 BossTimer[ON]을 좌표/폰트/색상까지 동일하게 복제
└── PlayerHUD/Hearts                  (PlayerHeartUI, 기존 그대로 재사용)
```

- **`BossTimer[ON]`**: `Minsung.UI.BossTimerUI`는 수정 없이 그대로 재사용(프리팹이 아니라 Map2처럼 씬에 개별로 얹은 비-프리팹 오브젝트). Boss1은 `BossController.BeginBattle()`(입구 트리거 경유)에서 `GameManager.Instance.StartBossTimer()`를 호출하지만, Boss2엔 별도 입장 트리거가 없어서 [Boss2AttackPatterns.cs:47](Assets/01.Scripts/03.Boss/Boss2/Boss2AttackPatterns.cs#L47) `Start()`(보스 스폰 시점)에서 바로 호출하도록 함 — "보스 스폰 == 전투 시작"으로 단순화한 것. `Minsung.Common` using 추가, `RewindManager.Instance?.Register(this)` 바로 다음 줄에 추가. `Boss2Health.TakeDamage`가 체력 0 도달 시 `GameManager.Instance?.StopBossTimer()`도 같이 호출한다.

## 7.5 보스 감정 (`BossEmotion`) 이식

`Minsung.Boss.BossEmotionController`/`BossEmotion`/`BossEmotionHUD`를 Boss2 전용으로 이식했다(명명 규칙은 위 "명명 예외" 참고). 규칙은 원본과 100% 동일 - 페이즈와 무관하게 공통 패턴을 변조:

| 감정 | 효과 |
|---|---|
| Black | 모든 공격 반사 (본체+분신) |
| White | 본체(`DamageSource.Player`) 공격만 반사 |
| Navy | 분신(`DamageSource.PlayerClone`) 공격만 반사 |
| Pink | 낙뢰 발생 간격 x2 배율(더 자주) |
| Blue | 낙뢰 발생 간격 x0.5 배율(덜 자주) + 맵에 하트 픽업 생성 |
| Angry | 10초마다 1초간 키반전(`PlayerStatusEffectController.InputInvert`) |

**코디네이터는 `Boss2AttackPatterns`** — `Minsung.Boss.BossController`가 하던 역할(감정 컨트롤러 소유/설정/시작, 반사 판정 노출)을 Boss2에선 이 클래스가 맡는다.
- `Awake()`에서 `Boss2EmotionController`를 `AddComponent`(★ `Start()`가 아니라 `Awake()`인 이유는 아래 함정 참고)
- `Start()`에서 `Configure(...)` 호출(`_target`에서 `PlayerController`를 구해서 넘김) 후 `StartEmotionLoop(applyImmediately: true)` — `EmotionInterval`(기본 8초)마다 자동으로 랜덤 감정 전환
- 낙뢰 패턴 생성자에 `_emotionController.LightningRateMultiplier`(메서드 그룹)를 넘겨서 매 사이클 배율 반영
- 리와인드 시작/종료 시 `_emotionController.StopEmotionLoop()`/`StartEmotionLoop()`도 같이 호출(패턴 정지/재시작과 동일한 수준)

**반사 판정은 `Boss2Health.TakeDamage`** — `GetComponentInParent<Boss2EmotionController>()`로 부모(Boss 루트)의 컨트롤러를 찾아 `ReflectIfNeeded(source, attacker)` 호출. true면 공격자가 `PlayerHealth.TakeDamageHalves(ReflectHalves)`로 대신 피해를 입고 보스는 무피해.

**HUD는 `Boss2EmotionHUD`** — `GameHUD/BossUI/BossHealthBar[ON]/EmotionIcon`(Image, 좌하단 48x48)과 `Boss/ReflectIcon`(SpriteRenderer, 머리 위)을 감정에 따라 갱신. 아이콘 스프라이트는 Boss1과 완전히 같은 에셋 재사용(`Assets/03.Images/UI/SheepyBossEmotion*.png`, `Assets/03.Images/Boss/Reflect/*.png`) — 보스 종류와 무관한 공용 UI 언어라 그대로 썼다.

**함정 - Awake vs Start 타이밍**: 처음엔 `Boss2EmotionController` 생성을 `Boss2AttackPatterns.Start()`에서 했더니 `Boss2EmotionHUD`가 구독을 놓쳐 아이콘이 전혀 안 켜지는 버그가 있었다. `Boss2EmotionHUD`가 `OnEnable`에서 `GetComponentInParent<Boss2EmotionController>()`로 컨트롤러를 찾아 구독하는데, 같은 오브젝트 위 서로 다른 컴포넌트의 `OnEnable` 호출 순서는 Unity가 보장하지 않아서 컨트롤러가 아직 없을 때 먼저 불릴 수 있었다. 두 가지로 고쳤다: (1) `Boss2AttackPatterns`는 컨트롤러를 `Awake()`에서 만든다(모든 오브젝트의 `Awake`가 끝난 뒤에야 `OnEnable`이 시작되는 건 보장됨), (2) `Boss2EmotionHUD`는 구독을 `OnEnable` 대신 `Start`에서 한다(모든 `Awake` 이후 실행이 보장). 둘 다 실제로 재현/수정 확인함.

**하트 픽업(`HeartPickup`) 미배치** — `Boss2AttackPatterns._heartPickup`은 인스펙터에서 비워둔 상태. `Minsung.Boss.HeartPickup`은 재사용 가능한 클래스지만, Boss1도 실제 씬(Map2/Map3)에 인스턴스를 배치한 적이 없다(`BossController._heartPickup`도 항상 `fileID: 0`) — null 체크로 안전하게 no-op 처리되므로 코드상 문제는 없지만, Blue 감정의 하트 픽업 연출은 Boss1/Boss2 둘 다 실전 미검증 상태.

**Play 모드 검증 완료**: `execute_code`로 `Boss2AttackPatterns.EmotionController`를 가져와 `SetEmotion()`을 직접 호출 -
- Black/White/Navy: `Boss2Health.TakeDamage()` 호출 시 반환값 `false` + 체력 불변(반사 확인), 좌하단/머리 위 아이콘 정확히 표시
- Pink/Blue: `LightningRateMultiplier()`가 각각 2/0.5 반환
- Angry: `SetEmotion` 즉시 `_confusionRoutine` 코루틴 시작 확인(리플렉션), None으로 돌아가면 정지
- None: 양쪽 아이콘 모두 비활성화

## 7.6 페이즈 전환 (3페이즈 -> 4페이즈)

`boss-design.md`엔 3~4페이즈 경계 전용 기믹이 없고("나머지 세부 패턴은 추후 확정"), 유일하게 확정된 규칙은 **"4페이즈 진입 시 타임 리와인드 사용 불가"**뿐이다. `Minsung.Boss.Phase4State`도 정확히 이 규칙만 구현하고(패턴은 Phase2State를 임시로 그대로 상속) 나머지는 비워뒀길래, Boss2도 동일한 최소 구현으로 맞췄다.

전부 `Boss2Health.cs`에 있다(별도 FSM/State 클래스 없이 — Boss2엔 애초에 페이즈별 상태 클래스가 없고, 지금은 패턴 교체 없이 하한만 넘어가는 정도라 코디네이터(`Boss2AttackPatterns`)나 별도 상태 클래스를 새로 만들 정도는 아니라고 판단):

- `TakeDamage`가 데미지를 적용한 뒤 `_currentHealth <= PhaseFloorHealth`이고 아직 마지막 페이즈가 아니면 `AdvancePhase()` 호출
- `AdvancePhase()`: `++_phaseIndex` -> `OnPhaseChanged` 이벤트 발행 -> 마지막 페이즈(`_phaseIndex >= PhaseCount - 1`)가 됐으면 `RewindManager.Instance.AcquireRewindLock(this)`로 리와인드 잠금(보스 처치/오브젝트 파괴 시까지 유지, `OnDestroy`에서 `Dispose`)
- `PhaseFloorHealth`/`PhaseCeilHealth`는 `_phaseIndex`를 그대로 참조하는 계산식이라 페이즈가 올라가면 자동으로 다음 구간 기준으로 재계산됨 - 그래서 하한 도달 즉시 그 프레임에 남은 데미지를 이어받지는 않고(이번 히트는 새 하한까지 클램프), 다음 히트부터 새 하한 기준으로 깎인다 (원본 `BossController.TakeDamage`와 동일한 클램프 방식)
- `OnPhaseChanged`는 지금은 아무도 구독하지 않는 훅 - 4페이즈 전용 공격 패턴/감정 강제(예: 3페이즈 "화남 고정" 같은)가 기획 확정되면 `Boss2AttackPatterns`가 구독해서 처리하면 된다
- `BossHealthBarUI`(Boss2)는 `CurrentHealth/MaxHealth` 비율만 그리므로 페이즈 경계에서 시각적으로 끊기지 않고 그대로 이어서 줄어든다 - 코드 수정 불필요했음

**Play 모드 검증 (execute_code, `MaxHealth=2/PhaseCount=2`로 테스트)**:
```
MaxHealth=2 PhaseIndex=0 CurrentHealth=2
Hit1(dmg=1): applied=True health=1 phaseIndex=1 phaseChangedFired=True newPhase=1   # 하한(1) 도달 -> 즉시 4페이즈 전환
Active rewind locks count=1   # 리와인드 잠금 확인
Hit2(dmg=0.5, 4페이즈 하한=0): applied=True health=0.5
Hit3(dmg=0.5): applied=True health=0   # 처치
IsBossRunActive after defeat=False   # StopBossTimer 정상 호출 확인
```

**주의 - `MaxHealth`는 현재 테스트용으로 `2`(실제 값 `5000`이 아님)로 바뀐 상태다.** `Assets/08.Data/Boss2/Boss2DB.asset`에서 되돌려야 함 - 밸런싱 확정 전까지는 이 상태로 페이즈 전환/데미지 상한을 빠르게 반복 테스트하기 위해 일부러 낮춰둔 것.

- **`HitCenter`**: 원래 `Boss`(루트)에 콜라이더+체력이 있었는데, 플레이어 오브 공격이 루트 피벗(시각적으로 턱 근처)에 꽂히는 버그가 있어서 시각적 중심(스프라이트 world bounds 기준 오프셋 `0.34, 1.07`)에 자식으로 분리했다. `AttackHitBox`도 같은 오프셋으로 옮겨서 돌진 판정이 실제 몸통에서 나가게 했다.
- `Boss` 루트의 `BoxCollider2D`는 **Trigger**로 되어 있다 — 처음엔 non-trigger라 플레이어를 물리적으로 밀쳐냈음.

## 4. 이동 (`BossFloatMovement.cs`)

| 요소 | 설명 |
|---|---|
| 배회 | 스폰 지점 반경(`RoamRadius`) 안 랜덤 지점으로 `SmoothDamp` 이동 → 도착 후 랜덤 대기 → 반복 |
| 추적 | 배회 중심(`_origin`)이 `FollowSpeed`(1/초, 이동 속도보다 훨씬 느림)로 플레이어를 향해 서서히 이동 — "쫓아가되 종속되지 않는" 느낌 |
| 상하/좌우 흔들림 | Sin파, `VerticalAmplitude`/`HorizontalAmplitude` |
| 높이 제한 | `_maxHeightAnchor`(현재 `Mountain_Temple_Portal_19632`) y + `MaxHeightMargin`을 상한으로 클램프 |
| 몸통박치기 (돌진) | `ChargeCooldown`(6초)마다 플레이어가 `ChargeRange` 안이면 시도. 예고 정지(`ChargeTelegraphTime`) → 스냅샷한 목표로 등속 직선 돌진(`ChargeSpeed`) → `AttackHitBox` 활성화(`DamageHazard`가 판정) |
| 물리 보간 | `Rigidbody2D.interpolation = Interpolate` — 없으면 FixedUpdate 틱 사이가 계단식으로 끊겨 보임 (실제로 이 버그가 있었고 고쳤음) |

**리와인드 연동 완료** — `IRewindable` 구현. 매 틱 최종 위치(흔들림 포함)를 `RingBuffer<Vector2>`에 기록/복원한다. 되감기 시작 시 배회/돌진 코루틴 정지(`_isRewinding` 플래그로 `FixedUpdate` 가드), 종료 시 복원된 위치를 새 `_origin`/`_waypoint`/`_baseX`/`_baseY`로 삼아 그 자리에서 재시작. 배회 웨이포인트/돌진 타이밍 자체의 결정 로그는 아직 없어서(랜덤은 리와인드 후 새로 뽑힘) 완전한 프레임 재현은 아니고, 위치만 정확히 되감긴다.

## 5. 체력 / 피격 (`Boss2Health.cs`)

- `IDamageable` 구현 — 플레이어 `AttackHitbox`/`PlayerOrbs`가 `hit.transform`에서 자동으로 찾아 피해를 준다(코드 수정 없이 그냥 인터페이스 구현만으로 연동됨).
- `MaxHealth = 5000`(TODO: 밸런싱/페이즈 확정 전 임시값), 페이즈 개념 없음 — 그냥 0까지 깎이는 단일 피통.
- `OnHealthChanged(current, max)` / `OnDefeated` 이벤트 제공. 사망 연출/처치 처리는 미구현(TODO).
- **리와인드 연동 완료** — `IRewindable` 구현. 매 틱 체력값을 `RingBuffer<float>`에 기록/복원. 되감기 중엔 `TakeDamage`를 차단한다(`_isRewinding` 가드 — 플레이어/몬스터 체력 가드와 동일한 관례).

## 6. UI

- **Player 하트**: `Minsung.Player.PlayerHeartUI` 그대로 재사용(수정 없음). `GameHUD` 프리팹의 `Hearts`가 이미 6칸+스프라이트까지 세팅되어 있어서 인스턴스화만 하면 자동으로 플레이어를 찾아 붙는다.
- **Boss 체력바**: `Minsung.UI.BossHealthBarUI`는 필드 타입이 `Minsung.Boss.BossController`로 고정돼 있어 재사용 불가. 그래서 같은 이름(`BossHealthBarUI`)에 네임스페이스만 뺀 새 클래스를 Boss2 폴더에 만들어 `Boss2Health`를 구독하게 했다. 페이즈 노치 로직(`_phaseNotches`)은 페이즈 개념이 없어서 뺐다.
- 노치는 원래 프리팹 기본값이 3개(25/50/75%, 4페이즈 기준)라 `PhaseNotch_1`/`PhaseNotch_3`을 비활성화하고 `PhaseNotch_2`(50%)만 남겨 2분할로 맞췄다.

## 7. 원거리 패턴 (낙뢰 / 강타 / 레이저)

`Boss2AttackPatterns`가 `Boss`에 붙어 세 패턴을 `Start()`에서 전부 `Play()`, `OnDestroy()`에서 `Dispose()`. 각 패턴은 독립 클래스로, **`Minsung.Boss.BossHazardPool`/`DamageHazard`를 그대로 재사용**한다(이 둘은 `BossController`에 묶여 있지 않은 범용 유틸이라 가능했음).

| 패턴 | 파일 | 동작 | 원본 참고 |
|---|---|---|---|
| 낙뢰 | `Boss2LightningPattern.cs` | 예고(노란 장판) → 강타(즉발, 크랙클 7프레임 순환) → 회수. 낙하 x는 플레이어 주변 랜덤 | `Minsung.Boss.BossLightningPattern` |
| 강타 | `Boss2WavePattern.cs` | 예고 파티클 → 폭발 강타(9프레임 순환, 뒤 5프레임 이후 무판정) → 회수. x는 아레나 전체 랜덤 | `Minsung.Boss.Phase2State`의 장풍 로직 |
| 레이저 | `Boss2LaserPattern.cs` | 빨간 점멸 경고 → 발사(회전 사각 판정) → 좁아지며 회수. 아레나를 가로지름, 시작/도착 높이 랜덤 | `Minsung.Boss.Phase3State`의 레이저 로직 |

**에셋 연결 (원본과 GUID/fileID까지 동일하게 맞춤)**
- 낙뢰 크랙클 7프레임: `Assets/03.Images/Boss/Boss2/zone5_boss2_fxs_ground-sheet0.png`
- 강타 폭발 9프레임: `Assets/03.Images/Boss/Boss2/boss2_fx_explosion-sheet0.png`
- 레이저는 스프라이트가 아니라 **순수 쉐이더 머티리얼** — `Assets/Resources/Phase3LaserBeamMat.mat`(`Minsung/Phase3LaserBeam` 쉐이더)를 `Resources.Load`로 그대로 재사용
- `_lightningColor`는 **흰색(1,1,1,1)** 이어야 함 — 스프라이트 자체가 보라색이고 렌더러 색과 곱해지는 구조라, 틴트를 넣으면 색이 틀어진다(실제로 노란빛을 넣었다가 빨갛게 나오는 버그가 있었음, 수정 완료)

**아레나 경계**는 `Boss2AttackPatterns` 인스펙터에 씬별 SerializeField로 있음(현재 `-10 ~ 10`, 바닥 `y=-3`, 임시값 — 원본도 `BossController`에 동일하게 씬 SerializeField로 둠).

**리와인드 연동 완료 (정지/재시작 수준)** — `Boss2AttackPatterns`가 `IRewindable` 구현. 원본 `BossLightningPattern`과 동일한 수준으로, 프레임 단위 스냅샷은 없고 되감기 시작 시 세 패턴 `Stop()`(정지+회수), 종료 시 `Play()`(재시작)만 한다. 원본 `Phase2State`/`Phase3State`처럼 진행 중이던 예고/강타를 정밀 스크럽하는 것과 결정 로그(랜덤값 리와인드 재현)는 아직 없다.

## 8. 검증 상태

Unity MCP로 Play 모드 진입 후 직접 확인한 것:

- [x] 배회/추적/높이 제한 — 위치 샘플링으로 확인
- [x] 몸통박치기 — 플레이어 체력 12→6 하프하트 차감 확인
- [x] Rigidbody2D interpolation — 처음엔 `Boss`에 Rigidbody2D 자체가 없어서 무의미했던 걸 발견, 추가 후 재확인
- [x] `Boss2Health.TakeDamage` — 리플렉션 호출 + 실제 오브 공격 양쪽으로 체력 감소 확인 (예: 2500→2480, 5000→4960)
- [x] 오브 공격이 `HitCenter`(시각적 중심)에 명중 — 위치 비교로 확인
- [x] Player/Boss HP UI 렌더링 — 스크린샷으로 확인
- [x] 낙뢰/강타/레이저 스폰 및 판정 — 활성 슬롯 스프라이트/머티리얼 이름으로 확인
- [x] 낙뢰 색상 수정 — 슬롯 색상값으로 확인
- [x] 리와인드 — 체력 5000→3500 데미지 후 되감기 -> 5000으로 완전 복원, `_isRewinding` 플래그 정상 해제, 되감기 종료 후 배회 이동도 정상 재개 확인
- [x] `BossTimer[ON]` UI 이식 + 타이머 시작/정지 트리거 — Play 진입 시 `Boss2AttackPatterns.Start()`가 자동으로 `GameManager.StartBossTimer()` 호출, `IsBossRunActive=True`로 전환되어 CanvasGroup alpha 1, `BOSS TIMER 00:24`까지 자동 증가하는 것을 스크린샷으로 확인. `Boss2Health` 체력 0 도달 시 `StopBossTimer()` 호출도 코드로 연결(체력 0 도달 자체는 아직 사망 연출 미구현이라 실전 시나리오로는 미검증)
- [x] 보스 감정(`Boss2Emotion`) 이식 — 반사(Black/White/Navy)/낙뢰 배율(Pink/Blue)/키반전(Angry) 로직 + HUD 아이콘 2종(체력바 좌하단, 머리 위 반사). `execute_code`로 각 감정 강제 전환 -> 반사 시 피해 무효 + 체력 불변, 낙뢰 배율 2/0.5 반환, 키반전 코루틴 시작/정지, 아이콘 표시/숨김 전부 확인. 하트 픽업(Blue)은 씬에 `HeartPickup` 인스턴스가 없어 미검증(Boss1도 동일 상태)
- [x] 페이즈 전환(3->4) — `MaxHealth=2`로 낮춰서 테스트: 하한 도달 시 `_phaseIndex` 증가 + `OnPhaseChanged` 발행 + 리와인드 잠금(잠금 개수 1로 확인) + 다음 하한(0)까지 데미지 계속 적용 + 0 도달 시 처치(`StopBossTimer` 호출까지) 전부 확인. 자세한 로그는 7.6절 참고

**주의**: 테스트 중 Unity 에디터가 **Pause 상태**에서는 코루틴이 전혀 진행되지 않아 "공격해도 안 맞는" 것처럼 보인 적이 있었다. 실제 버그가 아니라 Pause 버튼 상태 문제였음 — 재현되면 이것부터 확인할 것.

**중요 — Map3에 `ClonePool`이 없어서 되감기가 전체적으로 깨져 있었다.** `Minsung.Player.PlayerRewind.OnRewindEnd()`가 `_clonePool.Spawn(_buffer)`를 null 체크 없이 호출하는데(`PlayerRewind.cs:161`), Map3엔 `ClonePool` 오브젝트 자체가 없어서(Map2엔 있음) 되감기 종료마다 NullReferenceException이 터졌다. `RewindManager`의 되감기 브로드캐스트 루프에 예외 처리가 없어서, 이 예외 때문에 리스트상 그 뒤에 등록된 리와인더(Boss2Health 등)는 `OnRewindEnd`가 아예 호출되지 않고 있었다. **Map2와 동일하게 `ClonePool` 오브젝트(씬 루트, `Assets/02.Prefabs/Test/clonePrefab.prefab` 연결) + `Player`의 `PlayerRewind._clonePool` 참조를 추가해서 해결** — 코드는 안 건드리고 씬 배치만 채운 것. 이건 Boss2 전용 이슈가 아니라 Map3 전체(플레이어 자신의 분신 소환 포함)에 영향 있던 버그였다.

## 9. 남은 작업 (TODO)

- [ ] 결정 로그 기반 정밀 리와인드 — 지금은 위치/체력만 되감기고, 배회 웨이포인트·돌진 타이밍·낙뢰/강타/레이저 발사 시점은 되감기 후 새로 랜덤하게 결정된다(원본 Phase2/3State의 `_waveXLog`/`_laserLog` 같은 결정 로그가 없음). `Boss2EmotionController`는 `_emotionLog`/`_emotionCursor`를 갖고 있지만(원본 그대로 이식), 리와인드 시작/종료 시 커서를 되돌리는 `Capture()`/`Restore()`가 없어서 실질적으로는 "한 번 뽑은 감정 순서를 앞으로만 계속 소비"하는 수준 — 되감은 게임 시간과 정확히 동기화되지 않는다
- [x] 페이즈 전환(3->4) 기본 골격 — 하한 도달 시 `_phaseIndex` 증가 + 4페이즈 리와인드 잠금까지 구현·검증 완료(7.6절). 남은 건 4페이즈 전용 공격 패턴/감정(예: 3페이즈 "화남 고정")처럼 기획 미확정 상세뿐 — 확정되면 `Boss2Health.OnPhaseChanged` 구독해서 `Boss2AttackPatterns`에 반영. 페이즈 전환 컷신/기믹이 생기면 `Boss2AttackPatterns.Configure(...)`에 넘기는 `isTransitioning` 콜백(현재 `() => false` 고정)도 실제 전환 상태를 반영해야 한다
- [ ] `MaxHealth`(5000) 등 임시값 전체 밸런싱
- [ ] 사망 연출/처치 처리 (`Boss2Health.OnDefeated` 훅만 있고 실제 연출 없음) — `StopBossTimer()`는 이미 연결됨([Boss2Health.cs](Assets/01.Scripts/03.Boss/Boss2/Boss2Health.cs))
- [ ] 피격 리액션(넉백/플래시 등) — 지금은 체력만 깎임
- [ ] Idle 애니메이션만 있음(Run/Attack/Hit/Death 등 미구현, `Boss2.controller`에 State 추가 필요)
- [ ] 아레나 경계 값(`-10~10`, `y=-3`) 실제 Map3 스테이지 크기에 맞춰 재조정
- [ ] `AttackHitBox`/`HitCenter` 콜라이더 크기·오프셋은 특정 애니메이션 프레임 기준 근사값 — 정밀 조정 필요
- [ ] `HeartPickup` 씬 미배치 — Blue 감정이 하트 픽업을 생성하려 해도 `Boss2AttackPatterns._heartPickup`이 비어 있어 no-op. 배치하려면 `Minsung.Boss.HeartPickup` 컴포넌트(콜라이더 Trigger + 스프라이트) 붙은 오브젝트를 씬에 두고 인스펙터에 연결(Boss1도 실전 미배치 상태라 참고할 예시 씬이 없음)

## 10. 팀 협업 원칙 (반복 강조)

- `Minsung.*` 네임스페이스 코드는 **절대 수정하지 않는다.** 재사용 가능하면 그대로 참조, 안 되면 Boss2 폴더에 동일한 이름으로 새로 만든다.
- Boss2 관련 신규 `*DataSO`는 전부 `Assets/01.Scripts/00.Common/Data/`에, 나머지 스크립트는 `Assets/01.Scripts/03.Boss/Boss2/`에 둔다.
- `claude/PLAN.md`(민성 구현 목록)는 이 세션에서 수정하지 않았다.
