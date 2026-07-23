// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

// Project
using Minsung.CameraSystem;
using Minsung.Common;
using Minsung.Player;

namespace Minsung.TimeSystem
{
    // Map1에서 슬로우 능력을 획득하면 GetSlow 연출을 재생하고, Shift 안내와 Map2 이동 지점을 연다.
    [RequireComponent(typeof(Collider2D))]
    public class SlowAbilityUnlockTrigger : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string SHIFT_IMAGE_NAME = "ShiftImage[OFF]";
        private const string MAP2_GATE_NAME   = "GoToMap2Scene[OFF]";
        private const string GET_SLOW_STATE   = "GetSlow"; // 이 오브젝트의 Animator(GetSlow.controller) 스테이트 이름

        private const float LANDING_DROP_HEIGHT = 0.5f; // 재배치 시 살짝 띄워야 접지->비접지->접지 전이가 생겨 OnLanded가 발생한다

        [Header("연출 타이밍")]
        [SerializeField] private float _getSlowAnimDuration = 4.5f; // GetSlow.anim 길이와 맞춰 조절 - 짧으면 연출이 끝나기 전에 플레이어가 복귀한다
        [SerializeField] private float _shiftImageDisplayDuration = 10f; // 착지 후 ShiftImage를 표출할 시간

        [Header("카메라 연출")]
        [SerializeField] private float _cameraFocusSize = Constants.Camera.FOCUS_ORTHOGRAPHIC_SIZE;
        [SerializeField] private float _cameraBlendTime  = Constants.Camera.DEFAULT_BLEND_TIME;

        [Header("파워업 오브 연출")]
        [SerializeField] private Transform _orbA; // 씬에 미리 배치된 powerup2_15647 - 새로 생성하지 않고 이 오브젝트를 직접 움직인다
        [SerializeField] private Transform _orbAGuide1; // HornAimRight1
        [SerializeField] private Transform _orbAGuide2; // HornAimRight2
        [SerializeField] private Transform _orbAGuide3; // HornAimRight3
        [SerializeField] private Transform _orbAGuide4; // HornAimRight4 - 최종 도착 지점
        [SerializeField] private Transform _orbB;        // 씬에 미리 배치된 powerup2_15648
        [SerializeField] private Transform _orbBGuide1; // HornAimLeft1
        [SerializeField] private Transform _orbBGuide2; // HornAimLeft2
        [SerializeField] private Transform _orbBGuide3; // HornAimLeft3
        [SerializeField] private Transform _orbBGuide4; // HornAimLeft4 - 최종 도착 지점
        [SerializeField] private float _powerupVfxDelay = 2.25f; // GetSlow 재생 시작 후 오브 연출까지 대기시간 (애니메이션 중반)
        [SerializeField] private float _orbFlightDuration = 2f; // 오브가 가이드 경로를 따라 천천히 날아오는 데 걸리는 시간
        [SerializeField] private float _orbArrivalPause = 0.15f; // 목표 도달 후 되돌아가기 전 정지 시간
        [SerializeField] private float _orbReturnDuration = 0.4f; // 왔던 경로를 되돌아가는 시간 - 흡수되는 느낌을 위해 빠르지만, 캐릭터 앞까지 돌아오는 마지막 구간이 보일 만큼은 확보

        [Header("시작 연출 (충격파 + 밝기 섬광)")]
        [SerializeField] private SpriteRenderer _shockwaveRenderer; // GetSlowShockwaveMat이 적용된 확산 파동 스프라이트 (평소 비활성)
        [SerializeField] private float _shockwaveDuration = 0.5f; // 파동이 퍼지며 사라지는 시간
        [SerializeField] private float _brightFlashDuration = 0.4f; // 밝기 섬광이 원래대로 가라앉는 시간
        [SerializeField] private float _brightFlashPeak = 1.5f; // 캐릭터/오브 색상 배율 최대치 (1보다 커야 밝아진다)

        private static readonly int PROGRESS = Shader.PropertyToID("_Progress");

        private Collider2D _trigger;
        private Animator   _getSlowAnimator; // 이 오브젝트에 등록된, GetSlow 연출 전용 Animator
        private GameObject _shiftImage;
        private GameObject _map2Gate;
        private bool       _unlocked;

