# Effort 사용 기준표

> 목적: The Last Re:wind 저장소의 실제 작업 유형별로 권장 모델×effort 조합과 조정 신호를 정의한다. | 공식 근거: Anthropic 프롬프팅 가이드(platform.claude.com) + 모델 발표 문서, 그 외는 프로젝트 내부 규칙 | 원본: AI_Workflow_TemplatePack(공용) → 2026-07-11 이 프로젝트용으로 이식

---

## 1. 공식 사실 (이 수치만 단정 인용 가능)

- **effort 단계**: `low / medium / high / xhigh / max`. API 기본값은 **high**, **xhigh는 코딩·에이전트 작업 권장값이며 Claude Code의 기본값**이다.
- effort는 intelligence ↔ latency ↔ cost 트레이드오프를 조절하는 **유일한 primary control**이다. 공식 권고는 "루틴 작업 medium, 고난도 작업 xhigh"이며, **낮은 effort도 이전 세대 모델의 xhigh 능력을 초과**한다.
- `max` 단계는 존재만 공식 확인됨 — 어떤 작업에 쓰라는 공식 기준은 **공식 수치 미확인**. 본 문서에서는 "xhigh로 2회 실패한 작업의 재시도"에만 한정 사용을 내부 규칙으로 정한다.
- 높은 effort는 **턴이 분~시간 단위로 길어지는 것이 기본값**이다. Unity MCP 폴링(연결 시 `read_console` 등)을 끼운 장시간 작업일수록 이 지연을 감안해 effort를 정한다.
- 모델 가격은 시점에 따라 변하므로 이 문서에 하드코딩하지 않는다 — 필요 시 공식 가격 페이지에서 조회.

모델 선택 자체(최상위 모델 전용 vs 하위 모델 충분)의 상세 분류는 [최상위 모델 전용 작업 목록](FABLE_ONLY_TASKS.md), 컨텍스트 예산은 [컨텍스트 한도 기준](CONTEXT_LIMITS_POLICY.md)을 따른다 (모델·리소스 3부작).

## 2. 작업 유형별 권장 모델×effort 표

이 저장소에서 실제 반복되는 작업으로 행을 구성했다.

| 작업 유형 | 이 저장소의 실제 사례 | 권장 모델 | effort | 비고 |
|---|---|---|---|---|
| 오타·상수·문구 수정 | `Constants.combat.cs`의 상수 값 조정, 커밋 메시지 오탈자 | Haiku 4.5 / Sonnet 5 | low~medium | 단일 파일·기계적 수정 |
| 컨벤션 준수 리팩터링 | 중괄호 보강, 전위연산자 치환, `_camelCase` 정정 — `claude/coding-convention.md` 기준 | Sonnet 5 | medium | 규칙이 성문화되어 판단 여지가 적음 |
| GameDB(SO) 값/필드 추가 | `BossDataSO`에 필드+프로퍼티 추가, `BossDB.asset` YAML에 기본값 기입 | Sonnet 5 | medium | Awake 이후 사용 제약(필드 초기화식 호출 금지)만 주의 |
| 씬 YAML 직접 편집 | `Boss.unity`의 오브젝트 위치/와이어링 텍스트 편집 (Unity MCP 없을 때) | Sonnet 5 | **high** | 기계적으로 보여도 fileID/GUID 참조 하나 틀리면 씬 파손 |
| 코루틴/타이밍 로직 수정 | `HitStopController`처럼 코루틴 재시작 vs 연장 방식 차이가 실동작을 가른다 | Sonnet 5 | medium~high | 재현이 힘든 타이밍 버그는 high로 상향 |
| 커밋·브랜치 작업 | `claude/commit-convention.md` 형식 커밋, git-flow `feature/<기능명>` 브랜치 생성 | Sonnet 5 | medium | 절차 문서가 완비됨. [코드 작업 규칙](CODE_WORKFLOW_RULES.md) 참조 |
| Play모드 기능 검증 | Boss 씬에서 감정 전환/슬로우모션 등 런타임 동작 확인 | Sonnet 5 | medium~high | Unity MCP 없으면 사용자에게 검증 요청, [테스트·검증 표준](TEST_AND_VERIFICATION_STANDARD.md) |
| 버그 진단(원인 불명) | 슬로우모션이 특정 페이즈에서만 안 먹는 등 증상과 원인 파일이 다른 경우 | Opus 4.8 | **xhigh** | 코루틴 인터럽트처럼 표면 증상과 원인이 먼 버그는 낮은 effort가 오답을 냄 |
| 보스 페이즈/감정 시스템 설계 | 감정 자동 전환 구동부, 씬 이관(BossHandoff) 같은 교차 시스템 설계 | Opus 4.8 | **xhigh** | RewindManager/BossController/여러 PhaseState 교차 영향 판단 필요 |
| 기획-코드 갭 전수 감사 | 보스 기획서(README) vs `03.Boss/` 구현 전수 대조 | Opus 4.8 | high~xhigh | 절차는 [문서 분석 워크플로우](DOCUMENT_ANALYSIS_WORKFLOW.md) |
| 다단계 장기 작업 총괄 | AI_Workflow_TemplatePack 30종 이식처럼 여러 파일에 걸친 장기 작업 | Opus 4.8 | **xhigh** | Phase 분할·DoD는 [다단계 작업 SOP](MULTI_STAGE_WORKFLOW_SOP.md) 준수 |

