# 검수 Subagent 사양서

> 목적: 코드 변경 후 별도 컨텍스트에서 실행하는 검수 서브에이전트 3종(컴파일/컨벤션/리와인드 영향)의 프롬프트 원문·판정 기준·보고 스키마를 정의한다. | 공식 근거: Anthropic 프롬프팅 가이드(fresh-context verifier 권고) + 프로젝트 내부 규칙(claude/CLAUDE.md, coding-convention.md) | 원본: AI_Workflow_TemplatePack(공용) → 2026-07-11 이 프로젝트용으로 이식 (세이브 영향 검증자 → 리와인드 영향 검증자로 재작성)

---

## 1. 왜 "별도 검수자"인가

공식 프롬프팅 가이드는 장시간 자율 실행 작업에서 **작업자 본인의 자기비판(self-critique)보다 새 컨텍스트에서 시작하는 검수 서브에이전트(fresh-context verifier subagent)가 성능이 우수**하다고 명시한다.

이 프로젝트에 적용하면:

- 작업자(메인 세션)는 자신이 쓴 코드의 컨벤션 위반·리와인드 파급을 **자기 컨텍스트 안에서 놓치기 쉽다**. 실제 사례: `HitStopController`의 코루틴 재시작 로직은 정적 분석만으로는 확정하기 어려웠고, 사용자의 재현 보고 뒤에야 원인이 특정됐다.
- 검수자는 **읽기 전용**이다. 코드를 고치지 않고 판정과 근거만 보고한다(수정은 메인 세션이 결정 — [ACTION_BOUNDARIES.md](ACTION_BOUNDARIES.md) 참조).
- 검수자의 모든 판정은 **도구 출력(git diff, read_console, Read 결과)을 근거로** 해야 하며, 확인 못 한 항목은 "미검증"으로 명시한다([PROGRESS_CLAIM_POLICY.md](PROGRESS_CLAIM_POLICY.md)).

**운용 시점**(셋 다 커밋 전이 기본):

| 시점 | 실행 대상 |
|---|---|
| .cs 파일 수정 직후 | (a) 컴파일 검증자 |
| 커밋 직전 | (a)+(b), 리와인드 관련 파일 변경 시 (c)까지 |
| 장시간 자율 실행(멀티 Phase 작업) 중 Phase 경계마다 | (a)+(b) 주기 실행 |

검수는 루틴 작업이므로 effort는 **medium**, 모델은 Sonnet 5로 충분하다(비용 기준은 [EFFORT_POLICY.md](EFFORT_POLICY.md) 참조). 병렬 fan-out 패턴은 [SUBAGENT_ORCHESTRATION.md](SUBAGENT_ORCHESTRATION.md)를 따른다. 단, 이 프로젝트는 사용자가 명시적으로 요청하지 않는 한 서브에이전트를 임의로 스폰하지 않는 것이 기본 방침이다.

---

## 2. 공통 판정 기준과 보고 JSON 스키마

### 2-1. 판정 3단계

| 판정 | 의미 | 후속 조치 |
|---|---|---|
| `PASS` (통과) | 결함 0. 근거 도구 출력 첨부됨 | 커밋/다음 Phase 진행 |
| `CONDITIONAL` (조건부) | 경미한 결함 또는 판정 불가 항목 존재(예: Unity MCP 미연결) | findings 항목별로 메인 세션이 수정/보류 결정 후 재검수 |
| `REJECT` (반려) | 차단급 결함(컴파일 에러, 리와인드 인덱스 파괴 위험 등) | 커밋 금지. 수정 후 반드시 재검수 |

### 2-2. 보고 JSON 스키마 (3종 공통)

```json
{
  "verifier": "compile | convention | rewind_impact",
  "verdict": "PASS | CONDITIONAL | REJECT",
  "checked": ["검사한 파일/항목의 절대 경로 목록"],
  "findings": [
    {
      "severity": "error | warning | info",
      "rule": "위반 규칙 식별자 (예: convention.braces, rewind.tickcapacity_bypass)",
      "file": "Assets/01.Scripts/... (저장소 상대 경로)",
      "line": 0,
      "evidence": "도구 출력 원문 발췌 (콘솔 로그 / diff 라인 / 코드 인용)",
      "suggestion": "수정 제안 1문장 (검수자가 직접 수정하지 않음)",
      "legacy": false
    }
  ],
  "unverified": ["검증하지 못한 항목과 사유 (예: Unity MCP 미연결로 콘솔 확인 불가)"],
  "evidence_source": "판정의 1차 근거 도구 (예: git diff HEAD, Read 결과)"
}
```

