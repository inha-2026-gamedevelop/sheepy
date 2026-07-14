# 프로젝트 지침 통합 재작성

> 목적: `claude/CLAUDE.md` 밖 영속 메모리(`feedback_*.md`)에 흩어질 사용자 지침을 Why/How가 보존된 단일 지침서로 통합한다. | 공식 근거: Anthropic 프롬프팅 가이드, claude.ai Projects 지원 문서 — 그 외 전부 프로젝트 내부 규칙 | 원본: AI_Workflow_TemplatePack(공용) → 2026-07-11 이 프로젝트용으로 재작성

## 0. 이 문서의 위상

지침의 원천은 두 곳이다.

1. **`claude/CLAUDE.md`** — 시스템 프롬프트. 규칙의 "무엇"만 있음.
2. **영속 메모리** `C:\Users\inha\.claude\projects\d--unity2d\memory\`의 `feedback_*.md` — 규칙이 생긴 실제 사고 사례(Why)와 적용법(How)이 여기에만 있음. **2026-07-11 기준 비어 있음** — 이 문서는 이번 세션에서 실제로 발생한 사용자 교정 4건을 우선 반영하고, 향후 메모리가 채워지면 이 문서도 함께 갱신한다.

문제는 메모리가 없는 환경(claude.ai Projects, 신규 머신, 첫 세션)에서는 규칙의 절반(Why/How)이 사라진다는 것. 이 문서는 두 원천을 **규칙 + Why + How** 형태로 통합한 단일 지침서다. 원본 `claude/CLAUDE.md`는 이번 세션에서는 사용자 승인 하에 직접 수정된 사례가 있다(원본 미수정 원칙은 이 프로젝트에서 완화됨 — [MEMORY_RULES.md](MEMORY_RULES.md) 9절).

작성 기준: 각 행동을 명시적으로 나열하는 과잉 지시는 오히려 성능을 저하시킬 수 있다. 따라서 각 규칙은 절차 나열이 아니라 **목표·제약·근거** 수준으로 압축했다.

---

## 1. 행동 경계 — 위반 시 복구 비용이 가장 큰 규칙

### 1-1. 수정 범위는 `Minsung.*`(민성) + 팀원 코드는 소유자 확인 후
- **규칙**: `Minsung.*` 네임스페이스는 민성 코드 전용. 팀원(명진/진욱) 코드는 네임스페이스가 없다 — 수정 전 소유자 확인.
- **Why**: 팀이 담당 영역을 나눠 병행 작업한다. 소유자 모르게 코드가 바뀌면 충돌·혼란이 생긴다.
- **출처**: `claude/CLAUDE.md` Critical Rule 2. 전체 경계 목록은 [작업 경계선](ACTION_BOUNDARIES.md).

### 1-2. 리와인드 버퍼는 `RewindManager.TickCapacity`만
- **규칙**: 버퍼 용량을 직접 계산(`RecordSeconds / fixedDeltaTime`)하지 않는다. 기록 길이(초) 조정은 `TimeDB`(`GameDB.Time.RecordSeconds`)에서만.
- **Why**: 참여자마다 버퍼 크기가 미세하게 어긋나면 되감기 인덱스가 깨진다 — 조용히 발생하는 심각한 버그.
- **출처**: `claude/CLAUDE.md` Critical Rule 3.

### 1-3. 커밋·푸시는 요청 시만, 선별 스테이징
- **규칙**: `git add -A` 지양, 이 세션 작업분만 선별 스테이징. push는 사용자가 명시적으로 요청할 때만.
- **Why**: 사용자가 IDE에서 직접 병행 작업할 수 있다(실제 사례 있음). 남의 작업물을 함께 커밋하면 히스토리가 오염된다.
- **출처**: Git Safety Protocol(공통 규칙) + 이번 세션 실제 지시("커밋만하고 푸쉬하지마"). 절차 상세는 [코드 작업 규칙](CODE_WORKFLOW_RULES.md).

---

## 2. C# 코드 규칙 — 컨벤션·안정성

### 2-1. 모든 제어문에 중괄호 필수
- **규칙**: `if/for/while/foreach`는 본문이 한 줄이어도 반드시 `{ }`(Allman 스타일, 여는 중괄호는 다음 줄).
- **Why/출처**: `claude/coding-convention.md` 4절. 일관성과 향후 라인 추가 시 실수 방지.

### 2-2. 전위 증감 연산자만
- **규칙**: `++i`, `--i`만 사용(`i++` 금지).
- **출처**: `claude/coding-convention.md` 5절.

### 2-3. 매직넘버 금지 — GameDB/Constants 분리
- **규칙**: 밸런싱 수치는 `GameDB`(SO DB, `*DataSO`), 코드 계약값(키/epsilon/구조 상수)은 `Constants.*.cs`. `GameDB.*` 호출은 MonoBehaviour 필드 초기화식/생성자 금지(Awake 이후만).
- **Why**: `GameDB`는 Resources.Load 기반이라 초기화 순서 제약이 있다. 밸런싱 값을 코드에 하드코딩하면 기획자가 인스펙터에서 조정할 수 없다.
- **출처**: `claude/CLAUDE.md` Critical Rule 1, `claude/gamedb.md`.

### 2-4. 네이밍
- **규칙**: 클래스/메서드 PascalCase, 멤버 변수 `_camelCase`, 상수 `UPPER_SNAKE_CASE`, SO 클래스는 `*DataSO`, SO 변수는 `So` 어미(`_playerSo`). `public` 필드 금지 — `[SerializeField] private` + 프로퍼티.
- **출처**: `claude/coding-convention.md` 2절, 14절.

### 2-5. DontDestroyOnLoad 싱글톤은 `PersistentSingleton<T>`
- **규칙**: Awake 직접 구현 금지, `OnSingletonAwake()` 오버라이드.
- **Why**: 씬 재로드 시 중복 인스턴스·초기화 순서 문제를 공통 베이스가 흡수한다.
- **출처**: `claude/CLAUDE.md` Critical Rule 6.

### 2-6. 리와인드 참여 오브젝트 규칙
- **규칙**: `IRewindable` 구현 + `Register/Unregister` 쌍 호출. 랜덤 패턴은 결정 로그로 저장해 재현(`Phase1State.BuildSequence` 패턴 참고). 연출 오브젝트는 생성/파괴 대신 풀 활성/비활성.
- **출처**: `claude/CLAUDE.md` Critical Rule 4.

### 2-7. 런타임 GC 최소화
- **규칙**: `WaitForSeconds`/material/컴포넌트 참조는 캐싱, `TryGetComponent` 사용, 물리 쿼리는 NonAlloc.
- **출처**: `claude/coding-convention.md` 17절.

### 2-8. 단순·최소 변경 구현 우선
- **규칙**: 특수 케이스를 여러 파일에 흩뿌리기보다, 기존 메커니즘 하나로 끝낼 수 없는지 먼저 자문한다.
- **실사례**: 슬로우모션 히트스톱 버그를 "코루틴 정지 후 재시작" 방식 대신 "종료 목표 시각만 미루는" 단일 코루틴으로 수정 — 새 필드 1개, 로직 단순화로 해결했다.

---

## 3. 이 프로젝트에 없는 것 (다른 프로젝트 습관 주의)

과거 다른 프로젝트 경험이 있으면 아래를 무의식적으로 적용하지 않도록 주의한다.

| 흔한 습관 | 이 프로젝트의 실제 상태 |
|---|---|
| 로컬 세이브 시스템(AES 암호화, 버전 마이그레이션) | **없음**. Supabase는 랭킹/고스트 리플레이 백엔드일 뿐 |
| 타입 접두사 컨벤션(`go`/`tr`/`img` 등) | **없음**. `claude/coding-convention.md`에 그런 규칙 없음 |
| `Resources.Load` 직접 호출 금지 | **정반대**. GameDB는 `Resources.Load` 자동 로드가 공식 패턴 |
| "push 커스텀 스킬"(6단계 프로토콜) | **없음**. 수동 git 절차([코드 작업 규칙](CODE_WORKFLOW_RULES.md)) |
| Player 전용 Behavior Tree | **제거됨**. 플레이어 입력은 `PlayerController`가 직접 처리 |

---

## 4. Git·보고 규칙

### 4-1. 커밋 메시지에 AI 공저자 표기 금지
- **규칙**: `Co-Authored-By: Claude ...` 줄을 넣지 않는다.
- **Why**: 이번 세션에 사용자가 커밋 직후 명시적으로 지적, 직전 커밋을 amend로 정정한 전례.
- **출처**: 이번 세션 실제 지시.

### 4-2. 커밋 형식은 `type: Scope 제목`(괄호 없음)
- **규칙**: `type(scope): 제목`이 아니라 `type: Scope 한국어 제목`.
- **출처**: `claude/commit-convention.md`.

### 4-3. 수정 보고는 전 위치를 하이퍼링크로
- **규칙**: 코드를 수정하면 요청받지 않아도 모든 수정 위치를 `[파일:라인](경로#L라인)` 링크로 정리한다.
- **출처**: `claude/CLAUDE.md` Communication Protocol.

### 4-4. HANDOVER는 간결하게 — "이번에 고친 것/해야 할 것" 위주
- **규칙**: `claude/HANDOVER.md`는 누적 아카이브가 아니라 현재 재개 지점이다. 불필요한 이력은 `claude/PLAN.md`로 위임.
- **Why**: 이번 세션에 사용자가 "핸드오버파일 불필요한 내용 전부 제거하고 해야할것들과 이번에 고친것 내용만 포함해"라고 명시적으로 요구.
- **출처**: 이번 세션 실제 지시. 상세는 [체크포인트 규칙](CHECKPOINT_RULES.md).

---

## 5. 테스트 규칙 (요약 — 상세는 [테스트·검증 표준](TEST_AND_VERIFICATION_STANDARD.md))

- Unity MCP 연결 여부로 검증 경로가 갈린다. 미연결이면 코드 리뷰까지만 하고 사용자에게 "어느 씬에서 무엇을 확인해야 하는지" 구체적으로 요청한다.
- 미검증 항목을 검증된 것처럼 서술하지 않는다.

### 규칙 ↔ 원본 대응표

| 절 | 규칙 | 원본 |
|---|---|---|
| 1-1 | 네임스페이스 소유권 | `claude/CLAUDE.md` Critical Rule 2 |
| 1-2 | 리와인드 버퍼 | `claude/CLAUDE.md` Critical Rule 3 |
| 1-3 | 커밋·푸시 범위 | Git Safety Protocol + 이번 세션 지시 |
| 2-1~2-8 | 코딩 컨벤션 | `claude/coding-convention.md` |
| 4-1~4-4 | 커밋·보고·HANDOVER | 이번 세션 사용자 지시(메모리 미저장, 8절 참고) |
| 5 | 테스트 플로우 | [TEST_AND_VERIFICATION_STANDARD.md](TEST_AND_VERIFICATION_STANDARD.md) |

---

## 6. claude.ai Projects "Project Instructions" 붙여넣기 블록

Projects는 프로젝트별 커스텀 지시를 지원한다. 업로드한 프로젝트 지식 문서는 캐싱되며, 대형 문서(`claude/`의 AI 운영 문서 30종 등)는 프로젝트 지식에 업로드하고 아래는 지침 칸에 넣는 이원화를 권장한다.

Git 관련 세부 절차(4절)는 Claude Code 전용이므로 블록에서 제외했다. 아래를 그대로 복사해 붙여넣는다.

```text
[The Last Re:wind — Unity 2D 프로젝트 작업 지침]

역할·언어
- 모든 응답은 한국어. 시니어 Unity 개발자 톤(전문적·간결·기술적으로 정확).
- 환경: Unity 6000.4.7f1(2D URP), C#. 타임리와인드 메커닉 기반 2D 플랫폼 액션 RPG.

행동 경계 (최우선)
- Minsung.* 네임스페이스는 민성 전용. 팀원(명진/진욱) 코드는 네임스페이스 없음 — 수정 전 소유자 확인.
- 리와인드 버퍼 용량은 RewindManager.TickCapacity만 사용 — 직접 계산 금지.
- 리와인드 참여 오브젝트는 IRewindable + Register/Unregister 쌍, 랜덤은 결정 로그, 연출은 풀 활성/비활성.
- 확신 없는 파일 경로·API·수치는 추정으로 단정하지 말고 "미확인"으로 표기하거나 질문한다.

C# 코드 규칙
- 모든 제어문(if/for/while/foreach)은 본문이 한 줄이어도 중괄호 { } 필수(Allman 스타일).
- 증감 연산자는 전위(++i)만.
- 매직넘버 금지: 밸런싱 수치는 GameDB(*DataSO), 코드 계약값은 Constants.*.cs. GameDB 호출은 Awake 이후만.
- 네이밍: PascalCase 클래스/메서드, _camelCase 멤버 변수, *DataSO/So 어미(SO), public 필드 금지(SerializeField+프로퍼티).
- DontDestroyOnLoad 싱글톤은 PersistentSingleton<T> 상속, Awake 직접 구현 금지.
- 이 프로젝트에는 로컬 세이브 시스템이 없다. Resources.Load는 GameDB의 공식 로드 패턴(금지 아님).

보고 규칙
- 코드 수정을 제안/작성하면 대상 위치 전부를 [파일:라인](경로#L라인) 마크다운 링크 형식으로 제시.
- 완료·성공 주장은 근거와 함께. 검증 못 한 항목은 hedging 없이 "미확인"으로 명시.
- 커밋 메시지에 AI 공저자(Co-Authored-By) 표기 넣지 않음. 형식은 "type: Scope 한국어 제목"(괄호 없음).

테스트 규칙
- Unity MCP 연결 여부를 먼저 확인. 미연결 시 코드 리뷰까지만 하고, 어느 씬에서 무엇을 확인해야 하는지 구체적으로 요청.
```

---

## 관련 문서

- [시스템 프롬프트 재작성안](SYSTEM_PROMPT_V2.md) — `claude/CLAUDE.md` 본체의 재작성 감사(이번엔 변경 폭 작음)
- [과잉 지시 삭제 보고](INSTRUCTION_PRUNING_REPORT.md) / [CoT 강요 문구 감사](REASONING_EXTRACTION_AUDIT.md)
- [메모리·교훈 규칙](MEMORY_RULES.md) — feedback 메모리가 늘어날 때 이 문서로 승격하는 절차
- [작업 경계선](ACTION_BOUNDARIES.md) — 1절 경계의 전체 목록판
- [코드 작업 규칙](CODE_WORKFLOW_RULES.md) — 4절 Git 절차의 상세판
- [테스트·검증 표준](TEST_AND_VERIFICATION_STANDARD.md) — 5절 테스트 플로우의 상세판
- [1페이지 운영 매뉴얼](ONE_PAGE_AI_WORKFLOW_MANUAL.md) — 전체 문서 압축본
