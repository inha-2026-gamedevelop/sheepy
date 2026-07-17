# 인수인계 문서 (2026-07-17 갱신)

> 브랜치: `develop` (`feature/boss-batch` + `feature/boss-emotion-scene-handoff` + `feature/player` 병합)

## 2026-07-17 추가 - 보스 사망 연출(DeathBody+DeathLight) 결합

1. **DeathBody/DeathLight 동시 재생 구조** (`Boss.controller`, `DeathLightFx.controller` 신설, `Bossbody/Visual` 형제로 `DeathLightFx` 오브젝트 신설, `Boss.unity`) - 두 클립 모두 자기 자신의 `SpriteRenderer.m_Sprite`(경로 `""`)를 갈아끼우는 방식이라 같은 오브젝트/같은 Animator 레이어에서는 동시 재생이 불가능함(하나가 다른 하나를 덮어씀). `Visual`의 형제로 `DeathLightFx`(전용 `SpriteRenderer`+`Animator`, Sorting Order -1로 본체보다 뒤에 그려짐) 오브젝트를 만들고, 파라미터 없이 기본 상태로 `DeathLight.anim`만 재생하는 전용 컨트롤러(`DeathLightFx.controller`)를 연결해 별도 `SpriteRenderer`로 분리하는 방식으로 해결. 씬 시작 시 비활성 배치.
2. **`Boss.controller` Death 트리거 재배선** - 기존에는 `Death` 트리거(AnyState)가 옛날 단일 클립 `Death` 상태를 가리켰음. 목적지를 `DeathBody` 상태로 변경(`DeathBody`/`DeathLight`/`DeathCircle` 상태는 원래 존재했지만 아무 전이도 연결 안 된 고아 상태였음 - `DeathCircle`은 이번 세션에서 다루지 않음, 여전히 미연결).
3. **`BossController.cs`** - `_deathLightFx`(GameObject) 필드 추가, 보스 처치 처리부(`CoPhaseEnd`, 마지막 페이즈 종료 분기)에서 `PlayAnimTrigger(BOSS_ANIM_DEATH)` 직후 `_deathLightFx?.SetActive(true)` 호출해 본체 Death 트리거와 동시에 켜지도록 함. QA 전용 `QaForceDeath()`(`#if UNITY_EDITOR`) 신설 - 전투 진행 없이 사망 연출만 즉시 재생, 1페이즈처럼 본체(Visual)가 비활성이어도 강제로 켜서 확인 가능.
4. **QA 테스트 단축키 추가** (`BossPhaseQaDebug.cs`) - 기존 2/3/4(페이즈 즉시 이동)에 `5`번 키를 추가해 `QaForceDeath()` 연결. `Boss.unity` Play 모드 진입 후 `5`를 누르면 바로 확인 가능.
5. **검증** - Play 모드에서 실제 QA 경로(컴포넌트 참조 그대로) 호출로 Scene View 캡처해 확인. Play 모드 종료 시 `SetActive` 등 런타임 변경은 자동으로 편집 모드 상태로 복구됨(단, 별도 실행한 `EditorUtility.InstanceIDToObject` 기반 강제 조작은 예외적으로 복구가 안 된 사례 있었음 - 아래 주의사항 참고).

6. **`DeathLightFx` 위치/크기 튜닝** - 몸통 중심(`Visual` sprite bounds center y=0.215)에 맞춰 `localPosition (0, 0.2, 0)`, `localScale 1.4배`로 조정. 본인 확인 완료.

### 해야 할 것 (추가)

- [ ] `DeathCircle.anim`은 여전히 `Boss.controller`에서 고아 상태 - 3연출을 다 쓸지, Body+Light 둘만 쓸지 기획 확인 필요
- [ ] `Boss.unity`를 단독으로 Play(정상 부트스트랩 없이)하면 `BossController.cs:159/167`(Update/FixedUpdate), `LpManager.cs:129`에서 NullReferenceException 발생 - 이번 세션 변경과 무관한 기존 이슈로 추정, 원인 미확인

## 2026-07-16 추가 - 1페이즈 즉사 기믹 겹침 방지 / 낙뢰 비주얼 전면 교체 / 2페이즈 장풍 재설계

