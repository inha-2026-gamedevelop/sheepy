// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.UI
{
    // 메뉴 버튼 선택 시 클릭 지점에서 흩어지는 UI 파티클 버스트 (Screen Space Overlay 캔버스 전용)
    [AddComponentMenu("Minsung/UI/Ui Click Burst")]
    public class UiClickBurst : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private Sprite _particleSprite;
        [SerializeField] private int    _particleCount = 40;
        [SerializeField] private float  _minSpeed      = 200f;
        [SerializeField] private float  _maxSpeed      = 420f;
        [SerializeField] private float  _duration      = 0.4f;
        [SerializeField] private float  _minSize       = 10f;
        [SerializeField] private float  _maxSize       = 26f;
        [SerializeField] private Color  _color         = new Color(0.8f, 0.9f, 1f, 1f);

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 지정한 RectTransform의 화면 위치에서 파티클을 흩뿌린다 (화면 좌표 경유 변환으로 다른 부모 하위여도 위치가 어긋나지 않는다) </summary>
        public void Burst(RectTransform origin)
        {
            if (origin == null)
            {
                return;
            }

            RectTransform burstRect  = (RectTransform)transform;
            Vector2       screenPoint = RectTransformUtility.WorldToScreenPoint(null, origin.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(burstRect, screenPoint, null, out Vector2 localPoint);

            for (int i = 0; i < _particleCount; ++i)
            {
                StartCoroutine(CoParticle(localPoint));
            }
        }

        private IEnumerator CoParticle(Vector2 origin)
        {
            GameObject go = new GameObject("Particle");
            go.transform.SetParent(transform, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            float size = Random.Range(_minSize, _maxSize);
            rt.sizeDelta        = new Vector2(size, size);
            rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = origin;

            Image img = go.AddComponent<Image>();
            img.sprite        = _particleSprite;
            img.color         = _color;
            img.raycastTarget = false;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(_minSpeed, _maxSpeed);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // 일시정지(Time.timeScale = 0) 중에도 재생될 수 있어 실시간 기준으로 진행
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);

                rt.anchoredPosition = origin + dir * speed * t;
                rt.localScale       = Vector3.one * (1f - (t * 0.4f));

                Color c = _color;
                c.a *= (1f - t);
                img.color = c;

                yield return null;
            }

            Destroy(go);
        }
    }
}
