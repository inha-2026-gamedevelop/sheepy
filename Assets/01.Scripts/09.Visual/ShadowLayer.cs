// Unity
using UnityEngine;

namespace Minsung
{
    // 캐릭터 발 아래 반투명 타원 그림자를 자동으로 생성.
    [AddComponentMenu("Minsung/Shadow Layer")]
    public class ShadowLayer : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("그림자 설정")]
        [SerializeField] private Sprite  _shadowSprite;
        [SerializeField] private Color   _shadowColor        = new Color(0f, 0f, 0f, 0.3f);
        [SerializeField] private Vector2 _offset             = new Vector2(0f, -0.3f);  // 2D: XY만
        [SerializeField] private Vector2 _scale              = new Vector2(0.8f, 0.2f); // 2D: XY만
        [SerializeField, Range(-10, 10)] private int _sortingOrderOffset = -2;

        private SpriteRenderer _shadowRenderer;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            CreateShadow();
        }

        /****************************************
        *                Methods
        ****************************************/

        private void CreateShadow()
        {
            GameObject go = new GameObject("Shadow");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(_offset.x, _offset.y, 0f);
            go.transform.localScale    = new Vector3(_scale.x, _scale.y, 1f);

            _shadowRenderer        = go.AddComponent<SpriteRenderer>();
            _shadowRenderer.sprite = _shadowSprite;
            _shadowRenderer.color  = _shadowColor;

            SpriteRenderer parent = GetComponent<SpriteRenderer>();
            if (parent != null)
            {
                _shadowRenderer.sortingLayerID = parent.sortingLayerID;
                _shadowRenderer.sortingOrder   = parent.sortingOrder + _sortingOrderOffset;
            }
        }

        /// <summary> 점프 중 그림자 크기 줄이기 등 런타임 스케일 변경 </summary>
        public void SetScale(Vector2 scale)
        {
            if (_shadowRenderer == null)
            {
                return;
            }
            _shadowRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        /// <summary> 그림자 켜기/끄기 </summary>
        public void SetActive(bool active)
        {
            if (_shadowRenderer != null)
            {
                _shadowRenderer.enabled = active;
            }
        }
    }
}