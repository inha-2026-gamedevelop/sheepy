# 수업: 보스 3페이즈 레이저에 쉐이더/파티클 입히기

> 이 문서는 `claude/` 팀 공용 문서 목록(README/PLAN/UML 등)에 포함된 정식 문서가 아니라,
> 한 세션 동안 진행한 작업을 복기하며 남긴 개인 학습 노트다. 선생님이 제자에게 설명하듯
> "왜 이렇게 했는가"에 초점을 맞췄다.

대상 코드: [Phase3State.cs](../Assets/01.Scripts/03.Boss/Phase3State.cs), [BossHazardPool.cs](../Assets/01.Scripts/03.Boss/BossHazardPool.cs), [BossDataSO.cs](../Assets/01.Scripts/00.Common/Data/BossDataSO.cs), [Phase3LaserBeam.shader](../Assets/09.Shaders/Phase3LaserBeam.shader)

---

## 1. 시작하기 전에: 지금 뭘 고치는 건지부터 정확히 알기

새 기능을 만들 때 제일 먼저 할 일은 "이미 있는 코드가 뭘 하고 있는지" 읽는 것이다. 손부터 대면 안 된다.

`Phase3_Laser_0`이라는 이름의 오브젝트가 씬에 보여서, 이게 미리 배치된 오브젝트인 줄 알았지만
실제로는 [Phase3State.cs](../Assets/01.Scripts/03.Boss/Phase3State.cs)의 `Enter()`가 런타임에
`BossHazardPool`을 통해 동적으로 찍어내는 풀 슬롯이었다. 씬 뷰에 보인다고 다 프리팹/수동 배치는
아니라는 걸 확인한 것 - MCP로 컴포넌트를 직접 조회해서 확인했다.

발사 흐름은 이랬다:

```
CoFireCrossLaser()
  1) 경고: 판정 없는 슬롯을 Alloc, SetVisible로 점멸 토글
  2) 발사: 같은 슬롯을 판정 있는 상태로 재Alloc, 지속시간만큼 유지
  3) Free
```

그리고 색/두께/시간 같은 숫자는 전부 코드에 박혀 있지 않고 `BossDataSO`(GameDB)에 있었다.
이게 이 프로젝트의 규칙이다 - **매직넘버 금지, 밸런싱 값은 SO로**. 이 규칙을 알아야 이후에
"점멸 주기 줄여줘" 같은 요청이 왔을 때 코드가 아니라 SO 필드를 고쳐야 한다는 게 바로 나온다.

**교훈**: 기존 컨벤션을 먼저 찾아라. 이 프로젝트는 이미 `Phase1State`에서 왜곡 쉐이더를
매테리얼로 붙인 선례(`Resources.Load<Material>("WavyGimmickMat")`)가 있었다. 새로 만들기 전에
"비슷한 걸 이미 해놓은 코드가 있는가"부터 찾으면, 남는 건 그 패턴을 그대로 따라가는 것뿐이다.

---

## 2. 쉐이더를 붙이기로 한 이유 - "스프라이트냐 쉐이더냐"

이 질문이 이번 수업에서 제일 중요하다. 답은 하나가 아니라 **"뭘 만드는지에 달렸다"**.

| | 스프라이트시트(플립북) | 쉐이더 |
|---|---|---|
| GPU 연산 | 텍스처 샘플 1회, 제일 쌈 | ALU 연산 추가(smoothstep/sin/pow) - 그래도 언릿이면 가벼움 |
| 아트 비용 | 프레임 수만큼 그림 필요, 재작업 비쌈 | 코드로 즉시 튜닝(속도/색/강도) |
| 유연성 | 낮음(정해진 프레임만) | 높음(런타임 파라미터 조정) |

결론: **이 케이스는 쉐이더가 나은 선택**이었다. 이유는 연산량 자체보다 "동시에 몇 개나 떠 있냐"가
더 중요했기 때문이다.

```csharp
private const int LASER_POOL_SIZE = 2; // 동시 사용 최대: 경고 또는 발사 1 + 여유 1
```

