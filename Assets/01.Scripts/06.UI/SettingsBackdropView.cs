// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Common;

namespace Minsung.UI
{
    // 설정 패널 뒤 배경 - PauseController가 캡처해둔 강한 블러 텍스처를 어둡게 표시
    [AddComponentMenu("Minsung/UI/Settings Backdrop View")]
    public class SettingsBackdropView : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private RawImage _rawImage;

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> PauseController에 캡처된 최신 배경 텍스처를 반영 - 설정 패널을 열기 직전에 호출 </summary>
        public void Refresh()
        {
            if ((_rawImage == null) || (PauseController.Instance == null))
            {
                return;
            }

            _rawImage.texture = PauseController.Instance.CapturedSettingsBackdrop;

            float brightness = Constants.UI.SETTINGS_BACKDROP_BRIGHTNESS;
            _rawImage.color  = new Color(brightness, brightness, brightness, 1f);
        }
    }
}
