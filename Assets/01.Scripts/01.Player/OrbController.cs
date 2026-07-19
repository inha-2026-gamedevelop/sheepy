// System
using System;
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Utility;

namespace Minsung.Player
{
    // 플레이어를 따라다니는 오브 하나 - 평소엔 대기 지점 주변을 펄린 노이즈로 떠다니며 따라오고, Attack() 호출 시 대상에게 돌진해 도달하면 타격 콜백을 실행한 뒤 복귀한다.
    public class OrbController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string ORB_TRAIL_SHADER   = "Minsung/OrbTrail";
        private const int    ORB_ORDER_OFFSET   = 1;
        private const int    TRAIL_ORDER_OFFSET = -2;
        private const float  TRAIL_START_TIME   = 0f;
        private const float  TRAIL_END_TIME     = 1f;
        private const float  TRAIL_END_ALPHA    = 0f;

        private static readonly int GLOW_COLOR     = Shader.PropertyToID("_GlowColor");
        private static readonly int GLOW_INTENSITY = Shader.PropertyToID("_GlowIntensity");
        private static readonly int PULSE_SPEED    = Shader.PropertyToID("_PulseSpeed");
        private static readonly int PULSE_AMOUNT   = Shader.PropertyToID("_PulseAmount");

        [SerializeField] private Material _trailMaterial;

        private Transform _followTarget;
        private float _noiseSeed;   // 오브마다 다른 펄린 노이즈 좌표를 쓰기 위한 시드 (겹침 방지)
        private Vector2 _slotOffset; // 대기 지점 기준 오브간 간격 (겹치지 않도록 벌려놓는 고정 오프셋)
        private Vector3 _velocity;  // SmoothDamp용
        private Coroutine _coAttack;
        private SpriteRenderer _spriteRenderer;
        private TrailRenderer _trailRenderer;
        private Material _runtimeTrailMaterial;
        private MaterialPropertyBlock _spriteProperties;
        private PlayerDataSO _playerSo; // 오브 밸런싱 DB 캐시 (매 프레임 떠다니기 계산에 사용)

        public bool IsAttacking => _coAttack != null;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _playerSo = GameDB.Player;
            TryGetComponent(out _spriteRenderer);