        private SpriteRenderer _getSlowSpriteRenderer; // _getSlowAnimator가 그리는 캐릭터 스프라이트 - 밝기 섬광 대상
        private SpriteRenderer _orbASpriteRenderer;
        private SpriteRenderer _orbBSpriteRenderer;
        private TrailRenderer  _orbATrail;
        private TrailRenderer  _orbBTrail;
        private MaterialPropertyBlock _shockwaveProps;

        private PlayerController _pendingPlayer; // 착지 대기 중인 플레이어 (구독 해제용)

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            TryGetComponent(out _trigger);
            if (_trigger != null)
            {
                _trigger.isTrigger = true;
            }
            TryGetComponent(out _getSlowAnimator);
            if (_getSlowAnimator != null)
            {
                _getSlowAnimator.TryGetComponent(out _getSlowSpriteRenderer);
            }
            if (_orbA != null)
            {
                _orbA.TryGetComponent(out _orbASpriteRenderer);
                _orbA.TryGetComponent(out _orbATrail);
            }
            if (_orbB != null)
            {
                _orbB.TryGetComponent(out _orbBSpriteRenderer);
                _orbB.TryGetComponent(out _orbBTrail);
            }

            Transform parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            Transform shiftImage = parent.Find(SHIFT_IMAGE_NAME);
            Transform map2Gate   = parent.Find(MAP2_GATE_NAME);
            if (shiftImage != null)
            {
                _shiftImage = shiftImage.gameObject;
            }
            if (map2Gate != null)
            {
                _map2Gate = map2Gate.gameObject;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_unlocked || !other.TryGetComponent(out PlayerController playerController))
            {
                return;
            }

            _unlocked = true;
            if (_trigger != null)
            {
                _trigger.enabled = false; // 연출이 끝나기 전까지 코루틴이 살아있어야 하므로 오브젝트 자체는 끄지 않는다
            }
            SlowMotionController.UnlockAbility();

            if (_map2Gate != null)
            {
                _map2Gate.SetActive(true);
            }

            other.TryGetComponent(out PlayerAnimator playerAnimator);
            playerAnimator?.SetVisible(false); // 이 오브젝트의 GetSlow 연출이 그 자리를 대신 보여주는 동안 플레이어 스프라이트를 숨긴다
            if (_getSlowAnimator != null)
            {
                // 평소엔 SpriteRenderer를 꺼서 안 보이지만(뿔만 보임), GetSlow 애니메이션 재생 동안만 켜서 연출을 표출한다
                if (_getSlowSpriteRenderer != null)
                {
                    _getSlowSpriteRenderer.enabled = true;
                }
                _getSlowAnimator.Play(GET_SLOW_STATE, 0, 0f);
            }
            CameraManager.Instance?.Focus(transform, _cameraFocusSize, _cameraBlendTime);

