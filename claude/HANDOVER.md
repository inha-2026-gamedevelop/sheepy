# 인수인계 문서 (2026-07-13 갱신)

> 브랜치: `develop` (`feature/boss-batch` + `feature/boss-emotion-scene-handoff` + `feature/player` 병합)

## 2026-07-13 추가 - 로딩/일시정지/메인메뉴/세이브 흐름 신설

1. **씬 전환 로딩 흐름** (`GameManager`, `LoadingController`, `Loading.unity`) - `GameManager.LoadSceneWithLoading`/`LoadGameplayScene`이 `Constants.Scene.LOADING`을 경유해 씬 전환, `LoadingController`가 비동기 로드 진행바(`Slider`)를 갱신. `GameManager`도 `PersistentSingleton` 자동 생성 패턴 적용.
2. **세이브(진행도) 시스템** (`SaveManager`, `Constants.save.cs`) - `PlayerPrefs` 기반 최소 진행도(마지막 플레이 씬) 저장/로드, 로비 '이어하기' 버튼 활성화 판단에 사용.
3. **일시정지 흐름** (`PauseController`, `Pause.unity`) - ESC로 게임 씬 위에 `Pause.unity`를 additive 로드, `Time.timeScale=0` + 사운드 일시정지, 진입 시점 화면을 저해상도로 캡처해 블러 배경(`PauseBackgroundView`)으로 표시.
4. **메인메뉴** (`MainMenuController`, `MainMenu.unity`) - 게임 시작/이어하기/설정/종료, 로고/God Ray/Mist 연출.
5. **설정 패널 배경 블러/암전 + ESC 닫기** (`SettingsBackdropView`, `PauseMenuController`, `SettingsPanelController`, `MainMenuController`) - Pause/MainMenu 양쪽 설정 패널을 열 때 진입 시점 화면을 강한 블러(`Constants.UI.SETTINGS_BACKDROP_DOWNSAMPLE=40`)로 캡처해 어둡게(`SETTINGS_BACKDROP_BRIGHTNESS=0.15`) 표시, 뒤 UI 클릭도 차단. 메인메뉴 설정 패널은 ESC로도 닫힘.
6. **`PersistentSingleton<T>` 보강** - 씬에 자식으로 배치된 싱글톤도 `DontDestroyOnLoad`가 정상 동작하도록 루트로 자동 승격하는 안전장치 추가.

### 해야 할 것 (추가)

- [ ] Pause 씬은 ESC를 누르면 설정 패널이 열려 있어도 곧바로 전체 Resume(게임 복귀)으로 넘어감 - 설정이 열린 상태의 ESC는 설정만 닫도록 개선 여지 있음 (메인메뉴 쪽은 이미 반영됨)
- [ ] Loading/Pause/MainMenu 관련 이미지·셰이더(Logo, MainMenuBackground, GodRay/Mist/Shimmer)가 최종 아트인지 placeholder인지 미확인

## 이번에 고친 것 (2026-07-11)

