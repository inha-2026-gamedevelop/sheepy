// System
using System;
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

using Minsung.Backend;
using Minsung.Common;
using Minsung.Common.Data;
using Minsung.TimeSystem;

namespace Minsung.Player
{
    // 플레이어 코디네이터 - Input/Movement/Combat/Interaction/Rewind 컴포넌트를 주입/조율하며, 트리거 판정(HeartPickup/DamageHazard 등)이 "본체"를 이 컴포넌트로 식별하므로 루트에 유지한다.
    [RequireComponent(typeof(PlayerInput), typeof(PlayerMovement), typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerInteraction), typeof(PlayerRewind))]
    [RequireComponent(typeof(PlayerStatusEffectController))]
    public class PlayerController : MonoBehaviour, ICommandActor
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("참조")]
        [SerializeField] private Renderer       _renderer;       // 상태 색(임시 디버그) 표시용
        [SerializeField] private PlayerAnimator _playerAnimator; // 모든 하위 컴포넌트가 공유

        private PlayerInput       _input;
        private PlayerMovement    _movement;
        private PlayerCombat      _combat;
        private PlayerInteraction _interaction;
        private PlayerRewind      _rewind;
        private PlayerHealth      _health;
        private PlayerStatusEffectController _statusEffects;
        private string _objectId;

        private Material  _material; // _renderer의 인스턴스 머티리얼 캐시
        private CharaGlow _glow;     // 피격 플래시용 글로우 (없으면 플래시 생략)

        private bool _isDead;                    // 사망 -> 리스폰 완료까지 입력/물리 잠금
        private WaitForSeconds _waitDeathDelay;  // 사망 후 페이드 시작까지 대기 캐시

        // ---- 외부가 참조하는 상태 파사드 ----
        public bool IsGrounded      => _movement.IsGrounded;
        public bool IsRewinding     => _rewind.IsRewinding;
        public bool IsStunned       => _movement.IsStunned;
        public bool IsInteracting   => _interaction.IsInteracting;
        public bool IsInputInverted => _input.IsInverted;
        public bool IsDead          => _isDead;
        public PlayerStatusEffectController StatusEffects => _statusEffects;
        public string ObjectId => _objectId;

        public event Action<bool> OnInputInvertedChanged; // 혼란 아이콘 UI 연동용

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _objectId    = ManagedObjectManager.Register(EManagedObjectType.Player, this);
            _input       = GetComponent<PlayerInput>();
            _movement    = GetComponent<PlayerMovement>();
            _combat      = GetComponent<PlayerCombat>();
            _interaction = GetComponent<PlayerInteraction>();
            _rewind      = GetComponent<PlayerRewind>();
            _statusEffects = GetComponent<PlayerStatusEffectController>();
            if (_statusEffects == null)
            {
                _statusEffects = gameObject.AddComponent<PlayerStatusEffectController>();
            }
            if (!TryGetComponent(out PlayerHitFeedback hitFeedback))
            {
                gameObject.AddComponent<PlayerHitFeedback>();
            }
            if (!TryGetComponent(out PlayerSoundController soundController))
            {
                gameObject.AddComponent<PlayerSoundController>();
            }
            TryGetComponent(out _health);

            if (_renderer != null)
            {
                _material = _renderer.material;
            }

            // 컴포넌트 간 참조 주입 = 코디네이터의 핵심 역할.
            _input.Init(_movement, _combat, _rewind, _health);
            _movement.Init(this, _playerAnimator);
            _combat.Init(this, _playerAnimator, GetComponent<PlayerOrbs>(), _health);
            _interaction.Init(_movement);
            _statusEffects.Init(this, _movement);
            _rewind.Init(this, _movement, _combat, _interaction, _playerAnimator, _health, _statusEffects);

            _input.OnInvertedChanged += ForwardInvertedChanged;

            TryGetComponent(out _glow);
            if (_health != null)
            {
                _health.OnDeath   += HandleDeath;
                _health.OnDamaged += HandleDamaged;
            }
            _waitDeathDelay = new WaitForSeconds(GameDB.Player.DeathRespawnDelay);
        }

        private void Start()
        {
            // 이어하기로 진입한 경우, 저장된 위치/방향으로 1회 복원 (체크포인트 등록보다 먼저)
            TryRestoreContinuePosition();

            // 시작 지점을 기본 체크포인트로 등록 - 체크포인트 오브젝트를 지나기 전에 죽어도 복귀 가능
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCheckpoint(transform.position);
            }

            // 씬 전환(=새 씬 진입) 시점에 진행 상태를 저장+서버 미러 (종료 시점 웹요청이 불안정한 데스크톱 대비)
            PersistProgress();
        }

        /// <summary>
        /// 현재 진행 상태를 로컬 저장 + 서버 미러. 보스 진행 중이면 위치 대신 "Map2 기본 스폰"으로 기록해
        /// 이어하기 때 보스 한가운데가 아니라 Map2 스폰에서 재개되도록 한다.
        /// 호출: 씬 진입(Start) / 종료·일시정지(PlayerSaveOnExit) / 저장하고 나가기(PauseController).
        /// </summary>
        public void PersistProgress()
        {
            if (SaveManager.Instance == null)
            {
                return;
            }

            int facingDir = (_movement != null) ? _movement.FacingDir : 1;
            bool bossActive = (GameManager.Instance != null) && GameManager.Instance.IsBossRunActive;

            if (bossActive)
            {
                // 보스 중 저장 → Map2 기본 스폰으로 기록 (정확한 좌표 불필요)
                SaveManager.Instance.SavePlayerState(Constants.Scene.MAP_2, Vector3.zero, facingDir, useDefaultSpawn: true);
                BackendMirror.Instance?.MirrorPlayerProgress(Constants.Scene.MAP_2, Vector3.zero, facingDir, useDefaultSpawn: true);
                return;
            }

            // Rigidbody2D 구동이라 SetPose 직후 transform.position은 다음 물리 스텝까지 반영이 늦다.
            // 리지드바디 위치(_movement.Position)를 우선 사용해 복원 직후에도 정확한 좌표를 저장한다.
            Vector3 position = (_movement != null) ? (Vector3)_movement.Position : transform.position;

            string sceneName = SceneManager.GetActiveScene().name;
            SaveManager.Instance.SavePlayerState(sceneName, position, facingDir);
            BackendMirror.Instance?.MirrorPlayerProgress(sceneName, position, facingDir);
        }

        // 로비 '이어하기'로 진입했을 때만(1회 소비), 저장된 씬이 현재 씬과 일치하면 위치/방향을 복원한다.
        private void TryRestoreContinuePosition()
        {
            if (!GameManager.ConsumeContinueRestore())
            {
                return;
            }

            if ((SaveManager.Instance == null) || !SaveManager.Instance.TryLoadPlayerState(out SaveData data))
            {
                return;
            }

            // 저장된 씬과 현재 씬이 다르면 위치 복원은 건너뛴다(다른 맵에 잘못 떨어지는 것 방지).
            if (data.SceneName != SceneManager.GetActiveScene().name)
            {
                return;
            }

            // 보스 중 저장 등으로 "기본 스폰 사용"이면 위치 복원을 건너뛰고 씬 배치 스폰에 그대로 둔다.
            if (data.UseDefaultSpawn)
            {
                return;
            }

            _movement.SetPose(data.PlayerPosition, Vector2.zero, false); // Rigidbody 위치까지 반영
            if (_playerAnimator != null)
            {
                _playerAnimator.SetFacing(data.FacingDir); // 바라보던 방향 복원
            }
        }

        private void OnDestroy()
        {
            ManagedObjectManager.Unregister(this);
            if (_input != null)
            {
                _input.OnInvertedChanged -= ForwardInvertedChanged;
            }
            if (_health != null)
            {
                _health.OnDeath   -= HandleDeath;
                _health.OnDamaged -= HandleDamaged;
            }
        }

        private void Update()
        {
            if (_isDead)
            {
                return; // 사망 중 입력 잠금
            }
            _input.HandleInput();
        }

        private void FixedUpdate()
        {
            if (_isDead)
            {
                return; // 사망 중 물리/공격 정지
            }
            // 되감기 재생은 RewindManager가 ApplyRewindTick으로 구동한다.
            if (_rewind.IsRewinding)
            {
                return;
            }
            _movement.Tick(); // 이동/점프/매달림
            _combat.Tick();   // 예약 공격 실행
            ApplyVisual();
        }

        /****************************************
        *            Hit Reaction
        ****************************************/

        // PlayerHealth.OnDamaged 콜백 - 피해가 실제로 들어간 순간 글로우 플래시
        private void HandleDamaged()
        {
            if (_glow != null)
            {
                _glow.Flash(GameDB.Player.HitFlashColor, GameDB.Player.HitFlashDuration);
            }
        }

        /****************************************
        *            Death / Respawn
        ****************************************/

        // PlayerHealth.OnDeath 콜백 - 하트 0이 된 순간 1회
        private void HandleDeath()
        {
            if (_isDead)
            {
                return;
            }
            GameManager.Instance?.ResetBossTimer(); // 보스전 중 사망 - 진행 중이던 클리어 타이머 폐기
            StartCoroutine(CoDeathRespawn());
        }

        // 잠금 -> 대기 -> 페이드 아웃 -> 체크포인트 복귀 + 상태 복원 -> 페이드 인
        private IEnumerator CoDeathRespawn()
        {
            _isDead = true;
            _movement.SetPose(transform.position, Vector2.zero, _movement.IsGrounded);
            // 사망 애니메이션 - Animator에 Death 트리거가 추가되면 여기서 재생

            yield return _waitDeathDelay;

            // 보스전 사망은 BossController.HandlePlayerDeath가 Map2를 통째로 리로드해 복귀시킨다
            // 여기서 별도로 체크포인트 복귀 페이드를 걸면 같은 ScreenFade 슬롯을 다퉈 리로드용 페이드의
            // 콜백(씬 로드)이 취소될 수 있으므로, 보류 중인 보스 리스타트가 있으면 이 경로는 양보
            if (RespawnManager.IsBossRestartPending)
            {
                yield break;
            }

            bool respawned = false;
            respawned = RespawnManager.TryRespawn(this, OnRespawned);
            if (GameManager.Instance != null)
            {
                respawned = respawned || GameManager.Instance.RequestCheckpointRespawn(transform, OnRespawned);
            }
            if (!respawned)
            {
                OnRespawned(); // 매니저/체크포인트가 없으면 제자리 부활 (안전망)
            }
        }

        // 화면이 어두운 시점(위치 복귀 직후)에 호출 - 상태 복원
        private void OnRespawned()
        {
            _health.ResetHearts();
            _rewind.RequestClearClones(); // 사망 이전 분신 정리
            _isDead = false;
        }

        /****************************************
        *           External Facade
        *   (기존 호출부 시그니처를 그대로 유지)
        ****************************************/

        /// <summary> 일정 시간 이동/점프/공격 불가 (낙뢰 피격 경직). DamageHazard가 호출. </summary>
        public void ApplyStun(float duration) => _movement.ApplyStun(duration);

        /// <summary> 피격 넉백 (피해 지점 반대 방향). DamageHazard/MonsterController가 호출. </summary>
        public void ApplyKnockback(Vector2 sourcePosition) => _movement.ApplyKnockback(sourcePosition);

        /// <summary> 혼란(키반전) 상태 설정. BossController가 호출. </summary>
        public void SetInputInverted(bool inverted) => _input.SetInverted(inverted);

        /// <summary> 상호작용 연출 중 입력 잠금. LeverInteractive가 호출. </summary>
        public void SetInteracting(bool interacting) => _interaction.SetInteracting(interacting);

        /// <summary> E키 상호작용 실행 통지. PlayerInteractionSensor가 호출. </summary>
        public void NotifyInteracted(GameObject target) => _interaction.NotifyInteracted(target);

        /// <summary> 살아있는 분신 전부 회수. </summary>
        public void RequestClearClones() => _rewind.RequestClearClones();

        private void ForwardInvertedChanged(bool inverted) => OnInputInvertedChanged?.Invoke(inverted);

        /****************************************
        *   ICommandActor (되감기/커맨드 적용 대상)
        ****************************************/

        public void SetPose(Vector2 position, Vector2 velocity, bool grounded)
            => _movement.SetPose(position, velocity, grounded);

        public void PlayAttack(bool reversed, bool charged) => _combat.PlayAttack(reversed, charged);

        /****************************************
        *   Debug Visual (임시 - 스프라이트 넣으면 제거)
        ****************************************/

        // 되감기 중에는 코디네이터 FixedUpdate가 쉬므로 PlayerRewind가 이 메서드로 색을 갱신한다.
        public void RefreshVisual() => ApplyVisual();

        // 상태별 표시 색 (되감기 = 보라, 공격 플래시 = 시안, 풀차지 = 골드, 차지 중 = 주황, 접지 = 흰색, 공중 = 노랑).
        private void ApplyVisual()
        {
            if (_material == null)
            {
                return;
            }
            if (_rewind.IsRewinding)
            {
                _material.color = GameDB.Player.RewindTintColor;
                return;
            }
            if (_combat.IsFlashing)
            {
                _material.color = Color.cyan;
                return;
            }
            if (_combat.IsChargeReady)
            {
                _material.color = GameDB.Player.ChargeReadyColor;
                return;
            }
            if (_combat.IsCharging)
            {
                _material.color = GameDB.Player.ChargingColor;
                return;
            }
            _material.color = _movement.IsGrounded ? Color.white : Color.yellow;
        }
    }
}
