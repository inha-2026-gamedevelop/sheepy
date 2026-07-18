# 1페이지 운영 매뉴얼

> 목적: AI_Workflow 나머지 문서를 일상 세션 루틴(시작→의뢰→실행→보고→종료) 순서로 압축한 단일 진입점. | TheLastRewind 프로젝트 전용 버전(2026-07-11, AI_Workflow_TemplatePack에서 이식). 이 폴더 전체를 처음 접하면 이 문서부터 읽는다.

## 세션 루틴 체크리스트

**① 세션 시작 — 무엇을 읽나**
- [ ] `claude/CLAUDE.md`(자동 로드)와 영속 메모리(`C:\Users\inha\.claude\projects\d--unity2d\memory\MEMORY.md`, 현재 비어 있음) 확인
- [ ] `git status`로 병행 작업 변경분 식별 — 커밋 시 선별 스테이징의 전제
- [ ] 이어받는 작업이면 `claude/PLAN.md`(이번에 고친 것/해야 할 것)부터, 세부 로드맵은 `claude/PLAN.md`

**② 작업 의뢰 — 어떤 템플릿**
- [ ] 의뢰문 = 5필드([PROMPT_TEMPLATE_STANDARD](PROMPT_TEMPLATE_STANDARD.md)) + "왜" 4필드([REQUEST_CONTEXT_TEMPLATE](REQUEST_CONTEXT_TEMPLATE.md))
- [ ] 모델×effort 결정([EFFORT_POLICY](EFFORT_POLICY.md)) — 루틴 medium, 고난도 xhigh
- [ ] 기획 문서가 입력이면 [DOCUMENT_ANALYSIS_WORKFLOW](DOCUMENT_ANALYSIS_WORKFLOW.md) 5단계, 대형 작업이면 [MULTI_STAGE_WORKFLOW_SOP](MULTI_STAGE_WORKFLOW_SOP.md) Phase 분할

**③ 실행 중 — 경계·검증**
- [ ] 경계 준수([ACTION_BOUNDARIES](ACTION_BOUNDARIES.md)): 팀원(명진/진욱) 소유 코드는 소유자 확인 후, 리와인드 버퍼 구조 변경은 사전 질문
- [ ] 코드 변경 후 Unity MCP 연결 시 컴파일/콘솔 확인, 미연결 시 코드 리뷰 + "확인 필요" 명시 → 완료 주장 전 증거 등급 확보([PROGRESS_CLAIM_POLICY](PROGRESS_CLAIM_POLICY.md))
- [ ] 기능 검증은 대상 씬에서 Play — Unity MCP 없으면 사용자에게 확인 요청([TEST_AND_VERIFICATION_STANDARD](TEST_AND_VERIFICATION_STANDARD.md))

**④ 보고 — 스타일**
- [ ] 결론 우선, 수정 코드는 `[파일:라인](경로#L라인)` 전수 하이퍼링크, 미검증 항목 명시([FINAL_RESPONSE_STYLE_GUIDE](FINAL_RESPONSE_STYLE_GUIDE.md))

**⑤ 세션 종료 — 메모리·체크포인트**
- [ ] 사용자 교정·확정 결정이 있었다면 `feedback_*`/`project_*` 메모리로 저장([MEMORY_RULES](MEMORY_RULES.md)) — 2026-07-11 기준 메모리가 비어 있어 특히 중요
- [ ] 미완 중단이면 `claude/PLAN.md`를 "이번에 고친 것/해야 할 것" 형식으로 갱신([CHECKPOINT_RULES](CHECKPOINT_RULES.md))

## 표 1 — 문서 레지스트리 (루틴 단계순)

