// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.Boss
{
    // 보스 패턴 공용 해저드 풀 - 낙뢰/장풍/레이저/안전구역이 공유하는 사각 판정·연출 오브젝트
    public class BossHazardPool
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 슬롯 하나의 상태 스냅샷 (리와인드 기록용, 힙 할당 없음)
        public readonly struct HazardRecord
        {
            public readonly bool    Active;
            public readonly Vector2 Position;
            public readonly Vector2 Scale;
            public readonly float   RotationDeg;
            public readonly Color   Color;
            public readonly bool    HasCollider;
            public readonly int     DamageHalves;
            public readonly float   StunDuration;
            public readonly bool    InstantKill;

            public static readonly HazardRecord Inactive = new HazardRecord();

            public HazardRecord(Vector2 position, Vector2 scale, float rotationDeg, Color color,
                                bool hasCollider, int damageHalves, float stunDuration, bool instantKill)
            {
                Active       = true;
                Position     = position;
                Scale        = scale;
                RotationDeg  = rotationDeg;
                Color        = color;
                HasCollider  = hasCollider;
                DamageHalves = damageHalves;
                StunDuration = stunDuration;
                InstantKill  = instantKill;
            }
        }

        private struct PoolSlot
        {
            public GameObject     Go;
            public SpriteRenderer Renderer;
            public BoxCollider2D  Collider;
            public DamageHazard   Hazard;
        }

        /****************************************
        *                Fields
        ****************************************/

        private static Sprite  _squareSprite;  // 공용 1x1 흰 사각 스프라이트 (지연 생성 후 재사용)
        private static Texture2D _circleTexture; // 공용 원형 알파 텍스처 (파티클용, 지연 생성 후 재사용)

        private PoolSlot[] _slots;

        private bool _particleOnHitOnly; // true면 hasCollider(실제 판정)로 Alloc될 때만 파티클 재생 - 예고 단계는 재생 안 함

        public int Size => (_slots != null) ? _slots.Length : 0;

        /****************************************
        *              Constructor
        ****************************************/

        // customSprite: 기본 사각형이 아닌 특정 스프라이트를 사용할 때 지정 (미사용 슬롯의 초기 표시용으로도 쓰인다)
        // customMaterial: 기본 매테리얼이 아닌 특정 매테리얼(예: 왜곡 쉐이더)을 사용할 때 지정
        // sliceToScale: true면 Sliced + size(1,1)로 강제해 스프라이트 원본 크기와 무관하게 localScale만으로 렌더 크기가 정해진다(낙뢰처럼 긴 형태를 히트박스 비율에 맞출 때).
        //               false면 Simple 모드를 유지해 프레임마다 원본 픽셀 크기 그대로 그려진다(장풍 폭발처럼 프레임별 크기 차이를 그대로 보여주고 싶을 때).
        // particleOnHitOnly: true면 판정 있는(hasCollider) Alloc에서만 파티클을 재생한다(예고 단계 파티클 없음)
        // particleFlowAlongX: true면 파티클이 로컬 +X(가로/진행) 방향으로 지속적으로 흐른다(레이저처럼 방향성 있는 궤적용). false면 기존 임팩트 버스트(0.5초 1회)
        // prefab: 빈 게임 오브젝트 대신 미리 세팅된 프리팹(파티클 등 포함)을 기반으로 생성할 때 지정
        public BossHazardPool(int count, string namePrefix, Sprite customSprite = null, Material customMaterial = null,
                                bool attachParticle = false, float particleSize = 0.2f, Color[] particleColors = null,
                                bool sliceToScale = true, bool particleOnHitOnly = false, bool particleFlowAlongX = false,
                                float particleFlowSpeed = 3f, float particleRate = 30f)
        {
            _particleOnHitOnly = particleOnHitOnly;
            _slots = new PoolSlot[count];
            for (int i = 0; i < count; ++i)
            {
                GameObject go = new GameObject($"{namePrefix}_{i}");
                go.SetActive(false); // 비활성 상태로 생성해야 ParticleSystem의 playOnAwake 자동 재생을 막을 수 있다 (재생 중 duration 변경 시 예외 발생)

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

                if (customSprite != null)
                {
                    sr.sprite = customSprite;
                    if (sliceToScale)
                    {
                        // Sliced + size (1,1)이 스프라이트를 로컬 1x1 크기로 강제해 기존 스케일/판정 공식이 그대로 유효하다
                        sr.drawMode = SpriteDrawMode.Sliced;
                        sr.size     = Vector2.one;
                    }
                }
                else
                {
                    sr.sprite = SquareSprite();
                }

                if (customMaterial != null)
                {
                    sr.sharedMaterial = customMaterial;
                }

                // 타격 지점 표시가 필요한 풀만 파티클 컴포넌트 동적 추가
                if (attachParticle)
                {
                    ParticleSystem ps = go.AddComponent<ParticleSystem>();
                    var main = ps.main;
                    main.playOnAwake = false;
                    main.startSize = particleSize;
                    main.startColor = ParticleColorOf(particleColors); // 지정 색 중 파티클마다 랜덤
                    main.scalingMode = ParticleSystemScalingMode.Shape; // 방출 영역이 슬롯 스케일(가로/세로)을 그대로 따라가게 함

                    if (particleFlowAlongX)
                    {
                        // 레이저처럼 활성 구간 내내 진행 방향(로컬 +X)으로 흐르는 스트림 - Free() 시 비활성화로 정지
                        main.duration = 1f;
                        main.loop = true;
                        main.startLifetime = 1f;
                        main.startSpeed = 0f; // 이동은 velocityOverLifetime이 전담
                    }
                    else
                    {
                        main.duration = 0.5f;
                        main.loop = false;
                        main.startLifetime = 0.5f;
                        main.startSpeed = 10f;
                    }

                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(1f, 1f, 1f);

                    if (particleFlowAlongX)
                    {
                        var velocityOverLifetime = ps.velocityOverLifetime;
                        velocityOverLifetime.enabled = true;
                        velocityOverLifetime.space   = ParticleSystemSimulationSpace.Local;
                        velocityOverLifetime.x       = new ParticleSystem.MinMaxCurve(particleFlowSpeed);
                    }

                    var emission = ps.emission;
                    emission.rateOverTime = particleRate; // 바닥에서 스멀스멀 올라오며 튀는 연출 (흐름 모드는 밀도를 별도 지정 가능)

                    var psr = go.GetComponent<ParticleSystemRenderer>();
                    psr.material = new Material(Shader.Find("Sprites/Default")); // URP 에러(핑크색) 방지를 위해 2D 기본 쉐이더 사용
                    psr.material.mainTexture = CircleTexture(); // 기본 사각 파티클 대신 원형으로 렌더링
                    psr.sortingOrder = 100; // 배경 위에 확실히 렌더링되게 보장
                }

                _slots[i] = new PoolSlot { Go = go, Renderer = sr };
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 비활성 슬롯 하나를 지정 상태로 활성화. 전부 사용 중이면 -1 </summary>
        public int Alloc(Vector2 pos, Vector2 scale, Color color, bool hasCollider,
                        int damageHalves = Constants.Player.HALVES_PER_HEART,
                        float stunDuration = 0f, bool instantKill = false, float rotationDeg = 0f)
        {
            if (_slots == null)
            {
                return -1;
            }
            for (int i = 0; i < _slots.Length; ++i)
            {
                if ((_slots[i].Go != null) && (!_slots[i].Go.activeSelf))
                {
                    Configure(i, pos, scale, rotationDeg, color, hasCollider, damageHalves, stunDuration, instantKill);
                    return i;
                }
            }
            return -1;
        }

        /// <summary> 슬롯이 살아 있고 활성 상태인지. 풀 해제 후 잔여 코루틴의 안전 가드용 </summary>
        public bool IsActive(int i)
        {
            if ((_slots == null) || (i < 0) || (i >= _slots.Length) || (_slots[i].Go == null))
            {
                return false;
            }
            return _slots[i].Go.activeSelf;
        }

        /// <summary> 활성 슬롯 위치 이동 (낙뢰 낙하/장풍 상승 등 이동 패턴용) </summary>
        public void SetPosition(int i, Vector2 pos)
        {
            if (IsActive(i))
            {
                _slots[i].Go.transform.position = pos;
            }
        }

        /// <summary> 활성 슬롯 스케일 갱신 (레이저 회수처럼 점점 좁아지는 연출용). 콜라이더 크기도 함께 변한다 </summary>
        public void SetScale(int i, Vector2 scale)
        {
            if (IsActive(i))
            {
                _slots[i].Go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            }
        }

        /// <summary> 렌더러만 켜고 끈다 (판정은 유지). 슬로우 중에만 보이는 안전구역/경고 깜빡임용 </summary>
        public void SetVisible(int i, bool visible)
        {
            if (IsActive(i))
            {
                _slots[i].Renderer.enabled = visible;
            }
        }

        /// <summary> 콜라이더/피해 판정만 켜고 끈다 (시각 연출은 유지). 강타 후반 무판정 프레임 표현용 </summary>
        public void SetColliderActive(int i, bool active)
        {
            if (!IsActive(i))
            {
                return;
            }
            if (_slots[i].Collider != null)
            {
                _slots[i].Collider.enabled = active;
            }
            if (_slots[i].Hazard != null)
            {
                _slots[i].Hazard.enabled = active;
            }
        }

        /// <summary> 활성 슬롯의 스프라이트를 바꾼다 (낙뢰 크랙클 프레임 순환 등 연출용) </summary>
        public void SetSprite(int i, Sprite sprite)
        {
            if ((IsActive(i)) && (sprite != null))
            {
                _slots[i].Renderer.sprite = sprite;
            }
        }

        /// <summary> 슬롯 비활성화(풀 반환). 잔여 코루틴 대비 널/범위 가드 </summary>
        public void Free(int i)
        {
            if ((_slots == null) || (i < 0) || (i >= _slots.Length) || (_slots[i].Go == null))
            {
                return;
            }
            _slots[i].Go.SetActive(false);
            if (_slots[i].Collider != null)
            {
                _slots[i].Collider.enabled = false;
            }
            if (_slots[i].Hazard != null)
            {
                _slots[i].Hazard.enabled = false;
            }
        }

        /// <summary> 전체 슬롯 반환 </summary>
        public void FreeAll()
        {
            if (_slots == null)
            {
                return;
            }
            for (int i = 0; i < _slots.Length; ++i)
            {
                Free(i);
            }
        }

        /// <summary> 풀 파괴. 페이즈 Exit 등 소유자가 수명을 끝낼 때 호출 </summary>
        public void Dispose()
        {
            if (_slots == null)
            {
                return;
            }
            for (int i = 0; i < _slots.Length; ++i)
            {
                if (_slots[i].Go != null)
                {
                    Object.Destroy(_slots[i].Go);
                }
            }
            _slots = null;
        }

        /// <summary> 슬롯 상태를 스냅샷으로 캡처 (리와인드 기록) </summary>
        public HazardRecord Capture(int i)
        {
            if (!IsActive(i))
            {
                return HazardRecord.Inactive;
            }

            Transform tr = _slots[i].Go.transform;
            Vector3   ls = tr.localScale;
            bool  hasCollider   = (_slots[i].Collider != null) && (_slots[i].Collider.enabled);
            int   damageHalves  = (_slots[i].Hazard != null) ? _slots[i].Hazard.DamageHalves : 0;
            float stunDuration  = (_slots[i].Hazard != null) ? _slots[i].Hazard.StunDuration : 0f;
            bool  instantKill   = (_slots[i].Hazard != null) && (_slots[i].Hazard.InstantKill);

            return new HazardRecord(tr.position, new Vector2(ls.x, ls.y), tr.eulerAngles.z,
                                    _slots[i].Renderer.color, hasCollider, damageHalves, stunDuration, instantKill);
        }

        /// <summary> 스냅샷 내용대로 슬롯을 복원 (리와인드 역재생) </summary>
        public void Apply(int i, HazardRecord rec)
        {
            if ((_slots == null) || (i < 0) || (i >= _slots.Length) || (_slots[i].Go == null))
            {
                return;
            }
            if (rec.Active)
            {
                Configure(i, rec.Position, rec.Scale, rec.RotationDeg, rec.Color,
                        rec.HasCollider, rec.DamageHalves, rec.StunDuration, rec.InstantKill);
            }
            else
            {
                Free(i);
            }
        }

        // 슬롯을 지정 상태로 활성화. 콜라이더/해저드는 처음 필요할 때 1회만 추가(지연 생성)
        private void Configure(int i, Vector2 pos, Vector2 scale, float rotationDeg, Color color,
                                bool hasCollider, int damageHalves, float stunDuration, bool instantKill)
        {
            Transform tr = _slots[i].Go.transform;
            tr.position   = pos;
            tr.localScale = new Vector3(scale.x, scale.y, 1f);
            tr.rotation   = Quaternion.Euler(0f, 0f, rotationDeg);

            _slots[i].Renderer.color   = color;
            _slots[i].Renderer.enabled = true;
            _slots[i].Go.SetActive(true);

            // 파티클 시스템이 있다면 풀에서 꺼낼 때마다 강제 재생 (단, particleOnHitOnly면 판정 없는 예고 단계는 재생 안 함)
            ParticleSystem ps = _slots[i].Go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if ((!_particleOnHitOnly) || hasCollider)
                {
                    ps.Play(true);
                }
            }

            if (hasCollider)
            {
                if (_slots[i].Collider == null)
                {
                    _slots[i].Collider           = _slots[i].Go.AddComponent<BoxCollider2D>();
                    _slots[i].Collider.isTrigger = true;
                    _slots[i].Collider.size      = Vector2.one; // 판정 크기를 스프라이트 종류와 무관하게 로컬 1x1로 고정
                }
                _slots[i].Collider.enabled = true;

                if (_slots[i].Hazard == null)
                {
                    _slots[i].Hazard = _slots[i].Go.AddComponent<DamageHazard>();
                }
                _slots[i].Hazard.enabled = true;
                _slots[i].Hazard.Configure(damageHalves, stunDuration, instantKill);
            }
            else
            {
                // 텔레그래프/안전구역은 보이기만 하고 피해 판정이 없다
                if (_slots[i].Collider != null)
                {
                    _slots[i].Collider.enabled = false;
                }
                if (_slots[i].Hazard != null)
                {
                    _slots[i].Hazard.enabled = false;
                }
            }
        }

        private static Sprite SquareSprite()
        {
            if (_squareSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                _squareSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            return _squareSprite;
        }

        // 공용 원형 알파 텍스처 - 파티클이 기본 사각형 대신 동그랗게 보이도록 소프트 엣지로 1회 생성
        private static Texture2D CircleTexture()
        {
            if (_circleTexture == null)
            {
                const int SIZE = 32; // 작은 파티클용이라 이 해상도로 충분

                _circleTexture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
                Vector2 center = new Vector2((SIZE - 1) * 0.5f, (SIZE - 1) * 0.5f);
                float   radius = SIZE * 0.5f;

                for (int y = 0; y < SIZE; ++y)
                {
                    for (int x = 0; x < SIZE; ++x)
                    {
                        float dist  = Vector2.Distance(new Vector2(x, y), center);
                        float alpha = Mathf.Clamp01(1f - (dist / radius));
                        _circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                _circleTexture.Apply();
            }
            return _circleTexture;
        }

        // 파티클 시작 색 결정 - 색 사이를 블렌딩하지 않고 구간을 딱 나눠 4색 중 하나가 랜덤으로 뽑힌 것처럼 보이게 한다
        private static ParticleSystem.MinMaxGradient ParticleColorOf(Color[] colors)
        {
            if ((colors == null) || (colors.Length == 0))
            {
                return new ParticleSystem.MinMaxGradient(Color.white);
            }

            const float EDGE_EPSILON = 0.001f; // 구간 경계에서 색이 섞이지 않게 하는 최소 간격

            int colorCount = colors.Length;
            GradientColorKey[] colorKeys = new GradientColorKey[colorCount * 2];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };

            for (int i = 0; i < colorCount; ++i)
            {
                float segmentStart = (float)i / colorCount;
                float segmentEnd   = (float)(i + 1) / colorCount;

                colorKeys[i * 2]       = new GradientColorKey(colors[i], (i == 0) ? segmentStart : segmentStart + EDGE_EPSILON);
                colorKeys[(i * 2) + 1] = new GradientColorKey(colors[i], (i == colorCount - 1) ? segmentEnd : segmentEnd - EDGE_EPSILON);
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);

            return new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
        }
    }
}
