# Boss2 (Map3, 3~4페이즈 부유 보스) 인수인계

> 작성일: 2026-07-20
> 대상: `Assets/00.Scenes/Jinwook/Map3.unity`의 부유 보스(Boss2) 시스템
> 관련 커밋: `c44a956`(이동) / `2bade9c`(HP UI·히트박스·돌진) / `bd70503`(원거리 패턴)
> **이 문서는 진욱 작업분 전용이다. `claude/PLAN.md`(민성 구현 목록)는 건드리지 않았다.**

---

## 1. 개요

Map3의 보스(3~4페이즈)는 기존 `Minsung.Boss.BossController` 계열과 완전히 별개의 신규 시스템이다.

- **네임스페이스 없음** — 팀 컨벤션(`claude/CLAUDE.md` Critical Rule 2)에 따라 진욱 코드는 네임스페이스를 쓰지 않는다.
- **기존 `Minsung.Boss.*` 코드는 한 줄도 수정하지 않았다.** 재사용 가능한 것(`BossHazardPool`, `DamageHazard`, `IDamageable`, `DamageSource`)은 그대로 참조해서 쓰고, 재사용이 안 되는 것(`BossController` 타입에 고정된 `BossHealthBarUI` 등)만 같은 이름으로 Boss2 폴더에 새로 만들었다.
- 보스는 **부유체** — 중력 없음, `Kinematic Rigidbody2D` + 코드로 직접 위치를 제어한다.

## 2. 스크립트 위치

```
Assets/01.Scripts/03.Boss/Boss2/
├── BossFloatMovement.cs      # 이동 전체 (배회/추적/돌진)
├── Boss2Health.cs            # 체력 (IDamageable)
├── BossHealthBarUI.cs        # 체력바 UI (Minsung.UI.BossHealthBarUI와 동명, 네임스페이스만 다름)
├── Boss2AttackPatterns.cs    # 원거리 패턴 3종 코디네이터
├── Boss2LightningPattern.cs  # 낙뢰
├── Boss2WavePattern.cs       # 강타(장풍)
└── Boss2LaserPattern.cs      # 레이저

Assets/01.Scripts/00.Common/Data/
└── Boss2DataSO.cs            # Boss2 전용 밸런싱 DB (GameDB 트리에는 연결 안 함)
```

- `Boss2DataSO`는 `Assets/08.Data/Boss2/Boss2DB.asset`에 저장. `GameDatabaseSO`(민성 소유, `Minsung.Common.Data`)에는 슬롯을 추가하지 않았다 — 구조 변경은 소유자 확인이 필요해서, 완전히 독립된 SO로 분리했다. 컴포넌트 인스펙터에 직접 드래그해서 연결하는 방식.
- 새 `*DataSO`는 앞으로도 전부 이 폴더(`00.Common/Data/`)에 만들기로 함(사용자 지시).

## 3. 씬 구성 (`Map3.unity`)

```
--------Boss------------------
└── Boss                              (Rigidbody2D Kinematic, BossFloatMovement,
    │                                  Boss2AttackPatterns)
    ├── AttackHitBox                  (BoxCollider2D Trigger + Minsung.Boss.DamageHazard,
    │                                  평소 비활성 — 돌진 중에만 켜짐)
    ├── Visual                        (SpriteRenderer + Animator -> Boss2.controller)
    └── HitCenter                     (BoxCollider2D Trigger + Boss2Health)

GameHUD (Assets/02.Prefabs/UI/GameHUD.prefab 인스턴스)
├── BossUI/BossHealthBar[ON]          (Slider + BossHealthBarUI, Boss2Health 구독)
│   └── PhaseNotch_2만 활성(중앙 50% 표시선), 1/3은 비활성 — 2분할용
└── PlayerHUD/Hearts                  (PlayerHeartUI, 기존 그대로 재사용)
```

