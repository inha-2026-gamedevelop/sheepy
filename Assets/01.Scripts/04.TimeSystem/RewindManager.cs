// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;

namespace Minsung.TimeSystem
{
    // 전역 리와인드 코디네이터이자 타임라인 오너. 씬에 하나만 존재(없으면 자동 생성).
    [DefaultExecutionOrder(-100)] // 씬 배치 시에도 다른 Awake보다 먼저 Instance가 준비되게
    public class RewindManager : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        public static RewindManager Instance { get; private set; }

        // 모든 리와인드 버퍼가 공유하는 틱 용량 - 서로 다르면 되감기 인덱스가 어긋난다.
        public static int TickCapacity => Mathf.CeilToInt(GameDB.Time.RecordSeconds / Time.fixedDeltaTime);

        private float _rewindDuration; // 역재생 연출 길이 - TimeDB(GameDB.Time)에서 Awake 때 로드

        private readonly List<IRewindable> _rewindables = new List<IRewindable>();

        private int  _capacity;      // 기록 가능한 최대 틱 수
        private int  _recordedTicks; // 마지막 리와인드 이후 기록된 틱 수
        private bool _isRewinding;
        private bool _rewindEnabled = true; // false면 발동 잠금 (기록은 계속)
        private int  _rewindIndex;
        private int  _rewindStep;

        public bool IsRewinding => _isRewinding;

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 초기화되도록.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        // 씬에 배치하지 않아도 동작하도록 씬 로드 후 자동 생성.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject("RewindManager").AddComponent<RewindManager>();
            }
        }

        private void Awake()
        {
            if ((Instance != null) && (Instance != this))
            {
                Destroy(gameObject);
                return;
            }
            Instance  = this;
            _capacity = TickCapacity;
            _rewindDuration = GameDB.Time.RewindDuration;
        }

        private void Start()
        {
            StartCoroutine(CoTickLoop());
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 리와인드 참여자 등록. </summary>
        public void Register(IRewindable rewindable)
        {
            if (!_rewindables.Contains(rewindable))
            {
                _rewindables.Add(rewindable);
            }
        }

        /// <summary> 리와인드 참여자 해제. 파괴되는 오브젝트는 OnDestroy에서 반드시 호출해야 한다. </summary>
        public void Unregister(IRewindable rewindable)
        {
            _rewindables.Remove(rewindable);
        }

        /// <summary>
        /// 리와인드 발동 잠금/해제. 보스 페이즈 종료 기믹 중이거나
        /// 4페이즈(타임리와인드 삭제)에서 잠근다. 기록은 계속된다.
        /// </summary>
        public void SetRewindEnabled(bool enabled)
        {
            _rewindEnabled = enabled;
        }

        /// <summary> 되감기 시작. 잠겨 있거나 이미 되감는 중이거나 기록이 없으면 무시. </summary>
        public void StartRewind()
        {
            if (!_rewindEnabled || _isRewinding || (_recordedTicks == 0))
            {
                return;
            }

            _rewindIndex = _recordedTicks - 1;

            int fixedUpdates = Mathf.Max(1, Mathf.RoundToInt(_rewindDuration / Time.fixedDeltaTime));
            _rewindStep  = Mathf.Max(1, Mathf.CeilToInt((float)_recordedTicks / fixedUpdates));
            _isRewinding = true;

            // 브로드캐스트 루프는 전부 역방향 순회 - 참여자가 콜백 안에서 자신을 Unregister(분신 풀 반환)
            // 하거나 새 참여자를 Register(분신 소환)해도 안전하다.
            for (int i = _rewindables.Count - 1; i >= 0; --i)
            {
                _rewindables[i].OnRewindStart();
            }
        }

        private IEnumerator CoTickLoop()
        {
            WaitForFixedUpdate waitFixed = new WaitForFixedUpdate();
            while (true)
            {
                yield return waitFixed;

                if (_isRewinding)
                {
                    StepRewind();
                }
                else
                {
                    RecordTick();
                }
            }
        }

        private void RecordTick()
        {
            for (int i = _rewindables.Count - 1; i >= 0; --i)
            {
                _rewindables[i].RecordTick();
            }

            if (_recordedTicks < _capacity)
            {
                ++_recordedTicks;
            }
        }

        // _rewindStep 틱씩 건너뛰며 0번(가장 오래된 기록)에 도달하면 종료.
        private void StepRewind()
        {
            _rewindIndex -= _rewindStep;

            if (_rewindIndex <= 0)
            {
                BroadcastRewindStep(0);
                FinishRewind();
                return;
            }

            BroadcastRewindStep(_rewindIndex);
        }

        private void BroadcastRewindStep(int orderedIndex)
        {
            for (int i = _rewindables.Count - 1; i >= 0; --i)
            {
                _rewindables[i].ApplyRewindTick(orderedIndex);
            }
        }

        private void FinishRewind()
        {
            _isRewinding   = false;
            _recordedTicks = 0; // 참여자들도 OnRewindEnd에서 각자 버퍼를 비운다

            for (int i = _rewindables.Count - 1; i >= 0; --i)
            {
                _rewindables[i].OnRewindEnd(0);
            }
        }
    }
}
