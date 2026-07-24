// System
using System;
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Backend;
using Minsung.Common;
using Minsung.Common.Data;

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
        [SerializeField] private GameObject           _achievementPanel;
        [SerializeField] private SettingsBackdropView _achievementBackdrop;

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
                // 위치 기반 저장 데이터 기준 - 서버 권위 로그인(TryAutoLogin)이 내려받은 진행상황도 여기 반영된다.
                _continueButton.SetActive((SaveManager.Instance != null) && SaveManager.Instance.HasPlayerState());
            }
        }

        private void Update()
        {
            if ((_settingsPanel != null) && (_settingsPanel.activeSelf) && Input.GetKeyDown(Constants.System.KEY_PAUSE))
            {
                CloseSettings();
            }

            if ((_achievementPanel != null) && (_achievementPanel.activeSelf) && Input.GetKeyDown(Constants.System.KEY_PAUSE))
            {
                CloseAchievements();
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 게임 시작 (새로 시작) - 항상 Map1부터. 기존 진행(로컬+서버)을 초기화해 다음 서버 로그인 때 되살아나지 않게 한다. </summary>
        public void OnClickStart()
        {
            SaveManager.Instance?.ClearPlayerState();
            BackendMirror.Instance?.ResetProgress(Constants.Scene.MAP_1);
            PlayBurstThenLoad(_startButtonRect, Constants.Scene.MAP_1);
        }

        /// <summary> 이어하기 - 저장된 위치 데이터(SaveData) 기준으로 해당 씬에 진입. 데이터가 있는 유저는 서버에서 내려받은 진행상황을 그대로 이어간다. </summary>
        public void OnClickContinue()
        {
            string sceneName = Constants.Scene.MAP_1;
            if ((SaveManager.Instance != null) && SaveManager.Instance.TryLoadPlayerState(out SaveData data))
            {
                sceneName = data.SceneName;
            }

            // 과거 저장 데이터가 로딩/메뉴 씬을 가리키면 로딩씬이 자기 자신을
            // 다시 여는 루프가 생길 수 있으므로, 이어하기 대상은 게임 씬만 허용한다.
            if (!IsGameplayScene(sceneName))
            {
                Debug.LogWarning($"[MainMenu] Invalid continue scene '{sceneName}'. Falling back to {Constants.Scene.MAP_1}.");
                sceneName = Constants.Scene.MAP_1;
            }

            // 이어하기: 진입한 씬에서 플레이어를 저장된 위치로 1회 복원하도록 요청
            GameManager.Instance?.RequestContinueRestore();

            RectTransform origin = (_continueButton != null) ? _continueButton.GetComponent<RectTransform>() : null;
            PlayBurstThenLoad(origin, sceneName);
        }

        private static bool IsGameplayScene(string sceneName)
        {
            return string.Equals(sceneName, Constants.Scene.MAP_1, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(sceneName, Constants.Scene.MAP_2, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(sceneName, Constants.Scene.MAP_3, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(sceneName, Constants.Scene.BOSS, StringComparison.OrdinalIgnoreCase);
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

        /// <summary> 업적 패널 토글 - 전체 업적 진행 현황(깬 것/안 깬 것)을 보여준다 </summary>
        public void OnClickAchievements()
        {
            if (_achievementPanel == null)
            {
                return;
            }

            if (_achievementPanel.activeSelf)
            {
                CloseAchievements();
                return;
            }

            StartCoroutine(CoOpenAchievements());
        }

        // 설정 패널과 동일한 블러 배경 캡처 절차 재사용
        private IEnumerator CoOpenAchievements()
        {
            if (PauseController.Instance != null)
            {
                yield return PauseController.Instance.CoCaptureSettingsBackdrop();
            }

            _achievementBackdrop?.Refresh();
            _achievementPanel.SetActive(true);
        }

        // 업적 패널을 닫고 캡처해둔 배경 텍스처를 반납 - 닫기 버튼/ESC 공용
        private void CloseAchievements()
        {
            _achievementPanel.SetActive(false);
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
