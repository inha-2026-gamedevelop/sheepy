// Unity
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.UI
{
    // 아이콘 글로우 이미지와 텍스트 언더레이의 알파를 펄린 노이즈로 은은하게 펄싱
    [AddComponentMenu("Minsung/UI/Loading Neon Pulse")]
    public sealed class LoadingNeonPulse : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField, Min(0f)]         private float _speed  = 2.4f;
        [SerializeField, Range(0f, 0.5f)] private float _amount = 0.12f;

        private Image[] _glows;
        private TextMeshProUGUI[] _texts;
        private Color[] _glowColors;
        private Color[] _underlayColors;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _glows = GetComponentsInChildren<Image>(true);
            _texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            _glowColors = new Color[_glows.Length];
            _underlayColors = new Color[_texts.Length];

            for (int i = 0; i < _glows.Length; ++i)
            {
                _glowColors[i] = _glows[i].color;
            }

            for (int i = 0; i < _texts.Length; ++i)
            {
                Material mat = _texts[i].fontMaterial;
                _underlayColors[i] = ((mat != null) && mat.HasProperty("_UnderlayColor")) ? mat.GetColor("_UnderlayColor") : Color.clear;
            }
        }

        private void Update()
        {
            float pulse = 1f + ((Mathf.PerlinNoise(Time.unscaledTime * _speed, 0.37f) - 0.5f) * 2f * _amount);

            for (int i = 0; i < _glows.Length; ++i)
            {
                if (_glows[i].name != "IconGlow")
                {
                    continue;
                }

                Color c = _glowColors[i];
                c.a = Mathf.Clamp01(_glowColors[i].a * pulse);
                _glows[i].color = c;
            }

            for (int i = 0; i < _texts.Length; ++i)
            {
                Material mat = _texts[i].fontMaterial;
                if ((mat == null) || !mat.HasProperty("_UnderlayColor"))
                {
                    continue;
                }

                Color c = _underlayColors[i];
                c.a = Mathf.Clamp01(_underlayColors[i].a * pulse);
                mat.SetColor("_UnderlayColor", c);
            }
        }
    }
}
