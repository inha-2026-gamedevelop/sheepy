# The Last Re:wind — 코딩 컨벤션

> 기준: C# / Unity 6
> 네임스페이스는 민성만 사용. 팀원 B/C는 네임스페이스 생략.

---

## 1. 네임스페이스 (민성 전용)

```csharp
namespace Minsung
namespace Minsung.TimeSystem
namespace Minsung.Combat
namespace Minsung.Equipment
// 폴더 구조와 일치시킨다
```

---

## 2. 명명 규칙

| 대상 | 규칙 | 예시 |
|---|---|---|
| 클래스 / 구조체 | PascalCase | `PlayerController`, `GhostFrame` |
| 인터페이스 | I + PascalCase | `IRewindable`, `IDamageable` |
| public 메서드 | PascalCase | `TakeDamage()`, `SpawnClone()` |
| private 메서드 | PascalCase | `CheckGrounded()`, `ApplyVisual()` |
| 로컬 변수 | camelCase | `hitPoint`, `frameCount` |
| 멤버 변수 | 언더스코어 prefix + camelCase | `_health`, `_moveSpeed` |
| 상수 | UPPER_SNAKE_CASE | `MAX_CLONE_COUNT`, `BASE_DAMAGE` |
| 전역 배열 | G_ prefix | `G_SpawnTable` |
| SerializeField | 멤버 변수와 동일 | `_jumpForce` |
| enum | PascalCase (값도 PascalCase) | `ItemGrade.Epic` |
| ScriptableObject 클래스 | PascalCase + DataSO 접미사 (루트 DB는 예외적으로 GameDatabaseSO) | `PlayerDataSO`, `BossDataSO` |
| ScriptableObject 변수 | So 어미 (멤버는 `_camelCase` 규칙과 결합) | `_playerSo`, `_rootSo`, `bossSo` |

---

## 3. 매직넘버 절대 사용 금지

```csharp
// ❌ 절대 안 됨
if (_health > 100) { }
_rb.linearVelocity = new Vector2(5f, 9f);
for (int i = 0; i < 3; ++i) { }

// ✅ 반드시 이렇게
private const float MAX_HEALTH      = 100f;
private const float MOVE_SPEED      = 5f;
private const float JUMP_FORCE      = 9f;
private const int   MAX_CLONE_COUNT = 3;

if (_health > MAX_HEALTH) { }
_rb.linearVelocity = new Vector2(MOVE_SPEED, JUMP_FORCE);
for (int i = 0; i < MAX_CLONE_COUNT; ++i) { }
```

**Inspector에서 조절할 값은 `[SerializeField]`로, 코드 내부 고정값은 `const`로.**

```csharp
// Inspector 조절용 → SerializeField
[SerializeField] private float _moveSpeed = 5f;

// 코드 내부 고정값 → const
private const float GROUND_CHECK_EXTRA = 0.05f;
private const float ATTACK_FLASH_TIME  = 0.1f;
```

---

## 4. 중괄호 스타일 (Allman / BSD)

여는 중괄호는 **항상 다음 줄**에.  
**모든 제어문(if/for/while/foreach)에 중괄호 필수 — 단일 문장이라도 예외 없음.**

```csharp
// ✅
private void Move()
{
    if (_grounded)
    {
        _rb.linearVelocity = new Vector2(_moveSpeed, 0f);
    }
}

// ❌ 중괄호 생략 금지 (단일 문장이어도)
if (_isRewinding) return;
for (int i = 0; i < n; ++i) DoThing(i);

// ✅ 이렇게
if (_isRewinding)
{
    return;
}
```

**한 줄짜리 함수는 예외적으로 한 줄 허용:**

```csharp
public void Reset() { _health = MAX_HEALTH; }
public bool IsAlive() { return _health > 0f; }
```

---

## 5. 증감 연산자

**전위 증감만 사용 (`++i`, `--i`)**

```csharp
// ✅
for (int i = 0; i < MAX_CLONE_COUNT; ++i) { }
++_frameIndex;
--_health;

// ❌
i++;
_health--;
```

---

## 6. 조건문 괄호

**복합 조건은 각 조건을 괄호로 묶는다.**

```csharp
// ✅
if ((a > b) && (c > d)) { }
if ((_health <= 0f) || (_isDead)) { }

// ❌
if (a > b && c > d) { }
```

---

## 7. 필드 정렬

**`=` 기준 정렬. 가독성 최우선.**

