# 작업 경계선

> 목적: AI 에이전트가 이 저장소에서 절대 넘지 말아야 할 수정 금지·사전 질문 경계를 출처·리스크·예외 절차와 함께 총정리한다. | 공식 근거: Anthropic 프롬프팅 가이드의 "행동 경계 명시(unrequested action 방지)" 권고 + 프로젝트 내부 규칙 | 원본: AI_Workflow_TemplatePack(공용) → 2026-07-11 이 프로젝트용으로 재작성(세이브/모듈분리 전제를 소유권/리와인드 전제로 교체)

이 문서는 공식 권고 중 "system state 변경 전 evidence 검증, 실제 수정은 요청 시까지 유보"를 이 저장소의 실제 규칙으로 구체화한 것이다. 아래 경계는 전부 `claude/CLAUDE.md`와 실제 코드 구조에 결부되어 있다.

---

## 1. 경계 총정리 표

| # | 경계 | 출처 | 위반 시 핵심 리스크 |
|---|---|---|---|
| B1 | `Minsung.*` 밖(팀원 명진/진욱 소유) 코드 수정 전 소유자 확인 | `claude/CLAUDE.md` Critical Rule 2 | 팀원 작업 충돌, 담당 영역 침범 |
| B2 | 리와인드 버퍼는 `RewindManager.TickCapacity`만 사용 | `claude/CLAUDE.md` Critical Rule 3 | 참여자 간 인덱스 어긋남 — 리와인드 전체 파손 |
| B3 | 리와인드 참여 오브젝트는 `IRewindable` + Register/Unregister 쌍, 랜덤은 결정 로그, 연출은 풀 활성/비활성 | `claude/CLAUDE.md` Critical Rule 4 | 되감기 후 상태 불일치, 스냅샷 역재생 불가 |
| B4 | 몬스터/보스 FSM 상태 전이 우선순위와 리와인드 정지 규칙 유지 | `claude/CLAUDE.md` Critical Rule 5 | 상태 전이 누락, 리와인드 중 신규 행동 발생 |
| B5 | DontDestroyOnLoad 싱글톤은 `PersistentSingleton<T>` 상속 | `claude/CLAUDE.md` Critical Rule 6 | Awake 직접 구현 시 중복 인스턴스/초기화 순서 버그 |
| B6 | `GameDB.*` 호출은 MonoBehaviour 필드 초기화식/생성자 금지(Awake 이후만) | `claude/CLAUDE.md`, `claude/gamedb.md` | Resources.Load 제약 위반으로 null 참조 |
| B7 | 커밋·푸시는 요청 시만, 선별 스테이징 | Git Safety Protocol(공통 시스템 규칙) | 병행 작업자(팀원, 사용자 IDE 작업) 미완성 변경분 오염 커밋 |
| B8 | `Ex/` 폴더(샘플 코드)를 프로덕션(`Minsung.*`)이 참조 금지 | `claude/CLAUDE.md` Critical Rule 9 | 예제 코드에 대한 암묵적 의존 발생 |

---

## 2. 경계별 상세

### B1. 팀원 소유 코드 수정 전 소유자 확인

- **범위**: `Minsung.*` 네임스페이스 밖의 코드 전체(명진/진욱 담당으로 추정). 이 프로젝트는 폴더/모듈 단위 강제 분리가 아니라 **네임스페이스 기반 소유권** 규칙이다 — 다른 Unity 프로젝트의 "특정 폴더 절대 금지" 방식과 다르다.
- **출처**: `claude/CLAUDE.md` Critical Rule 2.
- **위반 시 리스크**: 팀원이 진행 중인 작업과 충돌하거나, 소유자가 모르는 사이 자기 코드가 바뀌어 혼란을 준다.
- **예외 승인 절차**: 사용자의 **명시적 승인**이 있을 때만. 승인 시에도 어느 파일을 왜 건드리는지 사전에 목록으로 보고한다.

### B2. 리와인드 버퍼 용량은 `RewindManager.TickCapacity`만

- **범위**: 모든 `RingBuffer<T>` 생성 지점. `RecordSeconds / fixedDeltaTime`을 직접 계산해 버퍼를 만드는 모든 코드.
- **출처**: `claude/CLAUDE.md` Critical Rule 3.
- **위반 시 리스크**: 참여자마다 버퍼 크기가 미세하게 달라지면, 되감기 시 인덱스가 어긋나 서로 다른 시점의 스냅샷을 되돌리게 된다 — 조용히 발생하는 심각한 버그.
- **예외 승인 절차**: 예외 없음. 기록 길이(초) 조정이 필요하면 `TimeDB`(`GameDB.Time.RecordSeconds`)에서만.

### B3. 리와인드 참여 오브젝트 규칙

- **범위**: `IRewindable` 구현 전체, 연출용 풀 오브젝트(레이저/장풍/안전구역/낙뢰 등).
- **출처**: `claude/CLAUDE.md` Critical Rule 4.
- **위반 시 리스크**: Register/Unregister 누락 시 리와인드 되감기가 해당 오브젝트를 놓치거나 파괴된 참조에 접근. 랜덤 패턴에 결정 로그가 없으면 되감기 후 다른 결과가 나와 플레이어가 혼란. 연출 오브젝트를 Destroy로 처리하면 되감기 스냅샷 역재생이 불가능.
- **예외 승인 절차**: 예외 없음 — 구조적 요구사항.