| 단계 | 문서 | 1줄 요약 |
|---|---|---|
| ①시작 | [PROJECT_INSTRUCTIONS_V2](PROJECT_INSTRUCTIONS_V2.md) | 흩어진 규칙·메모리를 규칙+Why+How 단일 지침서로 통합 |
| ①시작 | [PROJECT_KNOWLEDGE_INDEX](PROJECT_KNOWLEDGE_INDEX.md) | `claude/` 실존 md에 "언제 읽어야 하는가" 트리거를 부여한 문서 인덱스 |
| ①시작 | [RAG_KNOWLEDGE_MAP](RAG_KNOWLEDGE_MAP.md) | 질문 유형·키워드→문서/코드 진입점 매핑 + 탐색 함정 목록(예: `coding-convention.md` 파일명) |
| ①시작 | [MASTER_FILE_INDEX](MASTER_FILE_INDEX.md) | 시스템별 진입점 파일을 경로+역할+네임스페이스로 실존 검증 정리 |
| ②의뢰 | [PROMPT_TEMPLATE_STANDARD](PROMPT_TEMPLATE_STANDARD.md) | 목적·범위·제약·완료기준·검증 5필드 + 버그/신규 기능/조사 복붙 템플릿 3종 |
| ②의뢰 | [REQUEST_CONTEXT_TEMPLATE](REQUEST_CONTEXT_TEMPLATE.md) | 배경·기대 산출물·연관 시스템·과거 이력 4필드 "왜" 양식 |
| ②의뢰 | [FABLE_ONLY_TASKS](FABLE_ONLY_TASKS.md) | 최상위 모델 전용 고난도 작업 vs 하위 모델 충분 작업 배분 판단 기준 4가지 |
| ②의뢰 | [EFFORT_POLICY](EFFORT_POLICY.md) | 작업 유형별 모델×effort 권장표, 상향/하향 신호 |
| ②의뢰 | [DOCUMENT_ANALYSIS_WORKFLOW](DOCUMENT_ANALYSIS_WORKFLOW.md) | 기획 문서→코드 갭 분석 5단계 표준(보스 기획 대조 실사례) |
| ②의뢰 | [MULTI_STAGE_WORKFLOW_SOP](MULTI_STAGE_WORKFLOW_SOP.md) | Phase 분할 기준·게이트 DoD·롤백 수단(보스 P1/P2/P3 실사례) |
| ③실행 | [ACTION_BOUNDARIES](ACTION_BOUNDARIES.md) | 수정 금지·사전 질문 경계선 — 네임스페이스 소유권, 리와인드 버퍼 |
| ③실행 | [AUTONOMOUS_COMPLETION_RULE](AUTONOMOUS_COMPLETION_RULE.md) | 조기 종료·허락 구걸 방지 표준 문구 + 정당한 중단 조건의 쌍 배치 |
| ③실행 | [SUBAGENT_ORCHESTRATION](SUBAGENT_ORCHESTRATION.md) | fan-out 패턴 + 충돌 회피 원칙(단, 임의 스폰 금지가 기본 방침) |
| ③실행 | [VERIFIER_SUBAGENT_SPEC](VERIFIER_SUBAGENT_SPEC.md) | 컴파일/컨벤션/리와인드 영향 검수자 3종 프롬프트 원문 |
| ③실행 | [TEST_AND_VERIFICATION_STANDARD](TEST_AND_VERIFICATION_STANDARD.md) | MCP 연결/미연결 듀얼 경로 검증 표준 |
| ③실행 | [PROGRESS_CLAIM_POLICY](PROGRESS_CLAIM_POLICY.md) | "완료" 선언 전 증거 등급 + 표준 문구 |
| ③실행 | [CODE_WORKFLOW_RULES](CODE_WORKFLOW_RULES.md) | 표준 절차·git-flow 브랜치·커밋 타입/스코프 규약 |
| ③실행 | [CONTEXT_LIMITS_POLICY](CONTEXT_LIMITS_POLICY.md) | 대형 씬/프리팹 취급과 전수 탐색 fan-out 임계 |
| ③실행 | [FALLBACK_POLICY](FALLBACK_POLICY.md) | 도구 실패 유형별 대응표 + refusal 시 재구성→분할→모델 전환 |
| ③실행 | [DATA_RETENTION_AND_PRIVACY_RULES](DATA_RETENTION_AND_PRIVACY_RULES.md) | 비밀·민감정보 3등급 금지표(Supabase KEY.txt 등) |
| ④보고 | [FINAL_RESPONSE_STYLE_GUIDE](FINAL_RESPONSE_STYLE_GUIDE.md) | 한국어·결론 우선·수정 코드 전수 하이퍼링크 등 보고 규칙 |
| ⑤종료 | [MEMORY_RULES](MEMORY_RULES.md) | 영속 메모리의 저장·갱신·링크·절대날짜 규칙 |
| ⑤종료 | [CHECKPOINT_RULES](CHECKPOINT_RULES.md) | `claude/PLAN.md` 갱신 트리거 + 표준 목차(단일 파일 관례) |
| ⑤종료 | [CONVERSATION_SPLIT_RULES](CONVERSATION_SPLIT_RULES.md) | 세션 분리 트리거 + 이관 체크리스트 |
| ⑤종료 | [POST_FABLE_HANDOFF](POST_FABLE_HANDOFF.md) | **이 프로젝트엔 해당 없음** — `claude/PLAN.md`가 실질적으로 같은 역할 수행 |
| ⑥개정 | [SYSTEM_PROMPT_V2](SYSTEM_PROMPT_V2.md) | `claude/CLAUDE.md` 재작성 제안 양식(이 프로젝트는 이미 간결해 변경 폭 작음) |
| ⑥개정 | [SKILLS_AUDIT_TABLE](SKILLS_AUDIT_TABLE.md) | 스킬·문서 자산 전수 감사(유지/수정/통합/폐기) |
| ⑥개정 | [INSTRUCTION_PRUNING_REPORT](INSTRUCTION_PRUNING_REPORT.md) | 중복·과잉·사문화 지시 감사(2026-07-11 기준 과잉 0건) |
| ⑥개정 | [REASONING_EXTRACTION_AUDIT](REASONING_EXTRACTION_AUDIT.md) | CoT 강요·과잉 역할극 문구 감사(2026-07-11 기준 0건) |
| — | [ADAPTATION_GUIDE](ADAPTATION_GUIDE.md) | 이 팩을 **다른** 프로젝트에 이식할 때 참고할 원본 가이드 |