동시에 화면에 뜨는 인스턴스가 최대 2개인데, 여기에 ALU 몇 개 더 들어간다고 병목이 생기지 않는다.
**"쉐이더가 스프라이트보다 무겁다"는 명제 자체는 맞지만, 무거운 정도가 예산 안에 있으면 의미 없는
걱정**이라는 걸 기억해라. 성능 최적화는 항상 "실제로 병목이 되는 지점"을 먼저 찾고 나서
얘기해야 한다 - 추측으로 미리 겁먹지 말 것.

나중에 파티클을 추가하고 나서 이 질문을 다시 받았을 때도 같은 방식으로 답했다:
"어떤 부분이 새로 무거워졌는가"를 구체적으로 짚었다 - 파티클 80개/초, 근데 Unity 파티클
시스템은 한 시스템 안 파티클을 보통 드로우콜 1회로 배칭하니 정점 처리량 자체는 미미하다,
라고. **"괜찮다"고 말할 때는 항상 왜 괜찮은지 숫자나 메커니즘으로 설명할 것.** 그래야 나중에
정말 병목이 생겼을 때 뭘 의심해야 할지 알 수 있다.

---

## 3. 쉐이더 설계 - 레이저를 "그냥 색칠한 사각형"에서 "에너지빔"으로

`SpriteRenderer.color`만 쓰던 기존 코드는 그냥 단색 사각형이었다. 에너지빔처럼 보이려면
뭐가 필요한지 하나씩 나눠서 생각했다:

1. **코어(Core)** - 두께 방향(UV.y) 중심이 제일 밝고, 바깥으로 갈수록 페이드. `distFromCenter`를
   `abs(uv.y - 0.5) * 2`로 만들면 중심이 0, 가장자리가 1이 되는 간단한 트릭이다.
2. **글로우(Glow)** - 코어보다 더 넓게, 더 부드럽게 퍼지는 외곽광. `pow(1-distFromCenter, falloff)`.
3. **흐름(Flow)** - 길이 방향(UV.x)으로 스크롤되는 패턴. `_Time.y`를 UV에 더해서 시간에 따라
   움직이게 한다. 이게 없으면 그냥 멈춰있는 그라데이션 사각형일 뿐이라 "발사되는 느낌"이 안 산다.
4. **펄스(Pulse)** - `sin(_Time.y * speed)`로 밝기를 미세하게 흔들어서 살아있는 느낌을 준다.
5. **팁 페이드(Tip Fade)** - 시작/끝 지점을 살짝 죽여서 딱딱한 사각형 모서리를 감춘다.
   판정(BoxCollider2D)은 별개로 관리되니 시각만 taper해도 히트박스엔 영향 없다.

블렌드 모드는 `Blend SrcAlpha One`(가산)을 골랐다. 기존 `SheepyGodRay.shader`가 이미 이 패턴을
쓰고 있어서 그대로 따라갔다 - **팀 컨벤션이 있으면 그걸 베끼는 게 정답이다, 새로 발명하지 마라.**
가산 블렌딩을 쓸 때 주의할 점 하나: 프래그먼트가 `rgb * alpha`를 미리 계산해서 리턴하면 안 된다.
GPU가 `Blend SrcAlpha One`으로 알아서 `rgb * alpha`를 계산하니, 쉐이더는 `alpha` 채널에만
강도를 실어 보내면 된다. 이걸 안 지키면 이중으로 곱해져서 이상하게 어두워진다.

`_Color` 프로퍼티 이름은 우연이 아니다. `SpriteRenderer.color`를 바꾸면 Unity가 자동으로
매테리얼의 `_Color` 프로퍼티에 그 값을 밀어 넣어준다(프로퍼티 이름이 정확히 `_Color`여야 함).
그래서 기존 코드가 `_laserPool.Alloc(..., color, ...)`로 색을 넘기던 방식이 새 쉐이더에서도
그대로 작동한다 - 경고 때는 alpha 0.5라 은은하게, 발사 때는 alpha 1.0이라 진하게. 코드 한 줄도
안 건드리고 공짜로 얻은 효과다.