### B4. FSM 상태 전이

- **범위**: `Assets/01.Scripts/02.Monster/MonsterState.cs`, `MonsterController.cs`, `Assets/01.Scripts/03.Boss/BossState.cs`, `Phase1State.cs`~`Phase4State.cs`.
- **출처**: `claude/CLAUDE.md` Critical Rule 5.
- **위반 시 리스크**: 상태 전이 우선순위가 바뀌면 공격/추격이 누락되거나 반복되고, 리와인드 중 상태가 신규 행동을 실행하면 기록과 현재 상태가 어긋난다.
- **예외 승인 절차**: 새 상태를 추가할 때는 진입·퇴장·전이 조건과 리와인드 중 동작을 함께 검증한다.

### B5. DontDestroyOnLoad 싱글톤은 `PersistentSingleton<T>`

- **범위**: 씬 간 유지되는 매니저 클래스 전체(`GameManager`, `SoundManager`, `CameraManager`, `ScreenFade` 등).
- **출처**: `claude/CLAUDE.md` Critical Rule 6.
- **위반 시 리스크**: Awake를 직접 구현하면 씬 재로드 시 중복 인스턴스가 생기거나 초기화 순서가 어긋난다.
- **예외 승인 절차**: 씬 로컬 싱글톤(`RewindManager` 등 DontDestroyOnLoad가 아닌 것)은 이 규칙 예외 — `PersistentSingleton<T>`을 상속하지 않는다.

### B6. GameDB 호출은 Awake 이후만

- **범위**: `GameDB.Player/Boss/Time`을 참조하는 모든 코드.
- **출처**: `claude/gamedb.md`, `claude/CLAUDE.md`.
- **위반 시 리스크**: `GameDB`는 Resources.Load 기반이라, MonoBehaviour 필드 초기화식이나 생성자에서 호출하면 로드 순서 문제로 null이 될 수 있다.
- **예외 승인 절차**: 예외 없음. Awake에서 SO 참조를 캐싱(`_playerSo` 등 `So` 어미 변수)한 뒤 사용한다.

### B7. 커밋·푸시는 요청 시만, 선별 스테이징

- **범위**: 모든 `git add`/`git commit`/`git push`.
- **출처**: Git Safety Protocol(시스템 공통 규칙).
- **위반 시 리스크**: 이 저장소는 사용자가 IDE에서 직접 병행 작업할 수 있다(실제로 이번 세션 중 사용자가 커밋 메시지를 IDE에서 직접 수정한 사례가 있었다). `git add -A`는 남의 미완성 작업을 오염 커밋할 수 있다.
- **예외 승인 절차**: 사용자가 "전부 커밋"을 명시적으로 지시한 경우에만 전체 스테이징. 그 외에는 파일 단위 선별 `git add` 후, 스테이징에서 제외한 목록을 보고하는 것까지가 절차. push는 커밋과 별개로 항상 사용자의 명시적 요청이 필요하다.

### B8. `Ex/` 폴더 참조 금지

- **범위**: `Assets/01.Scripts/Ex/`(샘플/예제 코드, 네임스페이스 없음).
- **출처**: `claude/CLAUDE.md` Critical Rule 9.
- **위반 시 리스크**: 프로덕션 코드(`Minsung.*`)가 예제 코드에 의존하면, 예제가 바뀌거나 삭제될 때 프로덕션이 깨진다.
- **예외 승인 절차**: 참고할 구현이 있으면 정식 폴더(`06.UI` 등)로 승격해서 쓴다 — `Ex`를 직접 참조하지 않는다.

---

## 3. 경계 판단이 애매할 때의 기본 동작

1. **분석 요청 ≠ 수정 승인.** "왜 이런가?"류 질문에는 진단 보고까지만 하고 멈춘다.
2. **경계 걸침 여부가 불확실하면 사전 질문.** 특히 `00.Common/`은 여러 시스템이 공유하므로, 공용 코드 수정이 다른 시스템(팀원 코드 포함) 동작을 바꾸는지 먼저 판단해 보고한다.
3. **예외 실행 후에는 반드시 결과 보고에 예외였음을 명기**하고, 반복될 예외라면 메모리 등재를 사용자에게 제안한다.

---

## 4. 관련 문서

- [프로젝트 지침 통합 재작성](PROJECT_INSTRUCTIONS_V2.md) — 이 경계들을 포함한 전체 규칙의 단일 지침서
- [민감정보 기준](DATA_RETENTION_AND_PRIVACY_RULES.md) — Supabase 키 등 업로드 금지 목록 (경계·보안 짝 문서)
- [코드 작업 규칙](CODE_WORKFLOW_RULES.md) — B7(선별 스테이징)의 실행 절차 상세
- [검수 Subagent 사양서](VERIFIER_SUBAGENT_SPEC.md) — B2/B3 리와인드 영향 검증자
- [테스트·검증 표준](TEST_AND_VERIFICATION_STANDARD.md) — 경계 준수 확인 후의 표준 검증 플로우
- [1페이지 운영 매뉴얼](ONE_PAGE_AI_WORKFLOW_MANUAL.md) — 일상 루틴에서의 경계 요약본
