// System
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace Minsung.Interactive
{
    // Collider2D -> IInteractable 정적 레지스트리.
    public static class InteractableRegistry
    {
        /****************************************
        *                Fields
        ****************************************/

        // Collider를 키로 사용하여 빠른 조회
        private static readonly Dictionary<Collider2D, IInteractable> _registry = new Dictionary<Collider2D, IInteractable>();

        private static readonly HashSet<IInteractable> _allInteractables = new HashSet<IInteractable>();

        /// <summary> 등록된 개수 </summary>
        public static int Count => _registry.Count;

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> Interactable 등록 (BaseInteractive.OnEnable에서 자동 호출). </summary>
        public static void Register(Collider2D col, IInteractable interactable)
        {
            if ((col == null) || (interactable == null))
            {
                return;
            }

            _registry[col] = interactable;
            _allInteractables.Add(interactable);
        }

        /// <summary> Interactable 등록 해제 (BaseInteractive.OnDisable에서 자동 호출). </summary>
        public static void Unregister(Collider2D col)
        {
            if (col == null)
            {
                return;
            }

            if (_registry.TryGetValue(col, out IInteractable interactable))
            {
                _allInteractables.Remove(interactable);
                _registry.Remove(col);
            }
        }

        /// <summary> Collider로 Interactable 조회 (O(1)). 미등록이면 null. </summary>
        public static IInteractable Get(Collider2D col)
        {
            if (col == null)
            {
                return null;
            }

            _registry.TryGetValue(col, out IInteractable result);
            return result;
        }

        /// <summary> Interactable 존재 여부 확인. </summary>
        public static bool Contains(Collider2D col)
        {
            return ((col != null) && _registry.ContainsKey(col));
        }

        /// <summary> 모든 등록된 Interactable 목록 반환. </summary>
        public static IEnumerable<IInteractable> GetAll()
        {
            return _allInteractables;
        }

        /// <summary> 씬 전환 시 정리 (파괴된 Collider 잔존 참조 제거용, 필요할 때 호출). </summary>
        public static void Clear()
        {
            _registry.Clear();
            _allInteractables.Clear();
            #if UNITY_EDITOR
            Debug.Log("[Registry] Clear");
            #endif
        }
    }
}