1. **슬로우모션 버그 수정** (`HitStopController`) — 히트스톱이 연쇄 재요청되면 이전 코루틴이 복원 코드를 못 밟고 끊겨 `Time.timeScale`이 0에 눌러앉는 문제. 코루틴 재시작 대신 종료 목표 시각(`_stopUntil`)만 미루는 단일 코루틴 방식으로 수정.
2. **1페이즈 즉사 기믹 개선** (`Phase1State`, `CameraManager`) — 기믹 시작 시 카메라 줌아웃(`GimmickCameraOrthoSize`)/성공 시 원복, 예고 구간 전체 3색 안전구역 동시 표시, 실전 레이저 매 발사 5초(`GimmickRefireDelay`) 간격.
3. **감정 자동 전환 구동부** (`BossController`, `Phase3State`) — `EmotionInterval`(8초)마다 Black/White/Navy/Pink/Blue 랜덤 전환, 결정 로그로 되감기 재현. 3페이즈는 `SetAutoEmotionSuspended`로 화남 고정. Play 로그로 전환 자체는 확인됨.
4. **2->3페이즈 씬 이관 로직** (`BossHandoff` 신설, `BossController`, `Phase2State`) — 페이드 암전 시 피통/페이즈/전투경과/감정을 정적 캐리어에 저장 후 `SceneManager.LoadScene`, 새 씬 `BossController.Start`가 복원.
5. **보스 감정/반사 아이콘 분리** (`BossEmotionHUD`, `BossEmotion`, `Boss.unity`) — 기존 머리 위 단일 `EmotionIcon`(감정+반사 겸용, 월드 스페이스 SpriteRenderer)을 둘로 분리. 감정 아이콘은 UI `Image`로 전환해 `UICanvas/BossUI/BossHealthBar/EmotionIcon`(체력바 좌하단, anchor(0,0)/pivot(0,1)/48x48)에서 현재 감정을 항상 표시(`_icons` 맵). 반사 아이콘은 월드 스페이스 `ReflectIcon`(구 EmotionIcon 개명)으로 머리 위에서 반사 감정(Black/White/Navy)일 때만 표시(`_reflectIcons` 맵). `BossEmotion.IsReflect()` 헬퍼 추가. Play 모드에서 두 아이콘 동시 표시 검증 완료.
6. **장비/강화/소비아이템/MP게이지/이스터에그 범위 제외 + LP 수집 시스템 신설** (`claude/PLAN.md` 참고) — 장비 시스템 전체와 MP 게이지를 채택하지 않기로 확정(`TimeDataSO`의 미사용 MP 필드 제거). 아이템은 LP(단순 수집 카운터) 하나로 단순화:
   - `LpDataSO`(`GameDB.Lp`, `08.Data/Lp/LpDB.asset`) 신설 — DropChance/MagnetRadius/MagnetSpeed/CollectRadius/PoolSize
   - `LpPickupPool`(`12.Item/`) — `BossHazardPool`과 동일 패턴, 코드로 GameObject를 생성해 관리(프리팹/씬 배치 불필요, 현재 연두색 사각 placeholder)
   - `LpManager`(`12.Item/`, `IRewindable`) — `RewindManager`와 동일하게 씬 로드 시 자동 생성되는 싱글톤. 매 `FixedUpdate`에서 거리 판정으로 자석 이동/획득 처리(콜라이더 불필요), 개수(`RingBuffer<int>`) + 풀 슬롯별 위치/활성상태(슬롯당 `RingBuffer<LpSlotTick>`)를 기록해 되감기 시 둘 다 복원
   - `MonsterController`가 `MonsterHealth.OnDeath`에서 `LpManager.Instance?.TryDropLp(position)` 호출 (드랍 확률은 GameDB.Lp.DropChance)
   - HUD: `UICanvas/PlayerHUD/LpCounter`(Icon+TMP Count) 신설, `LpCounterUI`가 `LpManager.OnLpChanged` 구독
   - 검증: Play 모드에서 `IRewindable` 메서드를 직접 호출하는 격리 단위 테스트로 카운트 복원 + 풀 슬롯 비활성화/재등장 양방향 모두 확인 (에디터가 포커스를 잃으면 Play 루프가 실시간으로 진행되지 않아 `sleep` 기반 실측 테스트는 신뢰 불가 — 아래 "주의사항" 참고). 실제 플레이어 이동/실시간 되감기 입력을 통한 end-to-end 확인은 아직 못함 - 에디터에서 직접 플레이 확인 권장
   - 부수 수정: `Boss.unity`의 `Player` GameObject 태그가 `Untagged`로 잘못 설정돼 있어 `Constants.Tag.PLAYER` 태그 검색(LpManager/MonsterController 공통)이 실패하던 기존 씬 버그를 `Player` 태그로 수정
7. **몬스터 BT 프리팹 준비** — `Assets/02.Prefabs/Monster/Monster.prefab` 신설(Rigidbody2D/BoxCollider2D/SpriteRenderer(placeholder)/MonsterController/MonsterHealth/BehaviorGraphAgent). "Enemy" 태그/레이어를 프로젝트에 등록. 순찰/추격/공격 C# 노드(`Monster/BT/*.cs`)는 이미 완성돼 있었으나 실제 Behavior Graph 에셋과 프리팹이 없었음 - 프리팹까지는 준비 완료, **Behavior Graph 노드 와이어링은 비주얼 에디터 전용 작업이라 수동 진행 필요** (`.claude/TODO_MANUAL.md` 참고, 커밋 안 되는 개인 메모)

## 해야 할 것

