# AI 운영 문서 시스템 (AI_Workflow)

> 목적: Claude로 **The Last Re:wind**(Unity 2D) 프로젝트를 작업할 때의 운영 규범 30종을 한곳에 모은 인덱스. | 공용 템플릿(AI_Workflow_TemplatePack)에서 2026-07-11 이 프로젝트 전용으로 이식 완료 — 플레이스홀더 없음, 전부 실제 값·실제 코드 참조.

이 폴더는 "일회성 답변"이 아니라 **이후 세션이 그대로 이어받는 운영 골격**을 목표로 만들어졌습니다.
새 세션을 시작하면 [ONE_PAGE_AI_WORKFLOW_MANUAL.md](ONE_PAGE_AI_WORKFLOW_MANUAL.md) 한 장부터 읽으세요.

## ⭐ 시간이 없다면 이 6개부터

| 문서 | 역할 |
|---|---|
| [ONE_PAGE_AI_WORKFLOW_MANUAL.md](ONE_PAGE_AI_WORKFLOW_MANUAL.md) | 세션 루틴 전체 압축본 — **가장 먼저 읽을 문서** |
| [ACTION_BOUNDARIES.md](ACTION_BOUNDARIES.md) | 수정 금지·사전 질문 경계(네임스페이스 소유권, 리와인드 버퍼) |
| [PROJECT_INSTRUCTIONS_V2.md](PROJECT_INSTRUCTIONS_V2.md) | 흩어진 지침 통합 + claude.ai Projects 붙여넣기 블록 |
| [CODE_WORKFLOW_RULES.md](CODE_WORKFLOW_RULES.md) | git-flow 브랜치·커밋 컨벤션·수정 절차 |
| [TEST_AND_VERIFICATION_STANDARD.md](TEST_AND_VERIFICATION_STANDARD.md) | Unity MCP 연결/미연결 듀얼 경로 검증 |
| [MEMORY_RULES.md](MEMORY_RULES.md) | 영속 메모리 운영·교훈 축적 규칙 |

## 📋 전체 30종 (일상 루틴 순)

### A. 규범 재작성·감사 (세션 세팅 전 1회)
- [SYSTEM_PROMPT_V2.md](SYSTEM_PROMPT_V2.md) — `claude/CLAUDE.md` 재작성 감사(2026-07-11 기준 변경 폭 작음)
- [PROJECT_INSTRUCTIONS_V2.md](PROJECT_INSTRUCTIONS_V2.md) — 프로젝트 지침 통합
- [INSTRUCTION_PRUNING_REPORT.md](INSTRUCTION_PRUNING_REPORT.md) — 과잉 지시 삭제 감사
- [REASONING_EXTRACTION_AUDIT.md](REASONING_EXTRACTION_AUDIT.md) — CoT 강요 문구 감사
- [SKILLS_AUDIT_TABLE.md](SKILLS_AUDIT_TABLE.md) — 스킬·문서 자산 감사표

### B. 세션 시작 — 무엇을 읽나
- [PROJECT_KNOWLEDGE_INDEX.md](PROJECT_KNOWLEDGE_INDEX.md) — 반복 참조 문서 전수 인덱스
- [MASTER_FILE_INDEX.md](MASTER_FILE_INDEX.md) — 시스템별 핵심 진입점 파일
- [RAG_KNOWLEDGE_MAP.md](RAG_KNOWLEDGE_MAP.md) — 질문 유형 → 문서·코드 경로 매핑
- [MEMORY_RULES.md](MEMORY_RULES.md) — 메모리·교훈 규칙

### C. 작업 의뢰 — 어떤 템플릿
- [PROMPT_TEMPLATE_STANDARD.md](PROMPT_TEMPLATE_STANDARD.md) — 표준 의뢰 프롬프트(무엇을)
- [REQUEST_CONTEXT_TEMPLATE.md](REQUEST_CONTEXT_TEMPLATE.md) — 목적 설명 템플릿(왜)

### D. 모델·리소스 배분
- [FABLE_ONLY_TASKS.md](FABLE_ONLY_TASKS.md) — 최상위 모델 전용 작업 목록
- [EFFORT_POLICY.md](EFFORT_POLICY.md) — 작업 유형별 모델×effort 표
- [CONTEXT_LIMITS_POLICY.md](CONTEXT_LIMITS_POLICY.md) — 컨텍스트 한도·대형 씬/프리팹 취급
- [CONVERSATION_SPLIT_RULES.md](CONVERSATION_SPLIT_RULES.md) — 세션 분리·이관 기준

