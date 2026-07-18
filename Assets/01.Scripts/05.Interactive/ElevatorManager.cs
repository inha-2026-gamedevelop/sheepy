// System
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Utility;

namespace Minsung.Interactive
{
    // 씬에 배치된 엘리베이터를 ElevatorId로 등록하고 조회하는 영속 싱글톤
    [DefaultExecutionOrder(-100)]
    public class ElevatorManager : PersistentSingleton<ElevatorManager>
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string MANAGER_OBJECT_NAME = "ElevatorManager"; // 자동 생성 매니저 오브젝트 이름

        private readonly Dictionary<int, ElevatorController> _controllersById = new Dictionary<int, ElevatorController>();

        /****************************************
        *              Unity Event
        ****************************************/

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject(MANAGER_OBJECT_NAME).AddComponent<ElevatorManager>();
            }
        }

        protected override void OnSingletonAwake()
        {
            _controllersById.Clear();
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 씬의 엘리베이터를 ID로 등록한다. 같은 ID가 이미 있으면 false를 반환한다 </summary>
        public bool Register(ElevatorController controller)
        {
            if (controller == null)
            {
                return false;
            }

            int elevatorId = controller.ElevatorId;
            if (_controllersById.TryGetValue(elevatorId, out ElevatorController registered) && (registered != controller))
            {
                Debug.LogError($"[{nameof(ElevatorManager)}] ElevatorId {elevatorId}가 중복되었습니다.", controller);
                return false;
            }

            _controllersById[elevatorId] = controller;
            return true;
        }

        /// <summary> 파괴되거나 비활성화되는 엘리베이터의 등록을 해제한다 </summary>
        public void Unregister(ElevatorController controller)
        {
            if ((controller == null) || !_controllersById.TryGetValue(controller.ElevatorId, out ElevatorController registered))
            {
                return;
            }

            if (registered == controller)
            {
                _controllersById.Remove(controller.ElevatorId);
            }
        }

        /// <summary> ElevatorId로 현재 씬의 엘리베이터를 찾는다 </summary>
        public bool TryGetController(int elevatorId, out ElevatorController controller)
        {
            if (_controllersById.TryGetValue(elevatorId, out controller) && (controller != null))
            {
                return true;
            }

            _controllersById.Remove(elevatorId);
            controller = null;
            return false;
        }
    }
}
