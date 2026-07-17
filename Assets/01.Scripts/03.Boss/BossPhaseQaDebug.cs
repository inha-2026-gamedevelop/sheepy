#if UNITY_EDITOR
// Unity
using UnityEngine;

namespace Minsung.Boss
{
    // QA 전용 - 에디터에서 숫자 키로 보스 페이즈를 즉시 이동한다 (테스트 편의용, 빌드에는 포함되지 않음)
    // 2 -> 2페이즈, 3 -> 3페이즈, 4 -> 4페이즈
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
