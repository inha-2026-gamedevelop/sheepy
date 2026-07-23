// Unity
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Minsung.Item;

namespace Minsung.UI
{
    // 포션 수량과 사용 쿨타임을 표시한다. 기존 씬의 구형 카운터는 자동 인식하고, 쿨타임 표시가 있는 프리팹 HUD를 우선한다.
    public class PotionCounterUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private TMP_Text _countText;
        [SerializeField] private Image    _cooldownFill; // 경과 비율
        [SerializeField] private TMP_Text _cooldownText; // 남은 쿨타임

        private static PotionCounterUI _activeInstance;

        private PotionManager _potionManager;

        private bool HasCooldownPresentation => (_cooldownFill != null) || (_cooldownText != null);

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            CacheReferences();

            // Map2에 남아 있는 구형 카운터와 새 프리팹이 함께 로드될 때, 쿨타임 HUD가 있는 프리팹만 표시한다.
            if ((_activeInstance != null) &&
                ((!HasCooldownPresentation) || (_activeInstance.HasCooldownPresentation)))
            {
                gameObject.SetActive(false);
                return;
            }
            if (_activeInstance != null)
            {
                _activeInstance.gameObject.SetActive(false);
            }
            _activeInstance = this;

            SetCooldownVisible(false);
        }

        private void Start()
        {
            BindPotionManager();
            RedrawCount();
        }

        private void OnDestroy()
        {
            UnbindPotionManager();
            if (_activeInstance == this)
            {
                _activeInstance = null;
            }
        }

        private void Update()
        {
            if (_activeInstance != this)
            {
                return;
            }

            if (_potionManager != PotionManager.Instance)
            {
                BindPotionManager();
            }
            RefreshCooldown();
        }

        /****************************************
        *                Methods
        ****************************************/

        private void CacheReferences()
        {
            if (_countText == null)
            {
                Transform countTransform = transform.Find("PotionCount");
                if (countTransform != null)
                {
                    countTransform.TryGetComponent(out _countText);
                }
            }
            if (_cooldownFill == null)
            {
                Transform fillTransform = transform.Find("CooldownFill");
                if (fillTransform != null)
                {
                    fillTransform.TryGetComponent(out _cooldownFill);
                }
            }
            if (_cooldownText == null)
            {
                Transform textTransform = transform.Find("CooldownText");
                if (textTransform != null)
                {
                    textTransform.TryGetComponent(out _cooldownText);
                }
            }
        }

        private void BindPotionManager()
        {
            UnbindPotionManager();
            _potionManager = PotionManager.Instance;
            if (_potionManager != null)
            {
                _potionManager.OnPotionChanged += HandlePotionChanged;
            }

            RedrawCount();
        }

        private void UnbindPotionManager()
        {
            if (_potionManager != null)
            {
                _potionManager.OnPotionChanged -= HandlePotionChanged;
            }
            _potionManager = null;
        }

        private void HandlePotionChanged(int count)
        {
            RedrawCount();
        }

        private void RedrawCount()
        {
            if ((_countText == null) || (_potionManager == null))
            {
                return;
            }

            _countText.SetText("{0} / {1}", _potionManager.PotionCount, _potionManager.MaxCarryCount);
        }

        private void RefreshCooldown()
        {
            if (_potionManager == null)
            {
                SetCooldownVisible(false);
                return;
            }

            bool onCooldown = !_potionManager.IsPotionReady;
            SetCooldownVisible(onCooldown);
            if (!onCooldown)
            {
                return;
            }

            float duration = _potionManager.PotionCooldownDuration;
            float remaining = _potionManager.PotionCooldownRemaining;
            float elapsedRatio = (duration > 0f) ? Mathf.Clamp01(1f - (remaining / duration)) : 1f;

            if (_cooldownFill != null)
            {
                _cooldownFill.fillAmount = elapsedRatio;
            }
            if (_cooldownText != null)
            {
                _cooldownText.SetText("{0}", Mathf.CeilToInt(remaining));
            }
        }

        private void SetCooldownVisible(bool visible)
        {
            if (_cooldownFill != null)
            {
                _cooldownFill.enabled = visible;
                if (!visible)
                {
                    _cooldownFill.fillAmount = 0f;
                }
            }
            if (_cooldownText != null)
            {
                _cooldownText.enabled = visible;
            }
        }
    }
}
