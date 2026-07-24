// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.TimeSystem;

namespace Minsung.UI
{
    // 분신 개수 HUD. 활성 분신 수만큼 clone 아이콘을 왼쪽부터 켜서 표시한다.
    public class CloneCounterUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private ClonePool _clonePool;  // 구독할 분신 풀
        [SerializeField] private Image[]   _cloneIcons; // 분신 슬롯 이미지들 (왼쪽부터 순서대로, 전부 동일한 clone 스프라이트)

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnEnable()
        {
            if (_clonePool == null)
            {
                return;
            }
            _clonePool.OnActiveCountChanged += Redraw;
            Redraw(_clonePool.ActiveCount);
        }

        // 씬 로드 시 UI가 ClonePool보다 먼저 깨어나면 OnEnable 시점엔 못 찾을 수 있다 - 모든 Awake가 끝난 Start에서 한 번 더 연결한다
        private void Start()
        {
            if (_clonePool == null)
            {
                _clonePool = FindAnyObjectByType<ClonePool>();
                if (_clonePool != null)
                {
                    _clonePool.OnActiveCountChanged += Redraw;
                }
            }

            if (_clonePool == null)
            {
                return;
            }
            Redraw(_clonePool.ActiveCount);
        }

        private void OnDisable()
        {
            if (_clonePool == null)
            {
                return;
            }
            _clonePool.OnActiveCountChanged -= Redraw;
        }

        /****************************************
        *                Methods
        ****************************************/

        // 활성 분신 수만큼 왼쪽부터 아이콘을 켜고 나머지는 끈다 - 0개면 전부 꺼져 아무것도 표시되지 않는다
        private void Redraw(int activeCount)
        {
            for (int i = 0; i < _cloneIcons.Length; ++i)
            {
                _cloneIcons[i].enabled = i < activeCount;
            }
        }
    }
}
