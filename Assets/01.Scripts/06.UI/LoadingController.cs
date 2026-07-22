// System
using System;
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Minsung.Common;
using Minsung.Visual;

namespace Minsung.UI
{
    // 로딩씬 진행 - GameManager가 예약해둔 대상 씬을 비동기로 로드하며 진행바를 갱신한다
    [AddComponentMenu("Minsung/UI/Loading Controller")]
    public class LoadingController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private Slider _progressSlider;
        private LoadingAdviceSequence _adviceSequence;
        private string _targetSceneName;

        public void SetProgressSlider(Slider slider) => _progressSlider = slider;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            string targetScene = GameManager.ConsumePendingScene();
            if (string.IsNullOrEmpty(targetScene))
            {
                targetScene = Constants.Scene.MAIN_MENU;
            }

            // 잘못 저장된 pending 값으로 00.GameLoading이 반복 진입하지 않도록 차단한다
            if (string.Equals(targetScene, Constants.Scene.LOADING, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetScene, Constants.Scene.GAME_LOADING, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetScene, "Loading", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[Loading] Invalid target '{targetScene}'. Falling back to {Constants.Scene.MAIN_MENU}.");
                targetScene = Constants.Scene.MAIN_MENU;
            }

            _targetSceneName = targetScene;
            _adviceSequence = GetComponent<LoadingAdviceSequence>();
            StartCoroutine(CoLoad(targetScene));
        }

        /****************************************
        *                Methods
        ****************************************/

        private IEnumerator CoLoad(string sceneName)
        {
            float elapsed = 0f;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < Constants.UI.SCENE_ACTIVATION_PROGRESS)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(op.progress / Constants.UI.SCENE_ACTIVATION_PROGRESS);
                yield return null;
            }

            SetProgress(1f);

            while ((elapsed < Constants.UI.LOADING_MIN_DISPLAY_SECONDS) ||
                   ((_adviceSequence != null) && !_adviceSequence.IsComplete))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (ScreenFade.Instance == null)
            {
                new GameObject("ScreenFade").AddComponent<ScreenFade>();
            }

            ScreenFade.Instance.FadeOut(Constants.UI.SCENE_FADE_DURATION, () =>
            {
                SceneManager.sceneLoaded += HandleTargetSceneLoaded;
                op.allowSceneActivation = true;
            });
        }

        private void HandleTargetSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != _targetSceneName)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleTargetSceneLoaded;
            ScreenFade.Instance?.FadeIn(Constants.UI.SCENE_FADE_DURATION);
        }

        private void SetProgress(float value)
        {
            if (_progressSlider != null)
            {
                _progressSlider.value = value;
            }
        }
    }
}
