// System
using System;
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Utility;

namespace Minsung.Player
{
    // 플레이어 전투 담당 - 공격 예약/실행(오브 우선, 없으면 근접 히트박스), 공격 플래시 연출. 정방향은 Tick, 역재생 모션 전용 재생은 커맨드가 PlayAttack(reversed:true)으로 호출한다.
    public class PlayerCombat : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private PlayerController _coordinator; // 공유 잠금 상태 조회용
        private PlayerAnimator   _animator;
        private PlayerOrbs       _orbs;   // 오브 공격 대행(선택, 없으면 근접 히트박스만)
        private PlayerHealth     _health; // 히트박스 소유자(자기 피해 방지)

        private Rigidbody2D _rb; // 히트박스 스폰 위치

        private bool _attackPending;    // 예약된 공격 (다음 Tick에 소비)
        private bool _chargedPending;   // 예약된 차지공격 (다음 Tick에 소비)
        private bool _attackedThisTick; // 이번 물리 틱에 공격 실행 여부 (되감기 기록용)
        private bool _attackWasCharged; // 이번 틱 공격이 차지공격인지 (되감기 기록용)
        private bool _attackFlashing;
        private float _nextAttackTime;

        private bool  _isCharging;      // X 홀드로 차지 중
        private float _chargeStartTime; // 차지 시작 시각 (Time.time)

        private Coroutine     _coAttackFlash;
        private WaitForSeconds _waitAttackFlash;

        public bool AttackedThisTick => _attackedThisTick;
        public bool AttackWasCharged => _attackWasCharged;
        public bool IsFlashing       => _attackFlashing;

        public bool IsCharging    => _isCharging;
        public bool IsChargeReady => _isCharging && ((Time.time - _chargeStartTime) >= GameDB.Player.ChargeTime);

        /// <summary> 실제 공격 실행 순간(charged 여부 포함). 되감기 역재생(모션만)에서는 발생하지 않는다 (SFX 훅용) </summary>
        public event Action<bool> OnAttacked;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _waitAttackFlash = new WaitForSeconds(GameDB.Player.AttackFlashTime);
        }

        public void Init(PlayerController coordinator, PlayerAnimator animator, PlayerOrbs orbs, PlayerHealth health)
        {
            _coordinator = coordinator;
            _animator    = animator;
            _orbs        = orbs;
            _health      = health;
        }

        /****************************************
        *                Methods
        ****************************************/

        public void RequestAttack()
        {
            if (_coordinator.IsRewinding || _coordinator.IsStunned || _coordinator.IsInteracting)
            {
                return;
            }
            _attackPending = true;
        }

        /// <summary> X 홀드 시작 - 차지 타이머 개시 (일반 공격과 별개로 진행). </summary>
        public void BeginCharge()
        {
            if (_isCharging)
            {
                return;
            }
            if (_coordinator.IsRewinding || _coordinator.IsStunned || _coordinator.IsInteracting)
            {
                return;
            }
            _isCharging      = true;
            _chargeStartTime = Time.time;
        }

        /// <summary> X 홀드 해제 - 풀차지(CHARGE_TIME 이상)면 강화 공격 예약, 아니면 무시. </summary>
        public void ReleaseCharge()
        {
            if (!_isCharging)
            {
                return;
            }
            bool fullyCharged = IsChargeReady;
            _isCharging = false;

            if (!fullyCharged)
            {
                return;
            }
            if (_coordinator.IsRewinding || _coordinator.IsStunned || _coordinator.IsInteracting)
            {
                return;
            }
            _chargedPending = true;
        }

        // 코디네이터의 FixedUpdate가 이동 처리 뒤에 호출한다.
        public void Tick()
        {
            _attackedThisTick = false;
            _attackWasCharged = false;

            if ((_attackPending || _chargedPending) && (Time.time >= _nextAttackTime))
            {
                bool charged = _chargedPending;
                _attackedThisTick = true;
                _attackWasCharged = charged;
                _chargedPending = false;
                _nextAttackTime = Time.time + GameDB.Player.AttackCooldown;
                PlayAttack(reversed: false, charged: charged);
            }

            _attackPending = false;
        }

        // 범위 안에 적이 있으면 오브가 날아가 타격하고, 없으면 근접 히트박스로 폴백.
        // reversed(되감기 역재생)면 모션만 재생하고 피해는 주지 않는다.
        // charged면 피해에 ChargeDamageMult 배율을 적용한다 (분신 재연도 같은 배율).
        public void PlayAttack(bool reversed, bool charged)
        {
            if (_animator != null)
            {
                _animator.TriggerAttack();
            }

            if (!reversed)
            {
                UtilCoroutine.CheckRunCoroutine(ref _coAttackFlash, StartCoroutine(CoAttackFlash()), this);

                float damage = GameDB.Player.AttackDamage;
                if (charged)
                {
                    damage *= GameDB.Player.ChargeDamageMult;
                }

                if ((_orbs == null) || !_orbs.TryAttackNearest(damage))
                {
                    AttackHitbox.Spawn(_rb.position, damage, DamageSource.Player, _health);
                }

                OnAttacked?.Invoke(charged);
            }
        }

        // 공격 순간 잠깐 시안색으로 번쩍이는 연출 타이머.
        private IEnumerator CoAttackFlash()
        {
            _attackFlashing = true;
            yield return _waitAttackFlash;
            _attackFlashing = false;
            _coAttackFlash  = null;
        }
    }
}
