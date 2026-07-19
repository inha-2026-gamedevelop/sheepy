// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Minsung.Utility
{
    // 씬 안에서만 유지되는 싱글톤 공통 베이스
    public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        /****************************************
        *                Fields
        ****************************************/

        public static T Instance { get; protected set; }

        // EnsureCreated로 자동 생성하는 싱글톤의 오브젝트 이름 - 씬 재로드 시 재생성에 사용 (미사용 시 null)
        private static string _autoCreateName;

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

        // 파생 클래스의 SubsystemRegistration 훅에서 static 인스턴스를 초기화한다.
        // 이 시점(첫 씬 로드 이전)에 sceneLoaded를 구독해, 자동 생성 싱글톤이 씬 재로드마다 다시 만들어지게 한다.
        // 첫 씬 로드 이전에 구독하므로, 씬 로드 시 다른 재등록 콜백(GameManager 등)보다 먼저 인스턴스를 복구한다.
        protected static void ResetStatic()
        {
            Instance        = null;
            _autoCreateName = null;
            SceneManager.sceneLoaded -= HandleSceneLoadedAutoCreate;
            SceneManager.sceneLoaded += HandleSceneLoadedAutoCreate;
        }

        // 파생 클래스의 AfterSceneLoad 훅에서 씬 미배치 인스턴스를 자동 생성한다.
        // RuntimeInitializeOnLoadMethod(AfterSceneLoad)는 첫 씬에서 1회만 실행되므로, 이후 씬 재로드 재생성은 sceneLoaded가 담당한다.
        protected static void EnsureCreated(string objectName)
        {
            _autoCreateName = objectName;
            CreateIfMissing();
        }

        // Single 씬 로드마다 자동 생성 싱글톤이 유실됐으면 다시 만든다 (씬 로컬 싱글톤은 씬 언로드 시 파괴됨)
        private static void HandleSceneLoadedAutoCreate(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                CreateIfMissing();
            }
        }

        private static void CreateIfMissing()
        {
            if ((Instance == null) && !string.IsNullOrEmpty(_autoCreateName))
            {
                new GameObject(_autoCreateName).AddComponent<T>();
            }
        }

        // 최초 인스턴스로 확정된 뒤 1회 호출되는 초기화 훅
        protected virtual void OnSingletonAwake() { }

        // 최초 인스턴스가 파괴되기 직전에 호출되는 정리 훅
        protected virtual void OnSingletonDestroy() { }
    }
}
