// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.Boss2
{
    // BossController 대신 Boss2Health를 구독한다
    public class BossHealthBarUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private Boss2Health _boss;
        [SerializeField] private Slider      _slider;

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnEnable()
        {
            if (_boss == null)
            {
                Redraw(0f, 1f);
                return;
            }

            _boss.OnHealthChanged += Redraw;
            Redraw(_boss.CurrentHealth, _boss.MaxHealth);
        }

        // 씬 로드 시 UI가 보스보다 먼저 깨어나는 경우를 대비해 Start에서 인스펙터 미지정이면 자동 연결
        private void Start()
        {
            if (_boss == null)
            {
                _boss = FindAnyObjectByType<Boss2Health>();
                if (_boss == null)
                {
                    gameObject.SetActive(false); // 보스 없는 맵에서는 바를 숨긴다
                    return;
                }
                _boss.OnHealthChanged += Redraw;
            }
            Redraw(_boss.CurrentHealth, _boss.MaxHealth);
        }

        private void OnDisable()
        {
            if (_boss != null)
            {
                _boss.OnHealthChanged -= Redraw;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        private void Redraw(float current, float total)
        {
            if (_slider == null)
            {
                return;
            }

            float value = 0f;
            if (total > 0f)
            {
                value = Mathf.Clamp01(current / total);
            }
            _slider.value = value;
        }
    }
}
