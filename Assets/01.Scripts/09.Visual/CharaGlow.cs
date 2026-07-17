// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Utility;

namespace Minsung
{
    // 캐릭터 뒤에 글로우 스프라이트를 Additive 블렌드로 붙여 자동으로 글로우 자식 생성.
    [AddComponentMenu("Minsung/Chara Glow")]
    public class CharaGlow : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("글로우 설정")]
        [SerializeField] private Sprite  _glowSprite;
        [SerializeField] private Color   _glowColor  = new Color(0.5f, 0.8f, 1f, 0.4f);
        [SerializeField] private Vector2 _glowScale  = new Vector2(0.2f, 0.2f); // 글로우 텍스처(254px)가 캐릭터(24px)보다 훨씬 커서 작은 배율이 필요
        [SerializeField, Range(-10, 10)] private int _sortingOrderOffset = -1; // 캐릭터보다 뒤에

        [Header("펄스 애니메이션 (선택)")]
        [SerializeField] private bool  _pulse          = true;
        [SerializeField] private float _pulseSpeed     = 1.2f;
        [SerializeField] private float _pulseAmplitude = 0.08f;

        private SpriteRenderer _glowRenderer;
        private float          _baseAlpha;
        private bool           _isFlashing; // 플래시 중에는 펄스가 색을 덮지 않게
        private Coroutine      _coFlash;
        private float          _brightnessMultiplier = 1f;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            CreateGlow();
            _baseAlpha = _glowColor.a;
        }

        private void Update()
        {
            if (!_pulse || (_glowRenderer == null) || _isFlashing)
            {
                return;
            }

            float alpha = _baseAlpha + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmplitude;
            Color c = _glowRenderer.color;
            c.a = Mathf.Clamp01(alpha);
            _glowRenderer.color = c;
        }

        /****************************************
        *                Methods
        ****************************************/

        private void CreateGlow()
        {
            GameObject go = new GameObject("Glow");
            go.transform.SetParent(transform);
            // 2D: XY만 사용, Z = 1 고정
            go.transform.localScale    = new Vector3(_glowScale.x, _glowScale.y, 1f);

            _glowRenderer        = go.AddComponent<SpriteRenderer>();
            _glowRenderer.sprite = _glowSprite;
            _glowRenderer.color  = _glowColor;

            // URP 2D 환경: Sprite-Lit-Default 또는 Sprites/Default
            // Additive 블렌드를 위해 머티리얼 인스턴스 생성
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.SetFloat("_Mode", 1f);
            _glowRenderer.material = mat;

            // 캐릭터와 같은 Sorting Layer, 뒤 순서로 배치
            SpriteRenderer parent = GetComponent<SpriteRenderer>();
            if (parent != null)
            {
                _glowRenderer.sortingLayerID = parent.sortingLayerID;
                _glowRenderer.sortingOrder   = parent.sortingOrder + _sortingOrderOffset;

                // 피벗이 스프라이트 시각적 중심과 다를 수 있어(예: 발밑 피벗) localBounds 중심에 맞춘다
                Vector3 boundsCenter = parent.localBounds.center;
                go.transform.localPosition = new Vector3(boundsCenter.x, boundsCenter.y, 0f);
            }
            else
            {
                go.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary> 런타임 글로우 색상 변경. 피격/슬로우 상태 시각 피드백에 활용. </summary>
        public void SetGlowColor(Color color)
        {
            _glowColor = color;
            if (_glowRenderer != null)
            {
                _glowRenderer.color = color;
            }
            _baseAlpha = color.a;
        }

        /// <summary> 글로우 켜기/끄기 </summary>
        public void SetActive(bool active)
        {
            if (_glowRenderer != null)
            {
                _glowRenderer.enabled = active;
            }
        }

        /// <summary> 외부(예: 깜빡임 연출)에서 글로우 밝기를 0~1 배율로 제어. _pulse가 꺼져있을 때 사용. </summary>
        public void SetBrightness(float multiplier)
        {
            _brightnessMultiplier = Mathf.Clamp01(multiplier);
            if (_glowRenderer == null)
            {
                return;
            }

            Color c = _glowColor;
            c.a = _baseAlpha * _brightnessMultiplier;
            _glowRenderer.color = c;
        }

        /// <summary> 잠깐 글로우 색을 바꿨다가 원래 색으로 복귀 (피격 플래시). 연속 피격 시 새로 시작. </summary>
        public void Flash(Color color, float duration)
        {
            if (_glowRenderer == null)
            {
                return;
            }
            UtilCoroutine.CheckRunCoroutine(ref _coFlash, StartCoroutine(CoFlash(color, duration)), this);
        }

        // 실시간 기준 대기 - 히트스톱/슬로우 중에도 플래시 길이가 일정하다 (무할당)
        private IEnumerator CoFlash(Color color, float duration)
        {
            _isFlashing         = true;
            _glowRenderer.color = color;

            float end = Time.realtimeSinceStartup + duration;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            _glowRenderer.color = _glowColor; // SetGlowColor로 지정된 기준 색으로 복귀
            _isFlashing = false;
            _coFlash    = null;
        }
    }
}