1. **1페이즈 즉사 기믹 - 안전구역 겹침 방지** (`Phase1State`, 커밋 `fc3d55c`) - 색별 안전구역 3개를 "아레나 균등 분할 + 구간 내 랜덤 배치"로 바꿔 겹침을 원천 차단(`BuildSafeZoneCenters` 신설). 색-구간 배정은 매번 셔플해 특정 색이 항상 같은 위치에 오지 않게 함. 예고 단계는 3색 동시 표시(Shift 중에만)로 개편, 실전 단계 구조/판정은 무변경.
2. **낙뢰(LightningBolt) 비주얼 전면 교체** (`BossHazardPool`, `BossLightningPattern`, `BossDataSO`, `BossDB.asset`, 커밋 `5300c40`)
   - 크랙클 스프라이트를 새 아틀라스(`Assets/03.Images/Boss/Boss2/zone5_boss2_fxs_ground-sheet0.png`)로 교체, 프레임 3 -> 7개(원본 순서 0~6)로 확장. `LightningFrameInterval` 0.05 -> 0.02로 줄여서 같은 `LightningActiveTime`(0.15초, 피해 판정 지속시간) 안에서 전부 순환하도록 함 - 데미지 타이밍은 불변.
   - **아틀라스 pivot 버그**: Multiple 모드로 슬라이스된 서브스프라이트 pivot이 좌하단(0,0)으로 되어 있어, 기존 코드의 "중심 pivot" 전제 위치 계산과 어긋나 강타가 절반쯤 공중에 뜸 - 사용 프레임들의 `.meta` pivot을 (0.5, 0.5)로 수정해서 해결.
   - **`.cs` 기본값과 `.asset` 값 불일치 함정**: SO 필드의 C# 기본값을 바꿔도 이미 직렬화된 `BossDB.asset` 값은 자동으로 안 바뀜. `_lightningHeight`를 스크립트에서 9로 바꿨는데 실제 에셋엔 11이 남아있던 걸 뒤늦게 발견 - 동기화함. **아래 주의사항 참고.**
   - `_lightningGroundEmbed`(0.4) 신설 - pivot 수정 후에도 크랙클 아트 자체의 발광 여백(가장자리가 서서히 투명해지는 부분) 때문에 살짝 뜬 느낌이 남아, 강타 y좌표를 지면 아래로 밀어넣어 보정. 여전히 뜬 느낌이면 인스펙터에서 값만 올리면 됨.
   - 파티클: 크기(`LightningParticleSize`=0.08), 보라 계열 4색 랜덤(`LightningParticleColors`, `Gradient` 하드스텝 + `RandomColor` 모드), 원형 렌더링(`BossHazardPool.CircleTexture()` 절차적 생성, `_squareSprite`와 동일 캐싱 패턴).
3. **2페이즈 장풍(Phase2_Wave) - "상승 이동" -> "예고 파티클 + 폭발 강타" 구조로 재설계** (`Phase2State`, `BossHazardPool`, `BossDataSO`, `BossDB.asset`, 커밋 `920d664`)
   - `WaveState`/`FixedTick`(상승 이동)와 `Phase2Frame`/`RingBuffer` 정밀 스냅샷을 전부 제거. 되감기 훅은 낙뢰와 동일하게 "정지+회수 -> 재시작"으로 단순화(정밀 스크럽은 포기 - 지속시간이 짧아 문제 안 됨, 코드에 TODO로 명시). 결정 로그(`_waveXLog`/`_waveCursor`)는 프로젝트 규칙(랜덤 패턴은 결정 로그로 재현)대로 유지.
   - `CoTelegraphAndStrike` 신설: 예고(파티클만, 1초, 판정 없음) -> 강타(폭발 아틀라스 `boss2_fx_explosion-sheet0` 0~8 프레임 순환, 0.3초).
   - 폭발 프레임 앞 5개(0~4)만 피해 판정, 뒤 4개(5~8)는 무판정 종료 연출 - `BossHazardPool.SetColliderActive(int,bool)` 신설(시각은 유지한 채 판정만 토글).
   - `BossHazardPool`의 파티클 부착 조건을 `namePrefix == "LightningBolt"` 하드코딩에서 `bool attachParticle` 매개변수로 일반화(Lightning/Phase2 공용, 향후 다른 패턴도 재사용 가능).
   - 판정 박스 1x2 -> 2.5x2.5로 확대(폭발 비주얼에 맞춤). `scalingMode = Shape` 특성을 이용해 예고 파티클 방출 영역이 이 스케일을 자동으로 따라가게 함(별도 계산 불필요).
   - 폭발 아틀라스도 서브스프라이트 pivot이 (0,0)이라 사용 프레임 9개 모두 (0.5, 0.5)로 수정.
   - 파티클은 낙뢰와 같은 보라 4색 팔레트를 쓰되 독립 필드(`Phase2WaveParticleColors`)로 분리 - 이후 따로 튜닝 가능.