            playerController.SetInteracting(true); // 연출 재생 동안 이동/점프/공격 입력과 물리를 잠근다
            StartCoroutine(CoPlayGetSlowSequence(playerController, playerAnimator));
        }

        private void OnDestroy()
        {
            if (_pendingPlayer != null)
            {
                _pendingPlayer.OnLanded -= HandlePlayerLanded;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        // 연출 대기 -> 플레이어를 능력 지점으로 재배치 + 재표시 -> 잠금 해제 -> 착지 대기 순으로 진행한다.
        // 오브+충격파 체인이 기본 애니메이션 길이보다 길어질 수 있으므로, 둘 중 더 오래 걸리는 쪽이 끝날 때까지 기다린다.
        private IEnumerator CoPlayGetSlowSequence(PlayerController playerController, PlayerAnimator playerAnimator)
        {
            Coroutine vfxSequence = StartCoroutine(CoPlayPowerupSequence());

            yield return new WaitForSeconds(_getSlowAnimDuration);

            // GetSlow 애니메이션이 끝났으니 다시 숨긴다 (재생 전/후엔 뿔만 보여야 한다)
            if (_getSlowSpriteRenderer != null)
            {
                _getSlowSpriteRenderer.enabled = false;
            }

            yield return vfxSequence; // 오브 이동 + 충격파가 애니메이션보다 길면 그만큼 더 대기

            CameraManager.Instance?.UnFocus(); // 연출 종료 - 플레이어 카메라로 복귀

            if (playerController == null)
            {
                yield break;
            }

            Vector3 landingStartPos = transform.position + (Vector3.up * LANDING_DROP_HEIGHT);
            playerController.SetPose(landingStartPos, Vector2.zero, false);
            playerAnimator?.SetVisible(true);
            playerController.SetInteracting(false); // 이동/물리 복구 - 이후 중력으로 자연스럽게 착지한다

            _pendingPlayer = playerController;
            playerController.OnLanded += HandlePlayerLanded;
        }

        private void HandlePlayerLanded()
        {
            if (_pendingPlayer != null)
            {
                _pendingPlayer.OnLanded -= HandlePlayerLanded;
                _pendingPlayer = null;
            }
            StartCoroutine(CoShowShiftImage());
        }

        private IEnumerator CoShowShiftImage()
        {
            if (_shiftImage != null)
            {
                _shiftImage.SetActive(true);
            }

            yield return new WaitForSeconds(_shiftImageDisplayDuration);

            if (_shiftImage != null)
            {
                _shiftImage.SetActive(false);
            }
            gameObject.SetActive(false); // 시퀀스가 완전히 끝난 뒤에야 트리거 오브젝트를 정리한다
        }

        // GetSlow 중반 대기 -> 오브 왕복 -> 충격파 순으로 순차 진행(모두 끝나야 완료로 취급된다).
        private IEnumerator CoPlayPowerupSequence()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, _powerupVfxDelay));
            yield return CoPlayPowerupOrbs();
            yield return CoPlayShockwaveAndFlash();
        }

        // 씬에 미리 배치된 오브 2개(powerup2_15647/15648)가 가이드4 지점에서 시작해 4->3->2->1 곡선으로 뻗어나갔다가,
        // 잠시 멈춘 뒤 1->2->3->4로 되돌아와 가이드4에서 흡수되는(사라지는) 단일 왕복 파워업 연출.
        private IEnumerator CoPlayPowerupOrbs()
        {
            if (_orbA == null && _orbB == null)
            {
                yield break;
            }

            Vector3[] pathA = BuildOrbPath(_orbAGuide1, _orbAGuide2, _orbAGuide3, _orbAGuide4);
            Vector3[] pathB = BuildOrbPath(_orbBGuide1, _orbBGuide2, _orbBGuide3, _orbBGuide4);

            // 경로의 끝(가이드4)으로 스냅한 뒤 트레일을 켠다 - 대기 위치에서 가이드4까지 이어지는 잔상이 남지 않게
            SnapOrbToPathEnd(_orbA, pathA);
            SnapOrbToPathEnd(_orbB, pathB);
            SetOrbTrailEmitting(true);

            yield return MoveOrbsAlongPath(pathA, pathB, _orbFlightDuration, fromEndToStart: true);   // 4 -> 3 -> 2 -> 1

            yield return new WaitForSeconds(_orbArrivalPause);

            yield return MoveOrbsAlongPath(pathA, pathB, _orbReturnDuration, fromEndToStart: false);  // 1 -> 2 -> 3 -> 4

            SetOrbTrailEmitting(false);

            // 캐릭터에게 흡수된 것으로 처리 - 가이드4에서 그대로 숨긴다
            _orbA?.gameObject.SetActive(false);
            _orbB?.gameObject.SetActive(false);
        }

        // 배치된 가이드(널이면 건너뜀)를 1,2,3,4 순서로 이어 붙인 웨이포인트 배열을 만든다.
        private static Vector3[] BuildOrbPath(Transform guide1, Transform guide2, Transform guide3, Transform guide4)
        {
            List<Vector3> points = new List<Vector3>();
            if (guide1 != null) points.Add(guide1.position);
            if (guide2 != null) points.Add(guide2.position);
            if (guide3 != null) points.Add(guide3.position);
            if (guide4 != null) points.Add(guide4.position);
            return points.Count > 0 ? points.ToArray() : null;
        }

        private static void SnapOrbToPathEnd(Transform orb, Vector3[] path)
        {
            if ((orb != null) && (path != null) && (path.Length > 0))
            {
                orb.position = path[path.Length - 1]; // 마지막 가이드(가이드4)
            }
        }

        private void SetOrbTrailEmitting(bool emitting)
        {
            if (_orbATrail != null)
            {
                _orbATrail.Clear(); // 켜든 끄든 항상 이전 잔상 제거
                _orbATrail.emitting = emitting;
            }
            if (_orbBTrail != null)
            {
                _orbBTrail.Clear();
                _orbBTrail.emitting = emitting;
            }
        }

        // GetSlow 연출 초입에 재생 - 확산되는 파동파 스프라이트(_Progress 0->1)와 캐릭터/오브 밝기 섬광을 함께 진행한다.
        private IEnumerator CoPlayShockwaveAndFlash()
        {
            if (_shockwaveRenderer != null)
            {
                _shockwaveRenderer.gameObject.SetActive(true);
            }
            _shockwaveProps ??= new MaterialPropertyBlock();

            float elapsed = 0f;
            while ((elapsed < _shockwaveDuration) || (elapsed < _brightFlashDuration))
            {
                elapsed += Time.deltaTime;

                if ((_shockwaveRenderer != null) && (elapsed <= _shockwaveDuration))
                {
                    float shockT = Mathf.Clamp01(elapsed / _shockwaveDuration);
                    _shockwaveRenderer.GetPropertyBlock(_shockwaveProps);
                    _shockwaveProps.SetFloat(PROGRESS, shockT);
                    _shockwaveRenderer.SetPropertyBlock(_shockwaveProps);
                }

                float flashT = Mathf.Clamp01(elapsed / _brightFlashDuration);
                float brightness = Mathf.Lerp(_brightFlashPeak, 1f, flashT); // 시작 즉시 확 밝아졌다가 서서히 원래 밝기로
                SetSpriteBrightness(_getSlowSpriteRenderer, brightness);
                SetSpriteBrightness(_orbASpriteRenderer, brightness);
                SetSpriteBrightness(_orbBSpriteRenderer, brightness);

                yield return null;
            }

            if (_shockwaveRenderer != null)
            {
                _shockwaveRenderer.gameObject.SetActive(false);
            }
            SetSpriteBrightness(_getSlowSpriteRenderer, 1f);
            SetSpriteBrightness(_orbASpriteRenderer, 1f);
            SetSpriteBrightness(_orbBSpriteRenderer, 1f);
        }

        private static void SetSpriteBrightness(SpriteRenderer renderer, float multiplier)
        {
            if (renderer == null)
            {
                return;
            }
            float alpha = renderer.color.a;
            renderer.color = new Color(multiplier, multiplier, multiplier, alpha);
        }

        // pathA/pathB(가이드 1~4)를 Catmull-Rom 곡선으로 통과시켜 오브를 이동시킨다.
        // fromEndToStart가 true면 경로 끝(가이드4)에서 시작해 시작점(가이드1)으로, false면 그 반대로 이동한다.
        private IEnumerator MoveOrbsAlongPath(Vector3[] pathA, Vector3[] pathB, float duration, bool fromEndToStart)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pathT = fromEndToStart ? (1f - t) : t; // pathT=0 => 가이드1, pathT=1 => 가이드4

                if ((_orbA != null) && (pathA != null))
                {
                    _orbA.position = EvaluateCatmullRomPath(pathA, pathT);
                }
                if ((_orbB != null) && (pathB != null))
                {
                    _orbB.position = EvaluateCatmullRomPath(pathB, pathT);
                }
                yield return null;
            }
        }

        // points를 순서대로 지나는 Catmull-Rom 스플라인 위 t(0~1) 지점의 좌표. 양 끝은 첫/마지막 점을 그대로 팬텀 제어점으로 사용(clamped).
        private static Vector3 EvaluateCatmullRomPath(Vector3[] points, float t)
        {
            if (points.Length == 1)
            {
                return points[0];
            }

            int segmentCount = points.Length - 1;
            float scaledT = Mathf.Clamp01(t) * segmentCount;
            int seg = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
            float localT = scaledT - seg;

            Vector3 p0 = points[Mathf.Max(seg - 1, 0)];
            Vector3 p1 = points[seg];
            Vector3 p2 = points[Mathf.Min(seg + 1, points.Length - 1)];
            Vector3 p3 = points[Mathf.Min(seg + 2, points.Length - 1)];

            float t2 = localT * localT;
            float t3 = t2 * localT;
            return 0.5f * ((2f * p1)
                + (p2 - p0) * localT
                + ((2f * p0) - (5f * p1) + (4f * p2) - p3) * t2
                + ((3f * p1) - p0 - (3f * p2) + p3) * t3);
        }
    }
}
