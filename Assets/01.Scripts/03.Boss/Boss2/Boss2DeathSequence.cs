// System
using System.Collections;

// Unity
using UnityEngine;
using Unity.Cinemachine;

using Minsung.CameraSystem;

namespace Minsung.Boss2
{
    // 4페이즈 사망 연출 오케스트레이터 - Boss2Health.OnDefeated 발행 시 1회 발동한다.
    // 순서: 이동/패턴 정지 + 카메라를 보스로 포커스(줌아웃) + 오라 사방 분출 + 카메라 흔들림 동시 시작
    //   -> 보스 본체(Visual/Body/Eyes) 섬광(흰색) 후 서서히 소멸 -> 카메라 복귀
    public class Boss2DeathSequence : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("참조")]
        [SerializeField] private Boss2Health         _health;   // OnDefeated 구독 (HitCenter에 부착)
        [SerializeField] private Boss2DataSO         _dataSo;
        [SerializeField] private BossFloatMovement   _movement; // 사망 시 배회/돌진을 멈추고 그 자리에 고정
        [SerializeField] private Boss2AttackPatterns _patterns; // 사망 시 일반 패턴(낙뢰/강타/레이저 등) 정지

        [Header("오라 분출")]
        [SerializeField] private ParticleSystem _auraParticles; // Phase4Aura의 ParticleSystem - 사방으로 Emit

        [Header("본체 섬광/소멸")]
        [SerializeField] private SpriteRenderer[] _bodyRenderers; // Visual/Body/Eyes - 섬광 후 페이드아웃 대상

        [Header("카메라 흔들림")]
        [SerializeField] private CinemachineImpulseSource _impulseSource;

        private bool _played;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            if ((_impulseSource == null) && !TryGetComponent(out _impulseSource))
            {
                _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            }
            ConfigureImpulse();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDefeated += HandleDefeated;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDefeated -= HandleDefeated;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        // PlayerHitFeedback과 동일한 관례 - 런타임 AddComponent는 Reset()을 호출하지 않으므로 Bump 임펄스를 직접 구성한다
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

            _impulseSource.ImpulseDefinition.ImpulseChannel      = 1;
            _impulseSource.ImpulseDefinition.ImpulseShape        = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            _impulseSource.ImpulseDefinition.ImpulseDuration     = (_dataSo != null) ? _dataSo.DeathShakeDuration : 0.6f;
            _impulseSource.ImpulseDefinition.ImpulseType         = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            _impulseSource.ImpulseDefinition.DissipationDistance = 100f;
            _impulseSource.DefaultVelocity = Vector3.down;
        }

        private void HandleDefeated()
        {
            if (_played)
            {
                return;
            }
            _played = true;
            StartCoroutine(CoDeathSequence());
        }

        private IEnumerator CoDeathSequence()
        {
            // 보스를 그 자리에 완전히 고정 - 배회/돌진/좌우상하 흔들림과 일반 공격 패턴을 모두 정지한다(보스는 죽었으므로 재개하지 않는다)
            _movement?.TryBeginScriptedMovement();
            _patterns?.SuspendNormalPatterns();

            // 카메라를 보스 쪽으로 이동시키고 줌아웃
            float zoomSize    = (_dataSo != null) ? _dataSo.DeathCameraZoomSize : 6f;
            float cameraBlend = (_dataSo != null) ? _dataSo.DeathCameraBlend : 0.5f;
            CameraManager.Instance?.Focus(transform, zoomSize, cameraBlend);

            EmitAuraBurst();

            if (_impulseSource != null)
            {
                float force = (_dataSo != null) ? _dataSo.DeathShakeForce : 1.2f;
                _impulseSource.GenerateImpulseWithForce(force);
            }

            float flashDuration = (_dataSo != null) ? _dataSo.DeathFlashDuration : 0.15f;
            float fadeDuration  = (_dataSo != null) ? _dataSo.DeathFadeDuration : 1.2f;
            yield return CoFlashAndFade(flashDuration, fadeDuration);

            CameraManager.Instance?.UnFocus();
        }

        // Phase4Aura 파티클을 재사용해 사방(2D 360도)으로 즉시 분출한다.
        // 계속 돌던 배경 루프(4페이즈 진입 시 켜진 잔잔한 위로 뜨는 오라)를 완전히 멈추고 비운 뒤,
        // 중력/노이즈/크기 변화 곡선처럼 "잔잔하게 떠다니는" 모양을 만들던 모듈을 꺼서
        // EmitParams로 지정한 방향/속도가 그대로 직선으로 뻗어나가는 폭발처럼 보이게 한다
        private void EmitAuraBurst()
        {
            if (_auraParticles == null)
            {
                return;
            }

            _auraParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _auraParticles.gameObject.SetActive(true);

            ParticleSystem.MainModule main = _auraParticles.main;
            main.gravityModifier  = 0f;
            main.simulationSpace  = ParticleSystemSimulationSpace.World; // 보스가 움직여도 이미 나간 파티클이 끌려가지 않게

            ParticleSystem.NoiseModule noise = _auraParticles.noise;
            noise.enabled = false;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = _auraParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = false;

            // Emit()은 정지된 시스템을 암묵적으로 다시 재생 상태로 돌려놓는다 - Emission 모듈을 꺼서
            // 자동 발생(초당 rateOverTime개)이 버스트와 함께 되살아나지 않게 한다. 수동 Emit()은 이 모듈과 무관하게 동작한다
            ParticleSystem.EmissionModule emission = _auraParticles.emission;
            emission.enabled = false;

            int   count = (_dataSo != null) ? _dataSo.DeathAuraBurstCount : 40;
            float speed = (_dataSo != null) ? _dataSo.DeathAuraBurstSpeed : 5f;

            for (int i = 0; i < count; ++i)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                {
                    velocity      = direction * (speed * Random.Range(0.6f, 1f)),
                    startLifetime = Random.Range(0.4f, 0.8f),
                };
                _auraParticles.Emit(emitParams, 1);
            }
        }

        // 본체 스프라이트를 흰색으로 섬광시킨 뒤 알파를 0으로 서서히 낮춰 소멸시킨다
        private IEnumerator CoFlashAndFade(float flashDuration, float fadeDuration)
        {
            if ((_bodyRenderers == null) || (_bodyRenderers.Length == 0))
            {
                yield break;
            }

            Color[] originalColors = new Color[_bodyRenderers.Length];
            for (int i = 0; i < _bodyRenderers.Length; ++i)
            {
                if (_bodyRenderers[i] != null)
                {
                    originalColors[i] = _bodyRenderers[i].color;
                }
            }

            // 섬광 - 원래 색에서 흰색으로
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                float t = elapsed / flashDuration;
                for (int i = 0; i < _bodyRenderers.Length; ++i)
                {
                    if (_bodyRenderers[i] != null)
                    {
                        _bodyRenderers[i].color = Color.Lerp(originalColors[i], Color.white, t);
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 소멸 - 흰색을 유지한 채 알파만 0으로
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                for (int i = 0; i < _bodyRenderers.Length; ++i)
                {
                    if (_bodyRenderers[i] != null)
                    {
                        _bodyRenderers[i].color = new Color(1f, 1f, 1f, alpha);
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < _bodyRenderers.Length; ++i)
            {
                if (_bodyRenderers[i] != null)
                {
                    _bodyRenderers[i].color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_health == null)
            {
                Debug.LogWarning("[Boss2DeathSequence] _health 미배치 - HitCenter의 Boss2Health를 연결해야 OnDefeated를 구독할 수 있습니다.", this);
            }
            if (_movement == null)
            {
                Debug.LogWarning("[Boss2DeathSequence] _movement 미배치 - Boss의 BossFloatMovement를 연결해야 사망 시 이동이 멈춥니다.", this);
            }
            if (_patterns == null)
            {
                Debug.LogWarning("[Boss2DeathSequence] _patterns 미배치 - Boss의 Boss2AttackPatterns를 연결해야 사망 시 일반 패턴이 멈춥니다.", this);
            }
            if (_auraParticles == null)
            {
                Debug.LogWarning("[Boss2DeathSequence] _auraParticles 미배치 - Phase4Aura의 ParticleSystem을 연결해야 오라 분출이 재생됩니다.", this);
            }
            if ((_bodyRenderers == null) || (_bodyRenderers.Length == 0))
            {
                Debug.LogWarning("[Boss2DeathSequence] _bodyRenderers 미배치 - 보스 본체 SpriteRenderer들을 연결해야 섬광/소멸이 재생됩니다.", this);
            }
        }
#endif
    }
}
