// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Minsung.Achievement;
using Minsung.Backend;
using Minsung.Common;
using Minsung.Sound;
using Minsung.Visual;

namespace Minsung.UI
{
    // 설정 패널 - BGM/SFX 볼륨 슬라이더 + 데이터 초기화. 로비/일시정지 화면 공용
    [AddComponentMenu("Minsung/UI/Settings Panel Controller")]
    public class SettingsPanelController : MonoBehaviour
    {
        // 초기화 버튼 오발동 방지 - 어떤 초기화가 "확인 대기" 상태인지 구분
        private enum PendingReset
        {
            None,
            Progress,     // 기록삭제 (업적 제외)
            Achievements, // 업적 기록 제거
        }

        /****************************************
        *                Fields
        ****************************************/

        // 켜기 전에 반드시 안내해야 하는 문구 - 이 연출은 게임 창 일부를 투명하게 만들어 실제 바탕화면을 드러낸다
        private const string DESKTOP_REVEAL_NOTICE =
            "공간찢기 연출 중 게임 창 일부가 투명해져 바탕화면, 알림, 다른 앱 창이 화면과 방송/녹화에 그대로 노출됩니다.";
        private const string DESKTOP_REVEAL_UNSUPPORTED =
            "이 연출은 Windows 빌드에서만 사용할 수 있습니다.";

        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("실제 데스크톱 노출 연출 - Windows 빌드 전용, 기본 꺼짐")]
        [SerializeField] private Toggle   _desktopRevealToggle;
        [SerializeField] private TMP_Text _desktopRevealNoticeText; // 노출 위험 안내 (미지정 시 생략)

        [Header("데이터 초기화 - 같은 버튼을 제한시간 안에 한 번 더 누르면 실행")]
        [SerializeField] private TMP_Text _resetMessageText;               // 확인 안내/결과 메시지 표시 (미지정 시 생략)
        [SerializeField] private float    _confirmWindowSeconds = 3f;      // 확인 대기 시간(초)

        private PendingReset _pendingReset = PendingReset.None;
        private Coroutine    _pendingResetTimeoutRoutine;

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnEnable()
        {
            RefreshDesktopRevealToggle();

            if (SoundManager.Instance == null)
            {
                return;
            }

            if (_bgmSlider != null)
            {
                _bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BgmVolume);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
            }
        }

        // 패널을 닫았다 다시 열었을 때 이전 확인 대기 상태가 남아있지 않도록 초기화
        private void OnDisable()
        {
            CancelPendingReset();
            SetResetMessage(string.Empty);
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> BGM 볼륨 슬라이더 OnValueChanged 콜백 </summary>
        public void OnBgmSliderChanged(float value)
        {
            SoundManager.Instance?.SetBgmVolume(value);
        }

        /// <summary> SFX 볼륨 슬라이더 OnValueChanged 콜백 </summary>
        public void OnSfxSliderChanged(float value)
        {
            SoundManager.Instance?.SetSfxVolume(value);
        }

        /// <summary> 실제 데스크톱 노출 연출 토글 OnValueChanged 콜백 </summary>
        public void OnDesktopRevealToggleChanged(bool isOn)
        {
            if (isOn && !TransparentWindowController.IsSupported)
            {
                RefreshDesktopRevealToggle(); // 지원하지 않는 플랫폼에서는 켜지지 않게 되돌린다
                return;
            }

            TransparentWindowController.IsUserEnabled = isOn;
            SetDesktopRevealNotice(isOn ? DESKTOP_REVEAL_NOTICE : string.Empty);
        }

        // 현재 저장값/플랫폼 지원 여부를 토글에 반영한다
        private void RefreshDesktopRevealToggle()
        {
            if (_desktopRevealToggle == null)
            {
                return;
            }

            bool supported = TransparentWindowController.IsSupported;
            bool enabled    = supported && TransparentWindowController.IsUserEnabled;

            _desktopRevealToggle.interactable = supported;
            _desktopRevealToggle.SetIsOnWithoutNotify(enabled);

            if (!supported)
            {
                SetDesktopRevealNotice(DESKTOP_REVEAL_UNSUPPORTED);
                return;
            }
            SetDesktopRevealNotice(enabled ? DESKTOP_REVEAL_NOTICE : string.Empty);
        }

        private void SetDesktopRevealNotice(string message)
        {
            if (_desktopRevealNoticeText != null)
            {
                _desktopRevealNoticeText.text = message;
            }
        }

        /// <summary> 닫기 버튼 </summary>
        public void OnClickClose()
        {
            gameObject.SetActive(false);
            PauseController.Instance?.ReleaseCapturedSettingsBackdrop();
        }

        /****************************************
        *            데이터 초기화
        ****************************************/

        /// <summary> 기록삭제 (업적 제외) - 위치/씬/보스클리어 등 진행 기록만 초기화. 계정(닉네임)/업적은 유지. </summary>
        public void OnClickResetProgress()
        {
            if (_pendingReset == PendingReset.Progress)
            {
                CancelPendingReset();
                SaveManager.Instance?.ClearPlayerState();
                BackendMirror.Instance?.ResetProgress(Constants.Scene.MAP_1);
                SetResetMessage("기록이 삭제되었습니다.");
                return;
            }

            ArmPendingReset(PendingReset.Progress, "정말 기록을 삭제하시겠습니까? 한 번 더 누르면 삭제됩니다.");
        }

        /// <summary> 업적 기록 제거 - 해제한 업적만 초기화. 위치/보스클리어 등 진행 기록은 유지. </summary>
        public void OnClickResetAchievements()
        {
            if (_pendingReset == PendingReset.Achievements)
            {
                CancelPendingReset();
                AchievementManager.Instance?.ClearAll();
                BackendMirror.Instance?.MirrorClearAchievements();
                SetResetMessage("업적 기록이 제거되었습니다.");
                return;
            }

            ArmPendingReset(PendingReset.Achievements, "정말 업적 기록을 제거하시겠습니까? 한 번 더 누르면 제거됩니다.");
        }

        // 확인 대기 상태로 진입 - 제한시간 안에 같은 버튼을 다시 누르지 않으면 자동 취소된다.
        private void ArmPendingReset(PendingReset target, string message)
        {
            if (_pendingResetTimeoutRoutine != null)
            {
                StopCoroutine(_pendingResetTimeoutRoutine);
            }

            _pendingReset = target;
            SetResetMessage(message);
            _pendingResetTimeoutRoutine = StartCoroutine(CoCancelPendingResetAfterDelay());
        }

        private IEnumerator CoCancelPendingResetAfterDelay()
        {
            yield return new WaitForSecondsRealtime(_confirmWindowSeconds);
            CancelPendingReset();
            SetResetMessage(string.Empty);
        }

        private void CancelPendingReset()
        {
            _pendingReset = PendingReset.None;
            if (_pendingResetTimeoutRoutine != null)
            {
                StopCoroutine(_pendingResetTimeoutRoutine);
                _pendingResetTimeoutRoutine = null;
            }
        }

        private void SetResetMessage(string message)
        {
            if (_resetMessageText != null)
            {
                _resetMessageText.text = message;
            }
        }
    }
}