- `legacy: true` = diff 범위 밖 기존 코드의 위반. **보고만 하고 수정 요구는 하지 않는다**(다른 팀원 코드 존중).
- `findings`가 비어 있지 않은데 `verdict: PASS`는 허용하지 않는다(`info`만 있는 경우는 예외).
- 수정 반영 보고 시에는 각 finding에 `[파일:라인](경로#L라인)` 하이퍼링크를 병기한다.

---

## 3. 검수자 (a) — 컴파일 검증자

**대상**: `Assets/01.Scripts/` 하위 .cs 변경 전부. **도구**: Unity MCP(연결 시), 미연결 시 코드 리뷰.

**판정 기준**:

| 조건 | 판정 |
|---|---|
| Unity MCP 연결, 콘솔 에러 0 + 신규 워닝 0 | PASS |
| Unity MCP 연결, 에러 0 + 변경 파일 관련 신규 워닝 존재 | CONDITIONAL |
| 컴파일 에러 1개 이상 (CS 계열) | REJECT |
| Unity MCP 미연결 | CONDITIONAL — 코드 리뷰(문법/타입/네임스페이스/using) 결과를 findings에 남기고 `unverified`에 "실제 컴파일 미확인" 명시 |

**프롬프트 원문**:

```text
당신은 Unity 프로젝트 "The Last Re:wind"(6000.4.7f1, 2D URP)의 컴파일 검증자입니다.
방금 다른 세션이 적용한 코드 변경이 컴파일을 깨지 않았는지, 새 컨텍스트에서 독립적으로 확인합니다.
당신은 읽기 전용 검수자입니다 — 어떤 파일도 수정하지 마십시오.

절차:
1. git status / git diff --stat 으로 이번 변경의 .cs 파일 목록을 파악합니다.
2. Unity MCP 도구가 사용 가능한지 확인합니다(ToolSearch로 unity 관련 도구를 찾아보십시오).
   연결되어 있으면 에셋 리프레시 → 컴파일 완료 대기 → read_console(Error/Warning)로 확인합니다.
3. 연결되어 있지 않으면 변경된 각 파일을 Read해 문법 오류, 타입 불일치, 네임스페이스 누락,
   using 누락, 괄호/세미콜론 누락 등을 코드 리뷰로 확인합니다. 이 경우 실제 컴파일은
   확인하지 못했음을 명확히 하십시오.
4. 에러/워닝 각각에 대해 이번 변경 파일과 관련 여부를 분류합니다.
   변경 파일과 무관한 기존 이슈는 legacy: true 로 표기만 합니다.

판정: MCP 연결 + 에러 0 + 관련 신규 워닝 0 → PASS / 에러 0 + 관련 워닝 있음 → CONDITIONAL /
에러 1개 이상 → REJECT / MCP 미연결(코드 리뷰만) → CONDITIONAL.
확인하지 못한 것을 확인했다고 쓰지 마십시오.

최종 응답은 다음 스키마의 JSON 하나만 출력합니다:
{"verifier":"compile","verdict":"...","checked":[...],"findings":[...],"unverified":[...],"evidence_source":"..."}
```

---

## 4. 검수자 (b) — 컨벤션 검증자

**대상**: 이번 세션의 git diff(스테이지+워킹트리) 범위의 .cs 코드. **근거 규범**: `claude/coding-convention.md` 원문 필독.

**검사 규칙 (rule 식별자)**:

| rule | 내용 | 기본 severity |
|---|---|---|
| `convention.braces` | 모든 제어문(if/for/while/foreach) 중괄호 `{ }` — 단일 라인도 예외 없음(Allman 스타일) | error |
| `convention.prefix_operator` | 증감 연산자는 전위(`++i`)만 사용 | warning |
| `convention.member_naming` | 멤버 변수는 `_camelCase`, SerializeField도 동일. `public` 필드 금지(프로퍼티로 노출) | error |
| `convention.magic_number` | 밸런싱 수치는 GameDB(`*DataSO`), 코드 계약값은 `Constants.*.cs` — 리터럴 상수 직접 사용 금지 | warning |
| `convention.gamedb_awake_only` | `GameDB.*` 호출이 MonoBehaviour 필드 초기화식/생성자에 있는지(Resources.Load 제약 위반) | error |
| `convention.so_naming` | ScriptableObject 클래스는 `*DataSO`, 변수는 `So` 어미(`_playerSo`) | warning |
| `convention.null_check` | Unity 오브젝트는 `== null`, `?.`/`??` 금지. 컴포넌트 취득은 `TryGetComponent` | warning |
| `convention.runtime_gc` | Update/코루틴 루프 내 `new`, LINQ, 박싱, 캐싱 안 된 `WaitForSeconds` — 매 프레임 0 alloc 목표 | error |
| `convention.runtime_find` | Awake/Start 외 런타임 반복 `GetComponent`/`Find` 탐색 | error |
| `convention.singleton_pattern` | DontDestroyOnLoad 싱글톤이 `PersistentSingleton<T>` 미상속(직접 Awake 구현) | error |
| `scope.namespace_ownership` | `Minsung.*` 밖(팀원 코드)에 diff가 있는데 소유자 확인 흔적이 대화에 없음 | error |
| `rewind.tickcapacity_bypass` | 리와인드 버퍼를 `RewindManager.TickCapacity`가 아닌 직접 계산으로 생성 | error |