### 해야 할 것 (추가)

- [ ] 낙뢰 `LightningGroundEmbed`(0.4) / 2페이즈 강타 판정 크기(2.5x2.5) / 무판정 경계(앞 5프레임) 등은 이번 세션에서 잡은 초기값 제안임 - 실제 플레이로 확인 후 인스펙터에서 미세조정 필요
- [ ] `Assets/03.Images/Boss/Boss2/_back/` 폴더에 구버전 아틀라스(`zone5_boss2_fxs_ground-sheet0`, `boss2_fx_explosion-sheet0`)가 루트와 중복 존재 - 정리 필요 여부 확인
- [ ] 1페이즈 즉사 기믹 실패 시 "즉사 대신 1페이즈로 회귀" 기획 여전히 미정 (`Phase1State.CoJudgeLaser`에 TODO만 존재, 이번 세션에서도 그대로 유지)

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

- **GameDB(SO) 밸런싱 값은 `.cs`의 `[SerializeField]` 기본값이 아니라 실제 직렬화된 `.asset` 파일 값이 런타임에 쓰인다.** 코드에서 기본값을 바꿔도 이미 저장된 에셋(`BossDB.asset` 등)의 값은 자동으로 갱신되지 않는다 - 인스펙터(또는 `.asset` YAML)에서 직접 바꿔야 실제로 반영됨. 2026-07-16 세션에서 `_lightningHeight`를 스크립트에서만 바꾸고 에셋은 안 바뀌어 있어 혼란을 겪은 실제 사례 있음 - 밸런싱 값 변경 후에는 항상 에셋 쪽 값도 같이 확인할 것.
- **Boss 씬(`Boss.unity`)의 Player 태그가 `Untagged`로 되어 있던 기존 버그를 `Player`로 수정함.** `Constants.Tag.PLAYER` 태그 검색에 의존하는 코드(`MonsterController`, `LpManager` 등)가 이 씬에서 정상 동작하려면 필요한 수정이었음. 다른 씬(맵 3종 제작 시)도 Player 프리팹 배치 시 태그 확인할 것.
- **Boss 씬에서 리와인드(`PlayerRewind.OnRewindStart/OnRewindEnd` → `PlayerAnimator.SetReversed`)가 발동하면 콘솔에 `Parameter 'Hash -1564865577' does not exist` 에러가 반복 출력됨.** 이 씬의 Player Animator Controller에 리와인드 역재생용 파라미터가 없거나 다른 것으로 추정되는 기존 이슈 (발견만 하고 미수정 - Animator Controller 담당자 확인 필요).
- **Unity 에디터가 포커스를 잃은 상태(백그라운드)에서는 MCP로 Play 모드를 구동해도 물리 틱(FixedUpdate)이 실시간으로 진행되지 않는 것으로 관찰됨.** 자동화 테스트에서 `sleep` 후 상태 확인하는 방식이 신뢰할 수 없었음 - 프레임 진행이 필요한 검증은 `IRewindable` 메서드를 리플렉션으로 직접 호출하는 결정론적 방식을 쓰거나, 에디터에 포커스를 준 상태에서 확인할 것.
- **`Boss.unity`는 이번 병합에서 `Assets/00.Scenes/Minsung/Boss.unity`로 이동함** (경로 변경, 내용은 동일 + 이번 병합 반영분).
- **Play 모드 중 `EditorUtility.InstanceIDToObject`로 직접 얻은 오브젝트에 `SetActive` 등을 걸면, Stop 후에도 그 변경이 편집 모드로 새어나오는 사례를 관찰함**(정상 경로인 `GameObject.Find` -> 컴포넌트 참조로 호출한 `SetActive`는 Stop 시 정상적으로 되돌아감). MCP로 Play 모드 중 임시 상태를 만들어 확인할 때는 가능하면 정식 컴포넌트 참조 경로로 호출하고, Stop 후에는 항상 대상 오브젝트의 `activeSelf`를 다시 확인할 것.