**교훈**: 이펙트는 "레이어를 하나씩 쌓아라". 코어+글로우+흐름+펄스+팁페이드를 한 번에
설계하려 하지 말고, 각각이 뭘 위한 건지 말로 설명할 수 있어야 코드로도 깔끔하게 나온다.

---

## 4. 매테리얼을 코드에 연결하는 법 - 기존 패턴 재사용

쉐이더 파일만 만든다고 끝이 아니다. `SpriteRenderer`에 붙일 매테리얼 에셋이 필요하고,
그걸 코드에서 로드해야 한다. `Phase1State`가 이미 이렇게 하고 있었다:

```csharp
Material wavyMat = Resources.Load<Material>("WavyGimmickMat");
_pool = new BossHazardPool(POOL_SIZE, "Phase1_Gimmick", null, wavyMat);
```

그래서 똑같이:

```csharp
Material laserMat = Resources.Load<Material>("Phase3LaserBeamMat");
_laserPool = new BossHazardPool(LASER_POOL_SIZE, "Phase3_Laser", customMaterial: laserMat, ...);
```

매테리얼을 `Assets/Resources/`에 둬야 `Resources.Load`가 찾는다 - 이건 Unity의 규칙이라
어길 수 없다. `Phase3State`는 MonoBehaviour가 아니라 일반 C# 클래스라서
(`Phase3State : Phase2State`, 필드 초기화식에서 GameDB를 못 쓰는 MonoBehaviour의 제약과는
다른 얘기지만) 매테리얼도 SerializeField로 미리 참조를 박아둘 수 없으니, `Enter()` 시점에
`Resources.Load`로 가져오는 수밖에 없다. 이런 구조적 제약을 이해하고 있어야 "왜 굳이
Resources 폴더를 쓰나요, 그냥 필드에 참조 넣으면 안 되나요?" 라는 질문에 답할 수 있다.

---

## 5. 피드백을 받고 고친 것들 - 매번 "값이냐 구조냐"부터 구분

사용자 피드백 3개를 받았을 때, 첫 번째로 한 일은 "이게 숫자 하나 바꾸면 되는 건지,
구조를 새로 만들어야 하는 건지" 구분한 것이다.

### 5-1. "점멸 주기 좀 더 짧게"
이건 순수 밸런싱 값이다. `BossDataSO`의 `_phase3LaserBlinkInterval`을 `0.25f -> 0.1f`로.
코드 로직은 한 글자도 안 건드렸다.

### 5-2. "발사 색상 진하게(레드 계열)"
이것도 값 조정이 메인이지만, 한 군데 더 있었다. `_phase3LaserColor`를 `(1, 0.1, 0.1)`에서
`(0.85, 0, 0.05)`로 채도 있는 진한 빨강으로 바꿨는데, 쉐이더의 `_CoreColor`가 순백색
`(2,2,2)`였다는 걸 떠올려야 한다. 코어가 새하얗게 타버리면 아무리 베이스 색을 진하게 해도
"전체적으로는 하얀 빔"처럼 보인다. 그래서 매테리얼의 `_CoreColor`도 `(2.4, 0.35, 0.25)`처럼
붉은 기가 도는 HDR 값으로 같이 바꿔야 진짜 "진한 빨강"으로 읽힌다.
**교훈**: 색 하나를 진하게 만들어달라는 요청이어도, 그 색이 렌더링 파이프라인 어디서
다른 색과 섞이는지(여기선 코어 블렌딩) 같이 봐야 요청한 결과가 실제로 나온다.

### 5-3. "본 타격과 같은 색 계열 파티클을 진행방향으로 흐르게"
이건 값 조정이 아니라 **새 기능**이었다. 여기서부터가 진짜 설계 판단이 필요한 부분이다.

