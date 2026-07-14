# Subagent 분업 구조

> 목적: 이 저장소에서 실제로 유효한 서브에이전트 fan-out 패턴과 오케스트레이션 방식·충돌 회피·검수자 결합의 표준형을 정의한다. | 공식 근거: Anthropic 프롬프팅 가이드(병렬 subagent 위임·fresh-context verifier 권고), Claude Code 공식 문서(subagents/skills) | 원본: AI_Workflow_TemplatePack(공용) → 2026-07-11 이 프로젝트용으로 이식

---

## 1. 이 프로젝트에서 fan-out이 유효한/불필요한 이유

공식 프롬프팅 가이드는 독립적 subtask의 병렬 subagent 위임을 권고하며, 검수는 자기비판보다 fresh-context 검증자가 우수하다고 명시한다. 단, **이 프로젝트는 규모가 작다**(`Assets/01.Scripts/` 총 104개 스크립트, `claude/CLAUDE.md` 명시치) — 다른 대규모 프로젝트(수백~수천 개 프리팹/씬)를 전제로 한 fan-out 패턴을 그대로 적용하면 과잉이다.

| 대상군 | 이 프로젝트의 실측 규모 | 판단 |
|---|---|---|
| C# 스크립트 | 104개 (00.Common~11.CameraSystem, Ex/) | 대부분 본 세션에서 직접 순회 가능. 저장소 전역 전수 감사일 때만 폴더 단위 fan-out 고려 |
| GameDB(SO) 에셋 | `Assets/08.Data/{Player,Boss,Time}/*.asset` 소수 | fan-out 불필요, 직접 순회 |
| 씬/프리팹 | `Assets/00.Scenes/`, UI 프리팹 소수 | YAML 편집은 순차 처리가 안전(충돌 위험이 병렬화 이득보다 큼) |
| 로컬라이제이션 | **해당 없음** — 이 프로젝트엔 다국어 시스템 없음(2026-07-11 확인 안 됨) | 패턴 자체가 불필요 |
| 세이브 스키마 | **해당 없음** | 패턴 자체가 불필요 |

**결론**: 이 프로젝트에서 fan-out은 "전 저장소 규모"보다 **"명확히 독립적인 다수 대상"** 상황에 한정해서 쓴다. 사용자가 명시적으로 요청하지 않는 한 서브에이전트를 임의로 스폰하지 않는 것이 기본 방침이다(다른 시스템 프롬프트 규칙과 일치).

핵심 전제: **분할 단위가 파일(또는 git 이력이 독립인 정의 파일)일 때만 fan-out한다.** Unity 에디터의 전역 상태(컴파일, 씬 재임포트)를 건드리는 단계는 항상 부모(오케스트레이터) 단독 수행이다 — 4절 참조.

---

## 2. fan-out 패턴 카탈로그

### 패턴 A — GameDB(SO) 매직넘버 잔존 전수 감사

**적용 상황**: 여러 시스템(Player/Boss/Time)에 걸쳐 "밸런싱 수치가 GameDB가 아니라 코드에 리터럴로 남아있는지" 전수 확인이 필요할 때.

- **분할 축**: `00.Common/Data/*DataSO.cs` 대응 시스템 폴더 단위(예: `01.Player/`, `03.Boss/`, `04.TimeSystem/`).
- **워커 산출 스키마**: `파일:라인 | 발견된 리터럴 | 소속 시스템(GameDB/Constants 어느 쪽이어야 하는지) | 근거`.
- **필수 검수자**: 컨벤션 검수자([VERIFIER_SUBAGENT_SPEC.md](VERIFIER_SUBAGENT_SPEC.md) (b)) — 매직넘버 판정 기준이 겹친다.

### 패턴 B — 보스 페이즈 상태(PhaseNState) 구조 일관성 검사

이 프로젝트 고유 패턴. `Phase1State~Phase4State`는 `BossState` 공통 인터페이스(`Enter/Exit/Tick/FixedTick/CoPhaseEndGimmick/RecordTick/OnRewindStart/ApplyRewindTick/OnRewindEnd`)를 구현하는 4개의 독립 파일이라, "리와인드 훅을 빠짐없이 구현했는지", "풀 방식으로 연출 오브젝트를 관리하는지" 같은 검사를 페이즈별로 병렬 위임할 수 있다.

