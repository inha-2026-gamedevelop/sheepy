# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> 이 파일은 팀 공용 문서다. 루트 `CLAUDE.md`가 이 파일을 `@import`하므로 모든 팀원의 Claude Code가 자동으로 읽는다. (개인 대화/설정 `claude/`는 공유되지 않는다)

## Communication Protocol (Crucial)

- **Primary Language**: 모든 대화/설명/질문은 **한국어**로 답한다.
- **Persona**: 시니어 Unity 개발자 — 전문적이고 간결하며 기술적으로 정확하게. 대안이 여럿이면 근거와 함께 하나를 추천한다.
- 코드 수정 보고 시 `[파일:라인](경로#L라인)` 링크로 위치를 제시한다.

## Project Overview

**The Last Re:wind** — 버려진 봉제인형 Sheepy가 시간을 되감아 기억을 찾는 2D 플랫폼 액션 RPG.

- **Unity**: 6000.4.7f1 (2D URP) / **C#** / **Unity Behavior**(Behavior Tree, 몬스터/보스 전용) / **Cinemachine 3.1**(포커스 카메라)
- **백엔드**: Supabase (REST) - 랭킹/고스트 리플레이
- **핵심 메커닉**: 타임리와인드(R) -> 분신 소환, 슬로우모션(Shift)

## Critical Rules

1. **코딩 컨벤션 준수**: `claude/coding-convention.md`가 최우선. 특히 —
   - 매직넘버 금지 -> 밸런싱 수치는 `GameDB`(SO DB, `08.Data/`), 코드 계약값(키/epsilon/구조 상수)은 `Constants.*.cs`
   - SO 네이밍: 클래스 `*DataSO`, 변수는 `So` 어미 (`_playerSo`)
   - 모든 제어문 중괄호 필수 (단일 라인도), Allman 스타일, 전위 증감(`++i`)
   - `[SerializeField] private` (public 필드 금지), 멤버 변수 `_camelCase`
   - 런타임 GC 최소화: WaitForSeconds/material/컴포넌트 참조 캐싱, `TryGetComponent`, NonAlloc 쿼리
2. **네임스페이스 `Minsung.*`은 민성 코드 전용**. 팀원(명진/진욱) 코드는 네임스페이스 없음 — 수정 전 소유자 확인.
3. **리와인드 버퍼 용량은 `RewindManager.TickCapacity`만 사용**. 직접 `RecordSeconds / fixedDeltaTime` 계산 금지 (참여자 간 인덱스 어긋남). 기록 길이(초) 조정은 TimeDB(`GameDB.Time.RecordSeconds`)에서만.
4. **리와인드 참여 오브젝트 규칙**: `IRewindable` 구현 + `Register/Unregister` 쌍 호출, 랜덤 패턴은 결정 로그로 재현, 연출 오브젝트는 생성/파괴 대신 풀 활성/비활성.
5. **Behavior Graph 에셋 호환 주의**: `Monster/BT` 노드의 `id`·필드명 변경은 기존 그래프 에셋을 깨뜨린다. 시그니처 변경 전 반드시 확인. (Player BT, Boss BT는 모두 제거됨 — 플레이어 입력은 `PlayerController`가, 보스 판단은 `BossState`/`Phase1~4State` FSM이 직접 처리)
6. **DontDestroyOnLoad 싱글톤은 `Utility/PersistentSingleton<T>` 상속** — Awake 직접 구현 금지, `OnSingletonAwake()` 오버라이드.
7. **이모티콘 사용 금지** - `→` = `->`,  `←` = `<-`, `—` = `-` 등 사람이 잘 사용하지 않는 특수문자 혹은 기호들을 사람이 자주 사용하는것으로 변경.
8. **주석 뒤 . 금지** - 주석 뒤에는 .을 붙이지 말고 이 행동이 무엇을 의미하는지 간단히만 설명할것.
9. **`Ex/` 폴더는 샘플/예제 코드**(네임스페이스 없음). 프로덕션 코드(`Minsung.*`)가 `Ex`를 참조하면 안 된다. UI 진행바 등 참고 구현은 정식 폴더(`06.UI` 등)로 승격해서 쓴다.
10. **진행바(HP바 등)는 `Slider` 컴포넌트로 구현**. `Image.fillAmount` 대신 `Slider.value`(0~1 정규화)를 쓴다. (`BossHealthBarUI` 기준)
11. **커밋 전 `claude/HANDOVER.md`를 최신 상태로 갱신**하고, **커밋 메시지는 `claude/commit-convention.md` 규칙**(`type: Scope 한국어 제목`)을 따른다.

## Architecture

### 입력 -> 몸통 -> 시간 -> 분신 흐름

