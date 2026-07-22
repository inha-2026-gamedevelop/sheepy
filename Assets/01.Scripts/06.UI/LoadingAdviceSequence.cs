// System
using System.Collections;

// Unity
using UnityEngine;

namespace Minsung.UI
{
    // 로딩 화면의 접근성/시스템 안내 카드를 순환 표시 (페이드인 -> 유지 -> 페이드아웃)
    [AddComponentMenu("Minsung/UI/Loading Advice Sequence")]
    public sealed class LoadingAdviceSequence : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private CanvasGroup[] _cards;

        [SerializeField, Min(0f)]    private float _startDelaySeconds = 0f;
        [SerializeField, Min(0.1f)]  private float _displaySeconds    = 2.5f;
        [SerializeField, Min(0.05f)] private float _fadeSeconds       = 0.3f;

        public bool IsComplete { get; private set; }

        public void SetCards(CanvasGroup[] cards) => _cards = cards;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            if ((_cards == null) || (_cards.Length == 0))
            {
                IsComplete = true;
                return;
            }

            IsComplete = false;
            StartCoroutine(CoCycle());
        }

        /****************************************
        *                Methods
        ****************************************/

        private IEnumerator CoCycle()
        {
            foreach (CanvasGroup card in _cards)
            {
                card.alpha = 0f;
            }

            yield return new WaitForSecondsRealtime(_startDelaySeconds);

            for (int i = 0; i < _cards.Length; ++i)
            {
                yield return CoFade(_cards[i], 0f, 1f);
                yield return new WaitForSecondsRealtime(_displaySeconds);
                yield return CoFade(_cards[i], 1f, 0f);
            }

            IsComplete = true;
        }

        private IEnumerator CoFade(CanvasGroup card, float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                card.alpha = Mathf.Lerp(from, to, elapsed / _fadeSeconds);
                yield return null;
            }

            card.alpha = to;
        }
    }
}
