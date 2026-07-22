# 핵심 파일 인덱스

> 목적: 시스템별 진입점 파일의 정확한 경로·역할·네임스페이스를 실존 검증(Glob 기반, 2026-07-22)으로 제공해, 새 세션이 탐색 없이 코드에 착지하게 한다. | 이 문서는 코드 변경(특히 폴더/클래스 재구성)이 있으면 재생성 대상이다.

## 이 문서 사용법

- **"어느 파일부터 여는가"** 질문에 답하는 문서다. "어느 문서를 읽는가"는 [PROJECT_KNOWLEDGE_INDEX.md](PROJECT_KNOWLEDGE_INDEX.md), "질문 유형→탐색 경로" 매핑은 [RAG_KNOWLEDGE_MAP.md](RAG_KNOWLEDGE_MAP.md) 담당.
- 모든 경로는 저장소 루트 기준. 전부 Glob으로 실존 확인함(2026-07-22, `Assets/01.Scripts/` 총 180개 스크립트 중 대표 진입점만 발췌).

## 1. 리와인드/시간 시스템 (`04.TimeSystem/`)

| 파일 | 핵심 타입 | 네임스페이스 | 역할 |
|---|---|---|---|
| `Assets/01.Scripts/04.TimeSystem/RewindManager.cs` | `RewindManager`(씬당 1개, 자동 생성) | `Minsung.TimeSystem` | 타임라인 오너. `TickCapacity`가 버퍼 용량의 유일한 기준(직접 계산 금지) |
| `Assets/01.Scripts/04.TimeSystem/IRewindable.cs` | `IRewindable` 인터페이스 | `Minsung.TimeSystem` | 리와인드 참여 계약 — `RecordTick/OnRewindStart/ApplyRewindTick/OnRewindEnd` |
| `Assets/01.Scripts/04.TimeSystem/RingBuffer.cs` | `RingBuffer<T>` | `Minsung.TimeSystem` | 참여자별 틱 기록 버퍼 |
| `Assets/01.Scripts/04.TimeSystem/TickCommand.cs`, `MoveCommand.cs`, `AttackCommand.cs`, `InteractCommand.cs`, `AnimCommand.cs` | 커맨드 구조체 | `Minsung.TimeSystem` | 한 틱의 입력/공격/상호작용/애니메이션 스냅샷 |
| `Assets/01.Scripts/04.TimeSystem/ICommandActor.cs` | `ICommandActor` | `Minsung.TimeSystem` | 플레이어/분신 공용 커맨드 적용 계약 |
| `Assets/01.Scripts/04.TimeSystem/ClonePool.cs`, `CloneController.cs` | 분신 풀/컨트롤러 | `Minsung.TimeSystem` | 되감기 종료 시 커맨드 클립 정방향 재생 |
| `Assets/01.Scripts/04.TimeSystem/SlowMotionController.cs` | `SlowMotionController` | `Minsung.TimeSystem` | Shift 슬로우모션. `IsSlow`/`TargetTimeScale` 정적 참조 제공 |
| `Assets/01.Scripts/04.TimeSystem/HitStopController.cs` | `HitStopController`(자동 생성 싱글톤) | `Minsung.TimeSystem` | 타격 히트스톱. `Time.timeScale` 쓰기를 SlowMotionController와 협조 |

## 2. GameDB(SO) 데이터 (`00.Common/Data/`)

| 파일 | 핵심 타입 | 네임스페이스 | 역할 |
|---|---|---|---|
| `Assets/01.Scripts/00.Common/Data/GameDB.cs` | `GameDB` 정적 접근자 | `Minsung.Common.Data` | `GameDB.Player/Boss/Time` 진입점. Resources 자동 로드(Awake 이후에만 사용) |
| `Assets/01.Scripts/00.Common/Data/GameDatabaseSO.cs` | `GameDatabaseSO`(루트 DB) | `Minsung.Common.Data` | 하위 `*DataSO` 3종을 묶는 루트. 에셋: `08.Data/Resources/GameDB.asset` |
| `Assets/01.Scripts/00.Common/Data/PlayerDataSO.cs` / `BossDataSO.cs` / `TimeDataSO.cs` / `PotionDataSO.cs` | 도메인별 `*DataSO`(GameDatabaseSO 트리 4종) | `Minsung.Common.Data` | 밸런싱 값. 신규 필드는 여기 + 대응 `.asset`에 값 기입 |
| `Assets/01.Scripts/00.Common/Data/Boss2DataSO.cs` | `Boss2DataSO`(GameDB 트리 밖 독립 DB) | `Minsung.Common.Data` | Boss2(Map3, 3~4P) 전용 밸런싱. GameDatabaseSO에 연결하지 않고 컴포넌트에 직접 드래그 |
| `Assets/01.Scripts/00.Common/Constants/Constants*.cs` | `Constants`(partial class) | `Minsung.Common` | 코드 계약값(입력 키, epsilon, 태그/씬 이름 등) — GameDB와 역할 분리 |

