// Unity
using UnityEngine;

// Project
using Minsung.Player;

namespace Minsung.TimeSystem
{
    // Map1에서 슬로우 능력을 획득하면 Shift 안내와 Map2 이동 지점을 연다.
    [RequireComponent(typeof(Collider2D))]
    public class SlowAbilityUnlockTrigger : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string SHIFT_IMAGE_NAME = "ShiftImage[OFF]";
        private const string MAP2_GATE_NAME   = "GoToMap2Scene[OFF]";

        private Collider2D _trigger;
        private GameObject _shiftImage;
        private GameObject _map2Gate;
        private bool       _unlocked;

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

            Transform parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            Transform shiftImage = parent.Find(SHIFT_IMAGE_NAME);
            Transform map2Gate   = parent.Find(MAP2_GATE_NAME);
            if (shiftImage != null)
            {
                _shiftImage = shiftImage.gameObject;
            }
            if (map2Gate != null)
            {
                _map2Gate = map2Gate.gameObject;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_unlocked || !other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            _unlocked = true;
            SlowMotionController.UnlockAbility();

            if (_shiftImage != null)
            {
                _shiftImage.SetActive(true);
            }
            if (_map2Gate != null)
            {
                _map2Gate.SetActive(true);
            }

            gameObject.SetActive(false);
        }
    }
}
