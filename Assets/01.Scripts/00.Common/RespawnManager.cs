// System
using System;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

using Minsung.Player;
using Minsung.Utility;

namespace Minsung.Common
{
    // 현재 씬의 리스폰 지점을 소유하고, 사망 위치에서 가장 가까운 지점으로 복귀시킨다.
    [DefaultExecutionOrder(-190)]
    public class RespawnManager : PersistentSingleton<RespawnManager>
    {
        private const string MANAGER_OBJECT_NAME = "RespawnManager";

        private readonly List<RespawnPoint> _points = new List<RespawnPoint>();
        private RespawnPoint _forcedPoint;
        private bool _bossRestartPending;

        public static bool IsBossRestartPending => (Instance != null) && Instance._bossRestartPending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject(MANAGER_OBJECT_NAME).AddComponent<RespawnManager>();
            }
        }

        protected override void OnSingletonAwake()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDestroy();
        }

        public static void Register(RespawnPoint point)
        {
            EnsureInstance();
            if ((Instance != null) && (point != null) && !Instance._points.Contains(point))
            {
                Instance._points.Add(point);
            }
        }

        public static void Unregister(RespawnPoint point)
        {
            if (Instance == null)
            {
                // 씬/플레이 모드 종료 중에는 RespawnManager가 먼저 파괴될 수 있다.
                // 등록 해제를 위해 새 영속 오브젝트를 만들면 Unity의 미정리 GameObject 오류가 발생한다.
                return;
            }

            Instance._points.Remove(point);
            if (Instance._forcedPoint == point)
            {
                Instance._forcedPoint = null;
            }
        }

        // 보스방 즉사 기믹 등에서 사용할 명시적 복귀 지점. 없으면 일반 최근접 지점을 쓴다.
        public static void RequestBossReturn()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            Instance._forcedPoint = Instance.FindBossReturnPoint();
        }

        // 보스 패턴 사망은 Map2를 다시 로드해 보스/분신/해저드/타이머 전체를 초기화한 뒤 복귀 지점으로 옮긴다.
        public static void RestartBossAtReturnPoint()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            Instance._bossRestartPending = true;
            GameManager.Instance?.ResetBossTimer();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(Constants.Scene.MAP_2);
            }
            else
            {
                SceneManager.LoadScene(Constants.Scene.MAP_2);
            }
        }

        public static bool TryRespawn(PlayerController player, Action onRespawn = null)
        {
            EnsureInstance();
            if ((Instance == null) || (player == null))
            {
                return false;
            }

            RespawnPoint point = Instance.ConsumeTarget(player.transform.position);
            if (point == null)
            {
                return false;
            }

            if (GameManager.Instance != null)
            {
                return GameManager.Instance.RequestRespawnAt(player.transform, point.Position, onRespawn);
            }

            player.transform.position = point.Position;
            onRespawn?.Invoke();
            return true;
        }

        private RespawnPoint ConsumeTarget(Vector3 deathPosition)
        {
            RespawnPoint point = _forcedPoint;
            _forcedPoint = null;

            if ((point != null) && point.gameObject.activeInHierarchy)
            {
                return point;
            }

            RespawnPoint nearest = null;
            float nearestSqrDistance = float.MaxValue;
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            for (int i = _points.Count - 1; i >= 0; --i)
            {
                RespawnPoint candidate = _points[i];
                if (candidate == null)
                {
                    _points.RemoveAt(i);
                    continue;
                }

                if (!candidate.gameObject.activeInHierarchy || candidate.IsBossReturnPoint ||
                    (candidate.gameObject.scene != activeScene))
                {
                    continue;
                }

                float sqrDistance = (candidate.Position - deathPosition).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearest = candidate;
                    nearestSqrDistance = sqrDistance;
                }
            }

            return nearest;
        }

        private RespawnPoint FindBossReturnPoint()
        {
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            for (int i = 0; i < _points.Count; ++i)
            {
                RespawnPoint point = _points[i];
                if ((point != null) && point.IsBossReturnPoint && point.gameObject.activeInHierarchy &&
                    (point.gameObject.scene == activeScene))
                {
                    return point;
                }
            }

            return null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_bossRestartPending || (scene.name != Constants.Scene.MAP_2) || (mode != LoadSceneMode.Single))
            {
                return;
            }

            StartCoroutine(CoMovePlayerToBossReturnPoint());
        }

        private System.Collections.IEnumerator CoMovePlayerToBossReturnPoint()
        {
            // 씬의 OnEnable/Start가 모두 끝나야 새 Map2의 리스폰 포인트와 플레이어가 등록된다.
            yield return null;

            _bossRestartPending = false;
            RespawnPoint point = FindBossReturnPoint();
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if ((point == null) || (player == null))
            {
                yield break;
            }

            player.SetPose(point.Position, Vector2.zero, false);
            player.RequestClearClones();
            if (player.TryGetComponent(out PlayerHealth health))
            {
                health.ResetHearts();
            }
            GameManager.Instance?.SetCheckpoint(point.Position);
        }
    }
}
