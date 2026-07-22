// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Minsung.Common;

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

            while (op.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(op.progress / 0.9f);
                yield return null;
            }

            SetProgress(1f);

            while (elapsed < Constants.UI.LOADING_MIN_DISPLAY_SECONDS)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            op.allowSceneActivation = true;
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
