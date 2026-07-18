// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Player;
using Minsung.Utility;

namespace Minsung.TimeSystem
{
    // 분신 - 기록된 클립을 정방향 재생하다 끝나면 idle. 하트 0이어도 되감기로 부활해야 해서 즉시 풀 반환하지 않고 비활성화만 한다.
    [RequireComponent(typeof(Rigidbody2D))]
    public class CloneController : MonoBehaviour, ICommandActor, IRewindable
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("분신")]
        [SerializeField] private Renderer _renderer;

        [Header("판정")]
        [SerializeField] private LayerMask _groundLayer; // 클립 종료 시 공중이면 바닥으로 스냅하는 판정용 (Player.prefab의 _groundLayer와 동일하게 설정)

        private Color _color; // 분신 틴트 - TimeDB(GameDB.Time)에서 Awake 때 로드

        private Rigidbody2D _rb;
        private Collider2D  _col;
        private PlayerHealth _health; // 본체와 동일한 하트 체력
        private PlayerOrbs _orbs; // 본체와 같은 오브 공격 대행
        private PlayerAnimator _playerAnimator; // 프리팹에 본체와 같은 Animator + PlayerAnimator를 달면 자동 연결(선택)
        private ClonePool _pool;
        private readonly List<TickCommand> _clip = new List<TickCommand>();
        private int _index;
        private bool _finished;
        private bool _attackFlashing;
        private Coroutine _coAttackFlash;
        private Material _material;
        private WaitForSeconds _waitAttackFlash;

        // 리와인드 참여 상태
        private RewindManager _rewindManager;
        private RingBuffer<CloneTick> _rewindBuffer; // 클립 재생 위치 + 하트 기록
        private bool _isRewinding;
        private int _deadTicks; // 사망 후 경과 틱 - 기록 창을 벗어나면 부활 불가 -> 풀 반환

        // 한 틱의 분신 기록. 위치는 클립이 결정하므로 재생 인덱스와 체력(반칸)만 있으면 복원된다.
        private readonly struct CloneTick
        {
            public readonly int ClipIndex;
            public readonly int Halves; // 하트 반칸 단위

            public bool IsAlive => Halves > 0;

            public CloneTick(int clipIndex, int halves)
            {
                ClipIndex = clipIndex;
                Halves    = halves;
            }
        }

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _orbs = GetComponent<PlayerOrbs>();
            _playerAnimator = GetComponent<PlayerAnimator>();

            if (_renderer != null)
            {
                _material = _renderer.material;
            }
            _color           = GameDB.Time.CloneTintColor;
            _waitAttackFlash = new WaitForSeconds(GameDB.Time.CloneAttackFlashTime);

            // Hazard 등이 본체와 똑같이 GetComponent<PlayerHealth>로 피해를 줄 수 있게 한다.
            _health = GetComponent<PlayerHealth>();
            if (_health == null)
            {
                _health = gameObject.AddComponent<PlayerHealth>();
            }
            _health.OnDeath += HandleDeath;

            _col = GetComponent<Collider2D>();
            if (_col != null)
            {
                _col.isTrigger = true;
            }

            // 프리팹에 인스펙터로 설정 안 해도 동작하도록 기본값(Nothing)이면 Ground 레이어로 대체
            if (_groundLayer.value == 0)
            {
                _groundLayer = LayerMask.GetMask(Constants.Layer.GROUND);
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDeath -= HandleDeath;
            }
            _rewindManager?.Unregister(this);
        }

        private void FixedUpdate()
        {
            if (_isRewinding)
            {
                return;
            }
            if (!_finished)
            {
                PlayStep();
            }
            ApplyVisual();
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 풀 생성 직후 1회 호출. 사망 시 반환할 소속 풀을 기억해 둔다. </summary>
        public void Setup(ClonePool pool)
        {
            _pool = pool;
        }

        /// <summary> 기록 버퍼의 내용을 자기 클립 리스트로 복사해 재생을 시작한다. </summary>
        public void Init(RingBuffer<TickCommand> recorded)
        {
            recorded.CopyOrderedTo(_clip);
            _index          = 0;
            _attackFlashing = false;
            _finished       = (_clip.Count == 0);

            _health.ResetHearts();
            UtilCoroutine.CheckStopCoroutine(ref _coAttackFlash, this);

            if (!_finished)
            {
                _rb.position = _clip[0].Move.Position;
            }

            // 소환되는 순간부터 타임라인 참여자로 등록
            if (_rewindBuffer == null)
            {
                _rewindBuffer = new RingBuffer<CloneTick>(RewindManager.TickCapacity);
            }
            _rewindBuffer.Clear();
            _deadTicks   = 0;
            _isRewinding = false;

            if (_playerAnimator != null)
            {
                _playerAnimator.SetScrubbing(false); // 되감기 중 회수된 개체 재사용 시 speed=0 잔류 방지
            }

            _rewindManager = RewindManager.Instance;
            _rewindManager?.Register(this);
        }

        /// <summary> 풀로 돌아가는 순간 ClonePool이 호출 </summary>
        public void OnReturnedToPool()
        {
            _rewindManager?.Unregister(this);
            _rewindBuffer?.Clear();
            _deadTicks   = 0;
            _isRewinding = false;
        }

        /// <summary> 피격 시 하트 amount개 차감 </summary>
        public void TakeDamage(int amount = 1)
        {
            _health.TakeDamage(amount);
        }

        private void HandleDeath()
        {
            gameObject.SetActive(false);
        }

        public void RecordTick()
        {
            _rewindBuffer.Push(new CloneTick(_index, _health.CurrentHalves));

            // 사망 시점이 기록 창을 완전히 벗어나면 더는 부활할 수 없다 -> 그때 풀로 반환.
            if (!gameObject.activeSelf && (++_deadTicks >= RewindManager.TickCapacity))
            {
                ReturnToPool();
            }
        }

        public void OnRewindStart()
        {
            _isRewinding = true;

            if (_playerAnimator != null)
            {
                _playerAnimator.SetScrubbing(true); // 되감기 동안 분신도 프레임을 직접 스크럽한다
            }
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out CloneTick tick))
            {
                ApplyTick(tick);
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out CloneTick tick))
            {
                ApplyTick(tick);
            }

            if (_playerAnimator != null)
            {
                _playerAnimator.SetScrubbing(false); // 풀 반환 전에 반드시 복구 - 재사용 개체가 얼지 않게
            }

            _isRewinding = false;
            _rewindBuffer.Clear();

            // 클립 끝(공중일 수 있음)에서 되감기가 끝났으면 다시 바닥으로 스냅
            if (_finished)
            {
                SnapToGroundIfAirborne();
            }

            // 기록이 비워졌으니 이 분신은 더는 부활할 수 없다
            if (!gameObject.activeSelf)
            {
                ReturnToPool();
            }
        }

        private void ApplyTick(CloneTick tick)
        {
            if (tick.IsAlive && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                _deadTicks      = 0;
                _attackFlashing = false;
            }

            // 이 틱에도 죽어 있었으면 죽은 상태 유지
            if (!gameObject.activeSelf)
            {
                return;
            }

            _index    = Mathf.Min(tick.ClipIndex, _clip.Count);
            _finished = (_index >= _clip.Count);
            _health.RestoreHalves(tick.Halves);

            int poseIndex = Mathf.Clamp(_index - 1, 0, _clip.Count - 1);
            if (_clip.Count > 0)
            {
                _rb.position = _clip[poseIndex].Move.Position;

                if (_playerAnimator != null)
                {
                    _playerAnimator.ApplyAnimState(_clip[poseIndex].Anim); // 클립의 스냅샷 재사용 - 분신용 추가 기록 불필요
                }
            }
        }

        private void ReturnToPool()
        {
            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                OnReturnedToPool();
                Destroy(gameObject);
            }
        }

        private void PlayStep()
        {
            TickCommand tick = _clip[_index];
            tick.Move.Apply(this);
            if (tick.HasAttack)
            {
                tick.Attack.Execute(this);
            }
            if (tick.HasInteract)
            {
                tick.Interact.Execute(gameObject); // 되감기 전 상호작용을 그대로 재연 (레버 등)
            }

            ++_index;
            if (_index >= _clip.Count)
            {
                _finished = true;
                SnapToGroundIfAirborne();
            }
        }

        // 클립이 공중에서 끝난 경우(예: 기록 중 점프 도중 되감기 발동) 바닥에 붙여 영구히 뜬 채로 남지 않게 한다.
        private void SnapToGroundIfAirborne()
        {
            if (_col == null)
            {
                return;
            }

            float shortDist = _col.bounds.extents.y + Constants.Player.GROUND_CHECK_EXTRA;
            if (Physics2D.Raycast(_col.bounds.center, Vector2.down, shortDist, _groundLayer))
            {
                return; // 이미 접지 상태
            }

            RaycastHit2D hit = Physics2D.Raycast(_col.bounds.center, Vector2.down, Mathf.Infinity, _groundLayer);
            if (hit.collider == null)
            {
                return; // 아래에 바닥이 없으면(구덩이 등) 그대로 둔다
            }

            float delta = hit.point.y - _col.bounds.min.y;
            _rb.position += new Vector2(0f, delta);
        }

        public void SetPose(Vector2 position, Vector2 velocity, bool grounded)
        {
            _rb.position = position;

            if (_playerAnimator != null)
            {
                _playerAnimator.SetLocomotion(Mathf.Abs(velocity.x), grounded);
            }
        }

        // charged면 본체와 같은 배율로 재현 (기록된 AttackCommand가 전달)
        public void PlayAttack(bool reversed, bool charged)
        {
            UtilCoroutine.CheckRunCoroutine(ref _coAttackFlash, StartCoroutine(CoAttackFlash()), this);

            if (_playerAnimator != null)
            {
                _playerAnimator.TriggerAttack();
            }

            float damage = GameDB.Player.AttackDamage;
            if (charged)
            {
                damage *= GameDB.Player.ChargeDamageMult;
            }

            if ((_orbs == null) || !_orbs.TryAttackNearest(damage))
            {
                AttackHitbox.Spawn(_rb.position, damage, DamageSource.PlayerClone, _health);
            }
        }

        // 공격 순간 잠깐 흰색으로 번쩍이는 연출 타이머.
        private IEnumerator CoAttackFlash()
        {
            _attackFlashing = true;
            yield return _waitAttackFlash;
            _attackFlashing = false;
            _coAttackFlash  = null;
        }

        private void ApplyVisual()
        {
            if (_material == null)
            {
                return;
            }
            _material.color = (_attackFlashing) ? Color.white : _color;
        }
    }
}