※ ⑥개정(비정기)은 일상 루틴이 아니라 규범 개정 시에만 읽는다.

## 표 2 — 실행 중 즉시 참조

| 항목 | 값 / 절차 |
|---|---|
| 수정 범위 | `Minsung.*` 네임스페이스는 민성 전용. 팀원(명진/진욱) 코드는 네임스페이스 없음 — 수정 전 소유자 확인 |
| 리와인드 규칙 | 버퍼 용량은 `RewindManager.TickCapacity`만. `IRewindable`은 Register/Unregister 쌍, 랜덤은 결정 로그, 연출은 풀 활성/비활성 |
| 세이브 스키마 | **해당 없음** — 로컬 세이브 없음. Supabase는 랭킹/고스트 리플레이 백엔드(`Assets/StreamingAssets/KEY.txt`, gitignore) |
| 테스트 진입 | 대상 씬에서 Play(예: `Boss.unity`). 자동 치트 진입 스크립트는 미확인 |
| 커밋 | `claude/commit-convention.md` 형식(`type: Scope 한국어 제목`), git-flow `feature/<기능명>` 브랜치. push는 사용자 명시 요청 시만 |
| 에디터 연동 | Unity MCP(`http://127.0.0.1:8080/mcp`, `claude/PLAN.md` 근거) — 이번 세션 기준 대부분 미연결. 연결 여부는 매 세션 ToolSearch로 확인 |
| 감지 훅 | 없음(2026-07-11 기준 `.claude/hooks/` 미확인) |
| 아키텍처 질문 | `claude/CLAUDE.md` Architecture 절 먼저 읽기, 상세는 `claude/UML.md` |
