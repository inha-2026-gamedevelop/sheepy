// System
using System;
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.Video;

namespace Minsung.Visual
{
    /// <summary> 다음 씬으로 전환하기 전에 보스 아웃트로 영상을 재생한다. </summary>
    public class BossOutroVideoUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const float PREPARE_TIMEOUT_SECONDS = 10f;
        private const float MINIMUM_PLAY_TIMEOUT_SECONDS = 15f;
        private const float PLAY_TIMEOUT_PADDING_SECONDS = 5f;
        private const float INTRO_PRE_VIDEO_FADE_OUT_SECONDS = 1f;
        private const float INTRO_VIDEO_FADE_IN_SECONDS = 0.5f;
        private const float INTRO_POST_VIDEO_FADE_OUT_SECONDS = 1f;

        [SerializeField] private GameObject _root;
        [SerializeField] private VideoPlayer _player;
        [SerializeField] private VideoClip _clip;

        /****************************************
        *                Methods
        ****************************************/

        public IEnumerator CoPlay()
        {
            yield return CoPlayRoutine(0f, 0f);
        }

        public IEnumerator CoPlayIntroTransition(Action onScreenBlack)
        {
            yield return CoFadeOut(INTRO_PRE_VIDEO_FADE_OUT_SECONDS);
            onScreenBlack?.Invoke();
            yield return CoPlayRoutine(INTRO_VIDEO_FADE_IN_SECONDS, INTRO_POST_VIDEO_FADE_OUT_SECONDS);
        }

        private IEnumerator CoPlayRoutine(float videoFadeInDuration, float postVideoFadeOutDuration)
        {
            if ((_player == null) || (_clip == null))
            {
                Debug.LogError("보스 아웃트로 영상의 VideoPlayer 또는 VideoClip 참조가 없습니다.");
                yield break;
            }

            if (_root != null)
            {
                _root.SetActive(true);
            }

            _player.Stop();
            _player.clip = _clip;
            _player.skipOnDrop = false;
            _player.aspectRatio = VideoAspectRatio.FitOutside; // 화면 전체를 채우도록 비율 유지 크롭 (씬에 FitHorizontally로 잘못 설정된 경우 대비)

            bool prepared = false;
            bool finished = false;
            bool failed = false;

            void OnPrepareCompleted(VideoPlayer _)
            {
                prepared = true;
            }

            void OnLoopPointReached(VideoPlayer _)
            {
                finished = true;
            }

            void OnErrorReceived(VideoPlayer _, string message)
            {
                failed = true;
                Debug.LogError($"보스 아웃트로 영상을 재생할 수 없습니다: {message}");
            }

            _player.prepareCompleted += OnPrepareCompleted;
            _player.loopPointReached += OnLoopPointReached;
            _player.errorReceived += OnErrorReceived;

            _player.Prepare();
            float prepareDeadline = Time.realtimeSinceStartup + PREPARE_TIMEOUT_SECONDS;
            while (!prepared && !failed && (Time.realtimeSinceStartup < prepareDeadline))
            {
                yield return null;
            }

            if (prepared && !failed)
            {
                _player.Play();

                if (videoFadeInDuration > 0f)
                {
                    yield return CoFadeIn(videoFadeInDuration);
                }

                float playTimeout = Mathf.Max(
                    (float)_clip.length + PLAY_TIMEOUT_PADDING_SECONDS,
                    MINIMUM_PLAY_TIMEOUT_SECONDS);
                float playDeadline = Time.realtimeSinceStartup + playTimeout;

                while (!finished && !failed && (Time.realtimeSinceStartup < playDeadline))
                {
                    yield return null;
                }

                if (!finished && !failed)
                {
                    Debug.LogError("보스 아웃트로 영상 재생 시간이 초과되었습니다.");
                }
            }
            else if (!failed)
            {
                Debug.LogError("보스 아웃트로 영상 준비 시간이 초과되었습니다.");
            }

            _player.Stop();
            _player.prepareCompleted -= OnPrepareCompleted;
            _player.loopPointReached -= OnLoopPointReached;
            _player.errorReceived -= OnErrorReceived;

            if (postVideoFadeOutDuration > 0f)
            {
                yield return CoFadeOut(postVideoFadeOutDuration);
            }
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private static IEnumerator CoFadeOut(float duration)
        {
            if (ScreenFade.Instance == null)
            {
                yield break;
            }

            bool complete = false;
            ScreenFade.Instance.FadeOut(duration, () => complete = true);
            while (!complete)
            {
                yield return null;
            }
        }

        private static IEnumerator CoFadeIn(float duration)
        {
            if (ScreenFade.Instance == null)
            {
                yield break;
            }

            bool complete = false;
            ScreenFade.Instance.FadeIn(duration, () => complete = true);
            while (!complete)
            {
                yield return null;
            }
        }
    }
}
