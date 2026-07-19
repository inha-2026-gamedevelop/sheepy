# 다른 프로젝트 이식 가이드 (ADAPTATION_GUIDE)

> 목적: 이 AI_Workflow 30종을 다른 프로젝트로 옮길 때 무엇을 그대로 쓰고 무엇을 새로 채워야 하는지, 어떤 순서로 적용하는지를 정의한다. | 작성: 2026-07-06 (Claude Fable 5)

이 팩은 **공용 템플릿 버전**입니다. 특정 프로젝트의 고유 값(조직명·경로·클래스명·인명 등)은 모두
`{{PLACEHOLDER}}` 표기로 치환되어 있고, 본문에 남은 클래스·씬·기능 이름(FeatureA, SubSceneA, NPC_A 등)은
실제 자산이 아니라 **형식을 보여주기 위한 가상의 예시**입니다. 도입 시 플레이스홀더를 자기 프로젝트 값으로
채우고, 예시는 실제 사례로 교체하세요. 문서마다 이식 난이도가 다르므로 아래 3등급으로 분류했습니다.

## 이식 난이도 3등급

| 등급 | 의미 | 작업량 |
|---|---|---|
| **A. 그대로 사용** | 구조·규칙이 프로젝트 무관. 플레이스홀더 값만 치환 | 낮음 (찾기·바꾸기) |
| **B. 골격 재사용** | 목차·형식은 재사용, 내용은 새 프로젝트 규칙으로 재작성 | 중간 |
| **C. 재생성** | 감사·인덱스류. 새 프로젝트 코드/문서를 스캔해 새로 생성해야만 의미 | 높음 (에이전트 위임 권장) |

## 문서별 등급표

| 문서 | 등급 | 이식 시 해야 할 일 |
|---|---|---|
| EFFORT_POLICY.md | A | 작업 유형 표의 예시 행을 새 프로젝트 작업으로 교체. effort 5단계·기본값은 공식 사실이라 유지 |
| PROGRESS_CLAIM_POLICY.md | A | 증거 등급표는 유지, 검증 도구명({{MCP}}/{{TEST_FLOW}})만 치환 |
| AUTONOMOUS_COMPLETION_RULE.md | A | 표준 문구 유지, 정당한 중단 조건에 새 프로젝트 금지 경계 반영 |
| CONVERSATION_SPLIT_RULES.md | A | 세션 분리·이관 규칙 유지, 실사례만 교체 |
| CONTEXT_LIMITS_POLICY.md | A | 모델 공식 수치 유지, 대형 파일 예시를 새 프로젝트 자산으로 교체 |
| FINAL_RESPONSE_STYLE_GUIDE.md | A | {{RESPONSE_LANG}}·링크 규칙 확인 후 예시만 교체 |
| MEMORY_RULES.md | A | 메모리 스키마·규칙 유지, 예시 메모리명 교체 |
| CHECKPOINT_RULES.md | A | PLAN 목차 템플릿 유지, 보관 경로 치환 |
| PROMPT_TEMPLATE_STANDARD.md | A | 5필드 템플릿 유지, 기입 예시를 새 프로젝트 작업으로 교체 |
| REQUEST_CONTEXT_TEMPLATE.md | A | 4필드 템플릿 유지, 예시 교체 |
| VERIFIER_SUBAGENT_SPEC.md | A/B | 검수자 3종 개념 유지. "컨벤션 검증자"의 규칙 목록과 "세이브 검증자"는 프로젝트별 재작성 |
| SUBAGENT_ORCHESTRATION.md | A | fan-out 패턴 개념 유지, 예시를 새 프로젝트 전수 작업으로 교체 |
| MULTI_STAGE_WORKFLOW_SOP.md | A/B | Phase·DoD·롤백 틀 유지, 실사례 2건을 새 프로젝트 대형 작업으로 교체 |
| DOCUMENT_ANALYSIS_WORKFLOW.md | A | 갭 분석 절차·템플릿 유지, 도메인 대조표를 새 프로젝트 데이터로 교체 |
| FALLBACK_POLICY.md | A | refusal 대응 유지. 도구 실패 행({{MCP}} 관련)은 새 도구 환경으로 교체 |
| DATA_RETENTION_AND_PRIVACY_RULES.md | A | 등급 분류 유지, 금지 목록(키 파일 위치 등)을 새 프로젝트 경로로 교체 |
| FABLE_ONLY_TASKS.md | A/B | 판단 기준 4가지 유지, 고난도 후보를 새 프로젝트 시스템으로 교체 |
| SYSTEM_PROMPT_V2.md | B | 새 프로젝트 CLAUDE.md를 대상으로 재작성. Fable 5 원칙 절은 유지 |
| PROJECT_INSTRUCTIONS_V2.md | B | 새 프로젝트 메모리/규칙을 스캔해 통합 재작성 |
| ACTION_BOUNDARIES.md | B | 새 프로젝트의 수정 금지·사전 질문 경계를 코드 실사로 도출 |
| CODE_WORKFLOW_RULES.md | B | 새 프로젝트 브랜치 전략·커밋 규칙·push 절차로 재작성 |
| TEST_AND_VERIFICATION_STANDARD.md | B | 새 프로젝트 테스트 진입·자동화 도구로 재작성 |
| INSTRUCTION_PRUNING_REPORT.md | C | 새 프로젝트 CLAUDE.md+메모리를 전수 감사해 재생성 |
| REASONING_EXTRACTION_AUDIT.md | C | 새 프로젝트 문서를 Grep 전수 검색해 재생성 |
| SKILLS_AUDIT_TABLE.md | C | 새 프로젝트 스킬·문서 자산을 Read해 재생성 |
| PROJECT_KNOWLEDGE_INDEX.md | C | 새 프로젝트 문서를 Glob 전수해 재생성 |
| MASTER_FILE_INDEX.md | C | 새 프로젝트 코드를 Glob/Grep해 진입점 인덱스 재생성 |
| RAG_KNOWLEDGE_MAP.md | C | 새 프로젝트 질문 유형·진입점으로 재생성 |
| POST_FABLE_HANDOFF.md | C | 새 프로젝트 미완 작업 현황으로 재생성(또는 삭제) |
| ONE_PAGE_AI_WORKFLOW_MANUAL.md | C | 위 문서 완료 후 최종 재생성 |

