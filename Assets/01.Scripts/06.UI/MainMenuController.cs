// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.UI
{
    // 로비(메인메뉴) 버튼 - 게임 시작/이어하기/설정/종료
    [AddComponentMenu("Minsung/UI/Main Menu Controller")]
    public class MainMenuController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private GameObject           _continueButton;
        [SerializeField] private GameObject           _settingsPanel;
        [SerializeField] private SettingsBackdropView _settingsBackdrop;

        [Header("선택 파티클 버스트")]
        [SerializeField] private UiClickBurst  _clickBurst;
        [SerializeField] private RectTransform _startButtonRect;

        private bool _isTransitioning; // 버스트 대기 중 중복 클릭 방지

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            if (_continueButton != null)
            {
                _continueButton.SetActive((SaveManager.Instance != null) && SaveManager.Instance.HasSaveData());
            }
        }

        private void Update()
        {
            if ((_settingsPanel != null) && _settingsPanel.activeSelf && Input.GetKeyDown(Constants.System.KEY_PAUSE))
            {
                CloseSettings();
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 게임 시작 (새로 시작) - 임시로 보스방(Boss) 진입, 맵 완성 후 MAP_1로 교체 예정 </summary>
        public void OnClickStart()
        {
            PlayBurstThenLoad(_startButtonRect, Constants.Scene.BOSS);
        }

        /// <summary> 이어하기 - 마지막으로 저장된 씬으로 진입 </summary>
        public void OnClickContinue()
        {
            string sceneName = (SaveManager.Instance != null)
                ? SaveManager.Instance.LoadLastScene(Constants.Scene.MAP_1)
                : Constants.Scene.MAP_1;

            RectTransform origin = (_continueButton != null) ? _continueButton.GetComponent<RectTransform>() : null;
            PlayBurstThenLoad(origin, sceneName);
        }

        /// <summary> 설정 패널 토글 </summary>
        public void OnClickSettings()
        {
            if (_settingsPanel == null)
            {
                return;
            }

            if (_settingsPanel.activeSelf)
            {
                CloseSettings();
                return;
            }

            StartCoroutine(CoOpenSettings());
        }

        // 설정 패널을 열기 전에 현재 화면(패널 미노출 상태)을 캡처해 배경 블러 재료로 사용
        private IEnumerator CoOpenSettings()
        {
            if (PauseController.Instance != null)
            {
                yield return PauseController.Instance.CoCaptureSettingsBackdrop();
            }

            _settingsBackdrop?.Refresh();
            _settingsPanel.SetActive(true);
        }

        // 설정 패널을 닫고 캡처해둔 배경 텍스처를 반납 - 닫기 버튼/ESC 공용
        private void CloseSettings()
        {
            _settingsPanel.SetActive(false);
            PauseController.Instance?.ReleaseCapturedSettingsBackdrop();
        }

        /// <summary> 게임 종료 </summary>
        public void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // 클릭 지점에서 파티클을 흩뿌린 뒤 잠시 대기했다가 로딩씬으로 전환
        private void PlayBurstThenLoad(RectTransform origin, string sceneName)
        {
            if (_isTransitioning)
            {
                return;
            }
            _isTransitioning = true;

            if (_clickBurst != null)
            {
                _clickBurst.Burst(origin);
            }

            StartCoroutine(CoDelayedLoad(sceneName));
        }

        private IEnumerator CoDelayedLoad(string sceneName)
        {
            yield return new WaitForSecondsRealtime(Constants.UI.MENU_BURST_DELAY_SECONDS);
            GameManager.Instance.LoadGameplayScene(sceneName);
        }
    }
}
