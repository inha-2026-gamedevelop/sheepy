# UML — The Last Re:wind

> `Assets/01.Scripts` 전체(180개 스크립트 - 대부분 `Minsung.*` 네임스페이스, `03.Boss/Boss2/`는 팀 컨벤션상 무네임스페이스. 샘플 코드였던 `Ex/`는 2026-07-18 완전 제거됨)를 기준으로 생성. Mermaid `classDiagram` 문법 사용 (GitHub/대부분의 Markdown 뷰어에서 렌더링됨).
> 클래스당 전체 멤버가 아니라 역할을 보여주는 핵심 멤버만 표기. 코드가 실제 소스이며, 이 문서는 구조 파악용 스냅샷이다.

## 목차

1. [시스템 개요](#1-시스템-개요)
2. [TimeSystem 코어](#2-timesystem-코어-riewind-인프라)
3. [Player (컴포넌트 코디네이터)](#3-player-컴포넌트-코디네이터)
4. [Monster](#4-monster)
5. [Boss](#5-boss)
6. [Interactive](#6-interactive)
7. [Common / Utility (싱글톤 · 공통 인터페이스)](#7-common--utility)
8. [Achievement / Backend / Visual](#8-achievement--backend--visual)
9. [Sound / Camera / UI](#9-sound--camera--ui)

---

## 1. 시스템 개요

```mermaid
flowchart LR
    Player["Minsung.Player\n(Controller + Input/Movement/Combat/Rewind/Interaction/StatusEffect)"]
    Monster["Minsung.Monster"]
    Boss["Minsung.Boss"]
    TimeSystem["Minsung.TimeSystem\n(RewindManager/RingBuffer/ClonePool)"]
    Interactive["Minsung.Interactive\n(Lever/Radio)"]
    Achievement["Minsung.Achievement"]
    Backend["Minsung.Backend"]
    Visual["Minsung.Visual"]
    Sound["Minsung.Sound"]
    Camera["Minsung.CameraSystem"]
    UI["Minsung.UI"]
    Common["Minsung.Common / Utility"]

    Player -- "IRewindable(PlayerRewind)/ICommandActor(PlayerController) 등록" --> TimeSystem
    Monster -- "IRewindable 등록" --> TimeSystem
    Boss -- "IRewindable 등록" --> TimeSystem
    Interactive -- "IRewindable 등록(Lever)" --> TimeSystem
    TimeSystem -- "되감기 종료 시 분신 스폰" --> Player
    Interactive -- "SetInteracting/TriggerLever" --> Player
    Interactive -- "포커스 카메라" --> Camera
    Interactive -- "사운드 토글" --> Sound
    Boss -- "피해/스턴/상태이상/체력 회복" --> Player
    Player -- "Unlock()" --> Achievement
    Boss -- "Unlock()" --> Achievement
    TimeSystem -- "Unlock()" --> Achievement
    Player -- "VHS 오버레이" --> Visual
    Boss -- "화면 페이드 / HP바·감정 HUD" --> UI
    Interactive -- "키 가이드" --> UI
    Common -- "PersistentSingleton 상속" --> UI
    Common -- "PersistentSingleton 상속" --> Camera
    Common -- "PersistentSingleton 상속" --> Sound
    Common -- "PersistentSingleton 상속" --> Achievement
    Common -- "PersistentSingleton 상속" --> Visual
    Backend -.-> Common
```

---

## 2. TimeSystem 코어 (리와인드 인프라)

되감기에 참여하는 모든 클래스(Player/Monster/Boss/Clone/Interactive)가 공유하는 기반.

```mermaid
classDiagram
    class IRewindable {
        <<interface>>
        +RecordTick()
        +OnRewindStart()
        +ApplyRewindTick(int orderedIndex)
        +OnRewindEnd(int orderedIndex)
    }
    class ICommandActor {
        <<interface>>
        +SetPose(Vector2, Vector2, bool)
        +PlayAttack(bool)
    }
    class RewindManager {
        <<MonoBehaviour Singleton>>
        +Instance$ RewindManager
        +TickCapacity$ int
        +IsRewinding bool
        +Register(IRewindable)
        +Unregister(IRewindable)
        +SetRewindEnabled(bool)
        +StartRewind()
    }
    class RingBuffer~T~ {
        +Count int
        +Capacity int
        +Push(T)
        +TryGetOrdered(int, out T) bool
        +CopyOrderedTo(List~T~)
        +Clear()
    }
    class MoveCommand {
        <<struct>>
        +Position Vector2
        +Velocity Vector2
        +Grounded bool
        +Apply(ICommandActor)
    }
    class AttackCommand {
        <<struct>>
        +Execute(ICommandActor)
        +Undo(ICommandActor)
    }
    class TickCommand {
        <<struct>>
        +Move MoveCommand
        +HasAttack bool
        +Attack AttackCommand
        +HasInteract bool
        +Interact InteractCommand
        +Hearts int
    }
    class InteractCommand {
        <<struct>>
        +Target GameObject
    }
    class AnimCommand {
        <<struct>>
        +StateHash int
        +NormalizedTime float
        +FlipX bool
    }
    class ClonePool {
        <<MonoBehaviour>>
        +CanSpawn() bool
        +Spawn(RingBuffer~TickCommand~)
        +Release(CloneController)
        +ClearAll()
    }
    class CloneController {
        <<MonoBehaviour>>
        +Setup(ClonePool)
        +Init(RingBuffer~TickCommand~)
        +TakeDamage(int)
    }
    class SlowMotionController {
        <<MonoBehaviour>>
        +IsSlow$ bool
        +SetSlow(bool)$
    }

    RewindManager "1" o-- "*" IRewindable : 매 FixedUpdate 브로드캐스트
    TickCommand *-- MoveCommand
    TickCommand *-- AttackCommand
    TickCommand *-- InteractCommand
    MoveCommand ..> ICommandActor : Apply()
    AttackCommand ..> ICommandActor : Execute()/Undo()
    ClonePool o-- CloneController : 풀 관리
    ClonePool ..> RingBuffer~TickCommand~ : Spawn(buffer)
    CloneController ..|> ICommandActor
    CloneController ..|> IRewindable
    CloneController *-- RingBuffer~TickCommand~
```

> `RingBuffer<T>`는 이 외에도 `MonsterTick`, `CloneTick`, `BossFrame`, `Phase2Frame`, `Phase3Frame`, `Vector2`, `bool` 등 참여자별로 다른 타입 인자를 갖는다 (제네릭 재사용).

---

## 3. Player (컴포넌트 코디네이터)

2026-07 리팩토링으로 단일 `PlayerController`가 **코디네이터 + 기능 컴포넌트**로 분리됐다. `PlayerController`는 컴포넌트를 연결/조율하고 외부가 참조하는 상태를 파사드로 대표하며, `IRewindable`은 `PlayerRewind`가 구현한다.

```mermaid
classDiagram
    class PlayerController {
        <<MonoBehaviour Coordinator>>
        +IsGrounded bool
        +IsRewinding bool
        +IsStunned bool
        +IsInteracting bool
        +IsInputInverted bool
        +StatusEffects PlayerStatusEffectController
        +OnInputInvertedChanged Action~bool~
    }
    class PlayerInput {
        <<MonoBehaviour>>
        +IsInverted bool
    }
    class PlayerMovement {
        <<MonoBehaviour>>
        +IsGrounded bool
        +IsStunned bool
        +RequestMove(float)
        +RequestJump()
        +ApplyStun(float)
    }
    class PlayerCombat {
        <<MonoBehaviour>>
        +RequestAttack()
        +PlayAttack(bool)
    }
    class PlayerRewind {
        <<MonoBehaviour>>
        +IsRewinding bool
        +RequestRewind()
        +RequestClearClones()
    }
    class PlayerInteraction {
        <<MonoBehaviour>>
        +IsInteracting bool
        +SetInteracting(bool)
    }
    class PlayerStatusEffectController {
        <<MonoBehaviour>>
        +Apply(StatusEffectType, float)
        +Clear(StatusEffectType)
        +IsActive(StatusEffectType) bool
    }
    class StatusEffectType {
        <<enum>>
        Bind
        InputInvert
        RewindSeal
    }
    class PlayerHealth {
        <<MonoBehaviour>>
        +MaxHalves int
        +CurrentHalves int
        +IsInvincible bool
        +OnHealthChanged Action~int,int~
        +OnDeath Action
        +TakeDamage(int)
        +Heal(int)
        +RestoreHalves(int)
    }
    class PlayerHeartUI {
        <<MonoBehaviour>>
        +Redraw(int, int)
    }
    class PlayerOrbs {
        <<MonoBehaviour>>
        +TryAttackNearest() bool
    }
    class OrbController {
        <<MonoBehaviour>>
        +Init(Transform, Vector2, float)
        +Attack(Transform, Action)
    }
    class PlayerAnimator {
        <<MonoBehaviour>>
        +SetLocomotion(float, bool)
        +TriggerJump()
        +TriggerDoubleJump()
        +TriggerAttack()
        +TriggerLever()
        +SetReversed(bool)
    }
    class PlayerInteractionSensor {
        <<MonoBehaviour>>
        +SetSensorActive(bool)
        +ClearCurrentTarget()
    }
    class AttackHitbox {
        <<MonoBehaviour>>
        +Spawn(Vector2, float, DamageSource, PlayerHealth)$
    }
    PlayerController ..|> ICommandActor
    PlayerController *-- PlayerInput : RequireComponent
    PlayerController *-- PlayerMovement
    PlayerController *-- PlayerCombat
    PlayerController *-- PlayerRewind
    PlayerController *-- PlayerInteraction
    PlayerController *-- PlayerStatusEffectController
    PlayerController *-- PlayerHealth : TryGetComponent
    PlayerRewind ..|> IRewindable
    PlayerRewind --> ClonePool : 분신 소환
    PlayerRewind --> RewindManager : Register/Unregister
    PlayerInput --> PlayerMovement : 입력 전달
    PlayerInput --> PlayerCombat
    PlayerInput --> PlayerRewind
    PlayerCombat --> PlayerOrbs
    PlayerCombat ..> AttackHitbox : 근접 폴백
    PlayerMovement --> PlayerAnimator
    PlayerStatusEffectController ..> StatusEffectType
    PlayerHeartUI --> PlayerHealth : OnHealthChanged 구독
    PlayerOrbs o-- OrbController : 오브 인스턴스
    PlayerOrbs ..> IDamageable : 최근접 대상 탐색
    PlayerInteractionSensor --> PlayerController
    PlayerInteractionSensor ..> IInteractable
    AttackHitbox ..> IDamageable : 피해 적용
    AttackHitbox --> PlayerHealth : attacker 참조
```

> 입력은 `PlayerInput`이 읽어 각 컴포넌트로 전달한다(Player BT 제거, 2026-07-06). 상호작용 E키만 `PlayerInteractionSensor` 담당. 트리거 판정(HeartPickup/DamageHazard)이 "본체"를 루트 `PlayerController`로 식별하므로 `ICommandActor`는 코디네이터에 남긴다.

---

## 4. Monster

```mermaid
classDiagram
    class MonsterController {
        <<MonoBehaviour>>
        +PlayerTarget Transform
        +SpawnPosition Vector3
        +CurrentStateType MonsterStateType
        +ChangeState(MonsterStateType)
        +RequestMove(float)
        +RequestChaseMove(float)
        +RequestStop()
        +RequestAttackPlayer()
    }
    class MonsterHealth {
        <<MonoBehaviour>>
        +CurrentHealth float
        +IsDead bool
        +OnDeath Action
        +OnDamaged Action
        +TakeDamage(float, DamageSource, PlayerHealth)
        +RestoreHealth(float)
    }
    class MonsterAnimator {
        <<MonoBehaviour>>
        +SetLocomotion(float)
        +TriggerAttack()
        +TriggerHit()
        +TriggerDeath()
        +SetReversed(bool)
    }
    class MonsterState {
        <<abstract>>
        +Enter()
        +Exit()
        +FixedTick()
    }
    class MonsterPatrolState
    class MonsterChaseState
    class MonsterAttackState

    MonsterController ..|> IRewindable
    MonsterHealth ..|> IDamageable
    MonsterController *-- MonsterHealth : TryGetComponent
    MonsterController *-- MonsterAnimator : TryGetComponent
    MonsterController --> PlayerHealth : 타겟 참조
    MonsterController --> RewindManager : Register

    MonsterState --> MonsterController
    MonsterState <|-- MonsterPatrolState
    MonsterState <|-- MonsterChaseState
    MonsterState <|-- MonsterAttackState
```

---

## 5. Boss

가장 복잡한 서브시스템 — 4페이즈 상태 패턴 + 근접 유닛(본체/분신) 계층.

```mermaid
classDiagram
    class BossController {
        <<MonoBehaviour>>
        +Player PlayerController
        +Phase1Clones BossCloneController[]
        +Body BossBodyController
        +PhaseIndex int
        +IsTransitioning bool
        +CurrentEmotion BossEmotion
        +CurrentHealth float
        +OnPhaseChanged Action~int~
        +OnBossDefeated Action
        +OnEmotionChanged Action~BossEmotion~
        +OnHealthChanged Action~float,float~
        +BeginBattle()
        +RegisterPattern(IBossPattern)
        +KillPlayer()
        +SetEmotion(BossEmotion)
        +TakeDamage(float, DamageSource, PlayerHealth)
    }
    class BossState {
        <<abstract>>
        #Boss BossController
        +Enter()*
        +Exit()*
        +Tick()
        +FixedTick()
        +CoPhaseEndGimmick()
    }
    class Phase1State
    class Phase2State
    class Phase3State
    class Phase4State
    class BossMeleeUnitBase {
        <<abstract MonoBehaviour>>
        #_boss BossController
        +MoveSpeed float*
        +AttackRange float*
        +TakeDamage(...)*
        +BeginCombat()
        +StopAttackLoop()
    }
    class BossBodyController {
        +Activate()
        +Deactivate()
        +TakeDamage(...)
    }
    class BossCloneController {
        +IsAlive bool
        +OnCloneDied Action~BossCloneController~
        +Activate()
        +Deactivate()
        +TakeDamage(...)
        +Die()
    }
    class IBossPattern {
        <<interface>>
        +Play()
        +Stop()
        +Dispose()
        +OnRewindStart()
        +OnRewindEnd()
    }
    class BossLightningPattern
    class BossHazardPool {
        +Alloc(...)
        +Free(int)
        +FreeAll()
        +Capture(int) HazardRecord
        +Apply(int, HazardRecord)
    }
    class DamageHazard {
        +DamageHalves int
        +StunDuration float
        +InstantKill bool
        +Configure(int, float, bool)
    }
    class HeartPickup
    class BossEmotion {
        <<enum>>
        None
        Black
        White
        Navy
        Pink
        Blue
        Angry
    }

    BossController ..|> IRewindable
    BossController ..|> IDamageable
    BossController *-- "4" BossState : phase1~4
    BossState <|-- Phase1State
    BossState <|-- Phase2State
    Phase2State <|-- Phase3State
    Phase2State <|-- Phase4State
    BossState --> BossController : 역참조

    BossMeleeUnitBase ..|> IDamageable
    BossMeleeUnitBase ..|> IRewindable
    BossMeleeUnitBase <|-- BossBodyController
    BossMeleeUnitBase <|-- BossCloneController
    BossMeleeUnitBase --> BossController : _boss
    BossMeleeUnitBase *-- DamageHazard : 공격 히트박스

    BossController *-- BossBodyController
    BossController *-- "N" BossCloneController
    BossController *-- HeartPickup
    BossController --> PlayerController : Player
    BossController o-- IBossPattern : RegisterPattern
    BossController ..> BossEmotion

    BossLightningPattern ..|> IBossPattern
    BossLightningPattern --> BossHazardPool
    Phase1State --> BossHazardPool
    Phase1State --> BossCloneController : Phase1Clones
    Phase2State --> BossHazardPool
    Phase2State --> BossBodyController
    Phase3State --> BossHazardPool
```

---

## 5.5 Boss2 (Map3, 진욱 - 3~4페이즈 별도 시스템)

`Minsung.Boss`를 수정하지 않는 독립 시스템. 팀 컨벤션상 **네임스페이스 없음**(무네임스페이스). 재사용 가능한 공용 인프라(`BossHazardPool`/`DamageHazard`/`HeartPickup`/`RewindManager`/`IDamageable`/`IRewindable`)만 소비한다. 씬 배선·코드 흐름·좌표 변수 정리는 `claude/boss.md` 5장 참고.

```mermaid
classDiagram
    class Boss2Health {
        <<MonoBehaviour>>
        +MaxHealth float
        +CurrentHealth float
        +PhaseIndex int
        +OnHealthChanged Action~float,float~
        +OnPhaseChanged Action~int~
        +OnSpaceTearTriggered Action
        +TakeDamage(float, DamageSource, PlayerHealth) bool
        +EndSpaceTearFreeze()
    }
    class BossFloatMovement {
        <<MonoBehaviour>>
        +TryBeginScriptedMovement(...)
        +EndScriptedMovement()
        +OnRewindStart()/OnRewindEnd()
    }
    class Boss2AttackPatterns {
        <<MonoBehaviour>>
        +StartPatterns()
        +StopPatterns()
    }
    class Boss2EmotionController {
        <<MonoBehaviour>>
        +Current Boss2Emotion
        +ReflectIfNeeded(DamageSource, PlayerHealth) bool
    }
    class Boss2Emotion {
        <<enum>>
        Black / White / Navy / Pink / Blue / Angry
    }
    class Boss2LightningPattern
    class Boss2WavePattern
    class Boss2LaserPattern
    class Boss2GrabPattern
    class Boss2SpaceTearPattern
    class Boss2BrandController
    class Boss2AltarSpawner
    class Boss2AltarInteractive
    class Boss2DodgeableKillHazard {
        <<MonoBehaviour>>
        회피 가능 즉사 (IsDodgeInvincible이면 무시)
    }

    Boss2Health ..|> IDamageable
    Boss2Health ..|> IRewindable
    BossFloatMovement ..|> IRewindable
    Boss2Health --> Boss2EmotionController : ReflectIfNeeded 위임
    Boss2EmotionController ..> Boss2Emotion
    Boss2AttackPatterns *-- Boss2LightningPattern : new
    Boss2AttackPatterns *-- Boss2WavePattern : new
    Boss2AttackPatterns *-- Boss2LaserPattern : new
    Boss2LightningPattern --> BossHazardPool : 재사용
    Boss2WavePattern --> BossHazardPool : 재사용
    Boss2LaserPattern --> BossHazardPool : 재사용
    Boss2SpaceTearPattern --> Boss2DodgeableKillHazard : 돌진 판정
    Boss2SpaceTearPattern --> Boss2Health : 동결/해제
    BossFloatMovement --> DamageHazard : 돌진 히트박스(재사용)
    Boss2BrandController --> Boss2AltarSpawner
    Boss2AltarSpawner ..> Boss2AltarInteractive
    RewindManager --> Boss2Health : Register/RecordTick/Apply
    RewindManager --> BossFloatMovement
    RewindManager --> Boss2AttackPatterns
```

- `Boss2DataSO`(`00.Common/Data/`)는 Boss2 전용 밸런싱 DB로 **GameDatabaseSO 트리에 연결하지 않고** 컴포넌트에 직접 드래그한다(GameDB 정적 접근자 대상 아님).
- 감정 클래스는 `Minsung.Boss` 동명 타입과의 컴파일 충돌 회피를 위해 `Boss2` 접두사를 쓴다(`claude/boss.md` 5-1절).
- 공간찢기(민성 구현)는 절대 즉사 `DamageHazard`/`PlayerHealth.Kill()`을 건드리지 않고 전용 `Boss2DodgeableKillHazard`로만 처리한다.

---

## 6. Interactive

```mermaid
classDiagram
    class IInteractable {
        <<interface>>
        +OnInteract(GameObject)
        +OnFocus()
        +OnUnfocus()
        +GetTransform() Transform
    }
    class InteractableRegistry {
        <<static>>
        +Register(Collider2D, IInteractable)$
        +Unregister(Collider2D)$
        +Get(Collider2D) IInteractable$
    }
    class IHoldInteractable {
        <<interface>>
        +CanHoldInteract bool
        +OnHoldStart(GameObject) bool
        +OnHoldUpdate(GameObject, float) bool
        +OnHoldCancel(GameObject)
    }
    class BaseInteractive {
        <<abstract MonoBehaviour>>
        +OnFocus()*
        +OnInteract(GameObject)*
        +OnUnfocus()*
    }
    class LeverInteractive {
        +OnFocus()
        +OnInteract(GameObject)
        +OnUnfocus()
    }
    class LeverLightSwitch {
        <<MonoBehaviour>>
        +OnLeverPulled()
        +OnLeverReset()
    }
    class RadioInteractive {
        +OnFocus()
        +OnInteract(GameObject)
        +OnUnfocus()
    }
    class ElevatorController {
        <<MonoBehaviour>>
        +ElevatorId int
        +CanStart bool
        +SetLeverPulled(bool)
        +TryStartJourney() bool
    }
    class ElevatorButtonInteractive {
        +CanHoldInteract bool
        +OnHoldStart(GameObject) bool
        +OnHoldUpdate(GameObject, float) bool
        +OnHoldCancel(GameObject)
    }
    class ElevatorManager {
        <<MonoBehaviour Singleton>>
        +Instance$ ElevatorManager
        +Register(ElevatorController) bool
        +Unregister(ElevatorController)
        +TryGetController(int, out ElevatorController) bool
    }

    BaseInteractive ..|> IInteractable
    LeverInteractive --|> BaseInteractive
    RadioInteractive --|> BaseInteractive
    ElevatorButtonInteractive --|> BaseInteractive
    ElevatorButtonInteractive ..|> IHoldInteractable
    LeverInteractive ..|> IRewindable
    ElevatorController ..|> IRewindable
    BaseInteractive --> InteractableRegistry : 등록/해제
    LeverInteractive --> PlayerController : SetInteracting
    LeverInteractive --> PlayerAnimator : TriggerLever
    LeverInteractive --> KeyGuideManager : 키 가이드 표시
    LeverInteractive --> ElevatorController : SetLeverPulled(_elevatorId)
    LeverLightSwitch ..> LeverInteractive : onLeverPulled/onLeverReset UnityEvent
    RadioInteractive --> CameraManager : 포커스 전환
    RadioInteractive --> SoundManager : 사운드 토글
    RadioInteractive --> KeyGuideManager : 키 가이드 표시
    ElevatorButtonInteractive --> ElevatorManager : TryGetController(_elevatorId)
    ElevatorButtonInteractive --> ElevatorController : TryStartJourney
    ElevatorController --> ElevatorManager : Register/Unregister
    PlayerInteractionSensor ..> IInteractable : 조회
    PlayerInteractionSensor ..> IHoldInteractable : 홀드 상태 머신
    PlayerInteractionSensor --> InteractableRegistry
```

> `LeverInteractive`/`ElevatorController`는 `IRewindable`이라 분신이 `InteractCommand`로 재연한다(엘리베이터는 완료된 홀드만 기록). `RadioInteractive`는 사운드/카메라 포커스만 토글해 되감기 기록 대상이 아니다. `IHoldInteractable`은 상호작용 키를 일정 시간 눌러 유지해야 완료되는 오브젝트 계약(`PlayerInteractionSensor`가 상태 머신으로 처리) - 엘리베이터 버튼이 현재 유일한 구현체.

---

## 7. Common / Utility

싱글톤 보일러플레이트, 시스템 전역 공통 인터페이스, 그리고 밸런싱 데이터 DB(GameDB).

```mermaid
classDiagram
    class GameDB {
        <<static>>
        +Player$ PlayerDataSO
        +Boss$ BossDataSO
        +Time$ TimeDataSO
        +Potion$ PotionDataSO
    }
    class GameDatabaseSO {
        <<ScriptableObject>>
        +RESOURCES_PATH$ string
        -_playerSo PlayerDataSO
        -_bossSo BossDataSO
        -_timeSo TimeDataSO
        -_potionSo PotionDataSO
    }
    class PlayerDataSO {
        <<ScriptableObject>>
        이동/공격/체력/피격리액션/오브/시각효과
    }
    class BossDataSO {
        <<ScriptableObject>>
        피통/본체/분신/낙뢰/감정/기믹/페이즈패턴
    }
    class TimeDataSO {
        <<ScriptableObject>>
        리와인드/분신/슬로우
    }
    class PotionDataSO {
        <<ScriptableObject>>
        드랍/자석픽업/풀/소지·사용(MaxCarryCount·HealHalves)
    }

    GameDB --> GameDatabaseSO : Resources.Load(GameDB) 1회 캐싱
    GameDatabaseSO o-- PlayerDataSO
    GameDatabaseSO o-- BossDataSO
    GameDatabaseSO o-- TimeDataSO
    GameDatabaseSO o-- PotionDataSO
```

- 에셋: `08.Data/Resources/GameDB.asset`(루트) / `08.Data/Player|Boss|Time|Potion/*DB.asset`
- 코드 계약값(입력 키, epsilon, 구조 상수)은 `Constants`(partial) 유지

```mermaid
classDiagram
    class PersistentSingleton~T~ {
        <<abstract MonoBehaviour>>
        +Instance$ T
        #OnSingletonAwake()
    }
    class GameManager
    class KeyGuideManager
    class SpriteReference
    class AchievementManager
    class ScreenFade
    class ParticlePresets

    PersistentSingleton~T~ <|-- GameManager
    PersistentSingleton~T~ <|-- KeyGuideManager
    PersistentSingleton~T~ <|-- SpriteReference
    PersistentSingleton~T~ <|-- AchievementManager
    PersistentSingleton~T~ <|-- ScreenFade
    PersistentSingleton~T~ <|-- ParticlePresets

    class IDamageable {
        <<interface>>
        +TakeDamage(float, DamageSource, PlayerHealth) bool
    }
    IDamageable <|.. MonsterHealth
    IDamageable <|.. BossController
    IDamageable <|.. BossMeleeUnitBase

    class DamageSource {
        <<enum>>
        Player
        PlayerClone
    }
    class ColorType {
        <<enum>>
        Black
        White
    }
    class UtilCoroutine {
        <<static>>
        +CheckRunCoroutine(ref Coroutine, Coroutine, MonoBehaviour)$
        +CheckStopCoroutine(ref Coroutine, MonoBehaviour)$
    }
```

---

## 8. Achievement / Backend / Visual

```mermaid
classDiagram
    class AchievementData {
        <<ScriptableObject>>
        +Id string
        +Title string
        +Description string
        +Icon Sprite
    }
    class AchievementDatabase {
        <<ScriptableObject>>
        +TryGet(string, out AchievementData) bool
    }
    class AchievementIds {
        <<static>>
    }
    class AchievementManager {
        +OnAchievementUnlocked Action~AchievementData~
        +IsUnlocked(string) bool
        +Unlock(string)
    }
    class AchievementToastUI

    AchievementDatabase o-- "*" AchievementData
    AchievementManager --> AchievementDatabase
    AchievementToastUI --> AchievementManager : OnAchievementUnlocked 구독

    class GhostFrame
    class ScoreSubmit
    class ScoreEntry
    class SupabaseClient {
        +Register(...)
        +SubmitScore(...)
        +GetLeaderboard(...)
        +GetTopGhost(...)
    }
    class SupabaseTester

    ScoreSubmit *-- "*" GhostFrame
    ScoreEntry *-- "*" GhostFrame
    SupabaseTester --> SupabaseClient

    class VhsRewindOverlay {
        +Play()
        +Stop()
    }
    class CharaGlow
    class ShadowLayer
    class SpeedlineEffect
    class SheepyVisualSetup

    PlayerRewind --> VhsRewindOverlay
    GameManager --> ScreenFade
```

---

## 9. Sound / Camera / UI

되감기와 무관한 연출/피드백 계층. 대부분 `PersistentSingleton<T>`로 씬 전역 접근한다.

```mermaid
classDiagram
    class SoundManager {
        <<MonoBehaviour Singleton>>
        +PlaySFX(ESfxState, int, float)
        +PlayBGM(EBgm, bool, float)
        +StopBGM()
        +PlaySFX_Duration(...)
    }
    class MapBgmPlayer {
        <<MonoBehaviour>>
    }
    class SoundData {
        <<ScriptableObject>>
    }
    class CameraManager {
        <<MonoBehaviour Singleton>>
        +Focus(Transform, float, float)
        +UnFocus()
    }
    class BossHealthBarUI {
        <<MonoBehaviour>>
        +Redraw(float, float)
    }
    class BossEmotionHUD {
        <<MonoBehaviour>>
    }
    class PlayerStatusEffectUI {
        <<MonoBehaviour>>
    }
    class CaptionManager {
        <<MonoBehaviour Singleton>>
        +PlaySequence(CaptionEntry[])
        +StopSequence()
    }
    class KeyGuideManager {
        <<MonoBehaviour Singleton>>
        +ShowKeyGuide(EKeyGuide)
        +HideKeyGuide()
    }
    class SpriteReference {
        <<MonoBehaviour Singleton>>
    }

    SoundManager --> SoundData : 클립 조회
    MapBgmPlayer --> SoundManager : 씬 진입 BGM
    BossHealthBarUI --> BossController : OnHealthChanged 구독 (Slider.value)
    BossEmotionHUD --> BossController : OnEmotionChanged 구독
    PlayerStatusEffectUI --> PlayerStatusEffectController : 활성 효과 표시
    KeyGuideManager --> SpriteReference : 키 스프라이트 조회
```

> 과거 `Ex/`(BossManager/BossUIManager/BossConditionSlotUI/BossDataSO) 샘플 코드는 진행바의 `Slider` 참고 구현을 정식 `BossHealthBarUI`(Minsung.UI)로 승격한 뒤 2026-07-18 완전 삭제됐다.

---

## 참고

- 클래스/인터페이스 수: 총 180개 스크립트 (enum/struct/static 유틸 포함 - 대부분 `Minsung.*`, `03.Boss/Boss2/`는 무네임스페이스).
- `IRewindable` 구현체: `PlayerRewind`, `MonsterController`, `BossController`, `BossMeleeUnitBase`(->`BossBodyController`, `BossCloneController`), `CloneController`, `LeverInteractive`, `ElevatorController`, `PotionManager`, `Boss2Health`, `BossFloatMovement`(Boss2).
- `ICommandActor` 구현체: `PlayerController`(코디네이터), `CloneController`.
- `IDamageable` 구현체: `MonsterHealth`, `BossController`, `BossMeleeUnitBase`(->`BossBodyController`, `BossCloneController`), `Boss2Health`(Boss2).
- `IHoldInteractable` 구현체: `ElevatorButtonInteractive`.
- `PersistentSingleton<T>` 상속: `GameManager`, `KeyGuideManager`, `SpriteReference`, `AchievementManager`, `ScreenFade`, `ParticlePresets`, `CameraManager`, `SoundManager`, `CaptionManager`, `ElevatorManager`.
- 다이어그램은 스냅샷이므로, 클래스 추가/삭제나 인터페이스 변경 시 수동으로 갱신해야 한다.
