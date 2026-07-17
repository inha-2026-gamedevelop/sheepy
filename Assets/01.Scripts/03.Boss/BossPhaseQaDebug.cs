#if UNITY_EDITOR
// Unity
using UnityEngine;

namespace Minsung.Boss
{
    // QA 전용 - 에디터에서 숫자 키(2/3/4)로 해당 페이즈로 즉시 이동한다 (테스트 편의용, 빌드 미포함)
    [AddComponentMenu("Minsung/QA/Boss Phase QA Debug")]
    public class BossPhaseQaDebug : MonoBehaviour
    {
        [SerializeField] private BossController _boss;

        private void Update()
        {
            if (_boss == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _boss.QaJumpToPhase(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                _boss.QaJumpToPhase(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                _boss.QaJumpToPhase(3);
            }
        }
    }
}
#endif
