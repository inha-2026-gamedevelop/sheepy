// Unity
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Minsung.Boss;
using Minsung.Common.Data;

namespace Minsung.UI
{
    public class BossCloneHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BossController      _boss;
        [SerializeField] private BossCloneController _clone;
        [SerializeField] private Slider _slider;

        [Header("바 흔들림")]
        [SerializeField] private float _shakeAmplitude = 6f;   // 흔들림 크기(px)
        [SerializeField] private float _shakeDuration  = 0.15f;

        private float _lastHealth = 1f; // 페이즈 전환 시점에도 사망 여부를 판단할 수 있게 마지막 체력을 기억
        private Coroutine _shakeRoutine;
        private Vector2   _sliderBasePos;

        private void OnEnable()
        {
            if (_clone == null)
            {
                Redraw(0f, 1f);
                return;
            }

            _clone.OnHealthChanged += Redraw;
            if (_boss != null)
            {
                _boss.OnPhaseChanged += HandlePhaseChanged;
                _boss.OnBattleStarted += RefreshVisibility;
            }
            Redraw(_clone.CurrentHealth, GameDB.Boss.CloneHealth);
        }

        private void OnDisable()
        {
            if (_clone != null)
            {
                _clone.OnHealthChanged -= Redraw;
            }
            if (_boss != null)
            {
                _boss.OnPhaseChanged -= HandlePhaseChanged;
                _boss.OnBattleStarted -= RefreshVisibility;
            }
        }

        private void Redraw(float current, float total)
        {
            _lastHealth = current;

            if (_slider != null)
            {
                float value = (total > 0f) ? Mathf.Clamp01(current / total) : 0f;
                bool damaged = value < (_slider.value - 0.0001f);
                
                _slider.value = value;
                
                if (damaged)
                {
                    StartShake();
                }
            }
            RefreshVisibility();
        }

        private void HandlePhaseChanged(int _)
        {
            RefreshVisibility();
        }

        // 사망했거나(체력 0) 1페이즈가 끝났으면(분신 퇴장) 숨긴다
        private void RefreshVisibility()
        {
            bool alive    = _lastHealth > 0f;
            bool inPhase1 = (_boss == null) || (_boss.PhaseIndex == 0);
            bool battleStarted = (_boss == null) || _boss.IsBattleStarted;
            SetVisible(alive && inPhase1 && battleStarted);
        }

        // 슬라이더 자식(Background/Fill Area)만 껐다 켠다 - 루트(이 컴포넌트)는 계속 활성 상태로 유지해야
        // 부활/페이즈 전환 이벤트를 계속 받을 수 있다 (루트를 끄면 OnDisable에서 구독이 끊긴다)
        private void SetVisible(bool visible)
        {
            if (_slider == null)
            {
                return;
            }
            foreach (Transform child in _slider.transform)
            {
                child.gameObject.SetActive(visible);
            }
        }

        private void StartShake()
        {
            if (_shakeAmplitude <= 0f)
            {
                return;
            }

            RectTransform sliderRt = _slider != null ? _slider.transform as RectTransform : null;
            if (sliderRt == null)
            {
                return;
            }

            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                RestoreShakePositions(sliderRt);
            }

            _sliderBasePos = sliderRt.anchoredPosition;
            _shakeRoutine = StartCoroutine(CoShake(sliderRt));
        }

        private IEnumerator CoShake(RectTransform sliderRt)
        {
            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / _shakeDuration);
                Vector2 offset = Random.insideUnitCircle * (_shakeAmplitude * damp);

                sliderRt.anchoredPosition = _sliderBasePos + offset;
                yield return null;
            }

            RestoreShakePositions(sliderRt);
            _shakeRoutine = null;
        }

        private void RestoreShakePositions(RectTransform sliderRt)
        {
            if (sliderRt != null)
            {
                sliderRt.anchoredPosition = _sliderBasePos;
            }
        }
    }
}