### E. 실행 중 — 경계·오케스트레이션
- [ACTION_BOUNDARIES.md](ACTION_BOUNDARIES.md) — 작업 경계선(수정 금지·사전 질문)
- [CODE_WORKFLOW_RULES.md](CODE_WORKFLOW_RULES.md) — 코드 작업·git-flow 브랜치·커밋 절차
- [SUBAGENT_ORCHESTRATION.md](SUBAGENT_ORCHESTRATION.md) — 서브에이전트 fan-out 패턴
- [MULTI_STAGE_WORKFLOW_SOP.md](MULTI_STAGE_WORKFLOW_SOP.md) — 다단계 작업 SOP
- [DOCUMENT_ANALYSIS_WORKFLOW.md](DOCUMENT_ANALYSIS_WORKFLOW.md) — 기획 문서 갭 분석 절차
- [AUTONOMOUS_COMPLETION_RULE.md](AUTONOMOUS_COMPLETION_RULE.md) — 자율 실행 종료 방지 문구

### F. 검증·보고
- [VERIFIER_SUBAGENT_SPEC.md](VERIFIER_SUBAGENT_SPEC.md) — 검수 서브에이전트 3종(컴파일/컨벤션/리와인드 영향)
- [PROGRESS_CLAIM_POLICY.md](PROGRESS_CLAIM_POLICY.md) — 진행 보고 증거 규칙
- [TEST_AND_VERIFICATION_STANDARD.md](TEST_AND_VERIFICATION_STANDARD.md) — 테스트·검증 표준
- [FINAL_RESPONSE_STYLE_GUIDE.md](FINAL_RESPONSE_STYLE_GUIDE.md) — 최종 답변 스타일
- [FALLBACK_POLICY.md](FALLBACK_POLICY.md) — Refusal/실패 대응 플레이북

### G. 세션 종료·보안
- [CHECKPOINT_RULES.md](CHECKPOINT_RULES.md) — `claude/HANDOVER.md` 갱신 시점·표준 목차
- [DATA_RETENTION_AND_PRIVACY_RULES.md](DATA_RETENTION_AND_PRIVACY_RULES.md) — 민감정보 업로드 기준
- [POST_FABLE_HANDOFF.md](POST_FABLE_HANDOFF.md) — 모델 전환 인수인계 양식(이 프로젝트엔 현재 해당 없음)

### H. 총괄
- [ONE_PAGE_AI_WORKFLOW_MANUAL.md](ONE_PAGE_AI_WORKFLOW_MANUAL.md) — 1페이지 운영 매뉴얼

## 이 프로젝트에 맞춰 바뀐 핵심 전제

원본 공용 템플릿은 세이브 시스템·MCP 상시 연동·"push" 커스텀 스킬·인원별 모듈 분리를 전제로 했으나, 이 프로젝트는 다음과 같이 다르다:

- **세이브 시스템 없음** — 로컬 세이브 대신 Supabase(랭킹/고스트 리플레이)만 있음. 대응하는 구조적 위험은 **리와인드 버퍼 정합**([ACTION_BOUNDARIES.md](ACTION_BOUNDARIES.md) B2/B3).
- **브랜치는 기능별** — `feature/<기능명>`(예: `feature/boss-emotion-scene-handoff`), 인원별 아님.
- **Unity MCP는 세션마다 연결 여부가 다름** — [TEST_AND_VERIFICATION_STANDARD.md](TEST_AND_VERIFICATION_STANDARD.md)가 연결/미연결 듀얼 경로를 규정.
- **소유권 경계는 네임스페이스 기반** — `Minsung.*`(민성) vs 팀원(명진/진욱, 네임스페이스 없음), 폴더 강제 분리가 아님.

## 다른 프로젝트로 이식하려면

이 폴더를 다른 프로젝트에 다시 이식하려면 [ADAPTATION_GUIDE.md](ADAPTATION_GUIDE.md)를 참고하세요. 단, 현재 이 폴더의 문서들은 이미 이 프로젝트의 실제 값으로 채워져 있으므로, 다른 프로젝트에 쓰려면 플레이스홀더 형태로 되돌리기보다 이 문서들을 새 프로젝트 실사 기준으로 다시 작성하는 편이 낫습니다(원본 공용 템플릿을 별도로 보관해두는 것을 권장).
