# The Last Re:wind

버려진 봉제인형 Sheepy가 시간을 되감아 잃어버린 기억을 찾아 떠나는 2D 플랫폼 액션 RPG.

원작 : `sheepy: a short game`

---

## 소개

어느 날 눈을 뜬 봉제인형 Sheepy. 손에 쥔 것은 깨진 회중시계 하나뿐, 자신이 왜 여기 있는지도 무엇을 잃었는지도 모른다.

Sheepy는 한 아이와 함께 놀던 봉제인형이었다. 어느 날 버려졌고, 오랜 시간이 지나 살아났다. 시간을 되감아 그 시절로 돌아가려 하지만, 되감을수록 기억은 오히려 희미해진다.

## 핵심 시스템

- **타임리와인드(`R`)** - 최근 N초간의 행동을 역재생으로 되감은 뒤, 그 구간을 정방향으로 재연하는 분신을 소환한다.
- **분신 소환** - 되감기 종료 시 기록된 커맨드 클립을 그대로 재생하는 분신이 등장한다(최대 3개, `T`로 전체 삭제).
- **슬로우모션(`Shift`)** - 시간 배율을 낮춰 보스 패턴 회피, 안전구역 확인 등 타이밍이 중요한 순간에 대응한다.
- **하트 체력** - 플레이어/분신 공통으로 하트 6개, 피격 시 1개 차감 + 1초 무적.
- **보스** - 단일 피통 + 페이즈 하한 동결 구조(1~3페이즈 구현), 페이즈와 별개로 반사/낙뢰/혼란을 변조하는 감정 상태(`BossEmotion`)를 가진다.
- **업적 / 온라인 랭킹** - PlayerPrefs 기반 업적 4종, Supabase 연동 랭킹(Top 10) 및 1등 고스트 리플레이.

시스템별 구현 현황(완료/예정)과 상세 설계는 [claude/README.md](claude/README.md), [claude/PLAN.md](claude/PLAN.md), [claude/boss-design.md](claude/boss-design.md)를 참고한다.

## 조작법

| 입력 | 행동 |
|---|---|
| `<-` `->` | 이동 |
| `Space` | 점프 |
| `X` | 공격 (범위 안에 적이 있으면 오브 발사, 없으면 근접 히트박스) |
| `R` | 타임리와인드 + 분신 소환 |
| `Shift` | 슬로우모션 |
| `T` | 분신 전체 삭제 |
| `E` | 상호작용 (레버, 라디오 등) |

## 기술 스택

| 분류 | 내용 |
|---|---|
| 엔진 | Unity 6000.4.7f1 (2D URP) |
| 언어 | C# |
| 주요 패키지 | Unity Behavior(BT, 몬스터/보스 전용) / Cinemachine 3.1 / Input System |
| 백엔드 | Supabase (PostgreSQL + REST API) |
| 리소스 | Sheepy: A Short Adventure 리소스 추출 활용 |

## 폴더 구조

```text
Assets/
├── 00.Scenes/      씬 파일 (MainMenu / Loading / Map1~3 / Pause 등)
├── 01.Scripts/     게임 로직
├── 02.Prefabs/     프리팹
├── 03.Images/      스프라이트
├── 04.Models/      머티리얼 / 텍스처
├── 05.Sounds/      오디오
├── 06.Animations/  애니메이션 클립 / 컨트롤러
├── 07.Animator/    Animator Controller / Behavior Graph 에셋
├── 08.Data/        ScriptableObject 데이터베이스 (GameDB, SoundDB, 업적 등)
├── 09.Shaders/     셰이더 스택
└── StreamingAssets/ KEY.txt (Supabase 키)
```
## 팀 구성

| 이름 | 역할 | 담당 |
|---|---|---|
| 민성 | 리드 프로그래머 | TimeSystem, Combat, Equipment, Item, Boss, Backend, Visual, Camera, GameManager, Audio, SaveManager, Player, Boss, Monster |
| 명진 | 레벨 | 맵 레이아웃 구성 |
| 진욱 | 몬스터, 플레이어 | 플레이어, 몬스터 AI, 보스 애니메이터, 보스 로직 수정|