- **`HitCenter`**: 원래 `Boss`(루트)에 콜라이더+체력이 있었는데, 플레이어 오브 공격이 루트 피벗(시각적으로 턱 근처)에 꽂히는 버그가 있어서 시각적 중심(스프라이트 world bounds 기준 오프셋 `0.34, 1.07`)에 자식으로 분리했다. `AttackHitBox`도 같은 오프셋으로 옮겨서 돌진 판정이 실제 몸통에서 나가게 했다.
- `Boss` 루트의 `BoxCollider2D`는 **Trigger**로 되어 있다 — 처음엔 non-trigger라 플레이어를 물리적으로 밀쳐냈음.

## 4. 이동 (`BossFloatMovement.cs`)

| 요소 | 설명 |
|---|---|
| 배회 | 스폰 지점 반경(`RoamRadius`) 안 랜덤 지점으로 `SmoothDamp` 이동 → 도착 후 랜덤 대기 → 반복 |
| 추적 | 배회 중심(`_origin`)이 `FollowSpeed`(1/초, 이동 속도보다 훨씬 느림)로 플레이어를 향해 서서히 이동 — "쫓아가되 종속되지 않는" 느낌 |
| 상하/좌우 흔들림 | Sin파, `VerticalAmplitude`/`HorizontalAmplitude` |
| 높이 제한 | `_maxHeightAnchor`(현재 `Mountain_Temple_Portal_19632`) y + `MaxHeightMargin`을 상한으로 클램프 |
| 몸통박치기 (돌진) | `ChargeCooldown`(6초)마다 플레이어가 `ChargeRange` 안이면 시도. 예고 정지(`ChargeTelegraphTime`) → 스냅샷한 목표로 등속 직선 돌진(`ChargeSpeed`) → `AttackHitBox` 활성화(`DamageHazard`가 판정) |
| 물리 보간 | `Rigidbody2D.interpolation = Interpolate` — 없으면 FixedUpdate 틱 사이가 계단식으로 끊겨 보임 (실제로 이 버그가 있었고 고쳤음) |

**리와인드 연동 완료** — `IRewindable` 구현. 매 틱 최종 위치(흔들림 포함)를 `RingBuffer<Vector2>`에 기록/복원한다. 되감기 시작 시 배회/돌진 코루틴 정지(`_isRewinding` 플래그로 `FixedUpdate` 가드), 종료 시 복원된 위치를 새 `_origin`/`_waypoint`/`_baseX`/`_baseY`로 삼아 그 자리에서 재시작. 배회 웨이포인트/돌진 타이밍 자체의 결정 로그는 아직 없어서(랜덤은 리와인드 후 새로 뽑힘) 완전한 프레임 재현은 아니고, 위치만 정확히 되감긴다.

## 5. 체력 / 피격 (`Boss2Health.cs`)

- `IDamageable` 구현 — 플레이어 `AttackHitbox`/`PlayerOrbs`가 `hit.transform`에서 자동으로 찾아 피해를 준다(코드 수정 없이 그냥 인터페이스 구현만으로 연동됨).
- `MaxHealth = 5000`(TODO: 밸런싱/페이즈 확정 전 임시값), 페이즈 개념 없음 — 그냥 0까지 깎이는 단일 피통.
- `OnHealthChanged(current, max)` / `OnDefeated` 이벤트 제공. 사망 연출/처치 처리는 미구현(TODO).
- **리와인드 연동 완료** — `IRewindable` 구현. 매 틱 체력값을 `RingBuffer<float>`에 기록/복원. 되감기 중엔 `TakeDamage`를 차단한다(`_isRewinding` 가드 — 플레이어/몬스터 체력 가드와 동일한 관례).

## 6. UI

- **Player 하트**: `Minsung.Player.PlayerHeartUI` 그대로 재사용(수정 없음). `GameHUD` 프리팹의 `Hearts`가 이미 6칸+스프라이트까지 세팅되어 있어서 인스턴스화만 하면 자동으로 플레이어를 찾아 붙는다.
- **Boss 체력바**: `Minsung.UI.BossHealthBarUI`는 필드 타입이 `Minsung.Boss.BossController`로 고정돼 있어 재사용 불가. 그래서 같은 이름(`BossHealthBarUI`)에 네임스페이스만 뺀 새 클래스를 Boss2 폴더에 만들어 `Boss2Health`를 구독하게 했다. 페이즈 노치 로직(`_phaseNotches`)은 페이즈 개념이 없어서 뺐다.
- 노치는 원래 프리팹 기본값이 3개(25/50/75%, 4페이즈 기준)라 `PhaseNotch_1`/`PhaseNotch_3`을 비활성화하고 `PhaseNotch_2`(50%)만 남겨 2분할로 맞췄다.