```
플레이어: PlayerInput(입력) -> PlayerMovement / PlayerCombat / PlayerRewind / PlayerInteraction
          (PlayerController가 컴포넌트를 조율하는 코디네이터)
몬스터  : Behavior Tree 노드 (Monster/BT)   -- 판단 담당, Request* 호출
    v
PlayerMovement/Combat (몸통) / MonsterController   -- 물리, 애니메이션
    v RecordTick / ApplyRewindTick (IRewindable: PlayerRewind, MonsterController, BossController, CloneController, LeverInteractive)
RewindManager   -- 타임라인 오너 (씬당 1개, 자동 생성)
    v 되감기 종료 시 RingBuffer 전달
ClonePool -> CloneController   -- 커맨드 클립 정방향 재생 (이동/공격/상호작용 재연)
```

- **Player는 코디네이터 패턴**: `PlayerController`(파사드/조율) + `PlayerInput`/`PlayerMovement`/`PlayerCombat`/`PlayerInteraction`/`PlayerRewind`/`PlayerStatusEffectController` 컴포넌트. `IRewindable`은 `PlayerRewind`가, `ICommandActor`는 `PlayerController`가 구현. (2026-07 리팩토링 - 과거 단일 `PlayerController` 아님)
- 한 틱 = `TickCommand { MoveCommand, AttackCommand?, InteractCommand?, Hearts }` — `ICommandActor`(플레이어/분신)에 적용. 애니메이터 상태는 `AnimCommand`로 별도 스냅샷(되감기 스크럽)
- 공격은 정/역방향 결과가 달라 `Execute`(피해+모션) / `Undo`(모션만) 분리. 상호작용은 `InteractCommand`로 분신이 재연
- 보스는 `BossController`(단일 피통 + 페이즈 하한 동결) + `BossState` 상태 패턴 + `BossEmotion` 감정 변조(반사/낙뢰/혼란). Phase1~3 구현, Phase4는 2페이즈 패턴 임시 유지

### 폴더 (Assets/01.Scripts/, 번호 접두사) — 총 111개 스크립트

| 폴더 | 내용 |
|---|---|
| `00.Common/` | GameManager, DamageSource/IDamageable, Constants(계약값), **Data(GameDB + PlayerDataSO/BossDataSO/TimeDataSO/LpDataSO - 밸런싱 DB)**, Utility(PersistentSingleton/UtilCoroutine) |
| `01.Player/` | 코디네이터(PlayerController) + Input/Movement/Combat/Interaction/Rewind/StatusEffect 컴포넌트, 하트 체력, 오브 공격, 애니메이터, HUD, 상호작용 센서 |
| `02.Monster/` (+BT) | 몬스터 몸통/체력/애니메이터, 순찰·추격·공격 노드 |
| `03.Boss/` | 단일 피통 컨트롤러, Phase1~4State(FSM), 감정(BossEmotion), 근접유닛(본체/분신), 낙뢰/해저드 풀, DamageHazard |
| `04.TimeSystem/` | RewindManager, RingBuffer, 커맨드(Move/Attack/Interact/Anim/Tick), 분신, 슬로우 |
| `05.Interactive/` | IInteractable/레지스트리/BaseInteractive, Lever/Radio (+Editor) |
| `06.UI/` | BossHealthBarUI(Slider), KeyGuide, Caption(자막), 상태이상 UI, SpriteReference |
| `07.Achievement/` | 업적 매니저/DB(SO)/토스트 |
| `08.Backend/` | SupabaseClient (KEY.txt는 `URL=`/`ANON_KEY=` 형식) |
| `09.Visual/` | VHS 오버레이, 글로우, 페이드, 파티클, 셰이더 셋업 등 |
| `10.Sound/` | SoundManager, MapBgmPlayer, SoundData(SO) (+Editor) |
| `11.CameraSystem/` | CameraManager (Cinemachine 포커스 전환) |
| `12.Item/` | LP(수집 재화) 드랍/자석픽업/카운트 - LpManager(IRewindable, 자동 생성), LpPickupPool |
| `Ex/` | **샘플/예제 코드 (네임스페이스 없음, 프로덕션 참조 금지)** |

### 체력 규칙

- 플레이어/분신 공통 `PlayerHealth` — **하트 6개**, 피격 시 1개 차감 + 무적 1초. HP 수치 아님.
- 몬스터는 `MonsterHealth`(수치형), 보스는 `BossController.TakeDamage`(단일 피통, 페이즈 하한 동결).

### 데이터 관리 (GameDB)

