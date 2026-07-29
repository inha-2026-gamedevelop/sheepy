# The Last Re:wind

버려진 봉제인형 Sheepy가 시간을 되감아 잃어버린 기억을 찾아 떠나는 2D 플랫폼 액션 RPG.

원작 : `sheepy: a short game`

## 플레이 영상



## 소개

어느 날 눈을 뜬 봉제인형 Sheepy. 손에 쥔 것은 깨진 회중시계 하나뿐, 자신이 왜 여기 있는지도 무엇을 잃었는지도 모른다.

Sheepy는 한 아이와 함께 놀던 봉제인형이었다. 어느 날 버려졌고, 오랜 시간이 지나 살아났다. 시간을 되감아 그 시절로 돌아가려 하지만, 되감을수록 기억은 오히려 희미해진다.

## 핵심 시스템

- **타임리와인드(`D`)** - 최근 N초간의 행동을 역재생으로 되감은 뒤, 그 구간을 정방향으로 재연하는 분신을 소환한다. 버퍼 용량은 `RewindManager.TickCapacity` 하나로 통일되어 플레이어/몬스터/보스가 같은 틱 기준으로 기록·역재생된다.
- **분신 소환** - 되감기 종료 시 기록된 커맨드 클립을 그대로 재생하는 분신이 등장한다(최대 3개, `F`로 전체 삭제). 체력은 본체와 동일한 하트 규칙 공유.
- **슬로우모션(`W`)** - 시간 배율을 낮춰 보스 패턴 회피, 안전구역 확인 등 타이밍이 중요한 순간에 대응한다.
- **하트 체력** - 플레이어/분신 공통으로 하트 6개, 피격 시 1개 차감 + 1초 무적.
- **상태이상(디버프)** - 속박(Bind) / 키반전(InputInvert) / 되감기 봉인(RewindSeal). 리와인드 스냅샷에 넣지 않고 실시간으로 흐르므로 되감기해도 유지된다.
- **보스(Azathoth)** - 총 4페이즈, 씬 분할(Boss1: Map2 1~2P / Boss2: Map3 3~4P) 구조. 각 씬이 담당 구간의 단일 피통 + 페이즈 하한 동결을 관리하며, 페이즈와 별개로 반사/낙뢰/혼란을 변조하는 감정 상태(`BossEmotion`)를 가진다. 1~3페이즈 구현 완료, 4페이즈 구현 진행 중.
- **아이템(포션)** - 몬스터 처치 시 확률 드랍, 자석 픽업, 최대 3개 소지 후 `Q`로 사용해 하트 1칸 회복.
- **업적 / 온라인 랭킹** - PlayerPrefs 기반 업적 21종, Supabase 연동 랭킹
  - https://inha2026.netlify.app/


## 조작법

| 입력 | 행동 |
|---|---|
| `<-` `->` | 이동 |
| `Space` | 점프 |
| `Q` | 포션 사용 (하트 1칸 회복) |
| `W` | 슬로우모션 |
| `E` | 상호작용 (레버, 라디오, 엘리베이터 등) |
| `R` | 공격 (범위 안에 적이 있으면 오브 발사 |
| `R` 홀드 | 연속 공격 |
| `S` | 특수키 (4페이즈 공간찢기 회피) |
| `D` | 타임리와인드 + 분신 소환 |
| `F` | 분신 전체 제거 |

## 아키텍처

```
[입력]  플레이어 = PlayerInput (PlayerController가 조율) / 몬스터 = MonsterState FSM / 보스 = BossState, Phase1~4State FSM
           v Request* 호출
[몸통]  PlayerMovement/Combat / MonsterController        -- 물리, 애니메이션
           v RecordTick / ApplyRewindTick (IRewindable: PlayerRewind, Monster, Boss, Clone, Lever)
[시간]  RewindManager   -- 타임라인 오너 (씬당 1개, 자동 생성)
           v 되감기 종료 시 RingBuffer 전달
[분신]  ClonePool -> CloneController   -- 커맨드 클립 정방향 재생 (이동/공격/상호작용 재연)
```

- Player는 코디네이터 패턴: `PlayerController`(파사드/조율) + `PlayerInput`/`PlayerMovement`/`PlayerCombat`/`PlayerInteraction`/`PlayerRewind`/`PlayerStatusEffectController` 컴포넌트.
- 한 틱 = `TickCommand { MoveCommand, AttackCommand?, InteractCommand?, Hearts }`. 공격은 정/역방향 결과가 달라 `Execute`(피해+모션) / `Undo`(모션만)로 분리.

## 기술 스택

| 분류 | 내용 |
|---|---|
| 엔진 | Unity 6000.4.7f1 (2D URP) |
| 언어 | C# |
| AI 판단 | C# FSM (몬스터: `MonsterState`, 보스: `BossState`/`Phase1~4State`) |
| 주요 패키지 | Cinemachine 3.1 (포커스 카메라) / Input System |
| 백엔드 | Supabase (PostgreSQL + REST API) |
| 데이터 관리 | GameDB (ScriptableObject DB, `08.Data/`) - 밸런싱 수치 중앙 관리 |
| 리소스 | Sheepy: A Short Adventure 리소스 추출 활용 |

## 폴더 구조

```text
Assets/
├── 00.Scenes/      씬 파일 (MainMenu / Loading / Map1~3 / Pause 등)
├── 01.Scripts/     게임 로직 (00.Common ~ 12.Item, 번호 접두사, 전부 Minsung.*)
├── 02.Prefabs/     프리팹
├── 03.Images/      스프라이트
├── 04.Models/      머티리얼 / 텍스처
├── 05.Sounds/      오디오
├── 06.Animations/  애니메이션 클립 / 컨트롤러
├── 07.Animator/    Animator Controller 에셋
├── 08.Data/        ScriptableObject 데이터베이스 (GameDB, SoundDB, 업적 등)
├── 09.Shaders/     셰이더 스택
└── StreamingAssets/ KEY.txt (Supabase 키)
```

## 팀 구성

| 이름 | 역할 | 담당 |
|---|---|---|
| 민성 | 리드 프로그래머 | TimeSystem, Combat, Item, Boss(1~2P), Backend, Visual, Camera, GameManager, Audio, SaveManager, Player |
| 명진 | 레벨 | 맵 레이아웃 구성 |
| 진욱 | 몬스터, 플레이어 | 플레이어, 몬스터 AI, 보스(3~4P), 보스 애니메이터 |

