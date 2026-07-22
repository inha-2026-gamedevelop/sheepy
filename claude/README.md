# The Last Re:wind

> 버려진 봉제인형 Sheepy가 잃어버린 기억을 찾아 떠나는 2D 플랫폼 액션 RPG

---

## 📖 스토리

어느 날 눈을 뜬 봉제인형 Sheepy.  
자신이 왜 여기 있는지, 무엇을 잃었는지 모른 채 손에 쥔 것은 단 하나 — **깨진 회중시계**.

Sheepy는 한 아이와 함께 놀던 봉제인형이었다.  
어느 날 버려졌고, 오랜 시간이 지나 살아났다.

시간을 되감아 그 시절로 돌아가려 하지만,  
**되감을수록 기억은 오히려 희미해진다.**

> 보스를 처치하고 숨겨진 방에 들어가면  
> 깨진 액자 안에서 사진 한 장이 나온다.  
> 사진 속엔 아이와 Sheepy가 함께한 모습이 담겨있다.

---

## 🎮 핵심 시스템

### 타임리와인드 `R` ✅ 구현
최근 N초(기본 10초)간의 행동을 역재생 연출로 되감은 뒤 시작점으로 복귀한다.  
복귀 시 **분신**이 그 구간을 정방향으로 재생하며 소환된다.  
되감기 중에는 화면 전체에 **VHS 글리치 오버레이**(`VhsRewindOverlay`)가 재생된다.

- 타임라인 오너는 `RewindManager` (씬당 1개, 없으면 자동 생성)
- 참여자는 전부 `IRewindable`로 등록 — 플레이어/몬스터/보스 패턴이 **같은 틱 기준**으로 기록·역재생
- 버퍼 용량은 `RewindManager.TickCapacity` 하나만 사용 (서로 다르면 되감기 인덱스가 어긋남)

### 분신 ✅ 구현 (소환은 리와인드 경유)
기록된 클립을 정방향으로 재생하는 분신(`CloneController`)을 소환한다.  
클립이 끝나면 마지막 자세로 더미 잔류. 최대 3개까지 유지(`ClonePool` 오브젝트 풀).  
`T` 키로 전체 삭제. 체력은 본체와 동일한 하트 규칙(`PlayerHealth` 공유).  
> `E` 단독 소환은 예정 (현재는 R 되감기 종료 시에만 소환)

### 슬로우모션 `Shift` ✅ 구현
시간 배율을 0.4로 낮춘다 (`SlowMotionController`).  
`fixedDeltaTime`도 같은 비율로 보정해 슬로우 중에도 물리가 부드럽다.  
보스 패턴 회피, 안전구역 확인 등 타이밍이 중요한 순간에 사용.

### 스킬 게이지 `MP` 🔲 예정
리와인드와 슬로우가 공유하는 단일 게이지.  
시간이 지나면 자동 충전되고, 적 처치 시 추가 충전된다.  
게이지가 없으면 스킬 사용 불가. (상수는 `Constants.TimeSystem`에 준비됨)

### 업적 시스템 ✅ 구현
`AchievementManager`(자동 생성 싱글톤) + `AchievementDatabase`(SO 카탈로그) + 토스트 UI.  
첫 되감기 / 분신 최대치 / 보스 1페이즈 돌파 / 보스 처치 4종. PlayerPrefs 저장.

> **분신 없이는 깰 수 없는 보스 패턴**,  
> **슬로우 없이는 피할 수 없는 탄막**이 존재한다.  
> 스킬은 연출이 아니라 필수 생존 수단이다.

---

## ⚔️ 전투

| 입력 | 행동 | 상태 |
|---|---|---|
| `←` `→` | 이동 | ✅ |
| `Space` | 점프 | ✅ |
| `X` | 공격 — 범위 안에 적이 있으면 **오브가 날아가 타격**(Sheepy 스타일), 없으면 근접 히트박스 폴백 | ✅ |
| `R` | 타임리와인드 + 분신 소환 | ✅ |
| `Shift` | 슬로우모션 | ✅ |
| `T` | 분신 전체 삭제 | ✅ |
| `X` 홀드 | 차지공격 (데미지 2.5배) | ✅ |
| `Q` | 포션 사용 (소지 1개 소비, 하트 1칸 회복) | ✅ (2026-07-18) |
| `E` | 상호작용 (레버/라디오/엘리베이터 등) | ✅ |