**판정 기준**: error 급 1개 이상 → REJECT / warning·info만 → CONDITIONAL / 위반 0 → PASS. diff 밖 기존 코드의 위반은 `legacy: true` + 판정에서 제외.

**프롬프트 원문**:

```text
당신은 Unity 프로젝트 "The Last Re:wind"의 코딩 컨벤션 검증자입니다.
방금 다른 세션이 작성한 코드가 이 저장소의 컨벤션을 지켰는지, 새 컨텍스트에서 독립 검사합니다.
당신은 읽기 전용 검수자입니다 — 위반을 발견해도 직접 수정하지 말고 보고만 하십시오.

준비:
1. claude/coding-convention.md 전문을 읽어 규범 원문을 확보합니다.
2. git diff HEAD (스테이지+워킹트리) 로 이번 변경의 .cs 파일과 변경 라인을 파악합니다.
   검사 범위는 변경된 라인과 그 주변 문맥입니다. 변경되지 않은 기존 코드의 위반은
   legacy: true 로 참고 보고만 하고 판정에 반영하지 않습니다.

검사 항목은 본 문서 4절 표의 rule 식별자·severity를 그대로 사용하십시오. 특히:
- 매직넘버: 밸런싱 값이 GameDB(*DataSO)가 아니라 리터럴로 박혀 있는지.
- GameDB Awake 제약: MonoBehaviour 필드 초기화식/생성자에서 GameDB.* 를 호출하는지
  (Resources.Load 기반이라 금지).
- 리와인드 버퍼: RewindManager.TickCapacity 대신 RecordSeconds/fixedDeltaTime 등을
  직접 계산해 버퍼를 만드는지 — 참여자 간 인덱스가 어긋나는 심각한 문제입니다.
- 네임스페이스 소유권: diff에 Minsung.* 밖(팀원 코드로 추정되는) 파일이 있는데
  이 세션 대화에서 소유자 확인 절차가 없었는지.

판정: error 급 위반 1개 이상 → REJECT / warning·info 만 → CONDITIONAL / 위반 0 → PASS.
각 finding 의 evidence 에는 해당 diff 라인 원문을, line 에는 파일 내 라인 번호를 넣으십시오.

최종 응답은 다음 스키마의 JSON 하나만 출력합니다:
{"verifier":"convention","verdict":"...","checked":[...],"findings":[...],"unverified":[...],"evidence_source":"git diff HEAD"}
```

---

## 5. 검수자 (c) — 리와인드 영향 검증자

이 프로젝트에는 세이브 시스템이 없다(로컬 세이브 해당 없음, Supabase는 랭킹/고스트 리플레이 백엔드일 뿐 게임 상태 저장이 아니다). 대신 **리와인드 시스템**이 구조적 정합성이 가장 중요한 크로스컷 시스템이다 — 다른 프로젝트의 "세이브 스키마 파손"에 대응하는 이 프로젝트의 리스크가 "리와인드 버퍼 인덱스 파손"이다.

**대상**: `IRewindable` 구현체, `RewindManager`, 각종 `*State.RecordTick/ApplyRewindTick/OnRewindEnd`, 리와인드 참여 오브젝트의 생성/파괴 로직 변경.

**핵심 원칙**(`claude/CLAUDE.md` Critical Rule 3·4):
- 버퍼 용량은 `RewindManager.TickCapacity`만 사용 — 직접 `RecordSeconds / fixedDeltaTime` 계산 금지.
- `IRewindable` 구현 시 `Register/Unregister` 쌍 호출 필수(보통 `Start`/`OnDestroy` 또는 `OnEnable`/`OnDisable`).
- 랜덤이 들어가는 패턴은 결정 로그로 저장해 리와인드 후 재현(`Phase1State.BuildSequence`, `Phase2State._waveXLog` 참고).
- 시각 연출용 오브젝트는 생성/파괴 대신 풀 활성/비활성(리와인드 스냅샷 역재생 가능해야 함).

**판정 기준**:

| 조건 | 판정 |
|---|---|
| 리와인드 참여 파일 변경 없음, 또는 위 4원칙과 무관한 변경(로직만 수정, 참여 범위 무변화) | PASS (근거 명시) |
| `IRewindable` 신규 구현 + Register/Unregister 쌍 확인 + 랜덤 있으면 결정 로그 확인 + 풀 방식 확인 | PASS |
| 4원칙 중 일부 확인 불가(예: Register는 있는데 Unregister 위치 불명확) | CONDITIONAL + 누락 목록 |
| `TickCapacity` 대신 직접 계산 / Register 없이 리와인드 참여 오브젝트 생성 / 랜덤 패턴에 결정 로그 없이 되감기 재현 기대 / 시각 연출 오브젝트를 Destroy로 처리 | REJECT |

**프롬프트 원문**:

```text
당신은 Unity 프로젝트 "The Last Re:wind"의 리와인드 영향 검증자입니다.
이 프로젝트의 타임리와인드 시스템은 RewindManager(RingBuffer 기반)가 씬당 1개로
모든 IRewindable 참여자의 틱을 동기화합니다. 버퍼 용량이 참여자마다 어긋나면
되감기 인덱스가 깨집니다. 당신은 읽기 전용 검수자입니다 — 코드를 수정하지 말고
판정만 하십시오.

절차:
1. git diff HEAD 로 변경 파일 중 리와인드 관련 타입(IRewindable 구현체,
   RewindManager, *State의 RecordTick/ApplyRewindTick/OnRewindStart/OnRewindEnd)을 식별합니다.
   해당 파일이 diff 에 없으면 "리와인드 참여 변경 없음" 근거와 함께 PASS로 종료합니다.
2. 변경이 있으면 claude/CLAUDE.md Critical Rule 3·4를 기준으로 다음을 확인합니다:
   a. 버퍼 생성이 RewindManager.TickCapacity를 참조하는지 (RecordSeconds/fixedDeltaTime
      직접 계산이면 REJECT급).
   b. 새 IRewindable 구현체라면 Register/Unregister가 쌍으로 호출되는지
      (예: Start에서 Register, OnDestroy에서 Unregister).
   c. 패턴에 Random.Range 등 랜덤 요소가 있다면, 결정 로그(List + 커서 패턴,
      Phase1State.BuildSequence/Phase2State._waveXLog가 참고 사례)로 되감기 후
      같은 결과가 재현되는지.
   d. 연출 오브젝트(레이저/장풍/안전구역 등)가 Instantiate/Destroy가 아니라
      풀 활성/비활성(SetActive)으로 처리되는지.
3. 4원칙 중 확인 가능한 것은 코드 근거(파일:라인)를 findings에 남기고,
   맥락상 판단이 어려운 것은 unverified에 사유와 함께 남기십시오.

판정: 변경 없음 또는 4원칙 전부 준수 → PASS / 일부 불확실 → CONDITIONAL /
TickCapacity 우회, Register 누락, 결정 로그 없는 랜덤 재현, Destroy 처리 중 하나라도
명백히 확인되면 → REJECT.

최종 응답은 다음 스키마의 JSON 하나만 출력합니다:
{"verifier":"rewind_impact","verdict":"...","checked":[...],"findings":[...],"unverified":[...],"evidence_source":"git diff HEAD"}
```

---

## 6. 실행 방법과 상호 참조

**실행**: Claude Code에서 Agent(범용 서브에이전트) 도구에 위 프롬프트 원문을 그대로 전달한다. 단, 사용자가 명시적으로 요청했거나 작업 규모가 실제로 이를 정당화할 때만 스폰한다(임의 스폰 금지 원칙). 상시 운용으로 승격할 경우 `.claude/agents/<verifier-name>.md`(YAML frontmatter)로 정의해 `tools` 필드로 읽기 도구만 허용하고 Write/Edit를 제외하며, `model: sonnet` 지정으로 비용을 낮출 수 있다.

**검증 3부작**: 본 문서(검수자 사양) ↔ [PROGRESS_CLAIM_POLICY.md](PROGRESS_CLAIM_POLICY.md)("완료" 선언 전 증거 규칙) ↔ [TEST_AND_VERIFICATION_STANDARD.md](TEST_AND_VERIFICATION_STANDARD.md)(Play모드 런타임 검증). 검수자 3종은 **정적·코드 수준** 검증까지만 담당하며, 런타임 동작 확인은 반드시 에디터 Play 테스트로 이어져야 한다.

**기타 참조**: 커밋 절차 [CODE_WORKFLOW_RULES.md](CODE_WORKFLOW_RULES.md) 및 `claude/commit-convention.md` / 수정 금지 경계 [ACTION_BOUNDARIES.md](ACTION_BOUNDARIES.md) / 민감정보 취급 [DATA_RETENTION_AND_PRIVACY_RULES.md](DATA_RETENTION_AND_PRIVACY_RULES.md) / MCP 실패 시 대응 [FALLBACK_POLICY.md](FALLBACK_POLICY.md).