먼저 기존 `BossHazardPool`에 이미 `attachParticle` 옵션이 있었다(낙뢰/장풍 타격 시 스파크).
그런데 뜯어보니 이건 "한 번 Alloc될 때 0.5초짜리 버스트가 터지고 끝"인 구조였다
(`main.loop = false; main.duration = 0.5f;`). 우리가 원한 건 발사 지속시간(~1초) 내내
계속 흐르는 스트림이었으니 그대로 못 썼다.

또 하나 문제: 기존 구조는 `Alloc`될 때마다(예고든 발사든) 무조건 파티클을 재생했다.
그런데 우리는 "예고(점멸) 때는 파티클 없이, 발사 때만" 원했다. `hasCollider` 파라미터가
이미 예고(false)/발사(true)를 구분하고 있었으니, 이걸 게이트 조건으로 재사용하면 됐다.

그래서 두 가지 선택지가 있었다:
- (A) `Phase3State`에서 별도의 독립 파티클 시스템을 직접 만들어 관리
- (B) `BossHazardPool`을 확장해서 새 옵션(`particleOnHitOnly`, `particleFlowAlongX`)을 추가

(B)를 골랐다. 이유: 기존 `Phase2State`/`BossLightningPattern`도 다 이 풀을 통해 파티클을
쓰고 있어서, 같은 문제(버스트 vs 스트림, 예고 시 재생 여부)를 나중에 다른 패턴이 또 필요로
할 가능성이 높다고 봤다. 단, **기존 호출부를 절대 깨면 안 된다** - 그래서 새 파라미터는
전부 끝에 옵션(기본값 있는 파라미터)으로 추가해서, 기존 `Lightning`/`Phase2Wave` 호출은
코드 한 줄도 안 바꾸고 그대로 예전처럼 동작하게 했다.

```csharp
public BossHazardPool(int count, string namePrefix, Sprite customSprite = null, Material customMaterial = null,
                      bool attachParticle = false, float particleSize = 0.2f, Color[] particleColors = null,
                      bool sliceToScale = true, bool particleOnHitOnly = false, bool particleFlowAlongX = false,
                      float particleFlowSpeed = 3f, float particleRate = 30f)
```

**교훈**: 공유 클래스를 확장할 때는 "새 요구사항을 만족시키는 것"과 "기존 사용자를 안 건드리는 것"을
동시에 만족해야 한다. 그 답은 대개 "새 파라미터를 옵션으로, 기본값은 기존 동작 그대로"다.

색상 4종은 `BossDataSO`에 `Phase3LaserFlowColors`로 추가했다. 이것도 이미 `Phase2WaveParticleColors`,
`LightningParticleColors` 같은 선례가 있어서 그대로 패턴을 따랐다 - "보라 계열 4색" 처럼
"빨강 계열 4색"으로.

### 5-4. "파티클 좀 더 풍부하고 빠르게"
`particleFlowSpeed`(속도)와 `particleRate`(밀도)를 GameDB 값으로 올렸다. 여기서 재밌는 디테일:
기존 코드는 `emission.rateOverTime = 30`이 하드코딩이었다. 이걸 파라미터화하면서
"기본값을 30으로 유지"해서 다른 호출부엔 영향이 없게 했다 - 5-3에서 세운 원칙을 그대로 반복 적용.

### 5-5. "레이저 거둘 때 점점 좁아지는 느낌"
이것도 새 기능이다. 여기서 핵심 판단은 "판정과 시각을 분리해서 생각하라"였다.

좁아지는 애니메이션을 만들려고 두께(로컬 Y스케일)를 서서히 줄이면, `BoxCollider2D`는
로컬 좌표 기준 크기(`Vector2.one`)라서 **트랜스폼 스케일이 콜라이더 크기에도 그대로 곱해진다**.
즉 아무 조치 없이 스케일만 줄이면 판정도 같이 얇아진다 - 얼핏 괜찮아 보이지만, "판정이 아직
켜진 채로 서서히 사라지는 레이저"는 플레이어 입장에서 예측 불가능한 억울한 히트를 만들 수 있다.