플레이어 입력은 `PlayerInput`이 읽어 `PlayerMovement`/`PlayerCombat`/`PlayerRewind` 등 각 컴포넌트로 전달한다(코디네이터 `PlayerController`가 조율).  
키는 `Constants.Player`(KEY_JUMP/KEY_ATTACK/KEY_REWIND/KEY_CLEAR_CLONES/KEY_USE_POTION)에서 중앙 관리한다.  
(플레이어 입력은 `PlayerController`, 일반 몬스터는 `MonsterState` FSM, 보스는 `BossState`/`Phase1~4State` FSM이 판단)

### 상태이상 (디버프) ✅ 구현
`PlayerStatusEffectController`가 지속시간형 디버프를 관리한다.  
`Bind`(속박) / `InputInvert`(키반전, 보스 혼란) / `RewindSeal`(되감기 봉인, 보스 4페이즈).  
상태이상 시간은 **리와인드 스냅샷에 넣지 않고 실시간으로 흐른다**(되감기해도 유지). HUD는 `PlayerStatusEffectUI`.

### 사운드 / 카메라 ✅ 구현
`SoundManager`(SFX/BGM, `MapBgmPlayer`로 씬 진입 시 자동 BGM) + `CameraManager`(Cinemachine 우선순위 전환으로 라디오 등 포커스 연출). 둘 다 `PersistentSingleton`.

### 상호작용 ✅ 구현
`IInteractable` + `InteractableRegistry`(Collider2D 조회) 기반. `LeverInteractive`(되감기 재연 지원) / `RadioInteractive`(사운드 토글 + 카메라 포커스). E키 감지는 `PlayerInteractionSensor`, 키 가이드 HUD는 `KeyGuideManager`.
홀드형 상호작용은 `IHoldInteractable`로 별도 계약 - 레버로 해금한 `ElevatorController`(IRewindable)를 `ElevatorButtonInteractive`(3초 홀드)로 호출하는 엘리베이터가 대표 사례.

### 체력 — 하트 방식 (실크송류) ✅ 구현
- 플레이어/분신 공통 `PlayerHealth`: **하트 6개**, 피격 시 무조건 1개 차감
- 피격 후 1초 무적 → 같은 공격에 연속으로 깎이지 않음
- HUD는 `PlayerHeartUI`가 `OnHealthChanged` 이벤트 구독으로 갱신

### 데미지 공식 (대 몬스터/보스)
```
플레이어 공격력 20 → MonsterHealth / BossController.TakeDamage
(방어력 공식 실제 피해 = Max(1, 공격력 - 방어력) 은 장비 시스템과 함께 예정)
```

---

## 👹 보스 — Azathoth (Sheepy 원작 보스 리소스)

Sheepy: A Short Adventure의 보스 리소스를 활용.  
보스1 1,284프레임 / 보스2 4,881프레임의 풍부한 애니메이션 세트.

### 페이즈 구조 — 씬 분할(Boss1/Boss2) + 페이즈 하한 동결 ✅ 1~3P 구현 / 🔧 4P 진행 중

보스는 총 4페이즈이며 **두 개의 씬/시스템으로 분리**되어 하나의 흐름으로 이어진다.

- **Boss1 (`BossController` 계열, Map2, 1~2페이즈, 민성)**: 그 씬이 담당하는 페이즈 구간의 **단일 피통**(`GameDB.Boss.TotalHealth`, `_finalPhaseIndex+1`로 균등 분할)과 페이즈별 `BossState`(상태 패턴)를 관리한다. 페이즈 하한에 닿으면 체력을 동결하고 종료 기믹(`CoPhaseEndGimmick`)을 거쳐 다음 페이즈로 넘어간다.
- **Boss2 (`03.Boss/Boss2/`, Map3, 3~4페이즈, 진욱)**: 별도 오브젝트/DB(`Boss2DataSO`)와 독립 피통(`Boss2Health`)을 갖는 부유체 보스. 원거리 패턴 3종 + 낙인·제단 + 손아귀/공간찢기 등 전용 패턴.

전투 타이머 10분 초과 시 즉사. 페이즈와 별개로 **감정(`BossEmotion`/`Boss2Emotion`)**이 반사/낙뢰/혼란을 변조한다. 상세 기획·구현은 [claude/boss.md](boss.md) 참고.

**기본 수치 / 공통 규칙 (기획)**