## 3. 서브에이전트 effort 기준

공식 권고: 독립 subtask는 서브에이전트에 병렬 위임하고, 검수는 자기비판 대신 **fresh-context 검증자**를 쓴다. 단, 이 프로젝트는 명시적 요청 없이 서브에이전트를 임의로 스폰하지 않는 것이 기본 방침이다(사용자 지시 우선). 분업 패턴은 [Subagent 분업 구조](SUBAGENT_ORCHESTRATION.md), 검수자 프롬프트는 [검수 Subagent 사양서](VERIFIER_SUBAGENT_SPEC.md)를 따른다.

| 서브에이전트 역할 | effort | 근거 |
|---|---|---|
| 탐색·수집형 (Glob/Grep/Read로 경로·사용처 목록화) | **low~medium** | 판단 없이 사실 수집만 |
| 검증형 (컴파일/콘솔 확인, Play 검증 절차 실행) | medium | 절차가 [테스트·검증 표준](TEST_AND_VERIFICATION_STANDARD.md)에 고정되어 재량이 작음 |
| 검수형 (fresh-context verifier — 컨벤션/리와인드 규칙 판정) | **high** | 오탐·미탐 모두 비용이 큼 |
| 구현형 (코드 수정을 위임받는 경우) | 본체와 동일(표 2절 기준) | 위임했다고 난도가 내려가지 않음 |

**낮은 effort 위임의 전제 조건**: (1) 산출물 형식이 목록/표로 고정되어 있고 (2) 실패해도 본체가 재시도 가능하며 (3) 쓰기 권한이 불필요할 것. 셋 중 하나라도 깨지면 medium 이상으로 발주한다.

## 4. 상향/하향 조정 신호

### 상향 신호 (한 단계 올린다)

- **동일 접근 2회 실패**: 같은 컴파일 에러나 같은 증상이 두 번 반복되면 즉시 상향. xhigh에서 2회 실패 시에만 max 시도(1절 내부 규칙).
- **리와인드 시스템 접촉**: `RewindManager`/`RingBuffer`/각 `*State.RecordTick` 등 결정 로그·틱 기록 관련 파일이 diff에 등장하면 medium 미만 금지 — 인덱스가 어긋나면 전 시스템이 깨진다.
- **소유권 경계 판단 필요**: 수정이 `Minsung.*` 네임스페이스 밖(팀원 코드)에 닿는지 애매하면 상향 후 [작업 경계선](ACTION_BOUNDARIES.md) 기준으로 판정.
- **교차 시스템 파급**: 하나의 변경이 페이즈 2개 이상 또는 `BossController`/`GameDB` 양쪽에 파급될 때(예: 이번 세션의 씬 이관 작업).
- **증상-원인 분리 의심**: `HitStopController` 코루틴 인터럽트 버그처럼 증상(슬로우모션 무반응)과 원인(다른 파일의 코루틴 재시작 로직)이 분리된 유형 — 낮은 effort의 국소 패치가 오히려 회귀를 만든다.

### 하향 신호 (한 단계 내린다)

- **PLAN 문서가 이미 존재**: 재현 절차·수정 위치가 `claude/PLAN.md`에 특정된 이어받기 작업.
- **기계적 반복 편집**: 동일 패턴을 N개 파일에 적용(주석 정리, 네이밍 일괄 수정).
- **읽기 전용 산출물**: 코드를 바꾸지 않는 현황 보고·목록화. 이때는 모델도 함께 하향 가능(Haiku 4.5).

### 조정 시 기록

effort를 표 기준과 다르게 운용해 유의미한 결과 차이가 났다면, [메모리·교훈 규칙](MEMORY_RULES.md)에 따라 `feedback_*` 메모리로 남겨 이 표를 개정한다.

## 관련 문서

[FABLE_ONLY_TASKS.md](FABLE_ONLY_TASKS.md) · [CONTEXT_LIMITS_POLICY.md](CONTEXT_LIMITS_POLICY.md) · [SUBAGENT_ORCHESTRATION.md](SUBAGENT_ORCHESTRATION.md) · [VERIFIER_SUBAGENT_SPEC.md](VERIFIER_SUBAGENT_SPEC.md)
