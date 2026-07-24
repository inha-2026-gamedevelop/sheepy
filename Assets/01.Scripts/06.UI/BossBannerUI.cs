// System
using System.Collections;

// Unity
using UnityEngine;
using TMPro;

namespace Minsung.UI
{
    // 보스 특정 패턴 시 화면 중앙에 극적인 경고/대사 문구를 페이드 인/아웃으로 표시한다 (메이플 유피테르식 중앙 배너)
    public class BossBannerUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private GameObject      _bannerObject; // 배너 루트 (텍스트 + 배경 포함)
        [SerializeField] private TextMeshProUGUI _bannerText;
        [SerializeField] private float           _fadeDuration = 0.4f; // 페이드 인/아웃 시간(초)

        private CanvasGroup _canvasGroup;
        private Coroutine   _co;

        /// <summary> 배너가 표시(페이드 인/유지/페이드 아웃) 중인가 - 문구가 완전히 닫힐 때까지 대기하는 데 쓴다 </summary>
        public bool IsShowing => _co != null;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            if (_bannerObject == null)
            {
                return;
            }

            if (!_bannerObject.TryGetComponent(out _canvasGroup))
            {
                _canvasGroup = _bannerObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup.alpha = 0f;
            _bannerObject.SetActive(false);
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 배너 문구를 페이드 인 -> holdDuration 유지 -> 페이드 아웃으로 표시. 재호출 시 이전 표시를 대체한다 </summary>
        public void Show(string message, float holdDuration)
        {
            if ((_bannerObject == null) || (string.IsNullOrEmpty(message)))
            {
                return;
            }

            if (_co != null)
            {
                StopCoroutine(_co);
            }
            _co = StartCoroutine(ShowRoutine(message, holdDuration));
        }

        /// <summary> 표시 중인 배너를 즉시 숨긴다 </summary>
        public void Hide()
        {
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
            if (_bannerObject == null)
            {
                return;
            }

            _canvasGroup.alpha = 0f;
            _bannerObject.SetActive(false);
        }

        /****************************************
        *           private Methods
        ****************************************/

        private IEnumerator ShowRoutine(string message, float holdDuration)
        {
            _bannerText.text = message;
            _bannerObject.SetActive(true);

            yield return FadeRoutine(0f, 1f);
            yield return HoldRoutine(holdDuration);
            yield return FadeRoutine(1f, 0f);

            _bannerObject.SetActive(false);
            _co = null;
        }

        // holdDuration만큼 대기. 슬로우/일시정지 중에도 실시간으로 흐르도록 unscaled 사용
        private IEnumerator HoldRoutine(float holdDuration)
        {
            float elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 알파를 from -> to로 보간. 일시정지 중에도 동작하도록 unscaledDeltaTime 사용
        private IEnumerator FadeRoutine(float from, float to)
        {
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = to;
        }
    }
}
