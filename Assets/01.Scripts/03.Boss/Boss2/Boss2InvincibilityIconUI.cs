// Unity
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Minsung.Player;

// 4페이즈 전용 무적 키
public class Boss2InvincibilityIconUI : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/

    [SerializeField] private Boss2Health _boss;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Image     _icon;         // 4페이즈 이전엔 비표시
    [SerializeField] private Image     _cooldownFill;  // 경과 비율
    [SerializeField] private TMP_Text  _cooldownText;  // 남은 쿨타임

    private bool _isPhase4;

    /****************************************
    *              Unity Event
    ****************************************/

    private void Awake()
    {
        if (_icon == null)
        {
            _icon = GetComponent<Image>();
        }

        SetVisible(false);
        SetCooldownVisible(false);
    }

    private void Start()
    {
        if (_boss == null)
        {
            _boss = FindAnyObjectByType<Boss2Health>();
        }
        if (_playerHealth == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            _playerHealth = (player != null) ? player.GetComponent<PlayerHealth>() : null;
        }

        if (_boss != null)
        {
            _boss.OnPhaseChanged += HandlePhaseChanged;
            HandlePhaseChanged(_boss.PhaseIndex);
        }
    }

    private void OnDestroy()
    {
        if (_boss != null)
        {
            _boss.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void Update()
    {
        if (!_isPhase4 || (_playerHealth == null))
        {
            return;
        }
        RefreshCooldown();
    }

    /****************************************
    *                Methods
    ****************************************/

    private void HandlePhaseChanged(int phaseIndex)
    {
        _isPhase4 = (_boss != null) && _boss.IsFinalPhase;
        SetVisible(_isPhase4);
        if (!_isPhase4)
        {
            SetCooldownVisible(false);
        }
    }

    private void RefreshCooldown()
    {
        bool onCooldown = !_playerHealth.IsDodgeInvincibleReady;
        SetCooldownVisible(onCooldown);
        if (!onCooldown)
        {
            return;
        }

        float duration  = _playerHealth.DodgeInvincibleCooldownDuration;
        float remaining = _playerHealth.DodgeInvincibleCooldownRemaining;
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

    private void SetVisible(bool visible)
    {
        if (_icon != null)
        {
            _icon.enabled = visible;
        }
    }

    private void SetCooldownVisible(bool visible)
    {
        if (_cooldownFill != null)
        {
            _cooldownFill.enabled = visible;
        }
        if (_cooldownText != null)
        {
            _cooldownText.enabled = visible;
        }
    }
}
