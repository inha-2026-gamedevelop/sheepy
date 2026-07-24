using UnityEngine;

namespace Minsung.Boss
{
    /// <summary>
    /// 애니메이터가 붙어있는 Visual 오브젝트에서 애니메이션 이벤트를 받아
    /// 부모에 있는 BossMeleeUnitBase로 전달해주는 역할의 스크립트입니다.
    /// </summary>
    public class BossAnimationEventForwarder : MonoBehaviour
    {
        private BossMeleeUnitBase _bossMelee;

        private void Awake()
        {
            _bossMelee = GetComponentInParent<BossMeleeUnitBase>();
        }

        public void EnableAttackHitbox()
        {
            if (_bossMelee != null)
            {
                _bossMelee.EnableAttackHitbox();
            }
        }

        public void DisableAttackHitbox()
        {
            if (_bossMelee != null)
            {
                _bossMelee.DisableAttackHitbox();
            }
        }

        public void FinishAttackAction()
        {
            if (_bossMelee != null)
            {
                _bossMelee.FinishAttackAction();
            }
        }
    }
}
