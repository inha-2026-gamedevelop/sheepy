// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.TimeSystem;
using Minsung.Boss;
using Minsung.Player;

namespace Minsung.Boss2
{
    // 손아귀 패턴
    public class Boss2GrabPattern : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("참조 (보스한테 붙여주세요))")]
        [SerializeField] private BossFloatMovement   _movement;
        [SerializeField] private Boss2AttackPatterns _patterns;
        [SerializeField] private Boss2Health         _health;
        [SerializeField] private Transform           _player;
        [SerializeField] private Boss2DataSO         _dataSo;
        [SerializeField] private Transform           _handOrigin; // 손이 뻗어나가기 시작하는 지점

        private const float ARRIVE_EPSILON = 0.05f;   // 손 회수 도착 판정 거리(유닛)
        private const float DIR_EPSILON    = 0.0001f; // 방향 정규화 0 나눗셈 방지 임계
        private const float GIZMO_RADIUS   = 0.25f;   // 손 시작점 기즈모 반경

        private BossHazardPool _pool;
        private RewindManager.RewindLockHandle? _rewindLock;

        private PlayerController _playerController;
        private PlayerHealth     _playerHealth;
        private Collider2D       _playerCol;

        private readonly List<float> _pressTimes = new List<float>(); // 탈출 연타 입력 시각 기록

        private Coroutine _loop;
        private bool _running;   // 잡기 시퀀스 진행 중
        private bool _captured;  // 플레이어를 붙잡아 고정 중 (Cleanup에서 해제 보장용)

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            if (_movement == null)
            {
                TryGetComponent(out _movement);
            }
            if (_patterns == null)
            {
                TryGetComponent(out _patterns);
            }
            if (_health == null)
            {
                _health = GetComponentInChildren<Boss2Health>();
            }
            if (_player == null)
            {
                PlayerController found = FindAnyObjectByType<PlayerController>();
                if (found != null)
                {
                    _player = found.transform;
                }
            }
        }

        private void Start()
        {
            CachePlayer();

            if ((_dataSo != null) && (_movement != null))
            {
                _loop = StartCoroutine(CoLoop());
            }
        }

        private void OnDisable()
        {
            if (_running)
            {
                Cleanup(); // 씬 언로드/비활성에서도 포박/연출/잠금이 잔류하지 않도록
                _running = false;
            }
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }

        /****************************************
        *                Methods
        ****************************************/

        private void CachePlayer()
        {
            if (_player == null)
            {
                return;
            }
            _player.TryGetComponent(out _playerController);
            _player.TryGetComponent(out _playerHealth);
            _player.TryGetComponent(out _playerCol);
        }

        // 쿨타임마다 조건을 만족하면 잡기를 시전한다
        private IEnumerator CoLoop()
        {
            WaitForSeconds waitCooldown = new WaitForSeconds(_dataSo.GrabCooldown);

            while (true)
            {
                yield return waitCooldown;

                if (_running || (_player == null))
                {
                    continue;
                }
                // 4페이즈 인지 체크
                if ((_health != null) && !_health.IsFinalPhase)
                {
                    continue;
                }
                if ((RewindManager.Instance != null) && RewindManager.Instance.IsRewinding)
                {
                    continue;
                }
                if (Random.value > _dataSo.GrabChance)
                {
                    continue;
                }

                Vector2 origin = HandOriginPos();
                if (Vector2.Distance(origin, _player.position) > _dataSo.GrabRange)
                {
                    continue; // 사거리 밖
                }

                yield return CoGrab();
            }
        }

        private IEnumerator CoGrab()
        {
            // 다른 독점 패턴(공간찢기 등)이 이동을 잡고 있으면 시도 자체를 건너뛴다
            if ((_movement == null) || !_movement.TryBeginScriptedMovement())
            {
                yield break;
            }

            _running = true;
            _patterns?.SuspendNormalPatterns();
            _rewindLock = RewindManager.Instance?.AcquireRewindLock(this);
            EnsurePool();

            Vector2 start = HandOriginPos();
            Vector2 dir   = AimDir(start); // 예고 시작 시점 방향 스냅샷

            // 예고 - 손->플레이어 방향 리치 박스 점멸
            yield return CoTelegraph(start, dir);

            // 손 발사 - start에서 dir 방향으로 GrabReach까지 뻗으며 매 프레임 잡기 판정
            bool caught = false;
            int hand = _pool.Alloc(start, _dataSo.GrabHitboxSize, _dataSo.GrabHandColor, false);
            float traveled = 0f;
            Vector2 handPos = start;

            while (traveled < _dataSo.GrabReach)
            {
                float step = _dataSo.GrabHandSpeed * Time.deltaTime;
                traveled += step;
                handPos = start + (dir * Mathf.Min(traveled, _dataSo.GrabReach));
                _pool.SetPosition(hand, handPos);

                if (Overlaps(handPos))
                {
                    caught = true;
                    break;
                }
                yield return null;
            }

            if (caught)
            {
                yield return CoCaptureStruggle(hand, handPos);
            }
            else
            {
                // 헛나감 - 손을 시작점으로 회수하고 종료(회피 성공)
                yield return CoRetractHand(hand, handPos, HandOriginPos(), null);
            }

            Cleanup();
            _running = false;
        }

        // 붙잡힘 - 손을 보스로 회수하며 플레이어 고정 -> 포박 중 주기 피해 + 연타 탈출 판정 -> 탈출 실패 시 투척
        private IEnumerator CoCaptureStruggle(int hand, Vector2 handPos)
        {
            _captured = true;
            _playerController?.SetInteracting(true); // 입력 잠금 + 물리 Kinematic

            // 손을 보스 손 위치로 회수하며 플레이어를 손에 붙여 끌고 온다
            yield return CoRetractHand(hand, handPos, HandOriginPos(), PinPlayerToHand);

            _pressTimes.Clear();
            int   ticksDone = 0;
            float elapsed   = 0f;
            bool  escaped   = false;

            // 포박 유지: GrabDamageTickInterval마다 하트 한 칸씩 GrabDamageTickCount번 피해.
            // 그 사이 집계 창(GrabStruggleWindow) 안에 상호작용 키를 필요 횟수 이상 연타하면 탈출.
            while (ticksDone < _dataSo.GrabDamageTickCount)
            {
                PinPlayerToHand(HandOriginPos());

                if (Input.GetKeyDown(Constants.Player.KEY_INTERACT))
                {
                    _pressTimes.Add(Time.time);
                }
                PurgeOldPresses();
                if (_pressTimes.Count >= _dataSo.GrabStruggleRequiredPresses)
                {
                    escaped = true;
                    break;
                }

                // 포박 중 피해로 사망하면 포박 종료(투척 없음)
                if ((_playerHealth != null) && (_playerHealth.CurrentHalves <= 0))
                {
                    break;
                }

                elapsed += Time.deltaTime;
                if (elapsed >= ((ticksDone + 1) * _dataSo.GrabDamageTickInterval))
                {
                    _playerHealth?.TakeDamageHalves(_dataSo.GrabDamageHalves);
                    ++ticksDone;
                }

                yield return null;
            }

            _captured = false;
            _playerController?.SetInteracting(false);

            // 탈출하면 그대로 풀려나 떨어지고, 탈출 실패(포박 지속시간 소진)면 위로 던져진다(연출용, 데미지 없음)
            if ((!escaped) && ((_playerHealth == null) || (_playerHealth.CurrentHalves > 0)))
            {
                _playerController?.Launch(new Vector2(0f, _dataSo.GrabThrowForce));
            }
        }

        // 집계 창(GrabStruggleWindow)보다 오래된 입력 기록을 버린다 - 최근 창 안의 연타만 센다
        private void PurgeOldPresses()
        {
            float cutoff = Time.time - _dataSo.GrabStruggleWindow;
            int remove = 0;
            while ((remove < _pressTimes.Count) && (_pressTimes[remove] < cutoff))
            {
                ++remove;
            }
            if (remove > 0)
            {
                _pressTimes.RemoveRange(0, remove);
            }
        }

        // 손을 from에서 to로 회수 - onEachFrame이 있으면 매 프레임 손 위치를 넘겨 플레이어 핀 고정에 쓴다
        private IEnumerator CoRetractHand(int hand, Vector2 from, Vector2 to, System.Action<Vector2> onEachFrame)
        {
            Vector2 cur = from;
            while (Vector2.Distance(cur, to) > ARRIVE_EPSILON)
            {
                cur = Vector2.MoveTowards(cur, to, _dataSo.GrabRetractSpeed * Time.deltaTime);
                _pool.SetPosition(hand, cur);
                onEachFrame?.Invoke(cur);
                yield return null;
            }
            _pool.SetPosition(hand, to);
            onEachFrame?.Invoke(to);
        }

        // 손->플레이어 방향 리치 박스를 점멸 표시(판정 없음) - 사각형 공식은 Boss2LaserPattern/공간찢기 예고와 동일
        private IEnumerator CoTelegraph(Vector2 start, Vector2 dir)
        {
            float angle  = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector2 center = start + (dir * (_dataSo.GrabReach * 0.5f));
            Vector2 scale  = new Vector2(_dataSo.GrabReach, _dataSo.GrabTelegraphThickness);

            int slot = _pool.Alloc(center, scale, _dataSo.GrabTelegraphColor, false, rotationDeg: angle);

            bool  visible = true;
            float elapsed = 0f;
            float blink   = Mathf.Max(0.02f, _dataSo.GrabTelegraphBlink);
            while (elapsed < _dataSo.GrabTelegraphTime)
            {
                yield return new WaitForSeconds(blink);
                elapsed += blink;
                visible = !visible;
                _pool.SetVisible(slot, visible);
            }
            _pool.Free(slot);
        }

        private void PinPlayerToHand(Vector2 pos)
        {
            _playerController?.SetPose(pos, Vector2.zero, false);
        }

        // 손 위치 박스가 플레이어 콜라이더와 겹치는지
        private bool Overlaps(Vector2 handPos)
        {
            if (_playerCol == null)
            {
                return false;
            }
            Bounds handBounds = new Bounds(handPos, _dataSo.GrabHitboxSize);
            return handBounds.Intersects(_playerCol.bounds);
        }

        private Vector2 AimDir(Vector2 from)
        {
            if (_player == null)
            {
                return Vector2.right;
            }
            Vector2 d = (Vector2)_player.position - from;
            return (d.sqrMagnitude > DIR_EPSILON) ? d.normalized : Vector2.right;
        }

        private Vector2 HandOriginPos()
        {
            if (_handOrigin != null)
            {
                return _handOrigin.position;
            }
            return (_movement != null) ? (Vector2)_movement.transform.position : (Vector2)transform.position;
        }

        private void EnsurePool()
        {
            if (_pool != null)
            {
                return;
            }
            _pool = new BossHazardPool(2, "Boss2Grab");
        }

        private void Cleanup()
        {
            _pool?.FreeAll();
            _movement?.EndScriptedMovement();
            _patterns?.ResumeNormalPatterns();

            if (_captured)
            {
                // 포박 중 중단됐으면 반드시 풀어준다
                _playerController?.SetInteracting(false);
                _captured = false;
            }

            if (_rewindLock.HasValue)
            {
                _rewindLock.Value.Dispose();
                _rewindLock = null;
            }
        }

    #if UNITY_EDITOR
        // 손 시작점/사거리 배치 확인용
        private void OnDrawGizmosSelected()
        {
            Vector2 origin = HandOriginPos();
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(origin, GIZMO_RADIUS);
            if (_dataSo != null)
            {
                Gizmos.DrawWireSphere(origin, _dataSo.GrabRange);
            }
        }
    #endif
    }
}
