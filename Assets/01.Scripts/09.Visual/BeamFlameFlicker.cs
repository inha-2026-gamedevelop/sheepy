// Unity
using UnityEngine;

namespace Minsung.Visual
{
    // 램프 빛기둥(beam)을 불꽃처럼 일렁이게 한다 - Perlin 깜빡임 + 폭 흔들림 + 밝기 서지(코어가 하얗게 뜸)
    // 스프라이트는 세로 빔(beam_soft), 머티리얼은 가산(Additive) 전제. 밝기 서지가 색을 1 이상으로 밀어 Bloom이 흰 코어를 만든다
    [AddComponentMenu("Minsung/Beam Flame Flicker")]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BeamFlameFlicker : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("불꽃 색")]
        [SerializeField] private Color _flameColor = new Color(1f, 0.5f, 0.16f, 1f); // 주황 불길
        [SerializeField] private float _coreBoost  = 1.8f;                          // 서지 피크에서 HDR로 코어가 하얗게 뜬다

        [Header("깜빡임 (Perlin)")]
        [SerializeField] private float _flickerSpeed = 9f;
        [SerializeField, Range(0f, 1f)] private float _flickerAmount = 0.35f;

        [Header("폭 일렁임")]
        [SerializeField] private float _swaySpeed = 6f;
        [SerializeField, Range(0f, 0.5f)] private float _widthSway = 0.12f;

        private SpriteRenderer _renderer;
        private Vector3        _baseScale;
        private float          _seedFlicker;
        private float          _seedSurge;
        private float          _seedWidth;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _renderer    = GetComponent<SpriteRenderer>();
            _baseScale   = transform.localScale;
            _seedFlicker = Random.value * 100f;
            _seedSurge   = Random.value * 100f;
            _seedWidth   = Random.value * 100f;
        }

        private void Update()
        {
            float t = Time.time;

            // 기본 깜빡임
            float flicker = Mathf.PerlinNoise(_seedFlicker, t * _flickerSpeed);
            float bright  = 1f + ((flicker - 0.5f) * 2f * _flickerAmount);

            // 가끔 훅 밝아지며 코어가 하얗게(HDR) 뜨는 서지
            float surge = Mathf.Pow(Mathf.PerlinNoise(_seedSurge, t * _flickerSpeed * 0.5f), 3f);
            bright *= Mathf.Lerp(1f, _coreBoost, surge);

            Color c = _flameColor * bright;
            c.a = _flameColor.a;
            _renderer.color = c;

            // 폭이 불규칙하게 흔들려 불길처럼 보인다
            float ws = 1f + ((Mathf.PerlinNoise(_seedWidth, t * _swaySpeed) - 0.5f) * 2f * _widthSway);
            transform.localScale = new Vector3(_baseScale.x * ws, _baseScale.y, _baseScale.z);
        }
    }
}