- **분할 축**: `Phase1State.cs`~`Phase4State.cs` 파일 단위(4워커, 또는 그보다 적게).
- **워커 산출 스키마**: `페이즈 | 훅 구현 여부(O/X) | 결정 로그 사용 여부 | 풀 사용 여부 | 위반 근거`.
- **필수 검수자**: 리와인드 영향 검증자([VERIFIER_SUBAGENT_SPEC.md](VERIFIER_SUBAGENT_SPEC.md) (c)).

### 패턴 C — 씬/UI 에셋 일괄 배선

**대상 사례**: `BossEmotionHUD._icons` 아이콘 등록, `PlayerStatusEffectUI._iconMap` 배선처럼 여러 슬롯에 반복적으로 에셋을 연결하는 작업.

- **선행 제약**: Unity MCP가 연결돼 있지 않으면 씬/프리팹은 **YAML 직접 편집 → 부모가 1회 재확인 요청** 경로가 기본이다.
- **분할 축**: 씬/프리팹 파일 목록의 배타적 분할. 이 프로젝트는 씬 개수 자체가 적어(`Loading/Map1/Map2/Boss` 등) 실제로는 병렬화보다 순차 처리가 대부분 더 빠르다 — fan-out은 씬 하나 안에 슬롯이 매우 많을 때만 고려.
- **직렬화 완료 게이트**: 모든 편집이 끝난 뒤 부모가 사용자에게 에디터 재확인을 1회 요청한다. 개별 워커가 각자 확인을 요청하면 사용자 왕복이 낭비된다.
- **필수 검수자**: 컨벤션 검수자.

### 패턴 D — 코드 리뷰 차원 분할

분할 축이 파일이 아니라 **관점(dimension)** 인 패턴. 동일 diff를 관점별 에이전트에 각각 전달한다.

| 관점 | 판정 기준(이 저장소의 실제 규칙) |
|---|---|
| 컨벤션 | `claude/coding-convention.md` — 중괄호 필수, 전위 연산자, `_camelCase`, `*DataSO`/`So` 어미 |
| 리와인드 영향 | `RewindManager.TickCapacity` 사용 여부, `IRewindable` Register/Unregister 쌍, 결정 로그 |
| 런타임 성능 | 매 프레임 힙 할당 0 목표, Awake/Start 외 GetComponent/Find 금지 |
| 소유권 경계 | `Minsung.*` 밖(팀원 코드) 수정 시 소유자 확인 여부 |

- 중복 발견은 부모가 dedupe하고, 관점 간 상충은 부모가 최소 변경 우선으로 판정한다.
- 이 패턴의 관점은 [검수 Subagent 사양서](VERIFIER_SUBAGENT_SPEC.md)의 검수자 정의와 공유된다.

---

## 3. Agent 툴(단발 위임) vs Workflow(결정적 오케스트레이션) 선택 기준

**Agent 툴 (대화 내 단발 위임)** — Claude Code 세션 안에서 서브에이전트를 띄우고 결과 텍스트를 부모 컨텍스트로 회수한다. `.claude/agents/<이름>.md`(YAML frontmatter)로 정의하며 `tools`/`model` 필드로 도구·모델을 독립 설정할 수 있고, 내장으로 Explore(읽기 전용)·Plan·general-purpose가 제공된다.

**Workflow (오케스트레이션 스크립트)** — 외부 스크립트가 서브에이전트를 spawn하고 산출물을 결정적으로 수집한다. 이 프로젝트 규모에서는 거의 필요하지 않다 — 이 `AI_Workflow_TemplatePack/` 30종 이식처럼 대량의 문서를 순차 재작성하는 작업도 이번 세션에서는 오케스트레이션 스크립트 없이 메인 세션이 직접 처리했다.

| 판단 기준 | Agent 툴 선택 | Workflow 선택 |
|---|---|---|
| 반복 규모 | 워커 2~6개, 1회성 | 워커 10개 이상 또는 정기 반복 |
| 산출 형식 | 자유 서술 보고로 충분 | 스키마 고정 필수 |
| 사람 개입 | 중간 판단 개입 필요 | 완전 자율 실행 가능 |
| 이 프로젝트의 현실 | **대부분 이쪽** — 규모가 작아 단발 위임으로 충분 | 저장소 규모가 훨씬 커지면 재고 |

**중요**: 이 프로젝트는 사용자가 명시적으로 요청하지 않는 한 서브에이전트를 임의로 스폰하지 않는다 — 이 방침은 fan-out 패턴 자체의 유효성과 별개로 항상 우선한다.

---

