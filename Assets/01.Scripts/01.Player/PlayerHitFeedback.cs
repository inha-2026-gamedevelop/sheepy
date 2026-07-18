// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

using Minsung.Common.Data;
using Minsung.TimeSystem;

namespace Minsung.Player
{
    // 실제 피해 확정(PlayerHealth.OnDamaged) 뒤에 화면 단위 피격 연출을 함께 재생한다.
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerHitFeedback : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("연출 토글")]
        [SerializeField] private bool _useCameraShake   = true;
        [SerializeField] private bool _useVignetteFlash = true;
        [SerializeField] private bool _useHitStop       = true;

        [Header("참조")]
        [SerializeField] private CinemachineImpulseSource _impulseSource;

        private PlayerHealth _health;
        private PlayerDataSO _playerSo;
        private Image _vignetteImage;
        private Coroutine _vignetteRoutine;
        private float _vignetteUntil;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _health   = GetComponent<PlayerHealth>();
            _playerSo = GameDB.Player;

            if (_impulseSource == null)
            {
                if (!TryGetComponent(out _impulseSource))
                {
                    _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
                }
            }
            ConfigureImpulse();
            CreateVignetteImage();

            _health.OnDamaged += HandleDamaged;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamaged -= HandleDamaged;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        private void ConfigureImpulse()
        {
            if (_impulseSource == null)
            {
                return;
            }

            if (_impulseSource.ImpulseDefinition == null)
            {
                _impulseSource.ImpulseDefinition = new CinemachineImpulseDefinition();
            }

            // 런타임 AddComponent는 CinemachineImpulseSource.Reset()을 호출하지 않는다.
            // 기본 CinemachineImpulseDefinition은 이벤트가 생성되지 않으므로
            // 피격용 표준 Bump 임펄스 구성을 명시한다.
            _impulseSource.ImpulseDefinition.ImpulseChannel      = 1;
            _impulseSource.ImpulseDefinition.ImpulseShape        = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            _impulseSource.ImpulseDefinition.ImpulseDuration     = _playerSo.HitShakeDuration;
            _impulseSource.ImpulseDefinition.ImpulseType         = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            _impulseSource.ImpulseDefinition.DissipationDistance = 100f;
            _impulseSource.DefaultVelocity = Vector3.down;
        }

        private void HandleDamaged()
        {
            if (_useCameraShake && (_impulseSource != null))
            {
                _impulseSource.GenerateImpulseWithForce(_playerSo.HitShakeForce);
            }
            if (_useVignetteFlash)
            {
                PlayVignetteFlash();
            }
            if (_useHitStop)
            {
                HitStopController.Request(_playerSo.HitStopOnDamagedDuration);
            }

            // TODO: 피격 SFX 에셋 확정 후 SoundManager.PlaySFX 호출을 연결한다.
        }

        private void PlayVignetteFlash()
        {
            if (_vignetteImage == null)
            {
                return;
            }

            _vignetteUntil = Time.unscaledTime + _playerSo.HitVignetteDuration;
            SetVignetteAlpha(_playerSo.HitVignetteAlpha);

            if (_vignetteRoutine == null)
            {
                _vignetteRoutine = StartCoroutine(CoFadeVignette());
            }
        }

        // 히트스톱/timeScale 0 및 일시정지 중에도 감쇠하도록 unscaled 시간을 사용한다.
        private IEnumerator CoFadeVignette()
        {
            while (Time.unscaledTime < _vignetteUntil)
            {
                float remaining = _vignetteUntil - Time.unscaledTime;
                float alpha = _playerSo.HitVignetteDuration > 0f
                    ? _playerSo.HitVignetteAlpha * Mathf.Clamp01(remaining / _playerSo.HitVignetteDuration)
                    : 0f;
                SetVignetteAlpha(alpha);
                yield return null;
            }

            SetVignetteAlpha(0f);
            _vignetteRoutine = null;
        }

        private void CreateVignetteImage()
        {
            GameObject canvasObject = new GameObject("PlayerHitFeedbackCanvas");
            canvasObject.transform.SetParent(transform);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998; // ScreenFade(999) 아래에서만 표시
            canvasObject.AddComponent<CanvasScaler>();

            GameObject imageObject = new GameObject("PlayerHitVignette");
            imageObject.transform.SetParent(canvas.transform, false);

            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _vignetteImage               = imageObject.AddComponent<Image>();
            _vignetteImage.raycastTarget = false;
            SetVignetteAlpha(0f);
        }

        private void SetVignetteAlpha(float alpha)
        {
            Color color = Color.red;
            color.a = Mathf.Clamp01(alpha);
            _vignetteImage.color = color;
        }
    }
}
