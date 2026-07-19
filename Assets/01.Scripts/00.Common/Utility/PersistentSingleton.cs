// Unity
using UnityEngine;

namespace Minsung.Utility
{
    // DontDestroyOnLoad 싱글톤 공통 베이스.
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
    {
        /****************************************
        *                Fields
        ****************************************/

        public static T Instance { get; protected set; }

        /****************************************
        *              Unity Event
        ****************************************/

        protected virtual void Awake()
        {
            if ((Instance != null) && (Instance != this))
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;

            // DontDestroyOnLoad는 루트 오브젝트에서만 동작 - 씬에 자식으로 배치된 경우를 대비해 루트로 승격
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            OnSingletonAwake();
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        // 파생 클래스의 SubsystemRegistration 훅에서 static 인스턴스를 초기화한다
        protected static void ResetStatic()
        {
            Instance = null;
        }

        // 파생 클래스의 AfterSceneLoad 훅에서 미배치 영속 인스턴스를 자동 생성한다
        protected static void EnsureCreated(string objectName)
        {
            if (Instance == null)
            {
                new GameObject(objectName).AddComponent<T>();
            }
        }

        // 최초 인스턴스로 확정된 뒤 1회 호출되는 초기화 훅.
        protected virtual void OnSingletonAwake() { }
    }
}