## 플레이스홀더 사전 (찾기·바꾸기 대상)

문서 전체에서 아래 `{{PLACEHOLDER}}` 표기를 자기 프로젝트 값으로 일괄 치환하세요. 기입 예시는 가상의 값입니다.

| 플레이스홀더 | 의미 | 기입 예시 (가상) |
|---|---|---|
| `{{PROJECT_NAME}}` | 프로젝트명 | MyGame |
| `{{ORG}}` | 조직명 | AcmeStudio |
| `{{ENGINE}}` / `{{ENGINE_VERSION}}` | 엔진 / 엔진·버전 | Unity / Unity 2022.3 LTS |
| `{{RENDER_PIPELINE}}` | 렌더 파이프라인 | URP 14 |
| `{{LANG}}` | 주 언어 | C# |
| `{{RESPONSE_LANG}}` | 응답 언어 규약 | 한국어 |
| `{{REPO_ROOT}}` / `{{REPO_NAME}}` | 저장소 루트 / 저장소명 | ~/GitHub/mygame |
| `{{SRC_ROOT}}` | 소스 루트 폴더 | Assets/Scripts |
| `{{ALLOWED_MODULE}}` / `{{ALLOWED_SCOPE}}` / `{{ALLOWED_PATH}}` / `{{ALLOWED_NAMESPACE}}` | 수정 허용 모듈·범위·경로·네임스페이스 | Shop / 03.Shop / Assets/Scripts/Shop / Main.Shop |
| `{{FORBIDDEN_MODULE}}` / `{{FORBIDDEN_SCOPE}}` / `{{FORBIDDEN_PATH}}` | 수정 금지 모듈·범위·경로 | Battle / 04.Battle / Assets/Scripts/Battle |
| `{{SAVE_ROOT_CLASS}}` / `{{SAVE_SLOT_CLASS}}` / `{{SAVE_VERSION_CONST}}` | 세이브·직렬화 핵심(중앙 데이터 클래스 / 슬롯 클래스 / 버전 상수) | SaveData / SaveSlot / SAVE_VERSION |
| `{{RESOURCE_MGR}}` / `{{RESOURCE_PRELOADER}}` / `{{RESOURCE_SYSTEM}}` | 리소스 로딩 관문·예열기·시스템 | ResourceMgr / Preloader / Addressables |
| `{{ENTRY_SCENE}}` / `{{SCENE_A}}` / `{{SCENE_B}}` / `{{SCENE_C}}` / `{{BOOT_SCENE}}` | 테스트 진입 씬과 주요 씬들 | StartScene / MainScene 등 |
| `{{TEST_CHEAT_KEYS}}` / `{{TEST_CHEAT_SCRIPT}}` | 치트 진입 키 / 테스트 자동화 스크립트 | F1+F2 / AutoEnter.cs |
| `{{MCP_TOOL}}` / `{{MCP_REFRESH_CMD}}` | 에디터·빌드 연동 MCP 도구와 갱신 명령 | engine-mcp / refresh_assets |
| `{{MEMORY_DIR}}` | 영속 메모리 경로 | ~/.claude/projects/<프로젝트>/memory |
| `{{USER_ID}}` / `{{USER_NAME}}` / `{{TEAMMATE_ID}}` | 작업자 브랜치용 ID / 이름 / 팀원 ID | jdoe / 홍길동 / kim |
| `{{SKILLS_DIR}}` | 스킬·워크플로우 문서 폴더 | ClaudeSkills |
| `{{GLOBAL_UI_PREFAB}}` | 전역 UI 프리팹(있다면) | GlobalCanvas.prefab |
| `{{PROJECT_FONT}}` | 프로젝트 표준 폰트 | NotoSans SDF |
| `{{GENRE_A}}` / `{{GENRE_B}}` | 게임/제품의 주요 모드 설명 | 경영 파트 / 전투 파트 |
| `{{DAY_START}}` | 루프 시작 지점(있다면) | DayStart |
| `{{LOCALE_ROOT}}` | 로컬라이제이션 에셋 루트 | Assets/Locale |

