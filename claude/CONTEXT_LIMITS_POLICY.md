# 컨텍스트 한도 기준

> 목적: 모델별 공식 컨텍스트 수치와 이 저장소의 실측 대형 자산 규모를 근거로, 무엇을 읽고 무엇을 읽지 말아야 하는지의 판단 기준을 성문화한다. | 공식 근거: Anthropic 모델 문서(platform.claude.com) + support.claude.com 사용량 가이드, 그 외는 저장소 실측 | 원본: AI_Workflow_TemplatePack(공용) → 2026-07-11 이 프로젝트용으로 이식

---

## 1. 공식 수치 — 확인 필요 시 조회

컨텍스트 윈도·최대 출력 등 모델별 수치는 모델 세대에 따라 바뀌므로 이 문서에 하드코딩하지 않는다. 필요 시 공식 문서(platform.claude.com, support.claude.com)나 `claude-api` 스킬로 조회한다. 다만 일반 원칙은 유지된다:

- 컨텍스트/출력 한도는 "한 턴에 다룰 수 있는 텍스트 총량"의 상한이다. 대형 산출물(다중 파일 수정)은 출력 한도보다 **턴 분할 설계**가 먼저다 — [SUBAGENT_ORCHESTRATION.md](SUBAGENT_ORCHESTRATION.md) 참조.
- Claude Code는 대화가 길어지면 자동 압축(compact)으로 컨텍스트를 관리한다. 압축 트리거의 정확한 임계값은 공식 미확인.

## 2. 이 저장소에서 컨텍스트가 무너지는 지점

Unity 프로젝트 특성상 대형 텍스트 자산은 아래 종류로 실재한다. 정확한 크기는 시점에 따라 바뀌므로, 판단 전 `wc -l`/파일 크기로 실측할 것.

| 자산 종류 | 경로 예 | 판정 |
|---|---|---|
| 씬 파일(.unity) | `Assets/00.Scenes/Boss.unity` 등 | 수천 줄 이상 가능. 전체 Read 대신 Grep으로 오브젝트/컴포넌트 앵커(`--- !u!`, `m_Name:`) 좌표를 먼저 확정 |
| 프리팹(.prefab) | `Assets/01.Scripts/06.UI/Prefab/*.prefab` 등 | 씬과 동일 전략 |
| ScriptableObject 에셋(.asset) | `Assets/08.Data/Boss/BossDB.asset` 등 | 대체로 소형(수십~수백 줄) — 전체 Read 가능. 필드 구조는 정의 클래스(`00.Common/Data/*DataSO.cs`)가 진실 |
| 스프라이트/텍스처/폰트 애셋 | `Assets/05.Sounds/`, 이미지류 | 텍스트로 읽을 대상이 아님 — Read하지 않는다 |
| C# 스크립트 | `Assets/01.Scripts/` 하위 104개 스크립트 | 대부분 수백 줄 이내로 전체 Read 안전 |

- Read 도구는 기본 2,000줄까지만 읽는다. 대형 씬/프리팹을 "일단 Read"하면 앞부분만 보고 판단하게 된다 — 반드시 3절 절차를 따른다.

## 3. 자산 유형별 취급 전략

### 3-1. 씬/프리팹 YAML — 부분 Read + Grep 선행

Unity MCP가 연결돼 있지 않을 때(이번 세션 기본 상태) 씬/프리팹은 텍스트 YAML로 직접 다룬다.

