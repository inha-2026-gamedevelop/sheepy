// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Achievement;

namespace Minsung.UI
{
    // 엔딩 크레딧 페이지 재생기. 페이지(문구/이미지)는 씬에 미리 구성된 CanvasGroup들을 순서대로 참조만 하고,
    // 이 스크립트는 순차 페이드 인/대기/페이드 아웃 타이밍만 담당한다.
    public sealed class OuttroCreditsController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const float MIN_FADE_DURATION = 0.01f;

        [SerializeField] private CanvasGroup[] _creditPages;   
        [SerializeField] private float         _fadeDuration   = 1.2f;
        [SerializeField] private float         _displaySeconds = 3.5f;

        [Header("업적")]
        [SerializeField] private int _achievementPageIndex = 3; // 이 인덱스 페이지 진입 시점에 "아름다운 이별" 해제 (현재 구성 기준 Page_DevPeriod)

        private WaitForSeconds _waitDisplay;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _waitDisplay = new WaitForSeconds(_displaySeconds);
        }

        private void Start()
        {
            StartCoroutine(CoPlayCredits());
        }

        /****************************************
        *                Methods
        ****************************************/

        private IEnumerator CoPlayCredits()
        {
            for (int i = 0; i < _creditPages.Length; ++i)
            {
                CanvasGroup page = _creditPages[i];
                if (page == null)
                {
                    continue;
                }

                if (i == _achievementPageIndex)
                {
                    AchievementTrigger.EndingCreditsWatched();
                }

                page.gameObject.SetActive(true);
                yield return CoFade(page, 0f, 1f);
                yield return _waitDisplay;
                yield return CoFade(page, 1f, 0f);
                page.gameObject.SetActive(false);
            }
        }

        private IEnumerator CoFade(CanvasGroup page, float startAlpha, float endAlpha)
        {
            float duration = Mathf.Max(_fadeDuration, MIN_FADE_DURATION);
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                page.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }

            page.alpha = endAlpha;
        }
    }
}
