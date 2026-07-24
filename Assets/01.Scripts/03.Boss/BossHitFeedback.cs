// Unity
using UnityEngine;

using Minsung.TimeSystem;

namespace Minsung.Boss
{
    // 보스 피격 타격감 - IBossHittable.OnDamaged에 히트스톱 + 스파크 파티클을 연결한다 (Boss1/Boss2 공용)
    // 수치는 시각/체감 튜닝값이라 컴포넌트 SerializeField로 노출한다(밸런싱 DB 아님)
    [AddComponentMenu("Minsung/Boss Hit Feedback")]
    public class BossHitFeedback : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("참조 (비우면 부모에서 자동 탐색)")]
        [SerializeField] private MonoBehaviour _healthSource; // IBossHittable 구현체(BossController/Boss2Health)
        [SerializeField] private Transform     _hitCenter;    // 스파크가 튀는 중심 - 비우면 이 오브젝트

        [Header("히트스톱")]
        [SerializeField] private bool  _useHitStop      = true;
        [SerializeField] private float _hitStopDuration = 0.05f; // 적중 순간 프레임 정지(초)

        [Header("스파크")]
        [SerializeField] private bool      _useSparks       = true;
        [SerializeField] private int       _sparkCountMin   = 5;
        [SerializeField] private int       _sparkCountMax   = 9;
        [SerializeField] private float     _sparkSpeed      = 6f;
        [SerializeField] private float     _sparkSize       = 0.18f;
        [SerializeField] private float     _sparkLifetime   = 0.35f;
        [SerializeField] private float     _sparkSpawnRadius = 0.8f; // 중심에서 이 반경 안 랜덤 위치에서 터진다
        [SerializeField] private Color     _sparkColor      = new Color(1f, 0.85f, 0.4f, 1f); // 밝은 노랑-화이트
        [SerializeField] private Texture2D _sparkTexture;   // 소프트 원형 점(예: glow_portal) - 비우면 흰 사각형

        private IBossHittable  _health;
        private ParticleSystem _sparks;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            ResolveHealth();
            if (_hitCenter == null)
            {
                _hitCenter = transform;
            }
            if (_useSparks)
            {
                BuildSparks();
            }
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDamaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDamaged -= HandleDamaged;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        private void ResolveHealth()
        {
            if (_healthSource != null)
            {
                _health = _healthSource as IBossHittable;
            }
            if (_health == null)
            {
                _health = GetComponentInParent<IBossHittable>();
            }
        }

        private void HandleDamaged(float applied)
        {
            if (_useHitStop)
            {
                HitStopController.Request(_hitStopDuration);
            }
            if (_useSparks && (_sparks != null))
            {
                EmitSparks();
            }
        }

        private void EmitSparks()
        {
            Vector2 offset = Random.insideUnitCircle * _sparkSpawnRadius;
            _sparks.transform.position = _hitCenter.position + (Vector3)offset;
            _sparks.Emit(Random.Range(_sparkCountMin, _sparkCountMax + 1));
        }

        private void BuildSparks()
        {
            GameObject go = new GameObject("BossHitSparks");
            go.transform.SetParent(transform, false);
            _sparks = go.AddComponent<ParticleSystem>();
            _sparks.Stop();

            ParticleSystem.MainModule main = _sparks.main;
            main.loop             = false;
            main.playOnAwake      = false;
            main.startLifetime    = _sparkLifetime;
            main.startSpeed       = _sparkSpeed;
            main.startSize        = _sparkSize;
            main.startColor       = _sparkColor;
            main.gravityModifier  = 1.2f;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.maxParticles     = 200;

            ParticleSystem.EmissionModule emission = _sparks.emission;
            emission.rateOverTime = 0f; // 버스트만 - Emit로 직접 발사

            ParticleSystem.ShapeModule shape = _sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 0.05f;

            ParticleSystem.SizeOverLifetimeModule sizeOverLife = _sparks.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size    = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            ParticleSystemRenderer psRenderer = go.GetComponent<ParticleSystemRenderer>();
            psRenderer.material         = BuildSparkMaterial();
            psRenderer.sortingLayerName = "Default";
            psRenderer.sortingOrder     = 50; // 보스/구조물 앞에 튀도록
        }

        // 가산(Additive) 파티클 머티리얼을 만든다 - 검정 배경 없이 발광만 더해지고 Bloom과 어울린다
        private Material BuildSparkMaterial()
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Additive");
            }
            Material mat = new Material(shader);
            if (_sparkTexture != null)
            {
                mat.mainTexture = _sparkTexture;
            }
            return mat;
        }
    }
}
