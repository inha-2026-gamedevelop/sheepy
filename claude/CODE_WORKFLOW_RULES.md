# 코드 작업 규칙

> 목적: 이 저장소에서 AI가 코드를 수정·커밋·푸시할 때 따르는 표준 절차(읽기→수정→확인→보고)와 브랜치·커밋 규약을 성문화한다. | 공식 근거: `claude/commit-convention.md` + Git Safety Protocol(공통 시스템 규칙) + Anthropic 프롬프팅 가이드(진행 보고의 툴 결과 근거화) | 원본: AI_Workflow_TemplatePack(공용) → 2026-07-11 이 프로젝트용으로 재작성("push 커스텀 스킬" 전제를 수동 git 절차로 교체)

---

## 1. 적용 범위와 대전제

- **수정 범위**: `Minsung.*`(민성 소유) + 팀원 코드는 소유자 확인 후. 경계 전체 목록은 [작업 경계선](ACTION_BOUNDARIES.md) 참조.
- **코딩 컨벤션은 `claude/coding-convention.md`가 원본**: 네이밍(`_camelCase`, `*DataSO`/`So` 어미), 매직넘버 금지(GameDB/Constants 분리), 중괄호 필수, 전위 증감, `public` 필드 금지. 수정 전 위반 여부를 스스로 점검한다.
- **이 프로젝트에는 "push 커스텀 스킬"이 없다.** 커밋·푸시는 아래 표준 절차를 수동으로 따른다. push는 사용자의 명시적 요청이 있을 때만(Git Safety Protocol).

## 2. 코드 작업 표준 절차 (4단계)

모든 코드 수정은 아래 순서를 따른다. 단계를 건너뛴 채 "완료"를 선언하는 것은 [진행상황 검증 규칙](PROGRESS_CLAIM_POLICY.md) 위반이다.

### 단계 1 — 수정 전 Read

- 수정 대상 파일은 반드시 **먼저 Read**한다. 씬/프리팹(YAML)도 동일 — Unity MCP가 연결돼 있지 않으면 씬/프리팹은 YAML 직접 편집 방식이다([CONTEXT_LIMITS_POLICY.md](CONTEXT_LIMITS_POLICY.md) 3-1절).
- `claude/HANDOVER.md`에서 작업 영역과 겹치는 미완 항목이 있는지 확인한다.
- 구조 파악이 필요하면 `claude/CLAUDE.md`(아키텍처 절)와 `claude/UML.md`를 먼저 읽는다. 질문 유형별 진입점은 [지식 검색 맵](RAG_KNOWLEDGE_MAP.md) 참조.

### 단계 2 — 수정

- 최소 변경·기존 메커니즘 우선. 특수 케이스 누적, 불필요한 우회는 반려 대상이다.
- GameDB 필드를 추가했다면 `*DataSO.cs` + 대응 `.asset` 양쪽을 함께 수정한다(`claude/gamedb.md`).
- 리와인드 참여 오브젝트를 새로 만들면 `IRewindable` + Register/Unregister 쌍을 확인한다.

### 단계 3 — 컴파일/기능 확인

- **Unity MCP 연결 시**: `read_console`로 **컴파일 에러 0건을 확인한 뒤에** 다음 작업으로 넘어간다.
- **Unity MCP 미연결 시(이번 세션 기본 상태)**: 코드 리뷰로 문법·타입·네임스페이스를 재확인하고, 사용자에게 "에디터에서 컴파일/Play 확인 부탁드립니다"를 명시한다. **"컴파일됩니다"는 확인 없이는 말할 수 없다.**
- 런타임 동작 검증은 [테스트·검증 표준](TEST_AND_VERIFICATION_STANDARD.md), 대규모 변경의 제3자 검수는 [검수 Subagent 사양서](VERIFIER_SUBAGENT_SPEC.md)를 따른다.

### 단계 4 — 보고 (하이퍼링크 필수)

- 수정 위치는 **요청받지 않아도 전부** `[파일:라인](경로#L라인)` 마크다운 링크로 제시한다.
- 라인 번호는 편집 후 최종 위치 기준 — 대량 편집 후에는 Grep으로 최종 라인을 재확인한다(추정 금지). 보고 문체는 [최종 답변 스타일 가이드](FINAL_RESPONSE_STYLE_GUIDE.md) 참조.

## 3. 브랜치 전략 — git-flow

```
main                          # 릴리즈
 └─ develop                   # 통합 브랜치
     └─ feature/<기능명>       # 예: feature/boss-emotion-scene-handoff
```

