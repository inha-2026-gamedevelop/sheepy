// System
using System.Collections;

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
        [SerializeField] private EBgm _bgm       = EBgm.Radio; // 재생할 트랙을 고르는 카테고리. SFX 채널로 재생되며 실제 BGM은 건드리지 않는다
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
        private Coroutine       _autoStopRoutine; // 루프 없는 트랙이 스스로 끝났을 때 상태 정리용

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void Awake()
        {
            base.Awake();
            _focusSize = Constants.Camera.FOCUS_ORTHOGRAPHIC_SIZE;
            TryGetComponent(out _sfxEmitter);
        }

        // 라디오가 꺼진 상태(비활성)로 전환되면 재생 중이던 사운드/카메라 포커스도 함께 정리
        protected override void OnDisable()
        {
            base.OnDisable();
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

            AudioClip clip = SoundManager.Instance.GetBgmClip(_bgm, _clipIndex);
            if (clip == null)
            {
                return;
            }

            // BGM 채널을 건드리지 않고 SFX 지속 채널로 재생해 BGM/다른 SFX와 동시에 울릴 수 있게 한다
            SoundManager.Instance.PlaySFX_Duration(clip, GetInstanceID(), 1f, _isLoop);
            CaptionManager.Instance?.PlaySequence(_captions);
            _isPlaying = true;

            if (!_isLoop)
            {
                _autoStopRoutine = StartCoroutine(AutoStopAfterClipEnds(clip.length));
            }
        }

        private void StopRadio()
        {
            if (!_isPlaying)
            {
                return;
            }

            if (_autoStopRoutine != null)
            {
                StopCoroutine(_autoStopRoutine);
                _autoStopRoutine = null;
            }

            // 씬 종료 순서에 따라 SoundManager/CaptionManager가 먼저 사라질 수 있어 null 확인
            SoundManager.Instance?.StopSFX_Duration(ESfxState.NONE, -1, GetInstanceID());
            CaptionManager.Instance?.StopSequence();
            _sfxEmitter?.PlayDeactivate();
            _isPlaying = false;
        }

        // 루프 없는 트랙이 스스로 끝났을 때 상태 동기화 (수동 정지 시에는 StopRadio에서 코루틴을 먼저 취소함)
        private IEnumerator AutoStopAfterClipEnds(float clipLength)
        {
            yield return new WaitForSeconds(clipLength);

            _autoStopRoutine = null;
            CaptionManager.Instance?.StopSequence();
            _isPlaying = false;
        }
    }
}
