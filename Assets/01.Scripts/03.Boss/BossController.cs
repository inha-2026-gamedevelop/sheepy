// System
using System;
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

using Minsung.Achievement;
using Minsung.CameraSystem;
using Minsung.Common;
using Minsung.Common.Data;
using Minsung.Player;
using Minsung.TimeSystem;
using Minsung.UI;

namespace Minsung.Boss
{
    // Azathoth 보스. 총 피통 64,000을 4페이즈가 16,000씩 나눠 갖는 단일 피통 방식으로 동작한다
    public class BossController : MonoBehaviour, IRewindable, IDamageable
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 한 틱의 보스 기록. 페이즈/기믹 진행은 되돌리지 않고 피통/감정만 되돌린다
        private struct BossFrame
        {
            public float Health;
            public BossEmotion Emotion;
            public int EmotionCursor; // 자동 전환 결정 로그 커서 - 되감기 후 같은 순서로 이어가기 위함
        }

        /****************************************
        *                Fields
        ****************************************/

        [Header("참조")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private Animator _animator;                  // 페이즈 전환/사망 연출 트리거 (Roar/Death). 미연결 시 무시
        [SerializeField] private BossCloneController[] _phase1Clones; // 1페이즈 분신 2체 (씬 배치, 비활성 시작)
        [SerializeField] private BossBodyController _body;            // 보스 본체 (2페이즈~ 등장, 씬 배치, 비활성 시작)
        [SerializeField] private HeartPickup _heartPickup;            // 파랑 감정 하트 픽업 (씬 배치, 비활성 시작)

        [Header("패턴 배너 (특정 패턴 시 화면 중앙 경고 문구)")]
        [SerializeField] private BossBannerUI _patternBanner;        // 미연결 시 배너 무시
        [SerializeField] private float _bannerDuration = 2.5f;       // 문구 유지 시간(초, 페이드 제외)
        [SerializeField, TextArea] private string _phase1GimmickMessage = "경고: 전장에 즉사 패턴이 전개됩니다.";

        [Header("아레나 경계 (낙뢰/레이저/안전구역 배치 기준)")]
        [SerializeField] private float _arenaMinX = -10f;
        [SerializeField] private float _arenaMaxX = 10f;
        [SerializeField] private float _arenaGroundY = 0f;

        private BossState[] _states;       // 페이즈별 상태 객체 (인덱스 = 페이즈)
        private int _phaseIndex;           // 현재 페이즈 인덱스 (0부터)
        private float _currentHealth;      // 총 피통(64,000)에서 시작하는 단일 값
        private bool _healthFrozen;        // 페이즈 하한 도달 ~ 기믹 완료까지 피해 무시
        private bool _transitioning;       // 페이즈 종료 기믹/전환 진행 중
        private bool _isGlobalRewinding;   // 전역 되감기 중 여부 (패턴 Tick 정지용)
        private BossEmotion _emotion = BossEmotion.None;

        private float _battleElapsed;      // 보스전 경과(초, 실시간)
        private bool _battleStarted;
        private bool _timeOverKilled;      // 제한시간 즉사 1회 처리 플래그

        private PlayerHealth _playerHealth;          // 즉사/반사 대상 (본체)
        private readonly List<IBossPattern> _patterns = new List<IBossPattern>(); // 전 페이즈 공통 패턴 (낙뢰 등)
        private Coroutine _confusionRoutine;         // 화남 감정 혼란(키반전) 루프
        private WaitForSeconds _waitConfusionInterval;
        private WaitForSeconds _waitConfusionDuration;
        private RingBuffer<BossFrame> _rewindBuffer; // 피통/감정 리와인드 기록

        private Coroutine _emotionLoop;              // 2페이즈부터 도는 자동 감정 전환 루프
        private bool _autoEmotionSuspended;           // 3페이즈 화남 고정 등 - true면 주기가 와도 전환하지 않는다
        private WaitForSeconds _waitEmotionInterval;
        private readonly List<BossEmotion> _emotionLog = new List<BossEmotion>(); // 결정 로그 - 되감기 후 동일 순서 재현
        private int _emotionCursor;

        public PlayerController Player => _player;              // 페이즈 패턴이 플레이어를 조준할 때 사용
        public BossCloneController[] Phase1Clones => _phase1Clones;
        public BossBodyController Body => _body;                // 2페이즈부터 페이즈 상태가 활성/비활성 관리
        public int PhaseIndex => _phaseIndex;
        public bool IsTransitioning => _transitioning;
        public BossEmotion CurrentEmotion => _emotion;
        public float CurrentHealth => _currentHealth;
        public float BattleElapsed => _battleElapsed;           // 보스전 UI 타이머용

        public float ArenaMinX => _arenaMinX;
        public float ArenaMaxX => _arenaMaxX;
        public float ArenaGroundY => _arenaGroundY;

        // 현재 페이즈의 피통 경계. 하한에 닿으면 동결 + 종료 기믹
        private float PhaseFloorHealth => GameDB.Boss.TotalHealth - (GameDB.Boss.PhaseHealth * (_phaseIndex + 1));
        private float PhaseCeilHealth => GameDB.Boss.TotalHealth - (GameDB.Boss.PhaseHealth * _phaseIndex);

        public event Action<int> OnPhaseChanged;             // 새 페이즈 진입 시 (int = 새 페이즈 인덱스)
        public event Action OnBossDefeated;                  // 마지막 페이즈 피통이 0이 되었을 때
        public event Action<BossEmotion> OnEmotionChanged;   // 감정 아이콘/연출 UI 연동
        public event Action<float, float> OnHealthChanged;   // (현재, 총) 보스 HP 바 연동 - Minsung.UI.BossHealthBarUI가 구독

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _states = new BossState[]
            {
                new Phase1State(this),
                new Phase2State(this),
                new Phase3State(this),
                new Phase4State(this),
            };

            _waitConfusionInterval = new WaitForSeconds(GameDB.Boss.ConfusionInterval);
            _waitConfusionDuration = new WaitForSeconds(GameDB.Boss.ConfusionDuration);
            _waitEmotionInterval   = new WaitForSeconds(GameDB.Boss.EmotionInterval);
        }

