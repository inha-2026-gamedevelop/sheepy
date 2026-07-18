// Unity
using UnityEngine;

using Minsung.CameraSystem;
using Minsung.Common;
using Minsung.Sound;
using Minsung.UI;

namespace Minsung.Interactive
{
    // 라디오 상호작용 오브젝트. 상호작용 키를 누를 때마다 사운드 재생/정지를 토글한다.
    public class RadioInteractive : BaseInteractive
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("라디오 설정")]
        [SerializeField] private EBgm _bgm       = EBgm.Radio;
        [SerializeField] private int  _clipIndex = -1;         // 카테고리 내 클립 인덱스 (-1이면 무작위, 커스텀 인스펙터에서 드롭다운으로 선택)
        [SerializeField] private bool _isLoop    = true;       // 라디오 특성상 기본 루프 재생

        [Header("자막")]
        [SerializeField] private CaptionEntry[] _captions; // 재생 중 화면 하단에 순서대로 표시할 자막들 (루프 없이 한 번만 재생)

        [Header("카메라 연출")]
        [SerializeField] private Transform _cameraTip;                            // 포커스 카메라가 이동할 위치/회전 마커

        [SerializeField] private float     _blendTime = Constants.Camera.DEFAULT_BLEND_TIME; // 포커스 전환 블렌드 시간(초)

        private bool  _isPlaying;
        private float _focusSize;
        private LocalSfxEmitter _sfxEmitter;

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void Awake()
        {
            base.Awake();
            _focusSize = Constants.Camera.FOCUS_ORTHOGRAPHIC_SIZE;
            TryGetComponent(out _sfxEmitter);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SoundManager.OnBgmOverrideEnded += HandleBgmOverrideEnded;
        }

        // 라디오가 꺼진 상태(비활성)로 전환되면 재생 중이던 사운드/카메라 포커스도 함께 정리
        protected override void OnDisable()
        {
            base.OnDisable();
            SoundManager.OnBgmOverrideEnded -= HandleBgmOverrideEnded;
            StopRadio();
            CameraManager.Instance?.UnFocus();
        }

        /****************************************
        *                Methods
        ****************************************/

        public override void OnFocus()
        {
            KeyGuideManager.Instance.ShowKeyGuide(EKeyGuide.Interactive);
            CameraManager.Instance?.Focus(_cameraTip, _focusSize, _blendTime);
        }

        public override void OnUnfocus()
        {
            KeyGuideManager.Instance.HideKeyGuide();
            CameraManager.Instance?.UnFocus();
        }

        public override void OnInteract(GameObject interactor)
        {
            if (_isPlaying)
            {
                StopRadio();
            }
            else
            {
                PlayRadio();
            }
        }

        private void PlayRadio()
        {
            _sfxEmitter?.PlayActivate();

            if (SoundManager.Instance == null)
            {
                return;
            }

            // 지금 재생 중이던 BGM은 스냅샷으로 저장해뒀다가, 라디오가 멈추거나 끝나면 이어서 재생된다
            SoundManager.Instance.PlayBGMOverride(_bgm, _clipIndex, _isLoop);
            CaptionManager.Instance?.PlaySequence(_captions);
            _isPlaying = true;
        }

        private void StopRadio()
        {
            if (!_isPlaying)
            {
                return;
            }

            // 씬 종료 순서에 따라 SoundManager/CaptionManager가 먼저 사라질 수 있어 null 확인
            SoundManager.Instance?.StopBGMOverride();
            CaptionManager.Instance?.StopSequence();
            _sfxEmitter?.PlayDeactivate();
            _isPlaying = false;
        }

        // 루프 없는 라디오 사운드가 스스로 끝나 SoundManager가 원래 BGM으로 자동 복귀했을 때 상태 동기화
        private void HandleBgmOverrideEnded()
        {
            if (!_isPlaying)
            {
                return;
            }

            CaptionManager.Instance?.StopSequence();
            _isPlaying = false;
        }
    }
}
