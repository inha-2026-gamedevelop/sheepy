// Unity
using UnityEngine;

namespace Minsung.Interactive
{
    // 레버 당김 상태에 따라 지정된 오브젝트들을 켜고 끈다 (LeverInteractive의 onLeverPulled/onLeverReset에 연결해서 사용)
    [AddComponentMenu("Minsung/Lever Light Switch")]
    public class LeverLightSwitch : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("당겼을 때 켜지는 오브젝트")]
        [SerializeField] private GameObject[] _activateOnPulled;

        [Header("당겼을 때 꺼지는 오브젝트")]
        [SerializeField] private GameObject[] _deactivateOnPulled;

        /****************************************
        *                Methods
        ****************************************/

        // LeverInteractive.onLeverPulled에 연결
        public void OnLeverPulled()
        {
            SetActiveAll(_activateOnPulled, true);
            SetActiveAll(_deactivateOnPulled, false);
        }

        // LeverInteractive.onLeverReset에 연결 (되감기로 당기기 전 상태로 되돌아갔을 때)
        public void OnLeverReset()
        {
            SetActiveAll(_activateOnPulled, false);
            SetActiveAll(_deactivateOnPulled, true);
        }

        private void SetActiveAll(GameObject[] targets, bool active)
        {
            if (targets == null)
            {
                return;
            }
            foreach (GameObject go in targets)
            {
                if (go != null)
                {
                    go.SetActive(active);
                }
            }
        }
    }
}
