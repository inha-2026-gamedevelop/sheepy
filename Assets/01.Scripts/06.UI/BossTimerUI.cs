// Unity
using UnityEngine;

using TMPro;

using Minsung.Common;

namespace Minsung.UI
{
    // GameManager의 보스 클리어 타이머를 HUD에 표시한다.
    public class BossTimerUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private GameManager _gameManager;
        private int _lastDisplayedSecond = -1;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            // 프리팹을 다른 씬에도 재사용할 때 참조를 빠뜨려도 타이머 텍스트를 갱신할 수 있게 한다.
            if (_timeText == null)
            {
                _timeText = GetComponentInChildren<TMP_Text>(true);
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            _gameManager = GameManager.Instance;
            RefreshVisibility();
            RefreshTimeText();
        }

        private void Update()
        {
            if (_gameManager == null)
            {
                _gameManager = GameManager.Instance;
            }

            if (!RefreshVisibility())
            {
                return;
            }

            RefreshTimeText();
        }

        /****************************************
        *                Methods
        ****************************************/

        private void RefreshTimeText()
        {
            if ((_gameManager == null) || (_timeText == null))
            {
                return;
            }

            int elapsedSecond = _gameManager.BossClearTimeMs / 1000;
            if (elapsedSecond == _lastDisplayedSecond)
            {
                return;
            }

            _lastDisplayedSecond = elapsedSecond;
            _timeText.SetText("{0:00}:{1:00}", elapsedSecond / 60, elapsedSecond % 60);
        }

        private bool RefreshVisibility()
        {
            bool isVisible = (_gameManager != null) && _gameManager.IsBossRunActive;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = isVisible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable   = false;
            }

            if (!isVisible)
            {
                _lastDisplayedSecond = -1;
            }

            return isVisible;
        }
    }
}
