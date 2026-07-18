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

        private struct ParticleSlot
        {
            public RectTransform RectTransform;
            public Image Image;
            public Vector2 Origin;
            public Vector2 Direction;
            public float Speed;
            public float Elapsed;
        }

        private ParticleSlot[] _particles;
        private Coroutine _particleUpdateLoop;
        private int _activeParticleCount;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            InitializePool();
        }

        private void OnDisable()
        {
            if (_particleUpdateLoop != null)
            {
                StopCoroutine(_particleUpdateLoop);
                _particleUpdateLoop = null;
            }

            if (_particles == null)
            {
                return;
            }

            for (int i = 0; i < _particles.Length; ++i)
            {
                _particles[i].RectTransform.gameObject.SetActive(false);
            }
            _activeParticleCount = 0;
        }

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

            for (int i = 0; i < _particles.Length; ++i)
            {
                if (_particles[i].RectTransform.gameObject.activeSelf)
                {
                    continue;
                }

                ActivateParticle(i, localPoint);
            }

            if ((_activeParticleCount > 0) && (_particleUpdateLoop == null))
            {
                _particleUpdateLoop = StartCoroutine(CoUpdateParticles());
            }
        }

        private void InitializePool()
        {
            int particleCount = Mathf.Max(0, _particleCount);
            _particles = new ParticleSlot[particleCount];

            for (int i = 0; i < _particles.Length; ++i)
            {
                GameObject particle = new GameObject($"Particle {i + 1}");
                particle.transform.SetParent(transform, false);

                RectTransform rectTransform = particle.AddComponent<RectTransform>();
                rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

                Image image = particle.AddComponent<Image>();
                image.sprite        = _particleSprite;
                image.raycastTarget = false;

                _particles[i] = new ParticleSlot
                {
                    RectTransform = rectTransform,
                    Image = image,
                };
                particle.SetActive(false);
            }
        }

        private void ActivateParticle(int index, Vector2 origin)
        {
            ParticleSlot particle = _particles[index];
            float size = Random.Range(_minSize, _maxSize);
            float angle = Random.Range(0f, Mathf.PI * 2f);

            particle.Origin    = origin;
            particle.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            particle.Speed     = Random.Range(_minSpeed, _maxSpeed);
            particle.Elapsed   = 0f;
            particle.RectTransform.sizeDelta        = new Vector2(size, size);
            particle.RectTransform.anchoredPosition = origin;
            particle.RectTransform.localScale       = Vector3.one;
            particle.Image.color                    = _color;
            particle.RectTransform.gameObject.SetActive(true);

            _particles[index] = particle;
            ++_activeParticleCount;
        }

        // 일시정지(Time.timeScale = 0) 중에도 재생될 수 있도록 실시간 기준으로 모든 슬롯을 한 코루틴에서 갱신한다.
        private IEnumerator CoUpdateParticles()
        {
            while (_activeParticleCount > 0)
            {
                for (int i = 0; i < _particles.Length; ++i)
                {
                    if (!_particles[i].RectTransform.gameObject.activeSelf)
                    {
                        continue;
                    }

                    ParticleSlot particle = _particles[i];
                    particle.Elapsed += Time.unscaledDeltaTime;
                    float t = _duration > 0f ? Mathf.Clamp01(particle.Elapsed / _duration) : 1f;

                    particle.RectTransform.anchoredPosition = particle.Origin + particle.Direction * particle.Speed * t;
                    particle.RectTransform.localScale       = Vector3.one * (1f - (t * 0.4f));

                    Color color = _color;
                    color.a *= (1f - t);
                    particle.Image.color = color;
                    _particles[i] = particle;

                    if (t >= 1f)
                    {
                        particle.RectTransform.gameObject.SetActive(false);
                        --_activeParticleCount;
                    }
                }

                yield return null;
            }

            _particleUpdateLoop = null;
        }
    }
}
