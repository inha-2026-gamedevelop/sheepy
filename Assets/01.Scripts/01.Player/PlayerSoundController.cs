// System
using System;

// Unity
using UnityEngine;

using Minsung.Sound;

namespace Minsung.Player
{
    // 플레이어 동작(점프/착지/공격/차지공격/피격/사망)을 SoundManager SFX 재생에 연결한다.
    // 클립 자체는 SoundDB(ESfxState.Player)에서 관리하고, 여기서는 훅 on/off와 피치만 인스펙터로 조정한다.
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerSoundController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Serializable]
        private class SfxHook
        {
            [SerializeField] private bool _enabled = true;
            [SerializeField] private EPlayerSfx _sfx; // SoundDB의 Player 카테고리 중 어떤 항목을 재생할지 선택
            [SerializeField, Range(0.5f, 2f)] private float _pitch = 1f;

            public bool      Enabled => _enabled;
            public EPlayerSfx Sfx    => _sfx;
            public float      Pitch  => _pitch;

            public SfxHook(EPlayerSfx sfx)
            {
                _sfx = sfx;
            }
        }

        [Header("이동")]
        [SerializeField] private SfxHook _jumpSfx = new SfxHook(EPlayerSfx.Jump);
        [SerializeField] private SfxHook _landSfx = new SfxHook(EPlayerSfx.Land);

        [Header("전투")]
        [SerializeField] private SfxHook _attackSfx       = new SfxHook(EPlayerSfx.Attack);
        [SerializeField] private SfxHook _chargeAttackSfx = new SfxHook(EPlayerSfx.ChargeAttack);

        [Header("피격 / 사망")]
        [SerializeField] private SfxHook _hitSfx   = new SfxHook(EPlayerSfx.Hit);
        [SerializeField] private SfxHook _deathSfx = new SfxHook(EPlayerSfx.Death);

        private PlayerMovement _movement;
        private PlayerCombat   _combat;
        private PlayerHealth   _health;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _combat   = GetComponent<PlayerCombat>();
            _health   = GetComponent<PlayerHealth>();

            if (_movement != null)
            {
                _movement.OnJumped += HandleJumped;
                _movement.OnLanded += HandleLanded;
            }
            if (_combat != null)
            {
                _combat.OnAttacked += HandleAttacked;
            }
            if (_health != null)
            {
                _health.OnDamaged += HandleDamaged;
                _health.OnDeath   += HandleDeath;
            }
        }

        private void OnDestroy()
        {
            if (_movement != null)
            {
                _movement.OnJumped -= HandleJumped;
                _movement.OnLanded -= HandleLanded;
            }
            if (_combat != null)
            {
                _combat.OnAttacked -= HandleAttacked;
            }
            if (_health != null)
            {
                _health.OnDamaged -= HandleDamaged;
                _health.OnDeath   -= HandleDeath;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        private void HandleJumped() => Play(_jumpSfx);
        private void HandleLanded() => Play(_landSfx);

        private void HandleAttacked(bool charged) => Play(charged ? _chargeAttackSfx : _attackSfx);

        private void HandleDamaged() => Play(_hitSfx);
        private void HandleDeath()   => Play(_deathSfx);

        private void Play(SfxHook hook)
        {
            if (!hook.Enabled)
            {
                return;
            }
            SoundManager.Instance?.PlaySFX(ESfxState.Player, (int)hook.Sfx, hook.Pitch);
        }
    }
}