- 씬(보스 오브젝트)별로 담당 페이즈 구간의 총 피통을 갖고 그 안에서 페이즈 하한 동결 후 종료 기믹 진행 (아래 표의 HP 구간은 4페이즈 균등 분할 기준 예시)
- 피격 규칙: **보스 본체 공격 = 하트 1칸**, **보스 분신 공격 = 하트 반칸**
- 플레이어가 타임 리와인드하면 **보스 HP도 함께 되돌아간다**
- **전투 타이머는 리와인드와 무관하게 계속 흐른다** — 10분 초과 시 플레이어 즉사
- 랜덤 패턴은 **결정 로그**에 저장되어 **리와인드 후에도 같은 패턴이 재현**된다 (Phase1/2/3 공통)
- 해저드는 오브젝트 풀(`BossHazardPool`) + 매 틱 스냅샷 기록으로 역재생 지원

**낙뢰 (`BossLightningPattern`, 공통 패턴)**

- 4초에 한 번씩 맵 랜덤 위치에 위에서 아래로 떨어진다
- 피격 시 하트 1칸 차감 + **0.5초 이동 불가**
- 되감기 시 볼트 정리 후 루프 재시작 (스냅샷 기록은 예정)

**감정 상태 (`BossEmotion`, 페이즈와 별개로 공통 패턴 변조)**

| 감정 | 효과 |
|---|---|
| 검정(Black) | 모든 공격 반사 |
| 하양(White) | 플레이어 **본체** 공격만 반사 |
| 남색(Navy) | **분신** 공격만 반사 |
| 핑크(Pink) | 낙뢰 낙하 비율 x2 |
| 파랑(Blue) | 낙뢰 낙하 비율 /2 + 맵에 하트 1칸 회복 픽업 제공 |
| 화남(Angry) | 3페이즈 고정 — 10초마다 1초간 키반전(혼란), 혼란 상태 아이콘 표시 |

반사 판정은 `DamageSource`(Player/PlayerClone)로 구분한다.

**페이즈별 상세**

| 페이즈 | 씬 (담당) | 내용 | 상태 |
|---|---|---|---|
| 1페이즈 | Map2 (Boss1) | 보스 분신 2체(각 독립 피통) 근접전(애니메이터 활용) + 종료 시 즉사 레이저 색 암기 기믹 | ✅ 구현 |
| 2페이즈 | Map2 (Boss1) | 본체(BossBodyController) 등장, 1P 분신 근접 패턴 + 장풍(맵 아래->위 상승 해저드, 애니메이션 포함) + 종료 시 아웃트로 영상 후 Map3 전환 | ✅ 구현 |
| 3페이즈 | Map3 (Boss2) | 화남 감정 고정 + 원거리 패턴(낙뢰/강타/레이저) + 낙인 스택·정화 제단(7스택 즉사, 제단 E키 홀드로 초기화) | ✅ 구현 |
| 4페이즈 | Map3 (Boss2) | 화남 감정 고정 + 3P 패턴 + 전용 패턴(손아귀, 공간찢기=체력10% 즉사기, 원혼방출). 리와인드 봉인 규정은 폐기(4P도 되감기 정상 사용) | 🔧 구현 진행 중 |

- **1페이즈 즉사 레이저 기믹**: 분신 2체를 모두 잡으면 맵 전체에 레이저(빨강/파랑/초록)를 랜덤 순서로 3회 발사한다. 안전 구역은 **슬로우모션 중에만** 보이며 레이저 발사 시점까지만 표시된다. 5초 후 같은 순서로 전방에 다시 발사 — 레이저 색상에 맞지 않는 구역에 있으면 즉사. 파훼 성공 시 보스 본체가 필드에 등장(2페이즈 시작)
- **2페이즈 종료**: 컷신 등장 후 씬 변경으로 맵 전환
- **3페이즈 레이저**: 발사 전 1.5초간 빨간색 깜빡임 위험 표시 후 1초간 발사. 시작 방향과 도착 지점은 랜덤
- **4페이즈 공간찢기**: 체력 10% 도달 시 1회, 맵 흑백 전환 + 슬로우 후 5회 연속 돌진(로스트아크 영전류) — 전용 무적키로 파훼. 상세는 [claude/boss.md](boss.md) 4-4절

---

## 📦 아이템 ✅ 구현 (LP + 포션)

> 장비/강화석/골드/MP포션/회중시계(이스터에그)는 2026-07-11 범위 제외 확정. 아이템은 LP(수집 카운터) + 포션(회복 소비 아이템) 2종으로 단순화.