## 3. 씬 관리 / 진입 플로우

| 파일 | 핵심 타입 | 네임스페이스 | 역할 |
|---|---|---|---|
| `Assets/01.Scripts/00.Common/GameManager.cs` | `GameManager`(PersistentSingleton) | `Minsung.Common` | 씬 전환(`LoadScene`), 체크포인트 복귀, 클리어 타임 측정 |
| `Assets/01.Scripts/09.Visual/ScreenFade.cs` | `ScreenFade`(PersistentSingleton) | `Minsung.Visual` | 화면 페이드. `FadeOutIn`이 씬 전환의 표준 경로 |
| `Assets/01.Scripts/03.Boss/BossHandoff.cs` | `BossHandoff`(정적 캐리어) | `Minsung.Boss` | 씬 전환(2->3페이즈) 간 보스 상태(피통/페이즈/타이머/감정) 이관 |

## 4. 핵심 게임플레이 모듈

### Player (`01.Player/`) — 코디네이터 패턴

| 파일 | 핵심 타입 | 역할 |
|---|---|---|
| `PlayerController.cs` | 파사드/조율, `ICommandActor` 구현 | 컴포넌트 조율 |
| `PlayerInput.cs` / `PlayerMovement.cs` / `PlayerCombat.cs` / `PlayerInteraction.cs` / `PlayerRewind.cs`(`IRewindable` 구현) / `PlayerStatusEffectController.cs` | 각 책임 컴포넌트 | 입력/이동/전투/상호작용/리와인드/상태이상 |
| `PlayerHealth.cs` | 하트 6개 체력 | 플레이어/분신 공통 |

### Boss1 (`03.Boss/`, 민성, Map2 1~2P) — 씬 담당 구간 피통 + 페이즈 상태 패턴

| 파일 | 핵심 타입 | 역할 |
|---|---|---|
| `BossController.cs` | `BossController`(`IRewindable`, `IDamageable`) | 이 씬 담당 구간 피통(`GameDB.Boss.TotalHealth`, `_finalPhaseIndex+1`로 분할), 페이즈 전환, 감정 자동 전환 구동부 |
| `Phase1State.cs`~`Phase4State.cs` | `BossState` 파생 | 페이즈별 패턴·기믹. `Phase2State`가 본체+장풍, `Phase3/4State`는 `Phase2State` 상속 |
| `BossEmotion.cs` / `BossEmotionController.cs` | enum + 감정 컨트롤러 | 감정 6종의 반사/낙뢰배율 판정, 결정 로그 |
| `BossLightningPattern.cs` | `IBossPattern` 구현 | 전 페이즈 공통 낙뢰 |
| `BossHazardPool.cs` | 해저드 풀 | 레이저/안전구역/장풍 등 공유 판정·연출 오브젝트 |
| `BossCloneController.cs` / `BossBodyController.cs` / `BossMeleeUnitBase.cs` | 근접 유닛 | 1페이즈 분신 / 2페이즈~ 본체 |
| `BossHandoff.cs` | 정적 캐리어 | Map2->Map3 씬 전환 간 보스 상태 이관 |

### Boss2 (`03.Boss/Boss2/`, 진욱, Map3 3~4P) — 독립 피통 부유체 (네임스페이스 없음, `Minsung.Boss` 공용 인프라만 소비)

| 파일 | 핵심 타입 | 역할 |
|---|---|---|
| `Boss2Health.cs` | `Boss2Health`(`IDamageable`, `IRewindable`) | 독립 피통 + 페이즈(3->4) 전환 + 감정 반사 위임 + 공간찢기 동결 |
| `BossFloatMovement.cs` | 부유 이동 | 배회/추적/돌진(스크립트 돌진 포함), Kinematic Rigidbody2D 직접 제어 |
| `Boss2AttackPatterns.cs` | 원거리 패턴 코디네이터 | `Boss2LightningPattern`/`Boss2WavePattern`/`Boss2LaserPattern` 생성·구동 |
| `Boss2GrabPattern.cs` / `Boss2SpaceTearPattern.cs` | 4P 전용 패턴 | 손아귀(진욱) / 공간찢기(민성, `Boss2DodgeableKillHazard` 회피 즉사) |
| `Boss2BrandController.cs` / `Boss2AltarSpawner.cs` / `Boss2AltarInteractive.cs` | 3P 낙인·제단 | 낙인 스택(7=즉사) + 정화 제단(E키 홀드) |
| `Boss2Emotion.cs` / `Boss2EmotionController.cs` | 감정 이식 | 명명 충돌 회피로 `Boss2` 접두사(boss.md 5-1절) |

