// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.UI
{
    // 일시정지 화면 버튼 - 계속하기/설정/로비로 나가기/종료
    [AddComponentMenu("Minsung/UI/Pause Menu Controller")]
    public class PauseMenuController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private GameObject           _settingsPanel;
        [SerializeField] private SettingsBackdropView _settingsBackdrop;

        [Header("선택 파티클 버스트")]
        [SerializeField] private UiClickBurst  _clickBurst;
        [SerializeField] private RectTransform _returnButtonRect;

        private bool _isTransitioning; // 버스트 대기 중 중복 클릭 방지

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 계속하기 </summary>
        public void OnClickResume()
        {
            PauseController.Instance.Resume();
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
                _settingsPanel.SetActive(false);
                PauseController.Instance?.ReleaseCapturedSettingsBackdrop();
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

        /// <summary> 로비로 나가기 - 파티클 버스트 후 로딩씬 경유로 전환 </summary>
        public void OnClickReturnToMainMenu()
        {
            if (_isTransitioning)
            {
                return;
            }
            _isTransitioning = true;

            if (_clickBurst != null)
            {
                _clickBurst.Burst(_returnButtonRect);
            }

            StartCoroutine(CoDelayedReturn());
        }

        /// <summary> 게임 종료 </summary>
        public void OnClickQuit()
        {
            PauseController.Instance.QuitGame();
        }

        private IEnumerator CoDelayedReturn()
        {
            yield return new WaitForSecondsRealtime(Constants.UI.MENU_BURST_DELAY_SECONDS);
            PauseController.Instance.ReturnToMainMenu();
        }
    }
}