- **인원별이 아니라 기능별 브랜치**를 쓴다(예: `feature/aurenixs`가 아니라 `feature/boss-emotion-scene-handoff`) — 이번 세션에 확정된 방식.
- `develop`에서 분기해 작업하고, 완료 시 `develop`으로 병합(PR 여부는 사용자 지시에 따름).
- 현재 브랜치가 `develop`/`main`이면 직접 커밋하기 전에 사용자에게 확인하는 것을 권장한다.
- `--force`, `--no-verify`는 사용자 명시 요청 없이는 절대 사용하지 않는다(Git Safety Protocol).

## 4. 커밋은 사용자 요청 시에만 — 선별 스테이징

이 저장소는 사용자가 IDE에서 직접 병행 작업할 수 있다(실제 사례: 이번 세션 중 사용자가 커밋 메시지를 IDE Source Control에서 직접 수정·재커밋한 적이 있다).

1. `git add .` / `git add -A`는 지양 — 이 세션에서 Edit/Write한 파일만 `git add <파일>`로 선별 스테이징한다. 단, 사용자가 "전부 커밋해줘"라고 명시하면 예외.
2. 스테이징 전 `git status`로 전체 변경 목록을 확인하고, 이 세션 작업이 아닌 변경이 섞여 있으면 보고에 "제외된 파일" 목록을 명시한다.
3. 판단 기준: 이 세션의 작업 이력(Edit/Write 호출)에 없는 변경 = 기본 제외.
4. **push는 커밋과 별개로 항상 사용자의 명시적 요청이 필요하다** — "커밋만 하고 푸시하지 마"처럼 범위를 좁히는 지시가 있으면 그대로 따른다.

## 5. 커밋 메시지 규칙 (`claude/commit-convention.md` 원문 기준)

### 타입 (정의 외 타입 사용 금지)

| Type | 용도 |
|---|---|
| **feat** | 새 기능 추가 |
| **fix** | 버그 수정 |
| **refactor** | 동작 변경 없는 구조 개선 |
| **docs** | 문서 추가/수정 |
| **style** | 포맷팅, 동작 변경 없음 |
| **test** | 테스트 코드 추가/수정 |
| **chore** | 빌드/에셋/설정 등 |
| **perf** | 성능 개선 |

### 포맷 — 괄호 없음 주의

```text
type: Scope 한국어 제목

본문 (선택, "왜"에 집중)
```

- `type(scope): 제목`이 **아니라** `type: Scope 제목`(콜론 뒤에 Scope와 제목을 공백으로 이어씀). 다른 프로젝트 관례(괄호형)와 혼동하지 말 것 — 이 프로젝트에서 사용자가 명시적으로 지적한 부분이다.
- Scope는 `Assets/01.Scripts/` 폴더명 기준(Player/Boss/TimeSystem/Common 등). 여러 영역에 걸치면 생략 가능.
- 제목은 한국어 서술형, 완료형(~추가/~수정/~정리), 마침표 없음.
- **커밋 메시지에 AI 공저자(`Co-Authored-By: Claude ...`) 표기를 넣지 않는다** — 이 프로젝트에서 사용자가 명시적으로 요청. 다른 프로젝트의 기본 관례와 다르므로 특히 주의.

## 6. 커밋 실행 시 셸 함정 (Windows 환경)

- **Bash 툴에서 PowerShell here-string(`@'...'@`) 금지.** Bash 툴은 POSIX sh라서 `@`가 리터럴로 파싱되어 커밋 제목이 깨질 수 있다.
- 멀티라인 커밋 메시지는 다음 중 하나만 사용:
  1. 진짜 bash heredoc: `git commit -m "$(cat <<'EOF' ... EOF)"`
  2. 파일 + `-F`: 메시지를 스크래치패드 파일에 Write 후 `git commit -F <파일>`
  3. PowerShell **툴**을 쓸 때만 `@'...'@` here-string이 유효(닫는 `'@`는 반드시 0열)
- 커밋 전 HANDOVER 갱신 규칙(`claude/CLAUDE.md` Critical Rule 11): **커밋 전 `claude/HANDOVER.md`를 최신 상태로 갱신**하고, 커밋 메시지는 `claude/commit-convention.md` 형식을 따른다.

## 7. 관련 문서

- [테스트·검증 표준](TEST_AND_VERIFICATION_STANDARD.md) — 컴파일 확인 이후의 런타임 검증
- [진행상황 검증 규칙](PROGRESS_CLAIM_POLICY.md) — "완료" 선언 전 필수 증거
- [검수 Subagent 사양서](VERIFIER_SUBAGENT_SPEC.md) — 컴파일/컨벤션/리와인드 영향 검수자
- [작업 경계선](ACTION_BOUNDARIES.md) — 팀원 소유 코드 등 경계 총정리
- [체크포인트 규칙](CHECKPOINT_RULES.md) — 커밋 못 하고 세션이 끝날 때의 HANDOVER 갱신
- 원본 규약: `claude/commit-convention.md`
