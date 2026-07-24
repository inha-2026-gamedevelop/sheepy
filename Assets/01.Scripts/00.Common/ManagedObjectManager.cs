// System
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Utility;

namespace Minsung.Common
{
    // 게임플레이에서 수명과 조회가 필요한 객체의 분류. 새 유형은 여기와 담당 매니저에 함께 추가한다.
    public enum EManagedObjectType
    {
        Player,
        Monster,
        Boss,
        BossClone,
        PlayerClone,
        Interactive,
        Elevator,
        PotionPickup,
    }

    // 모든 게임플레이 객체의 런타임 고유 ID를 발급하고, 유형별로 소유/조회하는 단일 진입점.
    // 프리팹의 직렬화 ID를 복사하지 않아, 같은 프리팹을 여러 번 생성해도 ID가 중복되지 않는다.
    [DefaultExecutionOrder(-200)]
    public class ManagedObjectManager : PersistentSingleton<ManagedObjectManager>
    {
        private const string MANAGER_OBJECT_NAME = "ManagedObjectManager";

        private readonly Dictionary<string, Component> _objectsById = new Dictionary<string, Component>();
        private readonly Dictionary<Component, string> _idsByObject = new Dictionary<Component, string>();
        private readonly Dictionary<EManagedObjectType, HashSet<Component>> _objectsByType =
            new Dictionary<EManagedObjectType, HashSet<Component>>();

        private ulong _nextSequence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject(MANAGER_OBJECT_NAME).AddComponent<ManagedObjectManager>();
            }
        }

        protected override void OnSingletonAwake()
        {
            _objectsById.Clear();
            _idsByObject.Clear();
            _objectsByType.Clear();
            _nextSequence = 0;
        }

        public static string Register(EManagedObjectType type, Component target)
        {
            EnsureInstance();
            return (Instance != null) ? Instance.RegisterInternal(type, target) : string.Empty;
        }

        public static void Unregister(Component target)
        {
            Instance?.UnregisterInternal(target);
        }

        public static bool TryGet<T>(string objectId, out T target) where T : Component
        {
            target = null;
            if ((Instance == null) || !Instance._objectsById.TryGetValue(objectId, out Component registered) ||
                (registered == null))
            {
                return false;
            }

            target = registered as T;
            return target != null;
        }

        public static IReadOnlyCollection<Component> GetObjects(EManagedObjectType type)
        {
            if ((Instance != null) && Instance._objectsByType.TryGetValue(type, out HashSet<Component> objects))
            {
                return objects;
            }

            return System.Array.Empty<Component>();
        }

        private string RegisterInternal(EManagedObjectType type, Component target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            if (_idsByObject.TryGetValue(target, out string existingId))
            {
                return existingId;
            }

            string sceneName = target.gameObject.scene.IsValid() ? target.gameObject.scene.name : "Runtime";
            string objectId = $"{type}:{sceneName}:{++_nextSequence}";

            _objectsById.Add(objectId, target);
            _idsByObject.Add(target, objectId);

            if (!_objectsByType.TryGetValue(type, out HashSet<Component> objects))
            {
                objects = new HashSet<Component>();
                _objectsByType.Add(type, objects);
            }
            objects.Add(target);
            return objectId;
        }

        private void UnregisterInternal(Component target)
        {
            if ((target == null) || !_idsByObject.TryGetValue(target, out string objectId))
            {
                return;
            }

            _idsByObject.Remove(target);
            _objectsById.Remove(objectId);

            foreach (HashSet<Component> objects in _objectsByType.Values)
            {
                objects.Remove(target);
            }
        }
    }
}
