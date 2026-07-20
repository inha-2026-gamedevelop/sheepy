Map3(Assets/00.Scenes/Jinwook/Map3.unity)의 Boss2(3~4페이즈 부유 보스) 작업 이어서 진행.

## 선행 행동 - MCP연결 확인
직전 세션에서 UnityMCP가 끊겨서 여기로 넘어온 것. ToolSearch로 `mcp__UnityMCP__manage_editor` 등 검색해서 로드되는지 먼저 확인. 여전히 안 붙어있으면 사용자에게 Unity 쪽 MCP for Unity 브리지(플러그인) 상태 확인 요청할 것.

## 먼저 읽을 문서
- claude/boss2-handover.md - 지금까지 뭐가 됐고 뭐가 남았는지 (기능/검증 상태 체크리스트). 7.5절(감정), 7.6절(페이즈 전환)이 이번 세션에 새로 추가된 부분
- claude/boss2-code-guide.md - 코드가 실제로 어떻게 도는지 (디버깅용, 좌표 변수 의미/시나리오별 흐름 정리)
- claude/boss-design.md - 보스 원본 기획(Azathoth). 4페이즈는 "타임 리와인드 시스템 삭제"만 확정, 나머지 세부 패턴은 "추후 확정"이라고 명시돼 있음 - 이걸 근거로 이번 세션 페이즈 전환 구현 범위를 최소화했음

## 핵심 규칙 (계속 지킬 것)
- `Minsung.*` 네임스페이스 코드는 절대 수정 금지. 재사용 가능한 건 그대로 참조(BossHazardPool, DamageHazard, IDamageable, IRewindable, RewindManager, HeartPickup 등), 재사용 안 되면 Boss2 폴더에 새로 작성.
- **이름 충돌 주의**: `Minsung.Boss` 타입과 완전히 같은 이름으로 무네임스페이스 클래스를 만들면, `using Minsung.Boss;`로 그 타입을 참조하는 다른 네임스페이스 파일(예: `Minsung.UI.BossEmotionIconTooltip.cs`)과 실제로 컴파일 충돌한다(전역 네임스페이스가 using 지시문보다 이름 해석 우선순위가 높아서). 이번 세션에 `BossEmotionController` 이름으로 만들었다가 이 문제로 `Boss2EmotionController`로 개명한 사례 있음 - 새 Boss2 클래스가 Minsung.Boss 타입과 동명이면 먼저 다른 네임스페이스에서 `using Minsung.Boss;` + 그 이름 참조가 있는지 확인할 것.
- 신규 `*DataSO`는 전부 `Assets/01.Scripts/00.Common/Data/`에, 나머지 Boss2 스크립트는 `Assets/01.Scripts/03.Boss/Boss2/`에.
- 코드 작성하면 적용 전에 먼저 보여주고, 사용자가 확인해주면 적용 (작은 버그 수정은 바로 적용해도 됨).
- Unity 작업은 UnityMCP로 직접: 컴파일 확인 -> 씬 GameObject/컴포넌트 wiring -> Play 모드 진입 후 execute_code(리플렉션)나 스크린샷으로 실제 동작 검증 -> 저장. Scene .unity 파일을 텍스트로 직접 손대지 않는다.
- 테스트 중 "공격해도 반응 없다"류 증상이 나오면 에디터 Pause 버튼 상태부터 의심할 것.
- **컴포넌트 생성 타이밍 주의**: 같은 오브젝트 위 여러 컴포넌트의 `OnEnable` 호출 순서는 Unity가 보장하지 않는다. 다른 컴포넌트가 런타임에 동적으로 참조해야 하는 컴포넌트(`AddComponent`로 만드는 것 등)는 `Start()`가 아니라 `Awake()`에서 만들고, 그걸 참조/구독하는 쪽은 `OnEnable` 대신 `Start`에서 하는 게 안전하다(모든 오브젝트의 `Awake`가 끝난 뒤에야 `OnEnable`이 시작되는 건 보장되지만, `OnEnable`끼리의 순서는 보장 안 됨). 이번 세션에 `Boss2EmotionController`/`Boss2EmotionHUD`에서 실제로 겪은 버그.
- **커밋은 사용자가 직접 진행 중** - 이번 세션 작업은 이미 커밋 완료됐다(아래 git 상태 참고). 먼저 나서서 커밋하지 말 것.

## 현재 git 상태 (branch: feature/boss)
마지막 커밋: `9c48ae3` "fix : erase altar.png BG". **이번 세션 작업물은 전부 커밋 완료된 상태** (사용자가 진행 중 직접 커밋함) - working tree에 Boss2 관련 미커밋 변경 없음. `git status`에 남는 건 `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` 하나뿐인데 이번 세션 작업과 무관(건드린 적 없음, Unity 에디터가 자동으로 건드린 것으로 추정 - 무시하거나 필요시 사용자에게 확인).