```csharp
// ✅ const 정렬
private const float MAX_HEALTH      = 100f;
private const float MOVE_SPEED      = 5f;
private const float JUMP_FORCE      = 9f;
private const int   MAX_CLONE_COUNT = 3;

// ✅ SerializeField 정렬
[SerializeField] private float _moveSpeed   = 5f;
[SerializeField] private float _jumpForce   = 9f;
[SerializeField] private int   _maxClones   = 3;

// ✅ 일반 멤버 정렬
private float _health   = 0f;
private bool  _isDead   = false;
private int   _frameIdx = 0;
```

---

## 8. using 그룹 순서

```csharp
// System 계열
using System;
using System.Collections;
using System.Collections.Generic;

// Unity 계열
using UnityEngine;
using UnityEngine.UI;

// 서드파티
using Newtonsoft.Json;
using DG.Tweening;

// 프로젝트 내부 (민성 네임스페이스)
using Minsung.TimeSystem;
```

---

## 9. 파일 구조 (배너 주석)

### 기본 구조

```csharp
// System
using System;

// Unity
using UnityEngine;

namespace Minsung
{
    public class ClassName : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const float MAX_HEALTH = 100f;

        [Header("이동")]
        [SerializeField] private float _moveSpeed = 5f;

        private float _health;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake() { }
        private void Start()  { }
        private void Update() { }

        /****************************************
        *                Methods
        ****************************************/

        public void TakeDamage(float damage) { }
        private void Die() { }
    }
}
```

### 상속 구조가 있는 경우 — 클래스 선언 위에 상속 구조 주석 필수

인벤토리 코드 방식을 따른다. 클래스 선언 바로 위에 `#region` 또는 주석 블록으로 상속 계층을 명시한다.

```csharp
// Unity
using UnityEngine;

namespace Minsung.Combat
{
    /*
     * 상속 구조:
     *
     * MonoBehaviour
     *   └── EnemyBase          (공통 HP, 피격, 사망)
     *         ├── EnemyPatrol  (좌우 패트롤)
     *         └── EnemyBoss    (페이즈 전환, 패턴 호출)
     *               ├── BossPhase1
     *               └── BossPhase2
     */
    public abstract class EnemyBase : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        protected const float BASE_HEALTH = 100f;

        [Header("기본 스탯")]
        [SerializeField] protected float _maxHealth = BASE_HEALTH;

        protected float _health;

        /****************************************
        *              Unity Event
        ****************************************/

        protected virtual void Awake()
        {
            _health = _maxHealth;
        }

        /****************************************
        *                Methods
        ****************************************/

        public virtual void TakeDamage(float damage)
        {
            _health -= damage;
            if (_health <= 0f)
            {
                Die();
            }
        }

        protected abstract void Die();
    }
}
```

```csharp
// Unity
using UnityEngine;

namespace Minsung.Combat
{
    /*
     * 상속 구조:
     *
     * EnemyBase
     *   └── EnemyBoss  ← 현재 클래스
     *         ├── BossPhase1
     *         └── BossPhase2
     */
    public abstract class EnemyBoss : EnemyBase
    {
        /****************************************
        *                Fields
        ****************************************/

        protected int _currentPhase = 0;

        /****************************************
        *                Methods
        ****************************************/

        protected abstract void TransitionPhase(int phase);

        protected override void Die()
        {
            // 보스 사망 처리
        }
    }
}
```

---

## 10. 참조 타입 매개변수

```csharp
// null 가능성 없으면 참조
private void SetData(ItemData data) { }

// null 가능성 있으면 nullable 명시
private void SetTarget(Transform target) { }

// 수정이 필요하면 ref
private void ModifySnapshot(ref Snapshot snap) { }
```

---

## 11. switch 들여쓰기

```csharp
switch (_state)
{
    case State.Idle:
        HandleIdle();
        break;
    case State.Run:
        HandleRun();
        break;
    default:
        break;
}
```

---

## 12. 주석

**필요한 곳에만. 코드로 읽히면 주석 달지 않는다.**

```csharp
// ✅ 이유가 있는 주석
_rb.isKinematic = true; // 되감기 중 물리 끄기

// ❌ 코드가 말하는 것을 반복
_health -= damage; // 체력 감소
```

**public API는 XML 문서 주석:**

```csharp
/// <summary> 피해를 입힌다. 체력이 0이 되면 사망 처리. </summary>
public void TakeDamage(float damage) { }
```

---

## 13. null 체크

```csharp
// Unity 오브젝트는 == null 사용 (?. / ?? 금지 - fake null 미탐지)
if (_renderer == null)
{
    return;
}

// 컴포넌트 취득 + null 체크는 TryGetComponent로
if (other.TryGetComponent(out PlayerHealth health))
{
    health.TakeDamage();
}

// 일반 C# 객체(델리게이트/콜백)는 ?. 사용 가능
_callback?.Invoke();
onSuccess?.Invoke();
```