- [ ] **(명진) 맵 3종 제작** — 튜토리얼맵 / 일반맵 / 보스맵 2개(완전히 새로 제작, 기존 `Boss.unity`는 테스트베드였으므로 대체). 보스맵 2개 완성 후 Build Settings 등록 + `BossController._phase3SceneName`을 실제 3페이즈 맵 씬 이름으로 교체 (현재 임시로 `"Boss"` 자기 재로드).
- [ ] **(진욱) 보스 애니메이션 / 애니메이터 연동** — 현재 근접 유닛(`BossMeleeUnitBase`)은 트리거만 쏘고 히트박스는 시간 기반 임시 처리. Sheepy 보스 리소스 연결 후 Animator Controller 구성 + 히트박스를 애니메이션 이벤트 타이밍으로 교체.
- [ ] **보스 유닛 씬 배선** — `BossController._phase1Clones`(분신 2체)/`_body`(본체)/`_heartPickup` 연결. 없으면 근접 패턴·반사·하트픽업을 눈으로 확인 불가.
- [ ] **`BossEmotionHUD` 아이콘 스프라이트 교체** — `_icons`(감정 6종, 체력바)와 `_reflectIcons`(반사 3종 Black/White/Navy, 머리 위) 모두 현재 `boss2-sheet0` placeholder. 실제 아트로 교체 (반사 3종은 전체/본체/분신 구분 아이콘 필요).
- [ ] **`BossDB.asset` Total Health** 를 테스트값 `400`에서 프로덕션 `64000`으로.
- [ ] **2페이즈 근접 주체 확인** — 기획은 "1페이즈 분신 패턴 유지"인데 코드는 본체(Body)가 수행. 기획 해석 확인 필요.
- [ ] **1페이즈 기믹 트리거 정합** — 기획 "분신 2체 모두 처치" vs 코드 "총 피통 하한 도달". 오버킬 시 생존 분신이 남을 수 있음.
- [ ] **LP 아이콘/오브젝트 아트 교체** — `LpPickupPool`(드랍 오브젝트) / HUD `LpCounter/Icon` 모두 현재 흰 텍스처 기반 연두색 사각 placeholder. 실제 LP 아트 확보 후 `SpriteRenderer.sprite`/`Image.sprite` 교체.
- [ ] **LP 사용처 기획 확정** — 현재는 단순 수집 카운터. 상점/성장 등 소비처가 정해지면 `LpManager.AddLp`/`LpCount` 소모 API 추가 필요.
- [ ] **LP 획득 이펙트/사운드** — `LpManager.Collect()`에 TODO로 남겨둠 (ParticlePresets/SoundManager 연동).
- [ ] **몬스터 Behavior Graph 노드 와이어링** — `Monster.prefab`의 `BehaviorGraphAgent`에서 그래프 생성(New Graph) 후 Selector(Is Player In Attack Range -> Attack Player / Is Player Detected -> Chase Player / Patrol) 구성, Agent 블랙보드를 Self로 바인딩. 절차는 `.claude/TODO_MANUAL.md` 참고.
- [ ] **몬스터 아트/애니메이터** — `MonsterAnimator`는 Animator Controller/아트 없어서 이번엔 스킵. 확보되면 컴포넌트 추가 + 스프라이트 교체.
- [ ] 나머지 세부 항목은 `claude/PLAN.md` Boss 섹션(HIGH/MED/LOW) 참고.

## 주의사항

- **Boss 씬(`Boss.unity`)의 Player 태그가 `Untagged`로 되어 있던 기존 버그를 `Player`로 수정함.** `Constants.Tag.PLAYER` 태그 검색에 의존하는 코드(`MonsterController`, `LpManager` 등)가 이 씬에서 정상 동작하려면 필요한 수정이었음. 다른 씬(맵 3종 제작 시)도 Player 프리팹 배치 시 태그 확인할 것.
- **Boss 씬에서 리와인드(`PlayerRewind.OnRewindStart/OnRewindEnd` → `PlayerAnimator.SetReversed`)가 발동하면 콘솔에 `Parameter 'Hash -1564865577' does not exist` 에러가 반복 출력됨.** 이 씬의 Player Animator Controller에 리와인드 역재생용 파라미터가 없거나 다른 것으로 추정되는 기존 이슈 (발견만 하고 미수정 - Animator Controller 담당자 확인 필요).
- **Unity 에디터가 포커스를 잃은 상태(백그라운드)에서는 MCP로 Play 모드를 구동해도 물리 틱(FixedUpdate)이 실시간으로 진행되지 않는 것으로 관찰됨.** 자동화 테스트에서 `sleep` 후 상태 확인하는 방식이 신뢰할 수 없었음 - 프레임 진행이 필요한 검증은 `IRewindable` 메서드를 리플렉션으로 직접 호출하는 결정론적 방식을 쓰거나, 에디터에 포커스를 준 상태에서 확인할 것.
- **`Boss.unity`는 이번 병합에서 `Assets/00.Scenes/Minsung/Boss.unity`로 이동함** (경로 변경, 내용은 동일 + 이번 병합 반영분).
