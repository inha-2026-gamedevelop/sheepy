# 시스템 프롬프트(CLAUDE.md) 재작성안

> 목적: 현행 `claude/CLAUDE.md`를 프롬프팅 원칙(과잉 처방 제거, 목표·제약 중심 기술)에 맞춰 감사하고, 필요한 변경만 제안한다 — **이 프로젝트에서는 원본을 사용자 승인 하에 직접 수정하는 것이 이미 실사례로 확립되어 있으므로(예: 이번 세션의 커밋 컨벤션 규칙 추가), 이 문서는 "제안서"라기보다 "변경 근거 기록"에 가깝다.** | 원본: AI_Workflow_TemplatePack(공용) → 2026-07-11 이 프로젝트용으로 이식

---

## 1. 입력과 전제

이 재작성 감사는 다음 두 입력을 통합한 결과다.

1. **[과잉 지시 삭제 보고](INSTRUCTION_PRUNING_REPORT.md)** — `claude/CLAUDE.md` 11개 Critical Rule 전수 감사. 판정: **중복 0건, 과잉 0건, 사문화 1건(경미 — 스크립트 개수 수치), 승격 0건.**
2. **[CoT 강요 문구 감사](REASONING_EXTRACTION_AUDIT.md)** — CoT 강요 문구 0건 확인. Persona 지정 1건("시니어 Unity 개발자")은 과잉으로 보기 어려운 최소 분량이라 유지 판정.

**결론**: 이 프로젝트의 `claude/CLAUDE.md`는 이미 간결하고(118행), 원본 템플릿이 우려하는 "과잉 처방·사문화 다발" 문제가 애초에 거의 없다. 따라서 이번 재작성안은 **전면 교체가 아니라 최소 보강 1건**만 제안한다.

## 2. 반영된 변경 — AI 운영 문서 30종을 `claude/`에 평탄화 + 자동 로드

### 변경 근거

AI 운영 문서 30종은 처음엔 `claude/AI_Workflow_TemplatePack/` 하위 폴더로 이식됐으나, 사용자가 "그냥 claude 폴더에 옮기고 알아서 잘 실행시키게" 요청해 (1) 하위 폴더 없이 `claude/` 바로 아래 평탄화, (2) `claude/CLAUDE.md`가 [ONE_PAGE_AI_WORKFLOW_MANUAL.md](ONE_PAGE_AI_WORKFLOW_MANUAL.md)를 `@import`로 자동 로드하도록 변경했다. 이제 이 운영 규칙들은 매 세션 시작 시 사용자가 별도로 지시하지 않아도 CLAUDE.md 로드 체인에 함께 실린다.

### 적용된 내용

- `claude/AI_Workflow_TemplatePack/README.md` → `claude/AI_WORKFLOW_README.md`로 이름 변경 후 이동(기존 `claude/README.md`와 충돌 방지).
- 나머지 29개 파일은 원래 이름 그대로 `claude/` 바로 아래로 이동.
- `claude/CLAUDE.md`의 Documentation Map에 `AI_WORKFLOW_README.md`(총 인덱스) 행 추가 + `@claude/ONE_PAGE_AI_WORKFLOW_MANUAL.md` 자동 로드 추가(이 이식 작업의 5단계에서 직접 실행, 사용자 승인 하에 원본 직접 수정 — 이 프로젝트에서 이미 확립된 방식, [MEMORY_RULES.md](MEMORY_RULES.md) 9절 참고).
- 문서 상호 참조는 전부 같은 폴더 내 상대 링크(`[ACTION_BOUNDARIES.md](ACTION_BOUNDARIES.md)` 형태)라 평탄화 후에도 그대로 유효하다.

## 3. 검토했으나 변경하지 않은 것

| 검토 항목 | 판단 |
|---|---|
| Persona 지정("시니어 Unity 개발자") 삭제 | 유지 — 1줄이고 실질 효과(대안 제시 시 근거+추천)가 뒤따라와 장식적 과잉이 아니다 |
| Critical Rules 순서/번호 재부여 | 불필요 — 결번 없이 1~11 연속됨 |
| Architecture 절 압축 | 불필요 — 전부 모델이 코드를 안 봐도 알 수 없는 프로젝트 고유 정보(네임스페이스, 폴더 역할, GameDB 구조) |
| Documentation Map의 기존 8개 행 | 유지 — 전부 경로 실존 확인됨 |

## 4. 모델별 주의 (참고용 — 상시 유효한 사실만)

- 모델 세대가 바뀌면 컨텍스트/가격/effort 체계가 갱신될 수 있다. 정확한 수치는 이 문서에 하드코딩하지 않고 [EFFORT_POLICY.md](EFFORT_POLICY.md)·[CONTEXT_LIMITS_POLICY.md](CONTEXT_LIMITS_POLICY.md)를 참조 시점에 갱신한다.
- 이 프로젝트의 규칙은 특정 모델에 종속되지 않는다(세이브 시스템처럼 모델별로 다르게 다뤄야 할 민감 영역이 없다) — 어떤 Claude 모델이 세션을 맡아도 `claude/CLAUDE.md` + 이 폴더만으로 동일하게 작업 가능해야 한다는 것이 설계 목표다.

## 5. 적용 절차

1. `claude/CLAUDE.md` Documentation Map에 2절의 행 추가(이 이식 작업 5단계에서 실행, 사용자 승인 하에 원본 직접 수정).
2. 이후 CLAUDE.md에 반영할 새로운 사실이 생기면, 변경 폭이 작으면(이번처럼) 이 문서에 근거만 남기고 직접 수정, 변경 폭이 크면(전면 재작성 수준) 이 문서에 제안 전문을 작성한 뒤 별도로 승인받는다.

## 관련 문서

- [과잉 지시 삭제 보고](INSTRUCTION_PRUNING_REPORT.md) / [CoT 강요 문구 감사](REASONING_EXTRACTION_AUDIT.md) — 본 재작성의 두 입력 감사
- [프로젝트 지침 통합 재작성](PROJECT_INSTRUCTIONS_V2.md) — CLAUDE.md 밖 규칙(메모리 feedback 등)의 통합 지침서
- [메모리·교훈 규칙](MEMORY_RULES.md) — feedback 메모리의 CLAUDE.md 승격 절차
- [작업 경계선](ACTION_BOUNDARIES.md) — Critical Rules의 상세 경계
