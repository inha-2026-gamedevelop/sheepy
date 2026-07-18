// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.Interactive
{
    // 상호작용 오브젝트 공통 베이스. 활성/비활성 시점에 등록/해제를 자동 처리한다.
    public abstract class BaseInteractive : MonoBehaviour, IInteractable
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private Collider2D _col; // 감지용 콜라이더 (비우면 같은 오브젝트에서 자동 취득)
        private string _objectId;

        /****************************************
        *              Unity Event
        ****************************************/

        protected virtual void Awake()
        {
            _objectId = ManagedObjectManager.Register(EManagedObjectType.Interactive, this);
            if (_col == null)
            {
                TryGetComponent(out _col);
            }
        }

        protected virtual void OnEnable()
        {
            InteractableRegistry.Register(_col, this);
        }

        protected virtual void OnDisable()
        {
            InteractableRegistry.Unregister(_col);
        }

        protected virtual void OnDestroy()
        {
            ManagedObjectManager.Unregister(this);
        }

        /****************************************
        *                Methods
        ****************************************/

        public Transform GetTransform() { return transform; }
        public string ObjectId => _objectId;

        public abstract void OnFocus();
        public abstract void OnInteract(GameObject interactor);
        public abstract void OnUnfocus();
    }
}
