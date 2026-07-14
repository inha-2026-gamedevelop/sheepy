// Unity
using UnityEngine;

using TMPro;

using Minsung.Item;

namespace Minsung.UI
{
    // LP 보유 개수 HUD. LpManager.OnLpChanged를 구독해 숫자만 갱신한다.
    public class LpCounterUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private TMP_Text _countText;

        private LpManager _lpManager;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            // LpManager는 RewindManager처럼 씬 로드 시 자동 생성되므로 Start 시점엔 준비돼 있다.
            _lpManager = LpManager.Instance;
            if (_lpManager == null)
            {
                return;
            }
            _lpManager.OnLpChanged += Redraw;
            Redraw(_lpManager.LpCount);
        }

        private void OnDestroy()
        {
            if (_lpManager != null)
            {
                _lpManager.OnLpChanged -= Redraw;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        private void Redraw(int count)
        {
            if (_countText != null)
            {
                _countText.text = count.ToString();
            }
        }
    }
}
