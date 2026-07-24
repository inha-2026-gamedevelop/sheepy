// Unity
using UnityEngine;
using TMPro;

namespace Minsung.UI
{
    // 로딩 텍스트 뒤 점 개수를 1~3개로 반복 순환 (Loading . -> Loading .. -> Loading ...)
    [AddComponentMenu("Minsung/UI/Loading Dots Text")]
    public class LoadingDotsText : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const int MIN_DOTS = 1;
        private const int MAX_DOTS = 3;

        [SerializeField] private TMP_Text _text;
        [SerializeField] private string   _baseText        = "Loading ";
        [SerializeField] private float    _intervalSeconds = 0.4f;

        private float _elapsed;
        private int   _dotCount;

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnEnable()
        {
            _elapsed  = 0f;
            _dotCount = MIN_DOTS;
            ApplyText();
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < _intervalSeconds)
            {
                return;
            }
            _elapsed -= _intervalSeconds;

            _dotCount = (_dotCount % MAX_DOTS) + 1;
            ApplyText();
        }

        /****************************************
        *                Methods
        ****************************************/

        private void ApplyText()
        {
            if (_text == null)
            {
                return;
            }
            _text.text = _baseText + new string('.', _dotCount);
        }
    }
}