그래서 순서를 이렇게 잡았다: **판정을 먼저 확실히 끈다(`SetColliderActive(false)`) -> 그 다음에
시각만 서서히 줄인다.** 이미 `SetColliderActive`가 "강타 후반 무판정 프레임 표현용"으로
존재하고 있었으니 그대로 재사용. 새로 만든 건 `SetScale` 하나뿐이다.

```csharp
_laserPool.SetColliderActive(laserSlot, false);
float retractElapsed = 0f;
while (retractElapsed < GameDB.Boss.Phase3LaserRetractTime)
{
    yield return null; // 매 프레임 - WaitForSeconds 캐싱 규칙은 "반복 대기"에 대한 것이라 여긴 해당 없음
    retractElapsed += Time.deltaTime;
    float t = Mathf.Clamp01(retractElapsed / GameDB.Boss.Phase3LaserRetractTime);
    _laserPool.SetScale(laserSlot, new Vector2(scale.x, Mathf.Lerp(scale.y, 0f, t)));
}
_laserPool.Free(laserSlot);
```

한 가지 더 확인해야 했던 것: **이 되감기(리와인드) 게임에서 이 애니메이션이 되감기와 충돌하지
않는가?** 다행히 `BossHazardPool.Capture()`가 매 틱마다 `transform.localScale`을 그대로
스냅샷하고 있었다. 즉 좁아지는 애니메이션이 "코드 로직"이 아니라 "트랜스폼 상태"로 존재하는 한,
리와인드 시스템은 이미 그 상태를 프레임 단위로 기록/복원한다. **새 기능을 만들 때는 항상
"이게 이미 있는 다른 시스템(여기선 리와인드)과 자연스럽게 맞물리는가"를 확인해야 한다** - 여기선
운 좋게 맞물렸지만, 안 맞물렸다면 `Phase3Frame` 구조체에 필드를 더 추가해야 했을 것이다.

`yield return null`을 쓴 것도 의도적인 선택이다. 이 파일의 다른 대기는 전부
`WaitForSeconds` 필드를 캐싱해서 쓰는데(GC 방지 컨벤션), 그건 "고정된 시간만큼 반복 대기"할 때
얘기고, 여기처럼 "매 프레임 보간값을 계산해야 하는" 경우엔 애초에 GC 할당이 없는
`yield return null`이 맞는 도구다. 컨벤션은 문자 그대로 따르는 게 아니라 그 규칙이 왜
있는지(GC 압박 방지) 이해하고 적용해야 한다.

---

## 6. 전체 요약 - 다음에 비슷한 요청이 오면 이렇게 접근해라

1. **기존 코드부터 읽어라.** 비슷한 패턴이 이미 있으면(Phase1의 왜곡 쉐이더, Lightning/Phase2Wave의
   파티클) 그걸 베껴라. 발명하지 마라.
2. **값 조정과 구조 변경을 구분해라.** "짧게/진하게" 같은 요청은 대개 GameDB 필드 하나 고치면
   끝난다. "~하는 연출을 추가해줘"는 새 메서드/파라미터가 필요하다.
3. **공유 코드를 확장할 때는 기존 호출부를 절대 깨지 마라.** 새 파라미터는 끝에, 기본값은
   기존 동작 그대로.
4. **시각과 판정을 분리해서 생각해라.** 연출이 화려해질수록 "지금 이게 실제로 플레이어를
   때리고 있는가"를 항상 따로 확인해라.
5. **다른 시스템(리와인드, 슬로우모션)과 자연스럽게 맞물리는지 확인해라.** 운에 맡기지 말고
   왜 맞물리는지 설명할 수 있어야 한다.
6. **성능 질문엔 숫자로 답해라.** "쉐이더가 더 무겁다"는 맞지만 "그래서 문제가 되는가"는
   동시 인스턴스 수, 드로우콜 배칭 여부 같은 구체적 근거로 답해야 한다.