관련 커밋 2개:
- `02345d8` "add : Boss Emotion , Timer , Altar Resource" - BossTimer UI 이식 + Boss Emotion 시스템 전체 이식 + altar/count 원본 리소스 추가
- `9c48ae3` "fix : erase altar.png BG" - 페이즈 전환(3->4) 로직(`Boss2Health.cs`) + altar.png 배경 제거. **커밋 메시지엔 페이즈 전환 내용이 빠져있지만 diff엔 포함되어 있음** (`git show --stat 9c48ae3`로 확인 가능) - 착각해서 다시 짜지 않도록 주의.

## 지금까지 완료된 것 (claude/boss2-handover.md에 상세 기록됨)
1. **BossTimer[ON] UI 이식** (Map2 -> Jinwook/Map3) - `GameHUD/BossUI/BossTimer[ON]`, `Boss2AttackPatterns.Start()`에서 `GameManager.StartBossTimer()` 자동 호출. `Boss2Health.TakeDamage`에서 체력 0 도달 시 `StopBossTimer()`도 연결됨.
2. **보스 감정(`Boss2Emotion`) 전체 이식** - 반사(Black/White/Navy) + 낙뢰 배율(Pink/Blue) + 하트픽업(Blue) + 키반전(Angry) + HUD 아이콘 2종(`EmotionIcon`/`ReflectIcon`). 신규 파일: `Boss2Emotion.cs`/`Boss2EmotionController.cs`/`Boss2EmotionHUD.cs`. 코디네이터는 `Boss2AttackPatterns`(`Awake`에서 컨트롤러 생성, `Start`에서 Configure+루프 시작).
3. **페이즈 전환(3->4) 기본 골격** - `Boss2Health.cs`: 하한 도달 시 `AdvancePhase()` -> `_phaseIndex` 증가 + `OnPhaseChanged` 이벤트 발행, 최종 페이즈 진입 시 `RewindManager.AcquireRewindLock`으로 리와인드 영구 잠금(처치 시까지, `OnDestroy`에서 Dispose). Play 모드에서 `MaxHealth=2`로 낮춰 하한 도달->전환->데미지 계속 적용->처치까지 전부 검증 완료(로그는 boss2-handover.md 7.6절).
4. **`Assets/03.Images/Boss/Boss3/altar/altar.png` 배경 제거** - flood-fill로 검정 배경만 투명화(내부 어두운 디테일은 안 건드림), 안티에일리어싱 경계는 알파 역산으로 자연스럽게 처리. 텍스처 임포터 설정(Alpha Is Transparency 등)은 이미 맞게 되어 있어 손 안 댐. 같은 폴더 `count.png`는 아직 미처리(요청받으면).

## 주의 - 지금 씬/에셋에 남아있는 테스트용 값
- **`Assets/08.Data/Boss2/Boss2DB.asset`의 `MaxHealth`가 지금 `2`로 설정돼 있다** (원래 `5000`). 사용자가 페이즈 전환/데미지 상한을 빠르게 반복 테스트하려고 일부러 낮춘 것 - 실제 밸런싱 확정 전까지는 이 상태로 두고 계속 테스트해도 됨. 밸런싱 작업 들어갈 때 되돌릴 것.

## 다음 할 일 (사용자가 이어서 지시할 내용, claude/boss2-handover.md 9장 TODO 참고)
- 4페이즈 전용 공격 패턴/감정(예: 3페이즈 "화남 고정") - `boss-design.md`에 "추후 확정"이라고만 되어 있어 아직 미구현. 기획 나오면 `Boss2Health.OnPhaseChanged`를 `Boss2AttackPatterns`가 구독해서 반영.
- 결정 로그 기반 정밀 리와인드(배회/돌진/공격 패턴/감정 전환 타이밍) - 지금은 위치/체력만 되감기고 나머지는 되감기 후 새로 랜덤 결정됨.
- `MaxHealth`(위 참고) 등 임시값 전체 밸런싱.
- 사망 연출/처치 처리 (`Boss2Health.OnDefeated` 훅만 있고 실제 연출 없음).
- 피격 리액션(넉백/플래시 등), Idle 외 애니메이션(Run/Attack/Hit/Death) 미구현.
- 아레나 경계 값(`-10~10`, `y=-3`) 실제 스테이지 크기에 맞춰 재조정.
- `HeartPickup` 씬 미배치 - Blue 감정의 하트 픽업이 지금 no-op (Boss1도 동일 상태, 배치하면 바로 동작함).
