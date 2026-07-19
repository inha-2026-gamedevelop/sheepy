// System
using System;
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Utility;

namespace Minsung.Visual
{
    // 씬 전환 / 사망 / 보스 등장 등에 쓰이는 화면 페이드.
    [AddComponentMenu("Minsung/Screen Fade")]
    public class ScreenFade : PersistentSingleton<ScreenFade>
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("페이드 설정")]
        [SerializeField] private Color _fadeColor       = Color.black;
        [SerializeField] private float _defaultDuration = 0.5f;

        private Image _fadeImage;   // 자동 생성되는 전체 화면 이미지
        private Coroutine _coFade;  // 진행 중 페이드 (새 요청이 오면 교체해 겹침 방지)
        private Action _pendingOnComplete; // 진행 중 페이드의 완료 콜백 - 새 요청에 밀려도 반드시 한 번은 불러줘야 이를 기다리는 코루틴이 멈추지 않는다

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void OnSingletonAwake()
        {
            CreateFadeImage();
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 화면이 어두워짐 (알파 0 -> 1) </summary>
        public void FadeOut(float duration = -1f, Action onComplete = null)
        {
            StartFade(0f, 1f, duration, onComplete);
        }

        /// <summary> 화면이 밝아짐 (알파 1 -> 0) </summary>
        public void FadeIn(float duration = -1f, Action onComplete = null)
        {
            StartFade(1f, 0f, duration, onComplete);
        }

        // 진행 중이던 페이드를 교체 - 밀려난 콜백을 유실하면 그 완료를 기다리던 코루틴(CoFadeOut 등)이 영원히 멈춘다
        private void StartFade(float from, float to, float duration, Action onComplete)
        {
            float d = duration < 0f ? _defaultDuration : duration;

            Action interrupted = null;
            if (_coFade != null)
            {
                StopCoroutine(_coFade);
                interrupted = _pendingOnComplete;
            }

            _pendingOnComplete = onComplete;
            _coFade            = StartCoroutine(FadeRoutine(from, to, d, onComplete));

            // 새 페이드 상태를 먼저 확정한 뒤에 호출 - 콜백이 재진입으로 또 다른 페이드를 걸어도 방금 만든 상태를 덮어쓰지 않는다
            interrupted?.Invoke();
        }

        /// <summary> 어두워짐 -> onMidpoint 콜백 -> 밝아짐. 씬 전환에 사용. </summary>
        public void FadeOutIn(Action onMidpoint, float duration = -1f)
        {
            float d = duration < 0f ? _defaultDuration : duration;
            FadeOut(d, () =>
            {
                onMidpoint?.Invoke();
                FadeIn(d);
            });
        }

        // 알파를 from -> to로 보간. 페이드 중에는 클릭을 막는다.
        private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
        {
            _fadeImage.raycastTarget = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // 일시정지 중에도 동작
                SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }

            SetAlpha(to);
            _fadeImage.raycastTarget = (to > 0.01f); // 투명하면 클릭 통과
            _coFade             = null;
            _pendingOnComplete  = null; // 정상 완료 - StartFade의 인터럽트 처리기가 중복 호출하지 않도록 정리
            onComplete?.Invoke();
        }

        private void SetAlpha(float alpha)
        {
            Color c = _fadeColor;
            c.a = alpha;
            _fadeImage.color = c;
        }

        private void CreateFadeImage()
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject cgo = new GameObject("FadeCanvas");
                cgo.transform.SetParent(transform);
                canvas              = cgo.AddComponent<Canvas>();
                canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2000; // 영상 UI(1000)를 포함한 모든 화면 요소 위
                cgo.AddComponent<CanvasScaler>();
                cgo.AddComponent<GraphicRaycaster>();
            }

            GameObject igo = new GameObject("FadeImage");
            igo.transform.SetParent(canvas.transform, false);

            RectTransform rt = igo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _fadeImage               = igo.AddComponent<Image>();
            Color c                  = _fadeColor;
            c.a                      = 0f;
            _fadeImage.color         = c;
            _fadeImage.raycastTarget = false;
        }
    }
}
