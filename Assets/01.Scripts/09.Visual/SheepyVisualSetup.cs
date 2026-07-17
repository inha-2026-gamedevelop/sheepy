// Unity
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Minsung
{
    // 씬의 빈 오브젝트에 붙이면 셰이더 없이 Bloom/Color Grading/Vignette만으로 Sheepy 비주얼 전체를 한 번에 세팅 (2D URP)
    [AddComponentMenu("Minsung/Sheepy Visual Setup")]
    public class SheepyVisualSetup : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("Global Volume 참조 (없으면 자동 생성)")]
        [SerializeField] private Volume _globalVolume;

        [Header("기능 토글")]
        [SerializeField] private bool _useBloom        = true;
        [SerializeField] private bool _useColorGrading = true;
        [SerializeField] private bool _useVignette     = true;
        [SerializeField] private bool _useFilmGrain    = true;

        [Header("Bloom (빛번짐)")]
        [SerializeField, Range(0f, 1f)] private float _bloomThreshold = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _bloomIntensity = 0.4f;
        [SerializeField, Range(0f, 1f)] private float _bloomScatter   = 0.7f;

        [Header("Color Grading (색감/분위기)")]
        [SerializeField, Range(-1f, 1f)] private float _saturation = 0.1f;
        [SerializeField, Range(-1f, 1f)] private float _contrast   = 0.05f;
        [SerializeField]                 private Color _colorFilter = new Color(0.95f, 0.95f, 1f);

        [Header("Vignette (비네팅)")]
        [SerializeField, Range(0f, 1f)] private float _vignetteIntensity  = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _vignetteSmoothness = 0.4f;

        [Header("Film Grain (필름 질감 - 픽셀아트의 거친 몰입감)")]
        [SerializeField, Range(0f, 1f)] private float _grainIntensity = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _grainResponse  = 0.8f;

        private Bloom            _bloom;
        private ColorAdjustments _colorAdj;
        private Vignette         _vignette;
        private FilmGrain        _filmGrain;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            EnsureVolume();
            ApplyAll();
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            EnsureVolume();
            ApplyAll();
        }
        #endif

        /****************************************
        *                Methods
        ****************************************/

        private void EnsureVolume()
        {
            if (_globalVolume != null)
            {
                return;
            }

            _globalVolume = FindObjectOfType<Volume>();
            if (_globalVolume != null)
            {
                return;
            }

            GameObject go = new GameObject("Global Volume");
            _globalVolume          = go.AddComponent<Volume>();
            _globalVolume.isGlobal = true;
            _globalVolume.profile  = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        /// <summary> 모든 Post Processing 효과를 Inspector 값으로 즉시 반영 </summary>
        public void ApplyAll()
        {
            if ((_globalVolume == null) || (_globalVolume.profile == null))
            {
                return;
            }

            ApplyBloom();
            ApplyColorGrading();
            ApplyVignette();
            ApplyFilmGrain();
        }

        private void ApplyBloom()
        {
            if (!_globalVolume.profile.TryGet(out _bloom))
            {
                _bloom = _globalVolume.profile.Add<Bloom>(overrides: false);
            }
            _bloom.active          = _useBloom;
            _bloom.threshold.value = _bloomThreshold;
            _bloom.intensity.value = _bloomIntensity;
            _bloom.scatter.value   = _bloomScatter;

            _bloom.threshold.overrideState = true;
            _bloom.intensity.overrideState = true;
            _bloom.scatter.overrideState   = true;
        }

        private void ApplyColorGrading()
        {
            if (!_globalVolume.profile.TryGet(out _colorAdj))
            {
                _colorAdj = _globalVolume.profile.Add<ColorAdjustments>(overrides: false);
            }
            _colorAdj.active = _useColorGrading;
            _colorAdj.colorFilter.value  = _colorFilter;
            _colorAdj.saturation.value   = _saturation * 100f;
            _colorAdj.contrast.value     = _contrast   * 100f;

            _colorAdj.colorFilter.overrideState = true;
            _colorAdj.saturation.overrideState  = true;
            _colorAdj.contrast.overrideState    = true;
        }

        private void ApplyVignette()
        {
            if (!_globalVolume.profile.TryGet(out _vignette))
            {
                _vignette = _globalVolume.profile.Add<Vignette>(overrides: false);
            }
            _vignette.active            = _useVignette;
            _vignette.intensity.value   = _vignetteIntensity;
            _vignette.smoothness.value  = _vignetteSmoothness;

            _vignette.intensity.overrideState  = true;
            _vignette.smoothness.overrideState = true;
        }

        private void ApplyFilmGrain()
        {
            if (!_globalVolume.profile.TryGet(out _filmGrain))
            {
                _filmGrain = _globalVolume.profile.Add<FilmGrain>(overrides: false);
            }
            _filmGrain.active          = _useFilmGrain;
            _filmGrain.type.value      = FilmGrainLookup.Thin1; // 픽셀아트에는 가는 입자가 어울린다
            _filmGrain.intensity.value = _grainIntensity;
            _filmGrain.response.value  = _grainResponse;

            _filmGrain.type.overrideState      = true;
            _filmGrain.intensity.overrideState = true;
            _filmGrain.response.overrideState  = true;
        }
    }
}