## 4. 병렬 파일 수정 시 충돌 회피

### 원칙 1 — 쓰기 파일 집합의 배타 분할 (fan-out을 쓴다면 기본값)

부모가 fan-out 전에 워커별 쓰기 대상 파일 목록을 확정하고, 어떤 파일도 두 워커에 중복 배정하지 않는다. 다음은 **공유 정의 파일**이므로 반드시 단일 워커(또는 부모) 전담이다:

- `Assets/01.Scripts/00.Common/Data/GameDB.cs`(전역 접근자)
- `Assets/01.Scripts/04.TimeSystem/RewindManager.cs`
- 씬 파일(`Assets/00.Scenes/*.unity`)

### 원칙 2 — 전역 상태는 부모 단독

에디터 재확인 요청, Play 테스트는 에디터 프로세스 전역 상태이므로 워커 병렬 수행을 금지하고, 모든 워커 완료 후 부모가 1회 요청한다.

### 원칙 3 — worktree 격리가 필요한 경우

현행 하네스는 EnterWorktree/ExitWorktree 툴로 git worktree 격리를 지원한다. 다음 경우에만 사용한다:

1. **같은 파일을 두 전략으로 수정해 비교**할 때.
2. **컴파일 성공이 워커별 완료 조건**인 대규모 코드 수정 — 같은 작업 트리에서는 타 워커의 미완 편집이 컴파일을 오염시킨다.

**주의**: 이 프로젝트는 Unity라 worktree마다 `Library/` 재임포트 비용(수 분~수십 분)이 발생한다. 에디터 검증이 필요 없는 **코드 전용 변경에만** worktree를 권장한다. 팀원(명진/진욱)의 병행 작업과 겹치는 파일은 워커 배정에서 제외하고, 커밋 시 이 세션 작업분만 선별 스테이징한다(절차는 [코드 작업 규칙](CODE_WORKFLOW_RULES.md)).

---

## 5. 검수자 결합 표준형 — 파이프라인 마지막 단계

[검수 Subagent 사양서](VERIFIER_SUBAGENT_SPEC.md)의 3종 검수자(컴파일/컨벤션/리와인드 영향)를 fan-out 파이프라인 종단에 다음 표준형으로 결합한다.

```
[부모] 분할 계획 수립 (파일 집합 배타 분할, 공유 파일 전담 지정)
   ├─ [워커 1..N] 병렬 실행 — 고정 스키마로 결과 반환
[부모] 병합 + 에디터 재확인 요청 1회 (수정 fan-out인 경우)
   → [검수자 ①] 컴파일 검수 — 실패 시 해당 파일 담당 워커만 재기동
   → [검수자 ②] 컨벤션 검수 — coding-convention.md 위반 목록 반환
   → [검수자 ③] 리와인드 영향 검수 — 해당 시
[부모] 최종 보고 — 각 주장에 툴 결과 근거 첨부
```

- **fresh-context 원칙**: 검수자에게는 diff(또는 대상 파일 목록)와 판정 기준 문서만 전달하고, 워커의 작업 대화 이력·자기 평가는 전달하지 않는다.
- 검수 통과 후의 "완료" 선언 요건은 [진행상황 검증 규칙](PROGRESS_CLAIM_POLICY.md), 런타임 검증 절차는 [테스트·검증 표준](TEST_AND_VERIFICATION_STANDARD.md)을 따른다.

---

## 6. 관련 문서

- [검수 Subagent 사양서](VERIFIER_SUBAGENT_SPEC.md) — 3종 검수자 프롬프트 원문
- [진행상황 검증 규칙](PROGRESS_CLAIM_POLICY.md) · [테스트·검증 표준](TEST_AND_VERIFICATION_STANDARD.md) — 검증 3부작의 나머지
- [다단계 작업 SOP](MULTI_STAGE_WORKFLOW_SOP.md) — fan-out을 Phase로 편성할 때의 DoD·문서화 의무
- [Effort 사용 기준표](EFFORT_POLICY.md) · [최상위 모델 전용 작업 목록](FABLE_ONLY_TASKS.md) — 워커 모델·effort 배정 근거
- [자율 실행 종료 방지 문구](AUTONOMOUS_COMPLETION_RULE.md) — 워커 프롬프트에 삽입할 조기 종료 방지 표준 문단
- [체크포인트 규칙](CHECKPOINT_RULES.md) — 장시간 fan-out 중단 시 HANDOVER 작성 기준
