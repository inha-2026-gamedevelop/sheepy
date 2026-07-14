// System
using System;
using System.Collections;

// Unity
using UnityEngine;
using TMPro;

using Minsung.Utility;

namespace Minsung.UI
{
    // 자막 한 줄 + 표시 시간. 자막이 여러 개면 이 배열 순서대로 한 번만 재생하고 끝난다 (루프 없음)
    [Serializable]
    public class CaptionEntry
    {
        [SerializeField, TextArea] private string _text;
        [SerializeField] private float _duration = 3f; // 이 줄을 보여줄 시간(초, 페이드 시간 제외)

        private WaitForSeconds _wait;

        public string Text => _text;
        public WaitForSeconds Wait => _wait ??= new WaitForSeconds(_duration);
    }

    // 화면 하단 자막 HUD
    [AddComponentMenu("Minsung/Caption Manager")]
    public class CaptionManager : PersistentSingleton<CaptionManager>
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private GameObject      _captionObject;
        [SerializeField] private TextMeshProUGUI _captionText;
        [SerializeField] private float           _fadeDuration = 0.3f;

        private CanvasGroup _canvasGroup;
        private Coroutine   _coSequence;

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void OnSingletonAwake()
        {
            if (_captionObject == null)
            {
                return;
            }

            if (!_captionObject.TryGetComponent(out _canvasGroup))
            {
                _canvasGroup = _captionObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup.alpha = 0f;
            _captionObject.SetActive(false);
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 자막 배열을 순서대로 페이드인/아웃하며 재생. 루프 없이 끝까지 재생하면 자동으로 숨겨진다 </summary>
        public void PlaySequence(CaptionEntry[] entries)
        {
            StopSequence();

            if ((entries == null) || (entries.Length == 0) || (_captionObject == null))
            {
                return;
            }

            _coSequence = StartCoroutine(SequenceRoutine(entries));
        }

        /// <summary> 재생 중인 자막 시퀀스를 즉시 멈추고 숨긴다 </summary>
        public void StopSequence()
        {
            UtilCoroutine.CheckStopCoroutine(ref _coSequence, this);

            if (_captionObject == null)
            {
                return;
            }

            _canvasGroup.alpha = 0f;
            _captionObject.SetActive(false);
        }

        /****************************************
        *           private Methods
        ****************************************/

        private IEnumerator SequenceRoutine(CaptionEntry[] entries)
        {
            _captionObject.SetActive(true);

            for (int i = 0; i < entries.Length; ++i)
            {
                _captionText.text = entries[i].Text;

                yield return FadeRoutine(0f, 1f);
                yield return entries[i].Wait;
                yield return FadeRoutine(1f, 0f);
            }

            _captionObject.SetActive(false);
            _coSequence = null;
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
