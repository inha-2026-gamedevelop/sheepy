// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Boss;

namespace Minsung.UI
{
    // 타격감: 피격 시 메인 바는 즉시 줄고, 흰 딜레이바가 늦게 따라오며(잃은 만큼 흰색으로 남음), 바가 잠깐 흔들린다
    public class BossHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BossController _boss;
        [SerializeField] private Slider _slider;
        [SerializeField] private GameObject[] _phaseNotches;

        [Header("흰 딜레이바 (뒤에 깔려 늦게 따라옴 - 비우면 미사용)")]
        [SerializeField] private Slider _delaySlider;
        [SerializeField] private float  _delayHold    = 0.15f; // 피격 후 딜레이바가 줄기 시작하기까지 대기(초)
        [SerializeField] private float  _delayCatchup = 0.5f;  // 딜레이바가 따라오는 속도(정규값/초)

        [Header("바 흔들림")]
        [SerializeField] private float _shakeAmplitude = 6f;   // 흔들림 크기(px)
        [SerializeField] private float _shakeDuration  = 0.15f;

        private float     _target;
        private float     _delayValue;
        private float     _delayHoldUntil;
        private Coroutine _shakeRoutine;
        private Vector2   _sliderBasePos;
        private Vector2   _delayBasePos;

        private void OnEnable()
        {
            if (_boss == null)
            {
                Redraw(0f, 1f);
                return;
            }

            _boss.OnHealthChanged += Redraw;
            _boss.OnPhaseChanged  += RedrawPhaseVisibility;
            Redraw(_boss.CurrentHealth, _boss.TotalHealth);
            RedrawPhaseVisibility(_boss.PhaseIndex);
        }

        private void OnDisable()
        {
            if (_boss != null)
            {
                _boss.OnHealthChanged -= Redraw;
                _boss.OnPhaseChanged  -= RedrawPhaseVisibility;
            }
        }

        // 인스펙터 미지정이면 씬의 보스에서 자동 연결
        private void Start()
        {
            if (_boss == null)
            {
                _boss = FindAnyObjectByType<BossController>();
                if (_boss == null)
                {
                    gameObject.SetActive(false); // 보스 없는 맵에서는 바를 숨긴다
                    return;
                }
                _boss.OnHealthChanged += Redraw;
                _boss.OnPhaseChanged  += RedrawPhaseVisibility;
            }
            Redraw(_boss.CurrentHealth, _boss.TotalHealth);
            RedrawPhaseVisibility(_boss.PhaseIndex);
        }

        private void Update()
        {
            if (_delaySlider == null)
            {
                return;
            }

            // 유지 시간이 지나면 딜레이바가 목표까지 서서히 줄어든다
            if ((_delayValue > _target) && (Time.unscaledTime >= _delayHoldUntil))
            {
                _delayValue = Mathf.MoveTowards(_delayValue, _target, _delayCatchup * Time.unscaledDeltaTime);
                _delaySlider.value = _delayValue;
            }
        }

        private void RedrawPhaseVisibility(int phaseIndex)
        {
            bool visible = (_boss != null) && _boss.IsBattleStarted && (phaseIndex >= 1);
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(visible);
            }

            if (_delaySlider != null)
            {
                _delaySlider.gameObject.SetActive(visible); // 딜레이바는 형제라 별도로 메인 바 표시 상태를 따라간다
            }

            if (visible && (_boss != null))
            {
                RedrawNotches(_boss.CurrentHealth, _boss.TotalHealth);
            }
        }

        private void Redraw(float current, float total)
        {
            if (_slider == null)
            {
                return;
            }

            float value = (total > 0f) ? Mathf.Clamp01(current / total) : 0f;
            bool  damaged = value < (_slider.value - 0.0001f);

            _slider.value = value;
            _target       = value;

            if (_delaySlider != null)
            {
                if (value >= _delayValue)
                {
                    // 회복/되감기 - 딜레이바도 즉시 따라 올린다
                    _delayValue        = value;
                    _delaySlider.value = value;
                }
                else
                {
                    // 피격 - 딜레이바는 잠깐 유지 후 Update에서 줄어든다
                    _delayHoldUntil = Time.unscaledTime + _delayHold;
                }
            }

            if (damaged)
            {
                StartShake();
            }

            RedrawNotches(current, total);
        }

        // 노치 i는 페이즈 경계(total - PhaseHealth * (i+1)) 지점 - HP가 그 아래로 깎이면 숨긴다 (되감기로 회복하면 다시 나타남)
        private void RedrawNotches(float current, float total)
        {
            if (_phaseNotches == null)
            {
                return;
            }

            for (int i = 0; i < _phaseNotches.Length; ++i)
            {
                if (_phaseNotches[i] == null)
                {
                    continue;
                }
                float boundaryHealth = total - ((_boss != null ? _boss.PhaseHealthSpan : 0f) * (i + 1));
                _phaseNotches[i].SetActive(current > boundaryHealth);
            }
        }

        private void StartShake()
        {
            if (_shakeAmplitude <= 0f)
            {
                return;
            }

            RectTransform sliderRt = _slider != null ? _slider.transform as RectTransform : null;
            RectTransform delayRt   = _delaySlider != null ? _delaySlider.transform as RectTransform : null;
            if (sliderRt == null)
            {
                return;
            }

            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                RestoreShakePositions(sliderRt, delayRt);
            }

            _sliderBasePos = sliderRt.anchoredPosition;
            if (delayRt != null)
            {
                _delayBasePos = delayRt.anchoredPosition;
            }
            _shakeRoutine = StartCoroutine(CoShake(sliderRt, delayRt));
        }

        private IEnumerator CoShake(RectTransform sliderRt, RectTransform delayRt)
        {
            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / _shakeDuration);
                Vector2 offset = Random.insideUnitCircle * (_shakeAmplitude * damp);

                sliderRt.anchoredPosition = _sliderBasePos + offset;
                if (delayRt != null)
                {
                    delayRt.anchoredPosition = _delayBasePos + offset;
                }
                yield return null;
            }

            RestoreShakePositions(sliderRt, delayRt);
            _shakeRoutine = null;
        }

        private void RestoreShakePositions(RectTransform sliderRt, RectTransform delayRt)
        {
            if (sliderRt != null)
            {
                sliderRt.anchoredPosition = _sliderBasePos;
            }
            if (delayRt != null)
            {
                delayRt.anchoredPosition = _delayBasePos;
            }
        }
    }
}