| 아이템 | 획득처 | 효과 | 상태 |
|---|---|---|---|
| LP | 몬스터 처치 확률 드랍(자석 픽업) | 단순 수집 카운터 (사용처 미정) | ✅ |
| 포션 | 몬스터 처치 확률 드랍(자석 픽업) | 소지(최대 3개) 후 `Q`로 사용 - 하트 1칸 회복 | ✅ (2026-07-18) |

두 아이템 모두 `LpManager`/`PotionManager`(`12.Item/`, `IRewindable`, 자동 생성 싱글톤)가 드랍 확률/자석 이동/획득/개수를 관리하며, 되감기 시 개수와 필드에 남은 픽업 오브젝트가 함께 복원된다. 밸런싱은 `GameDB.Lp`/`GameDB.Potion`(`LpDataSO`/`PotionDataSO`).

---

## 🌐 온라인 랭킹 ✅ 클라이언트 구현

Supabase 기반 온라인 랭킹 시스템 (`SupabaseClient` — REST 래퍼 완성).  
닉네임 등록 / 점수 제출 / Top 10 리더보드 / 1등 **고스트 리플레이** 조회 지원.  
보스 클리어 타임(`GameManager.StartRunTimer/StopRunTimerMs`)을 기준으로 Top 10을 나열한다.  
> 게임 플로우 연동(클리어 시 자동 제출, 랭킹 UI)은 Week 3 예정.

---

## 🗂️ 폴더 구조 (01.Scripts 실제 기준)

```
Assets/
├── 00.Scenes/          씬 파일 (MainMenu / Nickname / Map1~4 / Loading / Pause + Minsung/Jinwook 테스트 씬)
├── 01.Scripts/         (총 180개 스크립트, 번호 접두사, 전부 Minsung.* 프로덕션 코드 - 샘플 폴더 Ex/는 2026-07-18 제거)
│   ├── 00.Common/      GameManager / SaveManager·SaveData / RespawnManager / PauseController / OneWayPlatform / DamageSource / IDamageable / Constants(분할) / Data(GameDB + Boss2DataSO) / Utility
│   ├── 01.Player/      코디네이터(PlayerController) + Input/Movement/Combat/Interaction/Rewind/StatusEffect / Health(하트) / Orbs / Animator / HeartUI / 상호작용 센서
│   ├── 02.Monster/     MonsterController / MonsterHealth / Animator(사망 모션 포함) / FSM(순찰·추격·공격 상태)
│   ├── 03.Boss/        Boss1(BossController 단일 피통 + Phase1~4State FSM / 감정 / 근접유닛 / 낙뢰·해저드 풀 / DamageHazard) + Boss2/(진욱, Map3 3~4페이즈 별도 시스템 - 부유 이동/체력/원거리 패턴/감정/낙인·제단/공간찢기·손아귀)
│   ├── 04.TimeSystem/  RewindManager / RingBuffer / 커맨드(Tick·Move·Attack·Interact·Anim) / 분신 / 슬로우
│   ├── 05.Interactive/ IInteractable / IHoldInteractable / 레지스트리 / BaseInteractive / Lever(+LightSwitch) / Radio / Elevator(Controller·Button·Manager) (+Editor)
│   ├── 06.UI/          BossHealthBarUI(Slider) / KeyGuide / Caption(자막) / 상태이상 UI / SpriteReference / Loading·Pause·MainMenu 컨트롤러
│   ├── 07.Achievement/ 업적 (Manager / Database SO / Toast UI / Ids)
│   ├── 08.Backend/     Supabase 클라이언트 / 모델 / 고스트 프레임
│   ├── 09.Visual/      VHS 오버레이 / 글로우 / 그림자 / 페이드 / 스피드라인 / 파티클 / URP 세팅 / 보스 아웃트로 영상
│   ├── 10.Sound/       SoundManager / MapBgmPlayer / SoundData(SO) (+Editor)
│   ├── 11.CameraSystem/ CameraManager (Cinemachine 포커스 전환)
│   └── 12.Item/        LP(수집 재화)/포션(회복 소비) 드랍/자석픽업/카운트 - LpManager·PotionManager(IRewindable) / LpPickupPool·PotionPickupPool
├── 02.Prefabs/         프리팹 (Player/Clone/Boss/Monster/FX/UI)
├── 03.Images/          스프라이트 (Sheepy 리소스 기반)
├── 04.Models/          머티리얼 / 텍스처
├── 05.Sounds/          BGM / SFX
├── 06.Animations/      애니메이션 클립 / 컨트롤러
├── 07.Animator/        Animator Controller / Behavior Graph 에셋
├── 08.Data/            ScriptableObject (업적 / 사운드 등)
├── 09.Shaders/         Sheepy 셰이더 스택 (캐릭터/배경/안개/갓레이)
└── StreamingAssets/    KEY.txt (Supabase 키)
```