        private void Start()
        {
            _phaseIndex    = 0;
            _currentHealth = GameDB.Boss.TotalHealth;
            _rewindBuffer  = new RingBuffer<BossFrame>(RewindManager.TickCapacity);

            if (_player != null)
            {
                _player.TryGetComponent(out _playerHealth);
            }

            RegisterPattern(new BossLightningPattern(this));

            _states[_phaseIndex].Enter();
            OnHealthChanged?.Invoke(_currentHealth, GameDB.Boss.TotalHealth);

            BeginBattle(); // TODO: 보스 입장 연출 완성 후, 연출이 끝나는 시점 호출로 이동
            RewindManager.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            foreach (IBossPattern pattern in _patterns)
            {
                pattern.Dispose();
            }
            _patterns.Clear();
            RewindManager.Instance?.Unregister(this);
        }

        private void Update()
        {
            TickBattleTimer();

            if ((!_transitioning) && (!_isGlobalRewinding))
            {
                _states[_phaseIndex].Tick();
            }
        }

        private void FixedUpdate()
        {
            if ((!_transitioning) && (!_isGlobalRewinding))
            {
                _states[_phaseIndex].FixedTick();
            }
        }

        /****************************************
        *            전투 타이머
        ****************************************/

        /// <summary> 보스전 타이머 시작 + 카메라 줌아웃(아레나 전체가 보이도록). 입장 연출이 끝나는 시점에 호출한다 </summary>
        public void BeginBattle()
        {
            _battleStarted = true;
            _battleElapsed = 0f;
            CameraManager.Instance?.SetPlayerZoom(Constants.Camera.BOSS_ORTHOGRAPHIC_SIZE);
        }

        /// <summary> 공통 패턴 등록 + 즉시 가동. 정지/파괴/리와인드 훅은 BossController가 일괄 관리한다 </summary>
        public void RegisterPattern(IBossPattern pattern)
        {
            _patterns.Add(pattern);
            pattern.Play();
        }

        /// <summary> 플레이어 즉사 (제한시간 초과 등 - 그 자리 체크포인트 리스폰) </summary>
        public void KillPlayer()
        {
            if (_playerHealth != null)
            {
                _playerHealth.Kill();
            }
        }