            ConfigureSpriteGlow();
            ConfigureTrail();
        }

        private void OnEnable()
        {
            SetTrailEmitting(true);
        }

        private void OnDisable()
        {
            _coAttack = null;
            SetTrailEmitting(false);
        }

        private void OnDestroy()
        {
            if (_runtimeTrailMaterial != null)
            {
                Destroy(_runtimeTrailMaterial);
            }
        }

        private void Update()
        {
            if (IsAttacking || (_followTarget == null))
            {
                return;
            }

            Vector3 anchor = _followTarget.position + (Vector3)WanderOffset();
            transform.position = Vector3.SmoothDamp(transform.position, anchor, ref _velocity, _playerSo.OrbFollowSmooth);
        }

        /****************************************
        *                Methods
        ****************************************/

        public void Init(Transform followTarget, float noiseSeed, Vector2 slotOffset)
        {
            _followTarget       = followTarget;
            _noiseSeed          = noiseSeed;
            _slotOffset         = slotOffset;
            transform.position  = followTarget.position + (Vector3)WanderOffset();

            ConfigureRenderingOrder();
            ClearTrail();
            SetTrailEmitting(true);
        }

        /// <summary> 대기 위치로 즉시 순간이동. 주인(분신)이 풀에서 다시 활성화될 때 호출. </summary>
        public void SnapToFollowTarget()
        {
            if (_followTarget == null)
            {
                return;
            }

            ClearTrail();
            _velocity          = Vector3.zero; // 이전 활성 시점의 관성 제거
            transform.position = _followTarget.position + (Vector3)WanderOffset();
        }

        // 몸통 왼쪽 위 대기 지점(OrbAnchorOffset) 기준, 펄린 노이즈로 자유롭게 떠다니는 오프셋
        private Vector2 WanderOffset()
        {
            float t  = Time.time * _playerSo.OrbWanderSpeed;
            float nx = (Mathf.PerlinNoise(_noiseSeed, t) * 2f) - 1f;
            float ny = (Mathf.PerlinNoise(_noiseSeed + 91.7f, t) * 2f) - 1f;
            Vector2 wander = new Vector2(nx, ny) * _playerSo.OrbWanderRadius;
            return _playerSo.OrbAnchorOffset + _slotOffset + wander;
        }

        // 오브 구체 글로우 설정
        private void ConfigureSpriteGlow()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            if (_spriteProperties == null)
            {
                _spriteProperties = new MaterialPropertyBlock();
            }

            _spriteRenderer.GetPropertyBlock(_spriteProperties);
            _spriteProperties.SetColor(GLOW_COLOR, _playerSo.OrbGlowColor);
            _spriteProperties.SetFloat(GLOW_INTENSITY, _playerSo.OrbGlowIntensity);
            _spriteProperties.SetFloat(PULSE_SPEED, _playerSo.OrbPulseSpeed);
            _spriteProperties.SetFloat(PULSE_AMOUNT, _playerSo.OrbPulseAmount);
            _spriteRenderer.SetPropertyBlock(_spriteProperties);
        }

        private void ConfigureTrail()
        {
            if (!TryGetComponent(out _trailRenderer))
            {
                _trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }

            Material trailMaterial = GetTrailMaterial();
            if (trailMaterial == null)
            {
                _trailRenderer.enabled = false;
                return;
            }

            _trailRenderer.sharedMaterial       = trailMaterial;
            _trailRenderer.time                 = _playerSo.OrbTrailDuration;
            _trailRenderer.minVertexDistance    = _playerSo.OrbTrailMinVertexDistance;
            _trailRenderer.widthMultiplier      = _playerSo.OrbTrailWidth;
            _trailRenderer.colorGradient        = CreateTrailGradient();
            _trailRenderer.textureMode          = LineTextureMode.Stretch;
            _trailRenderer.alignment            = LineAlignment.View;
            _trailRenderer.generateLightingData = false;
            _trailRenderer.emitting             = true;

            ConfigureRenderingOrder();
        }

        // 플레이어 기준으로 레이어 설정
        private void ConfigureRenderingOrder()
        {
            if ((_followTarget == null) || (_spriteRenderer == null))
            {
                return;
            }

            if (!_followTarget.TryGetComponent(out SpriteRenderer followRenderer))
            {
                return;
            }

            _spriteRenderer.sortingLayerID = followRenderer.sortingLayerID;
            _spriteRenderer.sortingOrder   = followRenderer.sortingOrder + ORB_ORDER_OFFSET;

            if (_trailRenderer == null)
            {
                return;
            }

            _trailRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
            _trailRenderer.sortingOrder   = _spriteRenderer.sortingOrder + TRAIL_ORDER_OFFSET;
        }

        private Material GetTrailMaterial()
        {
            if (_trailMaterial != null)
            {
                return _trailMaterial;
            }

            if (_runtimeTrailMaterial != null)
            {
                return _runtimeTrailMaterial;
            }

            Shader trailShader = Shader.Find(ORB_TRAIL_SHADER);
            if (trailShader == null)
            {
                return null;
            }

            _runtimeTrailMaterial = new Material(trailShader);
            return _runtimeTrailMaterial;
        }

        private Gradient CreateTrailGradient()
        {
            Color trailColor = _playerSo.OrbTrailColor;
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys =
            {
                new GradientColorKey(trailColor, TRAIL_START_TIME),
                new GradientColorKey(trailColor, TRAIL_END_TIME)
            };
            GradientAlphaKey[] alphaKeys =
            {
                new GradientAlphaKey(trailColor.a, TRAIL_START_TIME),
                new GradientAlphaKey(TRAIL_END_ALPHA, TRAIL_END_TIME)
            };
            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private void SetTrailEmitting(bool active)
        {
            if (_trailRenderer == null)
            {
                return;
            }

            _trailRenderer.emitting = active;
            if (!active)
            {
                ClearTrail();
            }
        }

        private void ClearTrail()
        {
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }
        }

        /// <summary> 대상에게 돌진해 도달하면 onHit을 1회 실행. 이미 공격 중이면 새 공격으로 교체. </summary>
        public void Attack(Transform target, Action onHit)
        {
            UtilCoroutine.CheckRunCoroutine(ref _coAttack, StartCoroutine(CoAttack(target, onHit)), this);
        }

        private IEnumerator CoAttack(Transform target, Action onHit)
        {
            Vector3 lastKnownPos = (target != null) ? target.position : transform.position;
            float elapsed = 0f;

            while (elapsed < _playerSo.OrbDashTimeout)
            {
                // 움직이는 대상 추적, 죽으면 마지막 위치로
                if (target != null)
                {
                    lastKnownPos = target.position;
                }

                transform.position = Vector3.MoveTowards(transform.position, lastKnownPos, _playerSo.OrbDashSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, lastKnownPos) <= _playerSo.OrbHitDistance)
                {
                    onHit?.Invoke();
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _velocity = Vector3.zero;
            _coAttack = null;
        }
    }
}
