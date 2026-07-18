// Unity
using UnityEngine;

using TMPro;

using Minsung.Item;

namespace Minsung.UI
{
    // 포션 보유 개수 HUD. PotionManager.OnPotionChanged를 구독해 "현재/최대" 형식으로 갱신한다.
    public class PotionCounterUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private TMP_Text _countText;

        private PotionManager _potionManager;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            // PotionManager는 RewindManager처럼 씬 로드 시 자동 생성되므로 Start 시점엔 준비돼 있다.
            _potionManager = PotionManager.Instance;
            if (_potionManager == null)
            {
                return;
            }
            _potionManager.OnPotionChanged += Redraw;
            Redraw(_potionManager.PotionCount);
        }

        private void OnDestroy()
        {
            if (_potionManager != null)
            {
                _potionManager.OnPotionChanged -= Redraw;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        private void Redraw(int count)
        {
            if (_countText != null)
            {
                _countText.text = $"{count}/{_potionManager.MaxCarryCount}";
            }
        }
    }
}
