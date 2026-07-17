// Unity
using UnityEngine;

using Minsung.Achievement;
using Minsung.Common;
using Minsung.Common.Data;
using Minsung.TimeSystem;
using Minsung.Visual;

namespace Minsung.Player
{
    // 플레이어 되감기 담당 - 틱 기록(RingBuffer), 되감기 재생, 종료 시 분신 소환.
    // RewindManager에 IRewindable로 등록되어 매 물리 틱 RecordTick / 되감기 중 ApplyRewindTick을 받는다.
    public class PlayerRewind : MonoBehaviour, IRewindable
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("되감기")]
        [SerializeField] private ClonePool        _clonePool;     // 되감은 궤적으로 분신 소환
        [SerializeField] private VhsRewindOverlay _rewindOverlay; // 되감기 중 화면 전체 VHS 연출

        private PlayerController  _coordinator; // ICommandActor(SetPose/PlayAttack) + 색 틴트 갱신
        private PlayerMovement    _movement;
        private PlayerCombat      _combat;
        private PlayerInteraction _interaction;
        private PlayerAnimator    _animator;
        private PlayerHealth      _health;
        private PlayerStatusEffectController _statusEffects;

        private RewindManager _rewindManager;
        private RingBuffer<TickCommand> _buffer;

        private bool _isRewinding;
        private int  _rewindPrevIndex; // 직전에 적용한 orderedIndex

        public bool IsRewinding => _isRewinding;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            // 버퍼 용량은 다른 리와인드 참여자와 동일 기준(TickCapacity)을 써야 인덱스가 일치한다.
            _buffer = new RingBuffer<TickCommand>(RewindManager.TickCapacity);
        }

        public void Init(PlayerController coordinator, PlayerMovement movement, PlayerCombat combat,
                         PlayerInteraction interaction, PlayerAnimator animator, PlayerHealth health,
                         PlayerStatusEffectController statusEffects)
        {
            _coordinator = coordinator;
            _movement    = movement;
            _combat      = combat;
            _interaction = interaction;
            _animator    = animator;
            _health      = health;
            _statusEffects = statusEffects;
        }

        private void Start()
        {
            // 싱글톤 Awake 순서 보장이 없으므로 Start에서 등록한다.
            _rewindManager = RewindManager.Instance;
            _rewindManager?.Register(this);
        }

        private void OnDestroy()
        {
            _rewindManager?.Unregister(this);
        }

        /****************************************
        *            Input Requests
        ****************************************/

        /// <summary> 되감기 시작 요청. 분신 여유가 없거나 기록이 없으면 무시. </summary>
        public void RequestRewind()
        {
            if ((_statusEffects != null) && _statusEffects.IsRewindSealed)
            {
                return;
            }
            if (_isRewinding)
            {
                return;
            }
            if ((_clonePool == null) || !_clonePool.CanSpawn()) // 분신이 최대치 → 무시
            {
                return;
            }
            if (_buffer.Count == 0)
            {
                return;
            }
            _rewindManager?.StartRewind();
        }

        /// <summary> 살아있는 분신을 전부 회수한다. 되감기 중에도 허용. </summary>
        public void RequestClearClones()
        {
            _clonePool?.ClearAll();
        }

        /****************************************
        *              IRewindable
        ****************************************/

        public void RecordTick()
        {
            MoveCommand move = new MoveCommand(_movement.Position, _movement.Velocity, _movement.IsGrounded);
            int halves = (_health != null) ? _health.CurrentHalves : GameDB.Player.MaxHeartHalves;

            bool interacted = _interaction.ConsumeInteract(out GameObject target);
            InteractCommand interact = interacted ? new InteractCommand(target) : default;
            AnimCommand anim = (_animator != null) ? _animator.CaptureAnimState() : default;

            // 차지 여부를 함께 기록 - 분신 재연/역재생이 같은 배율로 재현된다
            AttackCommand attack = new AttackCommand(_combat.AttackWasCharged);
            _buffer.Push(new TickCommand(move, _combat.AttackedThisTick, attack, interacted, interact, halves, anim));
        }

        public void OnRewindStart()
        {
            AchievementManager.Instance?.Unlock(AchievementIds.FIRST_REWIND);

            _isRewinding = true;
            _interaction.ForceStop(); // 상호작용 애니메이션 중이었어도 되감기가 우선
            _movement.OnRewindStart();

            _rewindPrevIndex = _buffer.Count - 1;

            _rewindOverlay?.Play();
            _animator?.SetScrubbing(true); // 모션 역재생 - 틱마다 기록 프레임을 직접 스크럽한다
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            if (_buffer.TryGetOrdered(orderedIndex, out TickCommand tick))
            {
                tick.Move.Apply(_coordinator); // ICommandActor → PlayerMovement.SetPose
                _health?.RestoreHalves(tick.Hearts);
                _animator?.ApplyAnimState(tick.Anim); // 기록 프레임 스크럽 - 레버 포함 모든 모션이 실제 역재생된다
            }

            if (WasAttackedBetween(orderedIndex, _rewindPrevIndex, out AttackCommand attack))
            {
                attack.Undo(_coordinator); // ICommandActor → PlayerCombat.PlayAttack(reversed)
            }

            _rewindPrevIndex = orderedIndex;
            _coordinator.RefreshVisual(); // 되감기 색 틴트 유지
        }

        // 타임리와인드 한 그대로 분신을 남긴다.
        public void OnRewindEnd(int orderedIndex)
        {
            if (_buffer.Count > 0)
            {
                _clonePool.Spawn(_buffer); // 분신이 버퍼 내용을 자기 클립으로 복사해 간다
            }

            _isRewinding = false;
            _movement.OnRewindEnd();

            _rewindOverlay?.Stop();
            _animator?.SetScrubbing(false);

            _buffer.Clear();
        }

        // [fromIndexInclusive, toIndexExclusive) 구간의 틱에 공격 기록이 있는지 검사.
        // 되감기 스텝은 여러 틱을 건너뛰므로, 건너뛴 구간의 공격도 놓치지 않고 Undo하기 위함.
        private bool WasAttackedBetween(int fromIndexInclusive, int toIndexExclusive, out AttackCommand attack)
        {
            for (int i = fromIndexInclusive; i < toIndexExclusive; ++i)
            {
                if (_buffer.TryGetOrdered(i, out TickCommand tick) && tick.HasAttack)
                {
                    attack = tick.Attack;
                    return true;
                }
            }
            attack = default;
            return false;
        }
    }
}