---

## 14. SerializeField vs public

```csharp
// Inspector 노출은 SerializeField. public 절대 사용하지 않는다.
// ✅
[SerializeField] private float _moveSpeed = 5f;

// ❌
public float moveSpeed = 5f;

// 외부에서 읽기만 필요하면 프로퍼티
public float MoveSpeed => _moveSpeed;
```

---

## 15. FixedUpdate 지양, 코루틴 지향

**물리 동기화가 반드시 필요한 경우가 아니면 `FixedUpdate` 사용을 지양하고, 코루틴(`IEnumerator` + `StartCoroutine`)으로 대체한다.**

```csharp
// ❌ 지양
private void FixedUpdate()
{
    _elapsed += Time.fixedDeltaTime;
    if (_elapsed >= _duration)
    {
        ApplyEffect();
    }
}

// ✅ 지향
private IEnumerator CoApplyEffect()
{
    yield return new WaitForSeconds(_duration);
    ApplyEffect();
}
```

- `Rigidbody`/`Rigidbody2D` 물리 연산(힘, 속도 적용 등)처럼 물리 스텝과 동기화가 필수인 경우에만 `FixedUpdate` 예외 허용.
- 타이머, 페이드, 연출, 상태 전환 등 시간 기반 로직은 코루틴으로 작성한다.

---

## 16. 데이터 관리 (GameDB / Constants)

코드에 숫자를 직접 쓰지 않는 것은 동일하다. 값의 성격에 따라 두 곳으로 나눠 관리한다.

- **밸런싱/기획 수치** (속도, 피통, 쿨다운, 패턴 간격, 연출 색 등) -> **GameDB (ScriptableObject DB)**
- **코드 계약값** (입력 키/축, 태그/레이어/씬 이름, 판정 epsilon, enum 연동 개수, 반칸 환산) -> **Constants**

### GameDB - ScriptableObject 기반 밸런싱 DB

```
스크립트: Assets/01.Scripts/00.Common/Data/     (namespace Minsung.Common.Data)
에셋:     Assets/08.Data/Resources/GameDB.asset  <- 루트 GameDatabaseSO (Resources 자동 로드)
          Assets/08.Data/Player/PlayerDB.asset   <- PlayerDataSO
          Assets/08.Data/Boss/BossDB.asset       <- BossDataSO
          Assets/08.Data/Time/TimeDB.asset       <- TimeDataSO
```

```csharp
// 읽기 - 정적 접근자 GameDB
float speed = GameDB.Player.MoveSpeed;
_waitConfusionInterval = new WaitForSeconds(GameDB.Boss.ConfusionInterval);

// 매 프레임 읽는 값은 Awake에서 SO 참조를 캐싱 (변수는 So 어미)
private PlayerDataSO _playerSo;
private void Awake() { _playerSo = GameDB.Player; }
```

**GameDB 규칙**
- SO 필드는 `[SerializeField] private` + 읽기 전용 프로퍼티. 필드에 단위/용도 주석
- **MonoBehaviour 필드 초기화식/생성자에서 GameDB 호출 금지** (Resources.Load 제약) - Awake 이후 사용
- 컴포넌트에 밸런싱 값의 SerializeField 미러를 두지 않는다 - DB 에셋이 단일 소스
- 새 도메인은 `*DataSO` 클래스 생성 -> `GameDatabaseSO`에 `_xxxSo` 참조 추가 -> `GameDB` 프로퍼티 추가 -> `08.Data/<도메인>/`에 에셋 생성
- 밸런싱 수치 변경은 DB 에셋 인스펙터에서 - 코드는 건드리지 않는다

### Constants - 코드 계약값 (partial class)

```
Constants.cs             → 공통 (레이어, 태그, 씬 이름)
Constants.player.cs      → 입력 키/축, 판정 epsilon, HALVES_PER_HEART, ANIM_DIR_*
Constants.combat.cs      → 히트스톱, 몬스터 SerializeField 기본값, 기믹 enum 연동 개수
Constants.ui/audio/...   → 각 시스템 계약값
```

```csharp
if (Input.GetKeyDown(Constants.Player.KEY_JUMP)) { }
float dist = _col.bounds.extents.y + Constants.Player.GROUND_CHECK_EXTRA;
```

**Constants 규칙**
- 해당 시스템 파일에만 추가, UPPER_SNAKE_CASE, 반드시 단위/용도 주석
- 파일 하나가 전부 밸런싱 값이 되면 GameDB의 `*DataSO`로 승격을 검토한다 (Constants.time.cs 전례)

