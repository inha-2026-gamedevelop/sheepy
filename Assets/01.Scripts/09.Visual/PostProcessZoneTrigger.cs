// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using Minsung.Player;

namespace Minsung.Visual
{
    // 플레이어가 이 트리거(Collider2D, IsTrigger)에 들어오면 전역 Volume의 포스트프로세싱 값을
    // 인스펙터에 설정한 목표 값으로 부드럽게 전환한다. 진입 시점(OnTriggerEnter2D)에만 동작하며 나갈 때는 되돌리지 않는다.
    // Bloom.intensity 는 PostProcessManager 가 소유권 기반으로 관리하므로 그쪽을 경유해 충돌을 피한다.
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Minsung/Post Process Zone Trigger")]
    public class PostProcessZoneTrigger : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("대상 Volume (비우면 씬에서 자동 탐색)")]
        [SerializeField] private Volume _targetVolume;

        [Header("전환 설정")]
        [SerializeField] private float _transitionTime = 1.0f; // 0 이면 즉시 적용
        [SerializeField] private bool  _onlyOnce       = false; // true 면 최초 1회만 반응

        [Header("ColorAdjustments")]
        [SerializeField] private float _postExposure = -0.35f;
        [SerializeField] private Color _colorFilter  = new Color(0.82f, 0.90f, 1.0f, 1f);
        [SerializeField, Range(-100f, 100f)] private float _saturation = -12f;
        [SerializeField, Range(-100f, 100f)] private float _contrast   = 6f;

        [Header("Vignette")]
        [SerializeField, Range(0f, 1f)]    private float _vignetteIntensity  = 0.55f;
        [SerializeField, Range(0.01f, 1f)] private float _vignetteSmoothness = 0.42f;

        [Header("Bloom (PostProcessManager 경유)")]
        [SerializeField] private float _bloomIntensity = 0.8f;

        private ColorAdjustments _color;
        private Vignette         _vignette;
        private Bloom            _bloom;
        private Coroutine        _co;
        private bool             _fired;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            if (_targetVolume == null)
            {
                _targetVolume = FindAnyObjectByType<Volume>();
            }

            if ((_targetVolume == null) || (_targetVolume.profile == null))
            {
                return;
            }

            // volume.profile 은 런타임 복제본이라 에셋 원본을 오염시키지 않는다. 없으면 복제본에만 추가한다.
            if (!_targetVolume.profile.TryGet(out _color))
            {
                _color = _targetVolume.profile.Add<ColorAdjustments>(overrides: false);
            }
            if (!_targetVolume.profile.TryGet(out _vignette))
            {
                _vignette = _targetVolume.profile.Add<Vignette>(overrides: false);
            }
            _targetVolume.profile.TryGet(out _bloom); // Bloom 은 매니저가 관리 - 없으면 그냥 건드리지 않는다
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_onlyOnce && _fired)
            {
                return;
            }
            if ((_targetVolume == null) || !other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            _fired = true;

            // Bloom 은 매니저 소유권 시스템을 통해 요청 (매니저가 없으면 직접 반영)
            if (_bloom != null)
            {
                if (PostProcessManager.Instance != null)
                {
                    PostProcessManager.Instance.SetBloomIntensity(this, _bloomIntensity);
                }
                else
                {
                    _bloom.intensity.overrideState = true;
                    _bloom.intensity.value         = _bloomIntensity;
                }
            }

            if (_co != null)
            {
                StopCoroutine(_co);
            }
            _co = StartCoroutine(CoTransition());
        }

        /****************************************
        *                Methods
        ****************************************/

        private IEnumerator CoTransition()
        {
            EnableOverrides();

            // 시작 값 캡처
            float startExp = _color.postExposure.value;
            Color startCf  = _color.colorFilter.value;
            float startSat = _color.saturation.value;
            float startCon = _color.contrast.value;
            float startVig = _vignette.intensity.value;
            float startVs  = _vignette.smoothness.value;

            float t = (_transitionTime <= 0f) ? 1f : 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / _transitionTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                ApplyLerp(startExp, startCf, startSat, startCon, startVig, startVs, k);
                yield return null;
            }
            ApplyLerp(startExp, startCf, startSat, startCon, startVig, startVs, 1f);
            _co = null;
        }

        private void EnableOverrides()
        {
            _color.postExposure.overrideState = true;
            _color.colorFilter.overrideState  = true;
            _color.saturation.overrideState   = true;
            _color.contrast.overrideState     = true;
            _vignette.intensity.overrideState  = true;
            _vignette.smoothness.overrideState = true;
        }

        private void ApplyLerp(float sExp, Color sCf, float sSat, float sCon, float sVig, float sVs, float k)
        {
            _color.postExposure.value = Mathf.Lerp(sExp, _postExposure, k);
            _color.colorFilter.value  = Color.Lerp(sCf, _colorFilter, k);
            _color.saturation.value   = Mathf.Lerp(sSat, _saturation, k);
            _color.contrast.value     = Mathf.Lerp(sCon, _contrast, k);
            _vignette.intensity.value  = Mathf.Lerp(sVig, _vignetteIntensity, k);
            _vignette.smoothness.value = Mathf.Lerp(sVs, _vignetteSmoothness, k);
        }
    }
}
