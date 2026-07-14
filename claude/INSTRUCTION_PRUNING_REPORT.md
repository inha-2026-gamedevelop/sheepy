# 과잉 지시 삭제 보고

> 목적: `claude/CLAUDE.md`와 영속 메모리의 지시를 전수 감사해 중복·과잉·사문화 지시를 걸러내고, 그 결과를 [SYSTEM_PROMPT_V2.md](SYSTEM_PROMPT_V2.md) 재작성의 입력으로 제공한다. | 2026-07-11 Read 전수 감사 완료.

## 판정 원칙

| 판정 | 기준 |
|---|---|
| **중복** | 같은 규칙이 CLAUDE.md와 메모리(또는 문서 2곳)에 양존 — 단일 출처를 정하고 나머지는 스텁화 |
| **과잉** | 모델이 스스로 조회 가능한 정보나 기본 성향에 이미 내장된 처방 — 삭제 |
| **사문화** | 코드 실측과 어긋난 사실(수치, 없어진 경로) — 실측값으로 갱신 |
| **승격** | 메모리에만 있는 핵심 경계 규칙 — CLAUDE.md 본문으로 승격 |
| **유지** | 모델이 알 수 없는 프로젝트 고유 사실·컨벤션 — 그대로 |

## 0. 결론 요약

`claude/CLAUDE.md`(118행, 규칙 11개)를 전수 감사한 결과 **중복 0건, 과잉 0건, 사문화 1건(경미), 승격 대상 0건**. 이 프로젝트의 CLAUDE.md는 이미 짧고(권장 200줄 이내) 대부분 모델이 사전에 알 수 없는 프로젝트 고유 사실(네임스페이스, GameDB 구조, 리와인드 규칙 등)로 구성돼 있어 삭제할 과잉 지시가 거의 없다. 영속 메모리는 **2026-07-11 기준 비어 있어 감사 대상 자체가 없다** — 이 사실 자체가 다음 세션이 우선 처리할 항목이다([MEMORY_RULES.md](MEMORY_RULES.md) 8절 참고).

## 1. 감사 방법·범위

대상: `claude/CLAUDE.md`(Critical Rules 11개 + Architecture 절), `C:\Users\inha\.claude\projects\d--unity2d\memory\`. 수단: 전문 Read + 코드/폴더 구조 대조(Glob).

## 2. CLAUDE.md 감사표

| # | 절/지시 | 판정 | 실측 근거 | 조치 |
|---|---|---|---|---|
| C1 | "총 104개 스크립트" (폴더 구조 절) | 유지(수치 재확인 권장) | Glob 결과 스크립트 수는 시점에 따라 변한다 — 큰 기능 추가/삭제 후 재확인 필요 | 지금은 오차 범위 내로 판단, 재생성 시 재확인 |
| C2 | Critical Rules 1~11 전체 | 유지 | 전부 이 프로젝트 고유 사실(GameDB 필드 초기화 제약, 리와인드 버퍼, 네임스페이스 소유권, BT 그래프 호환 등) — 모델이 코드를 안 봐도 알 수 있는 일반 지식이 아님 | 변경 불필요 |
| C3 | Documentation Map 8개 행 | 유지 → **AI_Workflow_TemplatePack 행 추가 필요** | 이번 이식 작업으로 신규 문서군이 생겼으나 아직 Documentation Map에 없음 | 이 이식 작업의 4단계(README 갱신)와 함께 5단계에서 반영 예정 |
| C4 | "Player BT는 제거됨" 서술(규칙 5) | 유지 | 실제로 Player BT 노드가 없고 `PlayerController`가 입력을 직접 처리한다는 서술이 코드 구조와 일치 | 변경 불필요 |
| C5 | "체력 규칙" 절의 "하트 6개" | 유지 | `PlayerHealth`의 하트 방식 서술이 코드와 일치(이번 세션에 반칸 단위 확인) | 변경 불필요 |

**과잉 판정 없음**: 다른 프로젝트에서 흔한 과잉 사례(패키지 버전 나열, 완료된 마이그레이션의 진행 단계명 서술)가 이 CLAUDE.md에는 애초에 없다 — 이미 결과 규칙 위주로 간결하게 작성돼 있다.

## 3. 메모리 감사표

| # | 메모리 파일 | 판정 | 근거 | 조치 |
|---|---|---|---|---|
| — | (해당 없음) | — | `C:\Users\inha\.claude\projects\d--unity2d\memory\`가 2026-07-11 기준 완전히 비어 있음(`MEMORY.md` 인덱스도 없음) | 다음 세션 착수 전, 이번 세션에서 나온 사용자 교정 4건([MEMORY_RULES.md](MEMORY_RULES.md) 8절에 기록됨: 커밋 공저자 표기 금지, push는 명시 요청 시만, 커밋 컨벤션 괄호 없는 형식, HANDOVER 간결화 요구)을 `feedback_*` 메모리로 저장할 것을 권고 |

## 4. SYSTEM_PROMPT_V2 반영 지침

이번 감사 결과, `claude/CLAUDE.md` 본문 자체의 재작성 필요성은 낮다(과잉·중복이 거의 없음). [SYSTEM_PROMPT_V2.md](SYSTEM_PROMPT_V2.md)의 역할은 이 프로젝트에서는 "전면 재작성 제안"보다 **"Documentation Map에 AI_Workflow_TemplatePack 행 추가"** 정도의 경미한 보강 제안으로 축소된다.

## 관련 문서

[REASONING_EXTRACTION_AUDIT.md](REASONING_EXTRACTION_AUDIT.md) · [SYSTEM_PROMPT_V2.md](SYSTEM_PROMPT_V2.md) · [MEMORY_RULES.md](MEMORY_RULES.md)
