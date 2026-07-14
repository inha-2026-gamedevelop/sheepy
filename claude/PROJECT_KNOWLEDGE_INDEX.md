# 반복 참조 문서 인덱스

> 목적: 저장소에 실존하는 md 문서 전수에 "언제 읽어야 하는가"의 트리거를 부여해, 세션이 필요한 문서만 골라 읽게 한다. | 2026-07-11 Glob 실측 기준 재생성. 문서 추가/삭제 시 재생성 대상.

"코드 파일" 인덱스는 [MASTER_FILE_INDEX.md](MASTER_FILE_INDEX.md) 담당 — 여기는 문서(md)만 다룬다.

## 1. 최상위 규범 (매 세션 자동 로드)

| 문서 | 1줄 요약 | 읽기 트리거 |
|---|---|---|
| `CLAUDE.md`(루트) | `claude/CLAUDE.md`를 import하는 얇은 진입점 | 항상 자동 로드 — 별도 트리거 불필요 |
| `claude/CLAUDE.md` | Communication Protocol, Critical Rules 11개, 아키텍처 요약, 폴더 구조, Documentation Map | 항상 자동 로드. 특히 규칙 위반 우려 시 재확인 |

## 2. 팀 공용 문서 (`claude/`)

| 문서 | 1줄 요약 | 읽기 트리거 |
|---|---|---|
| `claude/coding-convention.md` | 네이밍, 매직넘버 금지, 중괄호, GameDB/Constants 구분 등 컨벤션 전문 | 코드 작성/리뷰 전, 컨벤션 위반 의심 시 |
| `claude/commit-convention.md` | 커밋 타입/scope/제목 형식(`type: Scope 한국어 제목`) | 커밋 메시지 작성 시 |
| `claude/canvas-convention.md` | 모든 씬 메인 캔버스 공통 설정(Screen Space Overlay, 1920x1080) | 신규 씬/캔버스 작업 시 |
| `claude/gamedb.md` | GameDB(SO DB) 구조/사용법/확장 절차/필드 색인/AI 에이전트 규칙 | GameDB 필드 추가·수정, `GameDB.*` 호출 관련 작업 시 |
| `claude/UML.md` | 전체 클래스 구조 UML(Mermaid, 서브시스템별) | 시스템 간 관계 파악이 필요할 때 |
| `claude/README.md` | 게임 소개, 시스템 현황(완료/미완), 폴더 구조, 보스 기획 상세 | 신규 세션 오리엔테이션, 기획 원문 확인 시 |
| `claude/PLAN.md` | 구현 목록 — 완료/우선순위별(HIGH/MED/LOW) 남은 작업, 리팩토링 이력 | 무엇이 남았는지 확인할 때, 갭 분석 결과 기록 시 |
| `claude/HANDOVER.md` | **이번에 고친 것 / 해야 할 것** — 최신 재개 지점 (단일 파일, 누적 아카이브 아님) | 세션 시작 시 최우선 Read, 세션 종료 전 갱신 |

## 3. AI 운영 문서 (`claude/` 내 대문자 파일명 30종 — `AI_WORKFLOW_README.md`가 총 인덱스)

| 문서 | 1줄 요약 | 읽기 트리거 |
|---|---|---|
| `ONE_PAGE_AI_WORKFLOW_MANUAL.md` | 이 폴더 전체의 1페이지 압축본 | 새 세션에서 이 폴더를 처음 접할 때 |
| `ACTION_BOUNDARIES.md` | 수정 금지·사전 질문 경계 총정리 | 팀원 코드/리와인드 구조를 건드릴 것 같을 때 |
| `PROMPT_TEMPLATE_STANDARD.md` / `REQUEST_CONTEXT_TEMPLATE.md` | 작업 의뢰 표준 양식 | 사용자가 아닌 다른 세션/에이전트에 작업을 위임할 때 |
| `EFFORT_POLICY.md` / `FABLE_ONLY_TASKS.md` | 모델·effort 배분 기준 | 작업 난이도 판단, 서브에이전트 발주 시 |
| 그 외 27종 | 검증/체크포인트/대화분리/컨텍스트/경계 등 세부 운영 규칙 | `README.md`의 30종 목차에서 상황별로 선택 |

## 4. 영속 메모리

| 대상 | 1줄 요약 | 읽기 트리거 |
|---|---|---|
| `C:\Users\inha\.claude\projects\d--unity2d\memory\MEMORY.md` | 개인 메모리 인덱스 — **2026-07-11 기준 비어 있음** | 매 세션 자동 로드(내용이 채워지면). 규칙은 [MEMORY_RULES.md](MEMORY_RULES.md) |

## 5. 폐기 후보 / 트리거 없는 문서

2026-07-11 기준 없음 — `claude/`의 모든 문서가 `claude/CLAUDE.md` Documentation Map에 등록되어 있고 각기 명확한 트리거를 가진다.

## 관련 문서

[MASTER_FILE_INDEX.md](MASTER_FILE_INDEX.md) · [RAG_KNOWLEDGE_MAP.md](RAG_KNOWLEDGE_MAP.md) · [MEMORY_RULES.md](MEMORY_RULES.md)

## 재생성 방법 (필요 시)

```
PROJECT_KNOWLEDGE_INDEX.md를 이 저장소 기준으로 다시 생성해줘.
- Glob("**/*.md")으로 md 문서를 전수 수집하고, 문서군별 표(문서|1줄 요약|읽기 트리거)로 정리.
- 요약은 실제로 Read한 내용 기반으로만 작성하고, 트리거가 안 나오는 문서는
  "폐기 후보" 절로 분리해.
```
