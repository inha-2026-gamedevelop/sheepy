// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Common;
using Minsung.Sound;

namespace Minsung.UI
{
    // 설정 패널 - BGM/SFX 볼륨 슬라이더. 로비/일시정지 화면 공용
    [AddComponentMenu("Minsung/UI/Settings Panel Controller")]
    public class SettingsPanelController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnEnable()
        {
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

        /// <summary> 닫기 버튼 </summary>
        public void OnClickClose()
        {
            gameObject.SetActive(false);
            PauseController.Instance?.ReleaseCapturedSettingsBackdrop();
        }
    }
}
