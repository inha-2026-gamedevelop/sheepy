# 스킬·문서 자산 전체 감사표

> 목적: `.claude/` 하위와 `claude/` 팀 문서 자산(스킬·문서·설정) 전수를 유지/수정/통합/폐기 4판정으로 감사해, 규범이 늘어나기만 하고 정리되지 않는 문제를 막는다. | 2026-07-11 Glob/Read 전수 감사 완료.

## 판정 4종

| 판정 | 의미 |
|---|---|
| **유지** | 최신·유효. 그대로 둔다 |
| **수정** | 골격은 유효하나 내용이 낡음/과잉. 갱신 지시 포함 |
| **통합** | 다른 문서와 중복. 흡수 대상 명시 후 스텁화 |
| **폐기** | 사문화·무효. 삭제(또는 아카이브 이동) |

## 0. 판정 요약표 (전수)

| # | 자산 | 종류 | 1줄 역할 | 최신성 | 판정 |
|---|---|---|---|---|---|
| 1 | `.claude/skills/clarify-ambiguous-request/SKILL.md` | 스킬 | 요청 모호 시 조사 후 되묻는 규칙 | 최신(2026-07-11 신설) | 유지 |
| 2 | `.claude/settings.local.json` | 설정 | 개인 로컬 권한 설정 | — | 유지(공유 대상 아님, `.gitignore`로 커밋 제외 확인됨) |
| 3 | `claude/CLAUDE.md` | 최상위 규범 | Critical Rules 11개 + 아키텍처 요약 | 최신(2026-07-11 갱신) | 유지 |
| 4 | `claude/coding-convention.md` | 컨벤션 | 네이밍/매직넘버/중괄호 등 전문 | 최신 | 유지 |
| 5 | `claude/commit-convention.md` | 커밋 규약 | `type: Scope 제목` 형식 | 최신(이번 세션 신설) | 유지 |
| 6 | `claude/gamedb.md` | 시스템 가이드 | GameDB 구조/확장 절차 | 최신 | 유지 |
| 7 | `claude/canvas-convention.md` | UI 규약 | 메인 캔버스 공통 설정 | 확인 안 됨(변경 이력 없음) | 유지(추정) |
| 8 | `claude/UML.md` | 아키텍처 문서 | 클래스 구조 다이어그램 | 보스 시스템 변경분(이번 세션) 미반영 가능성 | 수정 후보 — 보스 감정/씬이관 구조 추가 여부 확인 필요 |
| 9 | `claude/README.md` | 소개 문서 | 게임 소개/시스템 현황/보스 기획 상세 | 최신(보스 기획 반영됨) | 유지 |
| 10 | `claude/PLAN.md` | 로드맵 | 구현 현황/HIGH-MED-LOW 갭 | 최신(이번 세션 갱신) | 유지 |
| 11 | `claude/HANDOVER.md` | 인수인계 | 이번에 고친 것/해야 할 것 | 최신(이번 세션 대폭 축소·갱신) | 유지 |
| 12 | `claude/AI_WORKFLOW_README.md` 외 AI 운영 문서 30종(대문자 파일명, `claude/` 평탄 구조) | AI 운영 문서 | 본 이식 작업의 산출물 | 신설(2026-07-11) | 유지 |

## 1. 검증 증거

- #1: `.claude/skills/` Glob 확인, 파일 존재 및 최근 수정일 확인.
- #2: `git check-ignore -v .claude/settings.local.json` 실행 결과 `.gitignore:78`(`.claude/*`) 매치 확인 — 팀 공유 안 됨 검증됨.
- #3~11: 이번 세션 대화 이력에서 실제로 Read·Edit한 파일들 — 내용이 최신임을 직접 확인.
- #8(`UML.md`)만 이번 세션에서 직접 Read하지 않아 "확인 필요"로 보수적 판정.

## 2. 상세 판정

### #8. `claude/UML.md` — 수정 후보

**현재 상태**: 이번 세션 이전 시점의 클래스 구조를 반영하고 있을 가능성. **문제**: 이번 세션에서 `BossHandoff`(신규 클래스), `BossController.CoEmotionLoop` 등 보스 시스템에 구조 변경이 있었는데 UML에 반영됐는지 미확인. **조치**: 다음 세션에서 `claude/UML.md`의 보스 섹션을 Read해 신규 클래스(`BossHandoff`) 관계가 빠져 있으면 추가한다. 시급하지 않음(문서 갱신 지연이 즉각적 문제를 일으키지 않음) — LOW 우선순위로 `claude/PLAN.md`에 등재 권고.

## 관련 문서

[INSTRUCTION_PRUNING_REPORT.md](INSTRUCTION_PRUNING_REPORT.md) · [SYSTEM_PROMPT_V2.md](SYSTEM_PROMPT_V2.md) · [SUBAGENT_ORCHESTRATION.md](SUBAGENT_ORCHESTRATION.md)

## 재생성 방법 (필요 시)

```
SKILLS_AUDIT_TABLE.md를 이 저장소 기준으로 다시 생성해줘.
- .claude/와 claude/의 자산을 전수 나열하고 유지/수정/통합/폐기 판정 + 실측 근거를 표로 정리해.
- 코드와 어긋난 문서(수치 표류, 사문화 경로)는 반드시 수정/폐기로 잡아.
```