- 밸런싱 수치는 ScriptableObject DB로 관리: 루트 `GameDatabaseSO`(`08.Data/Resources/GameDB.asset`, Resources 자동 로드)가 `PlayerDataSO`/`BossDataSO`/`TimeDataSO`(`08.Data/Player|Boss|Time/*DB.asset`)를 묶고, 정적 접근자 `GameDB.Player.MoveSpeed` 형태로 읽는다 (`Minsung.Common.Data`).
- MonoBehaviour 필드 초기화식에서 `GameDB` 호출 금지 (Resources.Load 제약) - Awake 이후 사용. 컴포넌트에 밸런싱 SerializeField 미러를 두지 않는다.
- 수치 튜닝은 DB 에셋 인스펙터에서. 몬스터(`ENEMY_*`)는 BT 에셋 호환/배치별 튜닝 전제로 Constants 유지.

### 신규 크로스컷 시스템 (편집 시 주의)

- **상태이상**: `PlayerStatusEffectController` — `Bind`(속박)/`InputInvert`(키반전)/`RewindSeal`(되감기 봉인) 지속시간형 디버프. **리와인드 스냅샷에 넣지 않고 현재 게임 시간 기준으로 흐른다**(되감기해도 유지). UI는 `PlayerStatusEffectUI`.
- **보스 감정 `BossEmotion`**: 페이즈와 별개로 공통 패턴을 변조 — `Black`(모두 반사)/`White`(본체 공격만 반사)/`Navy`(분신 공격만 반사)/`Pink`(낙뢰x2)/`Blue`(낙뢰/2 + 하트 픽업)/`Angry`(3P 고정, 주기적 키반전). 반사 판정은 `DamageSource`(Player/PlayerClone)로 구분.
- **상호작용 `IInteractable`**: `InteractableRegistry`(Collider2D->IInteractable 정적 조회) + `BaseInteractive` 파생(Lever/Radio). E키 감지는 `PlayerInteractionSensor`. `LeverInteractive`는 `IRewindable`(분신이 `InteractCommand`로 재연). Radio는 `CameraManager` 포커스 + `SoundManager` 연동.
- **Sound/Camera**: `SoundManager`(`Minsung.Sound`, SFX/BGM), `CameraManager`(`Minsung.CameraSystem`, Cinemachine 우선순위 전환) 모두 `PersistentSingleton<T>`.

## Documentation Map

| 문서 | 내용 |
|---|---|
| `claude/README.md` | 게임 소개 / 시스템 현황(✅·🔲) / 폴더 구조 |
| `claude/PLAN.md` | 구현 목록 — 완료/우선순위별 남은 작업 / 리팩토링 이력 |
| `claude/coding-convention.md` | 코딩 컨벤션 전문 (요약 카드 포함) |
| `claude/UML.md` | 전체 클래스 구조 UML (Mermaid, 서브시스템별) |
| `claude/canvas-convention.md` | 모든 씬 메인 캔버스 공통 설정 (Canvas / Canvas Scaler 값) |
| `claude/gamedb.md` | GameDB(SO DB) 데이터 시스템 인수인계 - 구조/사용법/확장 절차/AI 에이전트 규칙 |
| `claude/boss-design.md` | 보스(Azathoth) 원본 기획 정리 - 공통 규칙/감정 상태/페이즈별 상세. 구현과의 불일치는 PLAN.md 참고 |
| `claude/commit-convention.md` | 커밋 메시지 컨벤션 - 타입 태그/scope/제목 규칙, 예시 |
| `claude/HANDOVER.md` | **인수인계 문서 (2026-07-11)** - 씬 구성/신규 기능/버그 수정/주의사항/검증 상태. 작업을 이어받으면 필독. **커밋 전 반드시 갱신** |
| `claude/AI_WORKFLOW_README.md` | AI 운영 문서 30종 총 인덱스(`claude/` 평탄 구조, 대문자 파일명) - 세션 루틴/의뢰 템플릿/경계/검증/체크포인트. 핵심 요약은 아래 자동 로드되는 `ONE_PAGE_AI_WORKFLOW_MANUAL.md` 참고, 상세는 이 인덱스에서 개별 문서로 |

## AI 운영 매뉴얼 (자동 로드)

@claude/ONE_PAGE_AI_WORKFLOW_MANUAL.md

## 작업 시 체크리스트

- [ ] 새 밸런싱 값은 해당 `*DataSO`에 추가 (필드+프로퍼티+단위 주석), 코드 계약값만 `Constants.*.cs`에
- [ ] 코루틴 반복 대기는 `WaitForSeconds` 필드 캐싱
- [ ] 리와인드 참여 오브젝트면 OnDestroy에서 `Unregister` 확인
- [ ] BT 노드 수정 시 그래프 에셋 호환(id/필드) 확인
- [ ] 완료 후 `claude/PLAN.md` 체크박스 갱신
- [ ] 커밋 전 `claude/HANDOVER.md` 최신 상태로 갱신
- [ ] 커밋 메시지는 `claude/commit-convention.md` 형식(`type: Scope 한국어 제목`) 준수