본문 예시에 남아 있는 `FeatureA*`, `FeatureB*`, `SubScene*`, `SubShop*`, `NPC_A~E`, `ExampleDB*`,
`PLAN_ExampleTask*`, `project_known_bug_*` 등은 플레이스홀더가 아니라 **가상의 예시 이름**입니다.
자기 프로젝트의 실제 사례로 교체하거나, 형식만 참고하고 지워도 됩니다.

## 적용 순서 (권장)

새 프로젝트에 이식할 때 아래 순서를 따르면 의존 관계가 꼬이지 않습니다.

1. **플레이스홀더 확정** — 위 사전을 새 프로젝트 값으로 먼저 채운다.
2. **A등급 17종 치환** — 값만 바꿔 즉시 배치. 이 팩의 문서를 그대로 복사 후 프로젝트 고유 명사 교체.
3. **C등급 감사·인덱스 재생성** — SKILLS_AUDIT_TABLE, MASTER_FILE_INDEX, PROJECT_KNOWLEDGE_INDEX,
   RAG_KNOWLEDGE_MAP, INSTRUCTION_PRUNING_REPORT, REASONING_EXTRACTION_AUDIT를 새 프로젝트 코드/문서
   스캔으로 생성. 파일이 많으면 서브에이전트에 문서 1개씩 위임(SUBAGENT_ORCHESTRATION.md 패턴).
4. **B등급 5종 재작성** — 3의 감사 결과를 입력으로 SYSTEM_PROMPT_V2, PROJECT_INSTRUCTIONS_V2,
   ACTION_BOUNDARIES, CODE_WORKFLOW_RULES, TEST_AND_VERIFICATION_STANDARD를 새 프로젝트 규칙으로 작성.
5. **총괄 재생성** — POST_FABLE_HANDOFF(미완 작업)와 ONE_PAGE_AI_WORKFLOW_MANUAL을 마지막에 생성.
6. **연결** — 새 프로젝트 CLAUDE.md의 Documentation Map에 이 폴더를 등록하고,
   claude.ai Projects를 쓴다면 PROJECT_INSTRUCTIONS_V2의 붙여넣기 블록을 프로젝트 지침 칸에 넣는다.

## Claude에게 통째로 시키려면

새 프로젝트에서 이 팩을 열어둔 채 다음처럼 지시하면 위 순서를 자동 수행합니다.

```
이 AI_Workflow 팩(30종)을 우리 프로젝트에 이식해줘.
- 프로젝트: {{PROJECT_NAME}} / {{ENGINE}} / 응답 {{RESPONSE_LANG}}
- 먼저 ADAPTATION_GUIDE.md의 플레이스홀더 사전을 우리 값으로 채우고
- A등급은 값 치환해 배치, C등급 감사·인덱스는 우리 코드/문서를 스캔해 재생성,
- B등급은 그 감사 결과로 재작성, 총괄 2종은 마지막에.
- 모든 경로는 실존 확인한 것만 쓰고, 미확인은 '미확인'으로 표기.
```

## 주의

- 이 팩의 공식 근거(모델 ID·컨텍스트·effort 단계·가격)는 2026-06-24 캐시 기준입니다. 이식 시점에 모델이
  바뀌었으면 최신 값으로 갱신하세요(EFFORT_POLICY, CONTEXT_LIMITS_POLICY, FABLE_ONLY_TASKS가 영향).
- 비밀(키 값·계정·세이브 원문)은 어떤 문서에도 인용하지 않습니다. 경로만 언급합니다.
