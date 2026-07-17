// Unity
using UnityEngine;

namespace Minsung
{
    // 형광등 튜브가 낡아서 미세하게 흔들리다 가끔 훅 끊기는 깜빡임 연출. CharaGlow와 함께 붙여서 사용(CharaGlow의 _pulse는 꺼둘 것)
    [AddComponentMenu("Minsung/Neon Flicker")]
    [RequireComponent(typeof(SpriteRenderer))]
    public class NeonFlicker : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("기본 흔들림 (Perlin Noise)")]
        [SerializeField] private float _shimmerSpeed = 3f;
        [SerializeField, Range(0f, 1f)] private float _shimmerAmount = 0.12f;

        [Header("스터터 (훅 꺼졌다 켜짐)")]
        [SerializeField, Range(0f, 1f)] private float _stutterChancePerSecond = 0.6f;
        [SerializeField] private float _stutterMinDuration     = 0.08f;
        [SerializeField] private float _stutterMaxDuration     = 0.35f;
        [SerializeField] private float _stutterBlinkFrequency  = 18f; // 스터터 중 on/off 전환 빈도(Hz)
        [SerializeField, Range(0f, 1f)] private float _stutterMinBrightness = 0.08f;

        [Header("튜브 색상")]
        [SerializeField] private Color _tubeOnColor = Color.white;

        private SpriteRenderer _tubeRenderer;
        private CharaGlow      _glow;
        private float          _noiseSeed;
        private bool           _isStuttering;
        private float          _stutterTimeLeft;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _tubeRenderer = GetComponent<SpriteRenderer>();
            _glow         = GetComponent<CharaGlow>();
            _noiseSeed    = Random.Range(0f, 1000f); // 네온마다 다른 위상으로 흔들리게
        }

        private void Update()
        {
            UpdateStutterState();

            float shimmer = 1f + (Mathf.PerlinNoise(_noiseSeed, Time.time * _shimmerSpeed) - 0.5f) * 2f * _shimmerAmount;
            float brightness = _isStuttering ? SampleStutterBrightness(shimmer) : shimmer;
            brightness = Mathf.Clamp01(brightness);

            ApplyBrightness(brightness);
        }

        /****************************************
        *                Methods
        ****************************************/

        private void UpdateStutterState()
        {
            if (_isStuttering)
            {
                _stutterTimeLeft -= Time.deltaTime;
                if (_stutterTimeLeft <= 0f)
                {
                    _isStuttering = false;
                }
                return;
            }

            if (Random.value < _stutterChancePerSecond * Time.deltaTime)
            {
                _isStuttering    = true;
                _stutterTimeLeft = Random.Range(_stutterMinDuration, _stutterMaxDuration);
            }
        }

        private float SampleStutterBrightness(float shimmer)
        {
            bool blinkOff = (Mathf.FloorToInt(Time.time * _stutterBlinkFrequency) & 1) == 0;
            return blinkOff ? _stutterMinBrightness : shimmer;
        }

        private void ApplyBrightness(float brightness)
        {
            Color c = _tubeOnColor;
            c.r *= brightness;
            c.g *= brightness;
            c.b *= brightness;
            _tubeRenderer.color = c;

            if (_glow != null)
            {
                _glow.SetBrightness(brightness);
            }
        }
    }
}
