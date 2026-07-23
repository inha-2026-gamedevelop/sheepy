// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.Visual
{
    // 투명 창을 쓸 수 없을 때(에디터/비Windows/사용자 토글 OFF) 가짜 창 바깥을 채우는 폴백 데스크톱 배경
    // 실제 OS 화면을 읽거나 캡처하지 않는다 - 미리 준비한 텍스처나 단색만 사용한다
    public class FakeDesktopBackground : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("폴백 배경")]
        [Tooltip("데스크톱처럼 보일 준비된 텍스처. 비우면 아래 단색으로만 채운다")]
        [SerializeField] private Texture2D _desktopTexture;
        [SerializeField] private Color _fallbackColor = new Color(0.07f, 0.08f, 0.12f, 1f);

        private RawImage _image;

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> parent 전체를 덮는 폴백 배경을 켠다(가짜 창보다 뒤에 깔린다) </summary>
        public void Show(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            if (_image == null)
            {
                GameObject go = new GameObject("FakeDesktopBackground", typeof(RectTransform));
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                _image = go.AddComponent<RawImage>();
                _image.raycastTarget = false;
            }

            _image.transform.SetAsFirstSibling();
            _image.texture = _desktopTexture;
            _image.color   = (_desktopTexture != null) ? Color.white : _fallbackColor;
            _image.gameObject.SetActive(true);
        }

        /// <summary> 폴백 배경을 정리한다(두 번 호출해도 안전) </summary>
        public void Hide()
        {
            if (_image == null)
            {
                return;
            }
            Destroy(_image.gameObject);
            _image = null;
        }

        private void OnDisable()
        {
            Hide();
        }
    }
}