        /// <summary> 보스전을 처음부터 다시 시작한다 (현재 씬 재로드). TODO: 특정 씬/위치 재시작은 기획 확정 후 교체 </summary>
        public void RestartBossFight()
        {
            GameManager.Instance?.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary> 화면 중앙 패턴 경고 배너 표시. 특정 패턴을 시작하는 상태/코루틴이 호출한다 (배너 미연결 시 무시) </summary>
        public void ShowBanner(string message)
        {
            _patternBanner?.Show(message, _bannerDuration);
        }

        // 리와인드/슬로우 배율과 무관하게 실시간으로 누적한다 - "되감아도 시간은 계속 지나간다"
        private void TickBattleTimer()
        {
            if ((!_battleStarted) || (_timeOverKilled))
            {
                return;
            }

            _battleElapsed += Time.unscaledDeltaTime;
            if (_battleElapsed >= GameDB.Boss.TimeLimit)
            {
                _timeOverKilled = true;
                KillPlayer();
            }
        }

        /****************************************
        *                감정
        ****************************************/

        /// <summary>
        /// 자동 감정 전환 루프 시작 (2페이즈 진입 시 CoPhaseEnd가 1회 호출). 이미 돌고 있으면 무시
        /// </summary>
        private void StartEmotionLoop()
        {
            if (_emotionLoop == null)
            {
                _emotionLoop = StartCoroutine(CoEmotionLoop());
            }
        }

        private void StopEmotionLoop()
        {
            if (_emotionLoop != null)
            {
                StopCoroutine(_emotionLoop);
                _emotionLoop = null;
            }
        }

        /// <summary> 자동 감정 전환을 일시 정지/재개한다. 3페이즈처럼 감정을 고정해야 할 때 사용 </summary>
        public void SetAutoEmotionSuspended(bool suspended)
        {
            _autoEmotionSuspended = suspended;
        }

        // EmotionInterval마다 Black/White/Navy/Pink/Blue 중 하나로 랜덤 전환. 전환/기믹 중에는 건너뛴다
        private IEnumerator CoEmotionLoop()
        {
            while (true)
            {
                yield return _waitEmotionInterval;
                if (_transitioning || _autoEmotionSuspended)
                {
                    continue;
                }
                SetEmotion(GetOrMakeAutoEmotion());
            }
        }

        // 이미 만들어진 결정이 있으면 재사용(리와인드 후 재현), 없으면 새로 만들어 로그에 추가
        private BossEmotion GetOrMakeAutoEmotion()
        {
            if (_emotionCursor < _emotionLog.Count)
            {
                BossEmotion logged = _emotionLog[_emotionCursor];
                ++_emotionCursor;
                return logged;
            }

            BossEmotion emotion = RandomAutoEmotion();
            _emotionLog.Add(emotion);
            ++_emotionCursor;
            return emotion;
        }

        // BossEmotion 순서상 Black(1)~Blue(5)만 자동 전환 후보 - None(기본)/Angry(3페이즈 전용)는 제외
        private static BossEmotion RandomAutoEmotion()
        {
            return (BossEmotion)UnityEngine.Random.Range(1, (int)BossEmotion.Angry);
        }

        /// <summary> 감정 상태 변경. 반사/낙뢰 비율/혼란(화남) 변조가 즉시 적용된다 </summary>
        public void SetEmotion(BossEmotion emotion)
        {
            if (_emotion == emotion)
            {
                return;
            }

            BossEmotion prev = _emotion;
            _emotion = emotion;

            if (prev == BossEmotion.Angry)
            {
                StopConfusion();
            }
            if (emotion == BossEmotion.Angry)
            {
                _confusionRoutine = StartCoroutine(CoConfusionLoop());
            }
            if (emotion == BossEmotion.Blue)
            {
                SpawnHeartPickup(); // 파랑: 맵에 하트 한 칸 회복 제공
            }

            OnEmotionChanged?.Invoke(emotion);
        }

        private void StopConfusion()
        {
            if (_confusionRoutine != null)
            {
                StopCoroutine(_confusionRoutine);
                _confusionRoutine = null;
            }
            if (_player != null)
            {
                if (_player.StatusEffects != null)
                {
                    _player.StatusEffects.Remove(StatusEffectType.InputInvert);
                }
                else
                {
                    _player.SetInputInverted(false);
                }
            }
        }

        // 화남: 10초마다 1초간 키반전. 상태이상 컨트롤러가 만료와 UI 이벤트를 관리한다
        private IEnumerator CoConfusionLoop()
        {
            while (true)
            {
                yield return _waitConfusionInterval;
                if (_player == null)
                {
                    continue;
                }
                if (_player.StatusEffects != null)
                {
                    _player.StatusEffects.Apply(StatusEffectType.InputInvert, GameDB.Boss.ConfusionDuration);
                }
                else
                {
                    _player.SetInputInverted(true);
                }
                yield return _waitConfusionDuration;
                if ((_player != null) && (_player.StatusEffects == null))
                {
                    _player.SetInputInverted(false);
                }
            }
        }

        // 파랑: 아레나 랜덤 지점에 하트 픽업 활성화 (씬 배치 1개 재사용)
        private void SpawnHeartPickup()
        {
            if (_heartPickup == null)
            {
                return;
            }
            float x = UnityEngine.Random.Range(_arenaMinX, _arenaMaxX);
            _heartPickup.transform.position = new Vector3(x, _arenaGroundY + GameDB.Boss.HeartPickupHeight, 0f);
            _heartPickup.gameObject.SetActive(true);
        }

        /****************************************
        *                피해
        ****************************************/

        /// <summary> 보스 본체 피격(IDamageable) - 감정 반사 시 공격자가 대신 피해를 입고 false 반환 (1페이즈 분신은 별도 피통이라 이 메서드를 거치지 않음) </summary>
        public bool TakeDamage(float dmg, DamageSource source = DamageSource.Player, PlayerHealth attacker = null)
        {
            if ((_transitioning) || (_healthFrozen))
            {
                return false;
            }

            if (ReflectIfNeeded(source, attacker))
            {
                return false;
            }

            _currentHealth = Mathf.Max(PhaseFloorHealth, _currentHealth - dmg);
            OnHealthChanged?.Invoke(_currentHealth, GameDB.Boss.TotalHealth);

            // 페이즈별 자체 트리거(예: 1페이즈 분신 전멸)를 쓰는 경우 피통 하한 도달만으로는 기믹을 시작하지 않는다
            if ((_currentHealth <= PhaseFloorHealth) && (_states[_phaseIndex].UsesHealthFloorTrigger))
            {
                TriggerPhaseEnd();
            }
            return true;
        }

        /// <summary> 감정 반사 판정 - 반사되면 공격자가 대신 피해를 입고 true 반환 (본체/분신이 규칙 공유) </summary>
        public bool ReflectIfNeeded(DamageSource source, PlayerHealth attacker)
        {
            if (!_emotion.ShouldReflect(source))
            {
                return false;
            }
            if (attacker != null)
            {
                attacker.TakeDamageHalves(GameDB.Boss.ReflectHalves);
            }
            return true;
        }

        /// <summary> 페이즈 종료 시퀀스(피통 동결 + 종료 기믹) 시작 - 피통 하한 도달 시 자동 호출되거나 페이즈가 자체 조건으로 직접 호출 </summary>
        public void TriggerPhaseEnd()
        {
            if ((_healthFrozen) || (_transitioning))
            {
                return;
            }
            _healthFrozen = true;
            StartCoroutine(CoPhaseEnd());
        }

#if UNITY_EDITOR
        /// <summary> QA 전용 - 기믹/전환 연출 없이 지정 페이즈로 즉시 이동 (BossPhaseQaDebug 전용, 빌드 미포함) </summary>
        public void QaJumpToPhase(int targetPhaseIndex)
        {
            if (_transitioning || (targetPhaseIndex == _phaseIndex)
                || (targetPhaseIndex < 0) || (targetPhaseIndex >= _states.Length))
            {
                return;
            }

            _states[_phaseIndex].Exit();

            _phaseIndex    = targetPhaseIndex;
            _healthFrozen  = false;
            _currentHealth = PhaseCeilHealth;
            OnHealthChanged?.Invoke(_currentHealth, GameDB.Boss.TotalHealth);

            SetAutoEmotionSuspended(false);
            SetEmotion(BossEmotion.None);
            if (_phaseIndex >= 1)
            {
                StartEmotionLoop();
            }
            else
            {
                StopEmotionLoop();
            }

            _states[_phaseIndex].Enter();
            OnPhaseChanged?.Invoke(_phaseIndex);
        }
#endif

        // 페이즈 종료: 피통 동결 -> 종료 기믹(즉사 레이저/컷신 등) -> 다음 페이즈
        // 기믹 중에는 리와인드를 잠가 동결된 피통/기믹 진행이 되감기와 엉키지 않게 한다
        private IEnumerator CoPhaseEnd()
        {
            _transitioning = true;
            RewindManager.Instance?.SetRewindEnabled(false);
            PlayAnimTrigger(Constants.Combat.BOSS_ANIM_ROAR); // 기믹 시전 시그널

            // 1페이즈 즉사 기믹 진입 시 중앙 경고 배너 (다른 페이즈 기믹은 각자 상태에서 ShowBanner 호출)
            if (_phaseIndex == 0)
            {
                ShowBanner(_phase1GimmickMessage);
            }

            yield return _states[_phaseIndex].CoPhaseEndGimmick();
            _states[_phaseIndex].Exit();

            RewindManager.Instance?.SetRewindEnabled(true);

            if (_phaseIndex >= _states.Length - 1)
            {
                AchievementManager.Instance?.Unlock(AchievementIds.BOSS_DEFEATED);
                PlayAnimTrigger(Constants.Combat.BOSS_ANIM_DEATH);
                CameraManager.Instance?.ResetPlayerZoom();
                StopEmotionLoop();
                OnBossDefeated?.Invoke();
                yield break;
            }

            ++_phaseIndex;
            if (_phaseIndex == 1)
            {
                AchievementManager.Instance?.Unlock(AchievementIds.BOSS_PHASE1_CLEAR);
                StartEmotionLoop(); // 2페이즈부터 자동 감정 전환 시작 (3페이즈는 SetAutoEmotionSuspended로 정지)
            }

            // 새 페이즈 상한으로 피통을 스냅한다. 1페이즈(분신 별도 피통)를 지나 2페이즈에 진입하면
            // 보스 본체 피통이 75%(= TotalHealth - PhaseHealth)부터 표시된다.
            // 2->3, 3->4 전환은 이미 하한(=새 상한)에 도달해 있어 값 변화 없음
            _currentHealth = PhaseCeilHealth;
            OnHealthChanged?.Invoke(_currentHealth, GameDB.Boss.TotalHealth);

            _states[_phaseIndex].Enter(); // 1페이즈 종료 - 2페이즈부터 Phase2State.Enter가 본체(Body)를 등장시킨다
            OnPhaseChanged?.Invoke(_phaseIndex);
            PlayAnimTrigger(Constants.Combat.BOSS_ANIM_ROAR);

            if (_phaseIndex == 2) // 3페이즈 진입
            {
                PlayAnimTrigger(Constants.Combat.BOSS_ANIM_DEATH);
            }

            _healthFrozen  = false;
            _transitioning = false;
        }

        // 트리거 파라미터 재생. Animator 미연결/비활성/컨트롤러 미할당 시 무시
        private void PlayAnimTrigger(string trigger)
        {
            if ((_animator != null) && _animator.isActiveAndEnabled && (_animator.runtimeAnimatorController != null))
            {
                _animator.SetTrigger(trigger);
            }
        }

        /****************************************
        *            IRewindable
        ****************************************/

        // 피통/감정은 항상 기록하고(동결 중엔 값이 안 변하므로 무해), 패턴 기록은 상태에 위임한다

        public void RecordTick()
        {
            _rewindBuffer.Push(new BossFrame
            {
                Health         = _currentHealth,
                Emotion        = _emotion,
                EmotionCursor  = _emotionCursor,
            });
            if (!_transitioning)
            {
                _states[_phaseIndex].RecordTick();
            }
        }

        public void OnRewindStart()
        {
            _isGlobalRewinding = true;
            StopEmotionLoop();
            foreach (IBossPattern pattern in _patterns)
            {
                pattern.OnRewindStart();
            }
            if (!_transitioning)
            {
                _states[_phaseIndex].OnRewindStart();
            }
        }

        public void ApplyRewindTick(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out BossFrame frame))
            {
                ApplyFrame(frame);
            }
            if (!_transitioning)
            {
                _states[_phaseIndex].ApplyRewindTick(orderedIndex);
            }
        }

        public void OnRewindEnd(int orderedIndex)
        {
            if (_rewindBuffer.TryGetOrdered(orderedIndex, out BossFrame frame))
            {
                ApplyFrame(frame);
                _emotionCursor = frame.EmotionCursor;
            }
            _rewindBuffer.Clear();

            if (!_transitioning)
            {
                _states[_phaseIndex].OnRewindEnd(orderedIndex);
            }
            foreach (IBossPattern pattern in _patterns)
            {
                pattern.OnRewindEnd();
            }
            _isGlobalRewinding = false;

            if (_phaseIndex >= 1)
            {
                StartEmotionLoop(); // 루프가 이미 시작된 페이즈였다면 재개 - 같은 결정 로그로 이어진다
            }
        }

        // 피통/감정을 기록 시점으로 복원. 페이즈 경계는 넘지 않는다 -
        // 전환 직후 이전 페이즈 기록이 남아 있어도 현재 페이즈 구간으로 클램프된다
        // 감정은 사이드이펙트(반사/혼란 루프/하트 픽업) 없이 표시만 갱신한다
        private void ApplyFrame(BossFrame frame)
        {
            _currentHealth = Mathf.Clamp(frame.Health, PhaseFloorHealth, PhaseCeilHealth);
            OnHealthChanged?.Invoke(_currentHealth, GameDB.Boss.TotalHealth);

            if (_emotion != frame.Emotion)
            {
                _emotion = frame.Emotion;
                OnEmotionChanged?.Invoke(_emotion);
            }
        }
    }
}
