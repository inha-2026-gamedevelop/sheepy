// Unity
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Minsung.Visual
{
    // glow_portal을 밝게 빛나며 빙글빙글 도는 '터질 듯한 원자' 코어로 연출한다
    // 회전 + 스케일 맥동(숨쉬듯) + 주기적 버스트(훅 부풀림) + 코어 밝기 맥동 + 연결 Light2D 헤일로 맥동을 함께 구동한다
    [AddComponentMenu("Minsung/Portal Atom Glow")]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PortalAtomGlow : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("회전 - 빙글빙글")]
        [SerializeField] private float _spinSpeed = 110f; // 초당 회전 각도(deg)

        [Header("맥동 - 숨쉬듯 커졌다 작아짐")]
        [SerializeField] private float _pulseFrequency = 1.6f;                 // 초당 맥동 횟수
        [SerializeField, Range(0f, 0.5f)] private float _pulseAmount = 0.10f;  // 기본 스케일 대비 진폭

        [Header("버스트 - 주기적으로 훅 부풀며 터질듯")]
        [SerializeField] private float _burstInterval = 2.2f;                 // 버스트 주기(초)
        [SerializeField, Range(0f, 1f)] private float _burstAmount = 0.35f;   // 버스트 시 추가 팽창 비율
        [SerializeField] private float _burstSharpness = 5f;                 // 클수록 짧고 날카롭게 부풀었다 꺼진다

        [Header("코어 색/밝기")]
        [SerializeField] private Color _coreColor = new Color(0.7f, 1f, 1f, 0.95f); // 밝은 시안-화이트
        [SerializeField, Range(0f, 1f)] private float _brightnessPulse = 0.25f;     // 맥동/버스트에 따른 밝기 변화폭

        [Header("연결 Light2D - 헤일로 맥동 (선택)")]
        [SerializeField] private Light2D _haloLight;
        [SerializeField] private float _haloIntensityMin = 2.5f;
        [SerializeField] private float _haloIntensityMax = 5.5f;

        private SpriteRenderer _renderer;
        private Vector3        _baseScale;
        private float          _phase;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _renderer  = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            _phase     = Random.Range(0f, Mathf.PI * 2f); // 시작 위상 랜덤
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 회전
            transform.Rotate(0f, 0f, _spinSpeed * dt);

            // 기본 맥동(사인) 0..1
            _phase += dt * _pulseFrequency * Mathf.PI * 2f;
            float pulse = (Mathf.Sin(_phase) + 1f) * 0.5f;

            // 버스트 - 주기 시작 직후 1에서 빠르게 0으로 꺼지는 봉우리
            float burstT = Mathf.Repeat(Time.time, _burstInterval) / _burstInterval;
            float burst  = Mathf.Pow(Mathf.Clamp01(1f - burstT), _burstSharpness);

            // 스케일 팽창 = 맥동 + 버스트
            float expansion = (pulse * _pulseAmount) + (burst * _burstAmount);
            transform.localScale = _baseScale * (1f + expansion);

            // 코어 밝기 - 팽창할수록 더 밝게 (Unlit이라 화면에 그대로 반영된다)
            float bright = 1f + ((pulse - 0.5f) * 2f * _brightnessPulse) + (burst * _brightnessPulse);
            Color c = _coreColor * bright;
            c.a = _coreColor.a;
            _renderer.color = c;

            // 헤일로 라이트 맥동
            if (_haloLight != null)
            {
                float t = Mathf.Clamp01((pulse * 0.6f) + burst);
                _haloLight.intensity = Mathf.Lerp(_haloIntensityMin, _haloIntensityMax, t);
            }
        }
    }
}
