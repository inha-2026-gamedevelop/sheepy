// Unity
using UnityEngine;

namespace Minsung.Utility
{
    // 씬 안에서만 유지되는 싱글톤 공통 베이스
    public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
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
            OnSingletonAwake();
        }

        protected virtual void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            OnSingletonDestroy();
            Instance = null;
        }

        /****************************************
        *                Methods
        ****************************************/

        // 파생 클래스의 SubsystemRegistration 훅에서 static 인스턴스를 초기화한다
        protected static void ResetStatic()
        {
            Instance = null;
        }

        // 파생 클래스의 AfterSceneLoad 훅에서 씬 미배치 인스턴스를 자동 생성한다
        protected static void EnsureCreated(string objectName)
        {
            if (Instance == null)
            {
                new GameObject(objectName).AddComponent<T>();
            }
        }

        // 최초 인스턴스로 확정된 뒤 1회 호출되는 초기화 훅
        protected virtual void OnSingletonAwake() { }

        // 최초 인스턴스가 파괴되기 직전에 호출되는 정리 훅
        protected virtual void OnSingletonDestroy() { }
    }
}
