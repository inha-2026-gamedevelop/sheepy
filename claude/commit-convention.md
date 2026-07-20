# The Last Re:wind — 커밋 컨벤션

> 기준: Conventional Commits (타입 태그는 영어, 제목은 한국어)

---

## 1. 기본 형식

```
type: Scope 한국어 제목

본문 (선택)
```

```
feat: Player 대시 쿨다운 추가
fix: Boss Phase2 전환 시 낙뢰 풀 누수 수정
refactor: TimeSystem RingBuffer 인덱스 계산 정리
docs: gamedb.md 확장 절차 보강
```

---

## 2. 타입 태그

| 타입 | 의미 |
|---|---|
| `feat` | 새 기능 추가 |
| `fix` | 버그 수정 |
| `refactor` | 동작 변경 없는 구조 개선 |
| `docs` | 문서 추가/수정 (`claude/*.md` 등) |
| `style` | 포맷팅, 공백, 세미콜론 등 동작에 영향 없는 변경 |
| `test` | 테스트 코드 추가/수정 |
| `chore` | 빌드/에셋/설정 등 그 외 잡무 (패키지, ProjectSettings 등) |
| `perf` | 성능 개선 (GC 감소, 프레임 드랍 수정 등) |

---

## 3. Scope

`type:` 뒤, 제목 맨 앞에 오는 단어. `claude/CLAUDE.md`의 `Assets/01.Scripts/` 폴더 구조와 1:1 대응한다. 번호 접두사는 붙이지 않는다.

| Scope | 대응 폴더 |
|---|---|
| `Common` | `00.Common/` (GameManager, Constants, GameDB, Utility) |
| `Player` | `01.Player/` |
| `Monster` | `02.Monster/` (+BT) |
| `Boss` | `03.Boss/` |
| `TimeSystem` | `04.TimeSystem/` |
| `Interactive` | `05.Interactive/` |
| `UI` | `06.UI/` |
| `Achievement` | `07.Achievement/` |
| `Backend` | `08.Backend/` |
| `Visual` | `09.Visual/` |
| `Sound` | `10.Sound/` |
| `CameraSystem` | `11.CameraSystem/` |

- 여러 영역에 걸친 변경이거나 scope로 좁히기 애매하면 생략 가능: `docs: PLAN.md 우선순위 갱신`
- 코드 폴더가 아닌 대상(문서, 설정)은 scope 없이 타입만 사용

---

## 4. 제목 규칙

- 한국어 서술형, 완료형으로 작성 (`~추가`, `~수정`, `~정리`, `~보강`)
- 끝에 마침표(`.`) 금지
- 이모지 금지, 화살표는 `→`/`←` 대신 `->`/`<-` 사용 (`coding-convention.md` 규칙7과 동일)
- 무엇을 했는지는 diff로 드러나므로 제목에 나열하지 않는다 - 핵심 변경만 간결히

```
// ✅
fix: Player 리와인드 중 무적 프레임 미적용 버그 수정

// ❌ 마침표, 나열식
fix: Player 리와인드 중 무적 프레임 관련 여러 버그들을 수정함.
```

---

## 5. 본문 (선택)

본문은 "무엇을"이 아니라 **"왜"** 위주로 작성한다. 코드 리뷰 시 배경을 알아야 하는 경우에만 추가한다.

```
fix: Boss Phase2 낙뢰 풀 오브젝트 미반환 수정

되감기 스크럽 중 낙뢰가 풀로 반환되지 않고 파괴되어
다음 페이즈 진입 시 풀이 고갈되는 문제. 생성/파괴 대신
풀 활성/비활성 규칙(claude/CLAUDE.md 규칙4)을 지키지 않아 발생.
```

---

## 6. 커밋 단위

- 하나의 커밋은 하나의 목적만 담는다 - 기능 추가와 무관한 리팩토링/포맷팅을 함께 묶지 않는다
- `feat` + `fix`처럼 타입이 섞이면 커밋을 분리한다
- 밸런싱 수치 조정(`GameDB` 에셋)만 바꾼 경우 `chore: Common PlayerDB 이동속도 밸런싱 조정`처럼 명확히 구분

---

## 7. Co-Authored-By (금지)

**`Co-Authored-By` 트레일러를 커밋 메시지에 추가하지 않는다.** 

- 커밋은 개인 책임으로 기록되어야 한다 - AI 또는 다른 도구 지원 여부와 무관하게 작성자만 표시
- 협업/페어 프로그래밍이 필요하면 PR 설명이나 토론에서 별도 명시
- GitHub의 Co-Authored-By는 자동 생성(예: 웹 UI 병합 시)될 수 있으니, 로컬 커밋 메시지에는 절대 추가하지 말 것

```
// ✅ 올바름
fix: Player 회피 무적 기능 추가

// ❌ 금지
fix: Player 회피 무적 기능 추가

Co-Authored-By: Claude <noreply@anthropic.com>
```

---

## 예시 모음

```
feat: TimeSystem 분신 InteractCommand 재연 지원
fix: Interactive LeverInteractive 되감기 시 상태 불일치 수정
refactor: Boss BossEmotion 반사 판정 DamageSource 기준으로 통일
docs: PLAN.md 검증 상태 갱신
chore: Backend KEY.txt 예시 포맷 정리
perf: Monster OverlapCircleAll 호출을 NonAlloc 버퍼로 교체
```
