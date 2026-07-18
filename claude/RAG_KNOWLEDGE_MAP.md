# 지식 검색 맵

> 목적: 자주 나오는 질문 유형과 키워드를 문서/코드 진입점에 매핑해, 세션이 잘못된 경로를 헤매지 않게 한다. | 2026-07-11 재생성. 새 함정/질문 유형 발견 시 갱신.

## 검색 3계층 — 어디부터 찾는가

1. **인덱스 문서** — [MASTER_FILE_INDEX.md](MASTER_FILE_INDEX.md)(코드) / [PROJECT_KNOWLEDGE_INDEX.md](PROJECT_KNOWLEDGE_INDEX.md)(문서)에서 진입점 확인
2. **본 맵의 매핑표** — 질문 유형·키워드가 표에 있으면 지정 경로로 직행
3. **Glob/Grep 직접 탐색** — 위 두 계층에 없을 때만. 탐색 결과 재사용 가치가 있으면 본 맵에 행을 추가한다

## 질문 유형 → 참조 문서/코드 매핑표

| 질문 유형 | 1차 진입점 | 주의·함정 |
|---|---|---|
| 리와인드 버퍼를 건드려도 되나 | `claude/CLAUDE.md` Critical Rule 3 + [ACTION_BOUNDARIES.md](ACTION_BOUNDARIES.md) | `RewindManager.TickCapacity` 외 직접 계산 절대 금지 |
| 보스 페이즈/기믹 로직은 어디 있나 | `Assets/01.Scripts/03.Boss/Phase1~4State.cs` | Phase3/4State는 Phase2State를 상속(장풍 패턴 공유) |
| GameDB에 새 밸런싱 값을 추가하려면 | `claude/gamedb.md` | `*DataSO.cs` 필드 추가 + 대응 `.asset`에 값 기입 둘 다 필요. MonoBehaviour 필드 초기화식에서 호출 금지 |
| 커밋 메시지 형식이 뭔가 | `claude/commit-convention.md` | `type: Scope 한국어 제목` — 괄호 없음 주의(`type(scope):` 아님) |
| 코딩 컨벤션이 궁금하다 | `claude/coding-convention.md` | 이 프로젝트는 `go`/`tr`/`img` 같은 타입 접두사 컨벤션을 **쓰지 않는다** — 다른 Unity 프로젝트 관례와 혼동 금지 |
| 팀원 코드를 고쳐도 되나 | `claude/CLAUDE.md` Critical Rule 2 + [ACTION_BOUNDARIES.md](ACTION_BOUNDARIES.md) | `Minsung.*` 밖은 명진/진욱 소유 — 수정 전 소유자 확인 |
| Unity MCP로 에디터를 직접 조작하고 싶다 | `claude/PLAN.md`(연결 정보) | 이 세션에 연결 안 돼 있으면 씬/프리팹은 YAML 직접 편집, 컴파일/Play는 사용자 확인 요청으로 대체 |
| 테스트/검증을 어떻게 하나 | [TEST_AND_VERIFICATION_STANDARD.md](TEST_AND_VERIFICATION_STANDARD.md) | MCP 연결 여부에 따라 경로가 갈린다 |
| 감정/낙뢰/반사 로직이 실제로 발동하는지 | `BossController.CoEmotionLoop`, `BossEmotionExtensions` | 로직 존재 ≠ 발동 경로 존재 — `SetEmotion` 호출처를 직접 확인할 것 |
| 세이브 시스템 관련 작업 | **해당 없음** | 이 프로젝트는 로컬 세이브가 없다. Supabase는 랭킹/고스트 리플레이 백엔드일 뿐 |
| 로컬라이제이션(다국어) 관련 작업 | **해당 없음(2026-07-11 기준 미확인)** | 다국어 시스템 존재가 확인되지 않음 — 필요하면 먼저 Grep으로 실존 확인 |
| 브랜치 전략이 뭔가 | [CODE_WORKFLOW_RULES.md](CODE_WORKFLOW_RULES.md) | git-flow, `feature/<기능명>`(인원별 아님) |

## 키워드 → 진입점 파일 역인덱스

| 키워드 | 파일/문서 | 비고 |
|---|---|---|
| 리와인드, 되감기, TickCapacity | `Assets/01.Scripts/04.TimeSystem/RewindManager.cs` | 버퍼 용량의 유일한 기준 |
| 슬로우모션, Shift | `Assets/01.Scripts/04.TimeSystem/SlowMotionController.cs` | `HitStopController`와 timeScale 쓰기 협조(코루틴 인터럽트 버그 이력 있음, 2026-07-11 수정됨) |
| 감정, 반사, 낙뢰배율 | `Assets/01.Scripts/03.Boss/BossEmotion.cs`, `BossController.cs`(`CoEmotionLoop`) | |
| 씬 이관, 페이즈 전환 | `Assets/01.Scripts/03.Boss/BossHandoff.cs`, `Phase2State.CoPhaseEndGimmick` | |
| GameDB, SO DB, 밸런싱 | `Assets/01.Scripts/00.Common/Data/GameDB.cs`, `claude/gamedb.md` | |
| 하트, 체력 | `PlayerHealth.cs`(하트 6개, 반칸 단위) | `HALVES_PER_HEART` 상수 참고 |
| 스킬(에이전트 운영) | `.claude/skills/clarify-ambiguous-request/SKILL.md` | 요청 모호 시 조사 후 되묻는 규칙 |

## 탐색 함정 (실존하지 않거나 오해하기 쉬운 이름)

| 함정 이름 | 실제 상태 |
|---|---|
| `claude/codingconvention.md`(하이픈 없음) | 실제 파일명은 `claude/coding-convention.md`. 2026-07-11 이전 문서에 잘못된 참조가 다수 있었으나 전부 수정됨 |
| 세이브 시스템, `SaveManager`, `SaveData` | 이 프로젝트에 존재하지 않음. 게임 상태 저장 개념 자체가 없다(Supabase는 랭킹/리플레이 백엔드) |
| 타입 접두사 컨벤션(`go`/`tr`/`img` 등) | 이 프로젝트의 컨벤션이 아니다 — `claude/coding-convention.md`에는 그런 규칙이 없음. 다른 프로젝트 습관을 가져오지 말 것 |
| Player 전용 Behavior Tree | 제거됨 — 플레이어 입력은 `PlayerController`, 일반 몬스터는 `MonsterState` FSM, 보스는 `BossState` FSM이 직접 처리(`claude/CLAUDE.md` 참고) |

## claude.ai Projects RAG와 이 맵의 관계

Projects 지식창고에 이 폴더를 올린 경우 RAG가 유사 문서를 자동 인출하지만, **경로의 정확성은 보장하지 않는다**. 코드 착지에는 본 맵과 인덱스 2종이 우선이며, RAG 인출 결과는 교차 확인용으로 쓴다.

## 맵 유지보수 규칙

- 3계층에서 2회 이상 헤맨 질문은 매핑표에 행을 추가한다.
- 파일 이동·리네임 시 관련 행을 같은 커밋에서 갱신한다(실사례: `coding-convention.md` 파일명 불일치를 이번에 정정하며 이 문서에도 함정으로 남김).

## 재생성 방법 (필요 시)

```
RAG_KNOWLEDGE_MAP.md를 이 저장소 기준으로 다시 생성해줘.
- 최근 세션에서 자주 나온 질문 유형 10~15개를 뽑아 1차 진입점을 매핑하고,
- 키워드 역인덱스와 "실존하지 않는 이름" 함정 목록을 Glob/Grep 실측으로 채워.
```
