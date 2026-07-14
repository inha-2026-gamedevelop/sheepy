// System
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Achievement;
using Minsung.Common;
using Minsung.Common.Data;

namespace Minsung.TimeSystem
{
    // 분신 오브젝트 풀. 최대 N개까지만 생성한다(N은 TimeDB에서 조절)
    public class ClonePool : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private CloneController _prefab;

        private int _maxClones; // TimeDB(GameDB.Time) - Awake에서 로드

        private readonly Stack<CloneController> _free   = new Stack<CloneController>();
        private readonly List<CloneController>  _active = new List<CloneController>();

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _maxClones = GameDB.Time.MaxCloneCount;
            for (int i = 0; i < _maxClones; ++i)
            {
                CloneController clone = Instantiate(_prefab, transform);
                clone.Setup(this);
                clone.gameObject.SetActive(false);
                _free.Push(clone);
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 분신을 더 소환할 수 있는지 </summary>
        public bool CanSpawn()
        {
            return _free.Count > 0;
        }

        /// <summary> 기록 버퍼의 궤적으로 분신을 하나 소환. 최대치면 무시. </summary>
        public void Spawn(RingBuffer<TickCommand> recorded)
        {
            if (_free.Count == 0)
            {
                return; // 최대치 도달 -> 추가 생성 X
            }

            CloneController clone = _free.Pop();
            _active.Add(clone);
            clone.gameObject.SetActive(true);
            clone.Init(recorded);

            if (_active.Count >= _maxClones)
            {
                AchievementManager.Instance?.Unlock(AchievementIds.CLONE_FULL_SQUAD);
            }
        }

        /// <summary> 분신 하나를 풀로 반환 </summary>
        public void Release(CloneController clone)
        {
            if (_active.Remove(clone))
            {
                clone.OnReturnedToPool(); // 타임라인 참여 해제 + 기록 정리
                clone.gameObject.SetActive(false);
                _free.Push(clone);
            }
        }

        /// <summary> 활성 분신 전부 회수 </summary>
        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; --i)
            {
                Release(_active[i]);
            }
        }
    }
}