---

## 17. 런타임 GC 최소화 (프레임 드랍 방지)

**게임 루프에서 도는 코드는 힙 할당 0을 목표로 한다.**

```csharp
// ✅ WaitForSeconds 캐싱 - 코루틴이 반복 실행되면 필드로 1회 생성
private WaitForSeconds _waitInvincible;
private void Awake() { _waitInvincible = new WaitForSeconds(_invincibleDuration); }
// 코루틴 안에서: yield return _waitInvincible;

// ❌ 반복 코루틴 안에서 매번 new
yield return new WaitForSeconds(1f);

// ✅ renderer.material 캐싱 - Awake 1회 취득 후 재사용
private Material _material;
private void Awake() { _material = _renderer.material; }

// ❌ 매 프레임 접근 (프로퍼티 호출 비용 + 최초 인스턴스 생성 시점 불명확)
_renderer.material.color = Color.white;

// ✅ 물리 쿼리는 재사용 버퍼 + ContactFilter2D
private static readonly Collider2D[] _results = new Collider2D[16];
int count = Physics2D.OverlapCircle(origin, radius, _filter, _results);

// ❌ 호출마다 배열 할당
Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius);
```

**추가 규칙**
- 컴포넌트/참조 취득은 Awake/Start 1회 캐싱. Update/공격 루프에서 `GetComponent`/`Find` 금지
- 람다/클로저는 캡처가 있으면 힙 할당 — 루프 안에서 만들지 말고, 확정된 대상에 대해 1회만 생성
- 반복 사용하는 List는 필드로 두고 `Clear()` 후 재사용 (예: 분신 클립 `CopyOrderedTo`)
- 오브젝트 풀의 탐색은 O(1) 자료구조(Stack/Queue) 사용, `List.Contains` 루프 금지

---

## 18. 싱글톤

**DontDestroyOnLoad 싱글톤은 `Utility/PersistentSingleton<T>`를 상속한다.**  
Awake를 직접 만들지 말고 `OnSingletonAwake()`를 오버라이드한다.

```csharp
// ✅
public class GameManager : PersistentSingleton<GameManager>
{
    protected override void OnSingletonAwake()
    {
        // 최초 인스턴스 확정 후 1회 초기화
    }
}

// ❌ 클래스마다 Instance/Awake 중복 구현 금지
```

- 씬 로컬 싱글톤(예: `RewindManager`)은 별도 구현 유지 — DontDestroyOnLoad 여부가 다르다
- 씬 배치 없이도 동작해야 하면 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`로 자동 생성
- 도메인 리로드 OFF 대비: static 상태는 `SubsystemRegistration`에서 초기화

---

## 19. 리와인드 시스템 규칙 (프로젝트 특화)

- 리와인드 참여 오브젝트는 반드시 `IRewindable` 구현 + `RewindManager.Register/Unregister` 쌍 호출
- **기록 버퍼 용량은 `RewindManager.TickCapacity` 하나만 사용** — 직접 계산 금지 (인덱스 어긋남). 기록 길이(초) 조정은 TimeDB(`GameDB.Time.RecordSeconds`)에서만
- 랜덤이 들어가는 패턴은 결정 로그에 저장해 리와인드 후 재현 (Phase1State 방식 참고)
- 시각 연출용 오브젝트는 생성/파괴 대신 풀 활성/비활성 (스냅샷 역재생 가능해야 함)

---

## 요약 카드 (팀원 공유용)

```
✅ 클래스/메서드       PascalCase
✅ 멤버 변수           _camelCase
✅ 상수                UPPER_SNAKE_CASE
✅ 중괄호              다음 줄 (Allman) + 모든 제어문 필수
✅ 증감                ++i, --i (전위)
✅ 복합 조건           ((a > b) && (c < d))
✅ 매직넘버            밸런싱은 GameDB(SO DB), 계약값은 Constants.*.cs
✅ SO 네이밍           클래스 *DataSO, 변수 So 어미 (_playerSo)
✅ 필드 정렬           = 기준 정렬
✅ Inspector 노출      [SerializeField] private
✅ 상속 구조 명시      클래스 위에 계층 주석
✅ 네임스페이스        민성만 사용 (Minsung.XXX)
✅ FixedUpdate         지양, 코루틴 지향 (물리 동기화 예외)
✅ GC                  WaitForSeconds/material/참조 캐싱, NonAlloc 쿼리
✅ null 체크           Unity 오브젝트 == null, 컴포넌트는 TryGetComponent
✅ 싱글톤              PersistentSingleton<T> 상속
✅ 리와인드 버퍼       RewindManager.TickCapacity만 사용
```