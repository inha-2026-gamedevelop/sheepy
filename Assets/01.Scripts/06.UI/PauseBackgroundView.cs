// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Common;

namespace Minsung.UI
{
    // 일시정지 진입 시점에 PauseController가 캡처해둔 블러 배경(RenderTexture)을 표시
    [AddComponentMenu("Minsung/UI/Pause Background View")]
    public class PauseBackgroundView : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private RawImage _rawImage;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            if ((_rawImage != null) && (PauseController.Instance != null))
            {
                _rawImage.texture = PauseController.Instance.CapturedBackground;
            }
        }
    }
}
