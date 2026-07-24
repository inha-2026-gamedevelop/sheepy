// Unity
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using Minsung.Utility;

namespace Minsung.Visual
{
    // URP Volume(포스트 프로세싱) 오버라이드를 한 곳에서 관리한다.
    // 씬에 Volume을 한 번만 배선해두면, 다른 스크립트는 Instance를 통해 값만 요청하면 된다.
    [AddComponentMenu("Minsung/Post Process Manager")]
    public class PostProcessManager : SceneSingleton<PostProcessManager>
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("전역 Volume (비우면 씬에서 자동 탐색)")]
        [SerializeField] private Volume _volume;

        [Header("값 변경 시 보간 시간 (0이면 즉시 반영)")]
        [SerializeField] private float _blendDuration = 0.15f;

        private Bloom _bloom; // volume.profile은 런타임 복제본이라 에셋 원본을 오염시키지 않음

        private object _bloomOwner;        // 현재 Bloom 값을 점유 중인 주체 (해제 권한 판정용)
        private float  _bloomBaseIntensity; // 씬 시작 시점의 기본값 - 해제하면 여기로 되돌아간다
        private float  _bloomTarget;
        private float  _bloomCurrent;

        /// <summary> 인스펙터/프로파일에 설정된 Bloom 기본 강도 </summary>
        public float BloomBaseIntensity => _bloomBaseIntensity;

        /****************************************
        *              Unity Event
        ****************************************/

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetStatic();
        }

        protected override void OnSingletonAwake()
        {
            CacheOverrides();
        }

        private void Update()
        {
            if ((_bloom == null) || Mathf.Approximately(_bloomCurrent, _bloomTarget))
            {
                return;
            }

            if (_blendDuration <= 0f)
            {
                _bloomCurrent = _bloomTarget;
            }
            else
            {
                // 기본값과 목표값의 거리에 비례한 속도로 이동 - 값 폭과 무관하게 _blendDuration 안에 도달한다
                float speed = Mathf.Max(Mathf.Abs(_bloomTarget - _bloomBaseIntensity), 1f) / _blendDuration;
                _bloomCurrent = Mathf.MoveTowards(_bloomCurrent, _bloomTarget, speed * Time.unscaledDeltaTime);
            }

            _bloom.intensity.value = _bloomCurrent;
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> Bloom 강도를 owner 명의로 점유한다. 마지막 요청자가 우선권을 갖는다. </summary>
        public void SetBloomIntensity(object owner, float intensity)
        {
            if (_bloom == null)
            {
                return;
            }

            _bloomOwner  = owner;
            _bloomTarget = intensity;
        }

        /// <summary> owner가 점유 중일 때만 기본값으로 되돌린다 (다른 대상이 이미 가져갔으면 무시). </summary>
        public void ReleaseBloom(object owner)
        {
            if (!ReferenceEquals(_bloomOwner, owner))
            {
                return;
            }

            _bloomOwner  = null;
            _bloomTarget = _bloomBaseIntensity;
        }

        /// <summary> 점유자와 무관하게 기본값으로 강제 복귀 (씬 전환/컷신 진입 등). </summary>
        public void ResetBloom()
        {
            _bloomOwner  = null;
            _bloomTarget = _bloomBaseIntensity;
        }

        // Volume과 오버라이드를 캐싱한다. 프로파일에 Bloom이 없으면 런타임 복제본에만 추가한다.
        private void CacheOverrides()
        {
            if (_volume == null)
            {
                _volume = FindAnyObjectByType<Volume>();
            }

            if (_volume == null)
            {
                return; // Volume이 없는 씬에서는 모든 요청이 무시된다
            }

            if (!_volume.profile.TryGet(out _bloom))
            {
                _bloom = _volume.profile.Add<Bloom>(overrides: false);
            }

            _bloom.active                  = true;
            _bloom.intensity.overrideState = true;

            _bloomBaseIntensity = _bloom.intensity.value;
            _bloomTarget        = _bloomBaseIntensity;
            _bloomCurrent       = _bloomBaseIntensity;
        }
    }
}