> Boss2 상세 구조·씬 배선·코드 흐름·알려진 이슈는 `claude/boss.md` 5~14장 참고.

### Monster (`02.Monster/`)

| 파일 | 역할 |
|---|---|
| `MonsterController.cs` / `MonsterHealth.cs` / `MonsterAnimator.cs` | 몸통/체력/애니메이터 |
| `MonsterState.cs` | Patrol/Chase/Attack C# FSM 상태와 전이 판단 |

### Interactive (`05.Interactive/`)

| 파일 | 핵심 타입 | 역할 |
|---|---|---|
| `IInteractable.cs` / `InteractableRegistry.cs` | 인터페이스 + 정적 조회 레지스트리 | Collider2D -> IInteractable |
| `BaseInteractive.cs` | 공통 베이스 | `LeverInteractive`(`IRewindable`)/`RadioInteractive`가 파생 |

## 5. 입력 / UI 공통 / 사운드 / 카메라 / 업적 / 백엔드

| 파일 | 핵심 타입 | 네임스페이스 | 역할 |
|---|---|---|---|
| `Assets/01.Scripts/06.UI/` 하위 | `BossHealthBarUI`(Slider), `PlayerStatusEffectUI` 등 | `Minsung.UI` 계열 | HUD |
| `Assets/01.Scripts/10.Sound/SoundManager.cs` | `SoundManager`(PersistentSingleton) | `Minsung.Sound` | SFX/BGM |
| `Assets/01.Scripts/11.CameraSystem/CameraManager.cs` | `CameraManager`(PersistentSingleton) | `Minsung.CameraSystem` | Cinemachine 우선순위 전환, 포커스/줌 |
| `Assets/01.Scripts/07.Achievement/AchievementManager.cs` | `AchievementManager` | `Minsung.Achievement` | 업적 언락/토스트 |
| `Assets/01.Scripts/08.Backend/SupabaseClient.cs` | `SupabaseClient` | `Minsung.Backend` | Supabase REST(랭킹/고스트). 키는 `Assets/StreamingAssets/KEY.txt`(gitignore) |
| `Assets/01.Scripts/00.Common/Utility/PersistentSingleton.cs` | `PersistentSingleton<T>` | `Minsung.Utility` | DontDestroyOnLoad 싱글톤 공통 베이스 |

## 6. 경로 미확인·불일치 항목

| 항목 | 문서상 표기 | 실측 결과 | 조치 |
|---|---|---|---|
| `claude/coding-convention.md` | 과거 `claude/codingconvention.md`(하이픈 없음)로 여러 문서에서 참조됨 | 실제 파일명은 `coding-convention.md`(하이픈 있음) | 2026-07-11에 전 참조 수정 완료(커밋 `ab9a479`/`5c1b7ce` 등) — 재발 방지로 이 표에 남김 |
| 3~4페이즈 전용 맵 씬 | 과거 미제작(임시 자기 재로드) | `Assets/00.Scenes/05.Map3.unity` 제작됨 - Boss1(Map2) 2P 종료 시 아웃트로 후 Map3로 전환, Boss2 시스템이 3~4P 담당 | 해결됨. 잔여 미완 작업은 영속 메모리(`MEMORY.md`) 참고 |

## 관련 문서

[PROJECT_KNOWLEDGE_INDEX.md](PROJECT_KNOWLEDGE_INDEX.md) · [RAG_KNOWLEDGE_MAP.md](RAG_KNOWLEDGE_MAP.md) · [ACTION_BOUNDARIES.md](ACTION_BOUNDARIES.md)

## 재생성 방법 (필요 시)

```
MASTER_FILE_INDEX.md를 이 저장소 기준으로 다시 생성해줘.
- Assets/01.Scripts/ 하위 폴더(00.Common~12.Item, 03.Boss/Boss2 포함)별로 진입점 파일을 Glob으로 실존 확인해
  표(파일|핵심 타입|네임스페이스|역할)로 정리.
- 실존 확인 못 한 항목은 "경로 미확인·불일치 항목" 절로 분리.
- claude/CLAUDE.md의 폴더 설명과 대조해 불일치가 있으면 그것도 이 절에 기재.
```
