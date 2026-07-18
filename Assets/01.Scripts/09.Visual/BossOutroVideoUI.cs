// System
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

        [SerializeField] private GameObject _root;
        [SerializeField] private VideoPlayer _player;
        [SerializeField] private VideoClip _clip;

        /****************************************
        *                Methods
        ****************************************/

        public IEnumerator CoPlay()
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
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }
    }
}