### 아키텍처 한눈에

```
[입력]  플레이어 = PlayerInput / 몬스터 = FSM (MonsterState) / 보스 = FSM (BossState, Phase1~4State)
           v Request* 호출
[몸통]  PlayerMovement·Combat / MonsterController  -- 물리, 애니메이션
           v RecordTick / ApplyRewindTick (IRewindable: PlayerRewind, Monster, Boss, Clone, Lever)
[시간]  RewindManager (타임라인 오너, 씬당 1개 자동 생성)
           v 되감기 종료 시 버퍼 전달
[분신]  ClonePool -> CloneController (커맨드 클립 정방향 재생: 이동/공격/상호작용)
```

---

## 🛠️ 기술 스택

| 분류 | 내용 |
|---|---|
| 엔진 | Unity 6000.4.7f1 (2D URP 17.4) |
| 언어 | C# (C# 10) |
| 주요 패키지 | Unity Behavior 1.0.16 / Cinemachine 3.1 / Input System 1.19 / Newtonsoft Json |
| 백엔드 | Supabase (PostgreSQL + REST API) |
| 버전 관리 | Git (main / develop / feature 브랜치 전략) |
| 협업 | Notion (기능 투두리스트, GDD, 코딩컨벤션) |
| 리소스 | Sheepy: A Short Adventure 리소스 추출 활용 |

---

## 👥 팀 구성

| 이름 | 역할 | 담당 |
|---|---|---|
| 민성 | 리드 프로그래머 | TimeSystem, Combat, Equipment, Item, Boss, Backend, Visual, Camera, GameManager, AudioManager, SaveManager |
| 명진 | 유니티 / 레벨 | , , UI 전체, 씬 구성, 레벨 디자인 |
| 진욱 | 몬스터 / QA | 플레이어, 몬스터 AI, QA 전체 |

---

## ⚙️ 설치 및 실행

### 요구 사항
- Unity 6.x
- Universal Render Pipeline (URP) 패키지

### 설정
1. 레포지토리 클론
```bash
git clone https://github.com/[repo-url]/the-last-rewind.git
```

2. `Assets/StreamingAssets/KEY.txt` 생성 후 Supabase 키 입력  
   (키 이름은 `SupabaseClient.LoadKeys`가 파싱하는 아래 형식 그대로. `#`로 시작하는 줄은 주석)
```
URL=https://xxxx.supabase.co
ANON_KEY=xxxx
```

3. Unity에서 프로젝트 열기 → `00.Scenes/MainMenu` 씬 실행

---

## 📋 코딩 컨벤션

- 중괄호: Allman 스타일 (다음 줄) + **모든 제어문에 중괄호 필수** (단일 라인 예외 없음)
- 증감: 전위 (`++i`, `--i`)
- 멤버 변수: `_camelCase`
- 상수: `UPPER_SNAKE_CASE`
- 매직넘버: `Constants.*.cs` 파일에서 중앙 관리
- 네임스페이스: `Minsung` (민성 작성 코드 전용)
- Inspector 노출: `[SerializeField] private` (public 금지)
- 런타임 GC 최소화: `WaitForSeconds`/머티리얼/컴포넌트 참조는 캐싱, `TryGetComponent` 사용
- DontDestroyOnLoad 싱글톤은 `Utility/PersistentSingleton<T>` 상속

자세한 내용 → [claude/coding-convention.md](coding-convention.md) / [코딩컨벤션 노션 페이지](https://app.notion.com/p/38d7458fa94780f7901efb94df7d3c4d)

---

## 📄 관련 문서

| 문서 | 링크 |
|---|---|
| GDD | [Notion](https://app.notion.com/p/38c7458fa94781a6a952da1d433cd296) |
| 코딩컨벤션 | [Notion](https://app.notion.com/p/38d7458fa94780f7901efb94df7d3c4d) |
| 기능 투두리스트 | [Notion](https://app.notion.com/p/5ed0d06c9dbd4dc5807443e60099d05c) |
| Sheepy 리소스 분석 | [Notion](https://app.notion.com/p/38c7458fa9478119a1f4f3884816fa2e) |
| 보스 기획서 | [Notion](https://app.notion.com/p/3887458fa94781c19084ec043e80f688) |
