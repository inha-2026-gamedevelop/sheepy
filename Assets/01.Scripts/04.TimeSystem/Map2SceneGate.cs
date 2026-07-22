// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

// Project
using Minsung.Common;
using Minsung.Player;

namespace Minsung.TimeSystem
{
    // 슬로우 능력 획득 뒤 활성화되는 Map2 이동 지점.
    [RequireComponent(typeof(Collider2D))]
    public class Map2SceneGate : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private Collider2D _trigger;
        private bool       _isLoading;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            TryGetComponent(out _trigger);
            if (_trigger != null)
            {
                _trigger.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isLoading || !other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            _isLoading = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(Constants.Scene.MAP_2);
            }
            else
            {
                SceneManager.LoadScene(Constants.Scene.MAP_2);
            }
        }
    }
}