1. **Grep으로 좌표 확정**: `m_Name: <오브젝트명>`, 컴포넌트 스크립트 GUID(`m_Script: {fileID: ..., guid: ...}`), `--- !u!` 앵커(fileID)로 검색해 대상 블록의 줄 번호를 얻는다. 이번 세션에서 실제로 `BossEmotionHUD`의 `EmotionIcon` Transform을 이 방식으로 찾아 수정했다.
2. **Read는 offset/limit으로**: 확정한 줄 번호 주변만 읽는다.
3. **수정 후 반드시 재임포트/Play 확인 요청**: Unity MCP가 연결되면 `refresh_unity`(또는 해당 도구) → `read_console`로 오류를 확인한다. 미연결이면 사용자에게 에디터에서 재확인을 요청한다.
4. **Unity MCP 연결 시**: 씬/프리팹 조회·수정 전용 MCP 도구(hierarchy 조회, 컴포넌트 조회 등)가 YAML 직접 편집보다 안전하면 그쪽을 우선한다.

### 3-2. ScriptableObject 에셋 — 정의 클래스 우선

- `Assets/08.Data/{Player,Boss,Time}/*.asset`은 대부분 소형이라 전체 Read가 안전하다.
- 필드 구조를 알고 싶을 때는 **에셋보다 정의 클래스(`Assets/01.Scripts/00.Common/Data/*DataSO.cs`)를 먼저 읽는다** — 필드는 클래스가 진실이고, 에셋 YAML은 "현재 값"을 확인할 때만 본다.
- 필드를 추가했다면 에셋에도 반드시 대응 값을 기입한다([PROGRESS_CLAIM_POLICY.md](PROGRESS_CLAIM_POLICY.md) 2-3절).

### 3-3. C# 스크립트 전수 탐색

- `Assets/01.Scripts/` 전체는 약 104개 스크립트(2026-07-11 기준, `claude/CLAUDE.md` 명시치 — 변경 시 재확인)로, 시스템별 폴더(`00.Common`~`11.CameraSystem`, `Ex/`)로 나뉜다.
- 패턴 검색만 필요하면 Grep 단독(`output_mode: files_with_matches` → 히트 파일만 후속 Read).
- 특정 시스템(`03.Boss/` 등, 10~30개 파일) 내용 판독은 본 세션에서 직접 순회 가능.
- 저장소 전역(104개 전부) 전수 감사가 필요하면 폴더 단위로 서브에이전트에 위임하는 것을 고려한다(단, 사용자가 요청했거나 명백히 필요할 때만 — 임의 스폰 금지).

## 4. 세션 컨텍스트 예산 운영 수칙

1. **대형 파일 원문을 대화에 남기지 않는다**: 씬/프리팹 YAML을 부분 Read할 때도 필요 블록만.
2. **긴 세션은 이관이 정답**: 한 세션에서 대형 자산 탐색 + 구현 + 검증을 모두 끌고 가면 컨텍스트 압축으로 초기 규칙(코딩 컨벤션, 경계)이 유실된다. 전환 시점 판단은 [CONVERSATION_SPLIT_RULES.md](CONVERSATION_SPLIT_RULES.md), 이관 문서 작성은 [CHECKPOINT_RULES.md](CHECKPOINT_RULES.md)의 PLAN 표준을 따른다.
3. **모델 배분과 연동**: 대형 자산이 얽힌 고난도 탐색은 상위 모델·고effort 구간에 배정하고, 단순 순회·집계는 하위 모델로 내린다 — [FABLE_ONLY_TASKS.md](FABLE_ONLY_TASKS.md) · [EFFORT_POLICY.md](EFFORT_POLICY.md).
4. **읽기 전 항상 자문**: "이 질문의 답이 파일 전체에 있는가, 특정 블록에 있는가?" — 후자라면 Grep 좌표 확정이 먼저다. 이 저장소에서 전체 Read가 대체로 안전한 것은 C# 스크립트와 MD 문서, 소형 GameDB 에셋이다.

## 관련 문서

[EFFORT_POLICY.md](EFFORT_POLICY.md) · [SUBAGENT_ORCHESTRATION.md](SUBAGENT_ORCHESTRATION.md) · [CONVERSATION_SPLIT_RULES.md](CONVERSATION_SPLIT_RULES.md) · [CHECKPOINT_RULES.md](CHECKPOINT_RULES.md)