## 7. 원거리 패턴 (낙뢰 / 강타 / 레이저)

`Boss2AttackPatterns`가 `Boss`에 붙어 세 패턴을 `Start()`에서 전부 `Play()`, `OnDestroy()`에서 `Dispose()`. 각 패턴은 독립 클래스로, **`Minsung.Boss.BossHazardPool`/`DamageHazard`를 그대로 재사용**한다(이 둘은 `BossController`에 묶여 있지 않은 범용 유틸이라 가능했음).

| 패턴 | 파일 | 동작 | 원본 참고 |
|---|---|---|---|
| 낙뢰 | `Boss2LightningPattern.cs` | 예고(노란 장판) → 강타(즉발, 크랙클 7프레임 순환) → 회수. 낙하 x는 플레이어 주변 랜덤 | `Minsung.Boss.BossLightningPattern` |
| 강타 | `Boss2WavePattern.cs` | 예고 파티클 → 폭발 강타(9프레임 순환, 뒤 5프레임 이후 무판정) → 회수. x는 아레나 전체 랜덤 | `Minsung.Boss.Phase2State`의 장풍 로직 |
| 레이저 | `Boss2LaserPattern.cs` | 빨간 점멸 경고 → 발사(회전 사각 판정) → 좁아지며 회수. 아레나를 가로지름, 시작/도착 높이 랜덤 | `Minsung.Boss.Phase3State`의 레이저 로직 |

**에셋 연결 (원본과 GUID/fileID까지 동일하게 맞춤)**
- 낙뢰 크랙클 7프레임: `Assets/03.Images/Boss/Boss2/zone5_boss2_fxs_ground-sheet0.png`
- 강타 폭발 9프레임: `Assets/03.Images/Boss/Boss2/boss2_fx_explosion-sheet0.png`
- 레이저는 스프라이트가 아니라 **순수 쉐이더 머티리얼** — `Assets/Resources/Phase3LaserBeamMat.mat`(`Minsung/Phase3LaserBeam` 쉐이더)를 `Resources.Load`로 그대로 재사용
- `_lightningColor`는 **흰색(1,1,1,1)** 이어야 함 — 스프라이트 자체가 보라색이고 렌더러 색과 곱해지는 구조라, 틴트를 넣으면 색이 틀어진다(실제로 노란빛을 넣었다가 빨갛게 나오는 버그가 있었음, 수정 완료)

**아레나 경계**는 `Boss2AttackPatterns` 인스펙터에 씬별 SerializeField로 있음(현재 `-10 ~ 10`, 바닥 `y=-3`, 임시값 — 원본도 `BossController`에 동일하게 씬 SerializeField로 둠).

**리와인드 연동 완료 (정지/재시작 수준)** — `Boss2AttackPatterns`가 `IRewindable` 구현. 원본 `BossLightningPattern`과 동일한 수준으로, 프레임 단위 스냅샷은 없고 되감기 시작 시 세 패턴 `Stop()`(정지+회수), 종료 시 `Play()`(재시작)만 한다. 원본 `Phase2State`/`Phase3State`처럼 진행 중이던 예고/강타를 정밀 스크럽하는 것과 결정 로그(랜덤값 리와인드 재현)는 아직 없다.

## 8. 검증 상태

Unity MCP로 Play 모드 진입 후 직접 확인한 것:

- [x] 배회/추적/높이 제한 — 위치 샘플링으로 확인
- [x] 몸통박치기 — 플레이어 체력 12→6 하프하트 차감 확인
- [x] Rigidbody2D interpolation — 처음엔 `Boss`에 Rigidbody2D 자체가 없어서 무의미했던 걸 발견, 추가 후 재확인
- [x] `Boss2Health.TakeDamage` — 리플렉션 호출 + 실제 오브 공격 양쪽으로 체력 감소 확인 (예: 2500→2480, 5000→4960)
- [x] 오브 공격이 `HitCenter`(시각적 중심)에 명중 — 위치 비교로 확인
- [x] Player/Boss HP UI 렌더링 — 스크린샷으로 확인
- [x] 낙뢰/강타/레이저 스폰 및 판정 — 활성 슬롯 스프라이트/머티리얼 이름으로 확인
- [x] 낙뢰 색상 수정 — 슬롯 색상값으로 확인
- [x] 리와인드 — 체력 5000→3500 데미지 후 되감기 -> 5000으로 완전 복원, `_isRewinding` 플래그 정상 해제, 되감기 종료 후 배회 이동도 정상 재개 확인

**주의**: 테스트 중 Unity 에디터가 **Pause 상태**에서는 코루틴이 전혀 진행되지 않아 "공격해도 안 맞는" 것처럼 보인 적이 있었다. 실제 버그가 아니라 Pause 버튼 상태 문제였음 — 재현되면 이것부터 확인할 것.

**중요 — Map3에 `ClonePool`이 없어서 되감기가 전체적으로 깨져 있었다.** `Minsung.Player.PlayerRewind.OnRewindEnd()`가 `_clonePool.Spawn(_buffer)`를 null 체크 없이 호출하는데(`PlayerRewind.cs:161`), Map3엔 `ClonePool` 오브젝트 자체가 없어서(Map2엔 있음) 되감기 종료마다 NullReferenceException이 터졌다. `RewindManager`의 되감기 브로드캐스트 루프에 예외 처리가 없어서, 이 예외 때문에 리스트상 그 뒤에 등록된 리와인더(Boss2Health 등)는 `OnRewindEnd`가 아예 호출되지 않고 있었다. **Map2와 동일하게 `ClonePool` 오브젝트(씬 루트, `Assets/02.Prefabs/Test/clonePrefab.prefab` 연결) + `Player`의 `PlayerRewind._clonePool` 참조를 추가해서 해결** — 코드는 안 건드리고 씬 배치만 채운 것. 이건 Boss2 전용 이슈가 아니라 Map3 전체(플레이어 자신의 분신 소환 포함)에 영향 있던 버그였다.

## 9. 남은 작업 (TODO)

- [ ] 결정 로그 기반 정밀 리와인드 — 지금은 위치/체력만 되감기고, 배회 웨이포인트·돌진 타이밍·낙뢰/강타/레이저 발사 시점은 되감기 후 새로 랜덤하게 결정된다(원본 Phase2/3State의 `_waveXLog`/`_laserLog` 같은 결정 로그가 없음)
- [ ] 페이즈 시스템(3페이즈/4페이즈 전환, 체력 구간별 기믹) — 지금은 단일 피통
- [ ] `MaxHealth`(5000) 등 임시값 전체 밸런싱
- [ ] 사망 연출/처치 처리 (`Boss2Health.OnDefeated` 훅만 있고 실제 연출 없음)
- [ ] 피격 리액션(넉백/플래시 등) — 지금은 체력만 깎임
- [ ] Idle 애니메이션만 있음(Run/Attack/Hit/Death 등 미구현, `Boss2.controller`에 State 추가 필요)
- [ ] 아레나 경계 값(`-10~10`, `y=-3`) 실제 Map3 스테이지 크기에 맞춰 재조정
- [ ] `AttackHitBox`/`HitCenter` 콜라이더 크기·오프셋은 특정 애니메이션 프레임 기준 근사값 — 정밀 조정 필요

## 10. 팀 협업 원칙 (반복 강조)

- `Minsung.*` 네임스페이스 코드는 **절대 수정하지 않는다.** 재사용 가능하면 그대로 참조, 안 되면 Boss2 폴더에 동일한 이름으로 새로 만든다.
- Boss2 관련 신규 `*DataSO`는 전부 `Assets/01.Scripts/00.Common/Data/`에, 나머지 스크립트는 `Assets/01.Scripts/03.Boss/Boss2/`에 둔다.
- `claude/PLAN.md`(민성 구현 목록)는 이 세션에서 수정하지 않았다.
