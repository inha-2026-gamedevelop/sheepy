// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

using Minsung.Utility;

namespace Minsung.UI
{
    // 키 가이드 종류.
    public enum EKeyGuide
    {
        Interactive, // 상호작용 (E키)
    }

    // 상호작용 대상에 포커스됐을 때 뜨는 키 가이드 HUD.
    public class KeyGuideManager : PersistentSingleton<KeyGuideManager>
    {
        /****************************************
        *                Fields
        ****************************************/

        [FormerlySerializedAs("goKeyGuide")]
        [SerializeField] private GameObject _keyGuideObject; // 가이드 패널 루트
        [FormerlySerializedAs("imgKeyGuide")]
        [SerializeField] private Image      _keyGuideImage;  // 키 스프라이트 표시 이미지

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 플레이어 머리 위 패널/이미지를 표시 대상으로 등록한다 </summary>
        public void SetTarget(GameObject keyGuideObject, Image keyGuideImage)
        {
            _keyGuideObject = keyGuideObject;
            _keyGuideImage  = keyGuideImage;

            SetActiveKeyGuidePanel(false);
            if (_keyGuideImage != null)
            {
                _keyGuideImage.gameObject.SetActive(true); // 패널 재활성 시 이미지가 바로 보이도록 자체 활성 상태는 켜둔다
            }
        }

        /// <summary> 가이드 패널만 켜고 끈다 </summary>
        public void SetActiveKeyGuidePanel(bool value)
        {
            if (_keyGuideObject == null)
            {
                return;
            }
            _keyGuideObject.SetActive(value);
        }

        /// <summary> 해당 종류의 키 가이드를 표시한다. </summary>
        public void ShowKeyGuide(EKeyGuide keyGuide)
        {
            if (_keyGuideImage == null)
            {
                return;
            }
            SetActiveKeyGuidePanel(true);
            _keyGuideImage.sprite = SpriteReference.Instance.GetKeySprite(keyGuide);
        }

        /// <summary> 키 가이드를 숨긴다. </summary>
        public void HideKeyGuide()
        {
            if (_keyGuideImage == null)
            {
                return;
            }
            SetActiveKeyGuidePanel(false);
            _keyGuideImage.sprite = null;
        }
    }
}
