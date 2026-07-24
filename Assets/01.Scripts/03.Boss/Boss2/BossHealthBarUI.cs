// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.Boss2
{
    // BossController 대신 Boss2Health를 구독한다
    // 타격감: 피격 시 메인 바는 즉시 줄고, 흰 딜레이바가 늦게 따라오며(잃은 만큼 흰색으로 남음), 바가 잠깐 흔들린다
    public class BossHealthBarUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private Boss2Health _boss;
        [SerializeField] private Slider      _slider;

        [Header("흰 딜레이바 (뒤에 깔려 늦게 따라옴 - 비우면 미사용)")]
        [SerializeField] private Slider _delaySlider;
        [SerializeField] private float  _delayHold       = 0.15f; // 피격 후 딜레이바가 줄기 시작하기까지 대기(초)
        [SerializeField] private float  _delayCatchup    = 0.5f;  // 딜레이바가 따라오는 속도(정규값/초)

        [Header("바 흔들림")]
        [SerializeField] private float _shakeAmplitude = 6f;   // 흔들림 크기(px)
        [SerializeField] private float _shakeDuration  = 0.15f;

        private float      _target;
        private float      _delayValue;
        private float      _delayHoldUntil;
        private Coroutine  _shakeRoutine;
        private Vector2    _sliderBasePos;
        private Vector2    _delayBasePos;

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

        /****************************************
        *                Methods
        ****************************************/

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
