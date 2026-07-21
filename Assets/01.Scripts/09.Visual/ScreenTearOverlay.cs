// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Minsung.Visual
{
    // 화면 균열(공간찢기) 연출 코디네이터 - 소스 카메라를 조각 수만큼 복제해 부채꼴이 아닌 '직선 자르기로 생긴 볼록다각형' 조각으로 나눠 보여준다
    // 시작은 화면 전체 사각형 1개 - _cutLines를 하나씩 그으면(ConvexPolygonSplitter) 그 선이 실제로 가로지르는 조각 하나만 둘로 쪼개져 조각 수가 늘어난다(종이 자르기와 동일)
    // 조각 역할: 면적이 가장 작은 조각=예비(아무 동작 없음), 가장 큰 조각=플레이어 추적용, 나머지=돌진(보스) 추적용 슬롯
    // 로스트아크 '영전'식 순차 파훼: 처음엔 전 조각이 원본과 동일(이음매 없음) -> 외부(패턴)가 조각 하나씩 "추적 시작 -> 고정"을 호출하면
    // 그 조각만 실시간으로 대상을 따라가다 멈춰서 그 자리에 얼어붙는다 - 반복될수록 화면이 시간차로 갈라져 보인다
    public class ScreenTearOverlay : MonoBehaviour
    {
        /****************************************
        *                Types
        ****************************************/

        [System.Serializable]
        public struct CutLine
        {
            [Tooltip("절단선 시작점(뷰포트 0~1)")]
            public Vector2 StartUV;
            [Tooltip("절단선 끝점(뷰포트 0~1) - 실제로는 이 두 점을 지나는 무한 직선으로 취급된다")]
            public Vector2 EndUV;
        }

        // 절단 결과 생긴 영역에 '이 영역은 이 지점을 비춘다'를 직접 지정 - SampleUV가 들어가는 영역의 카메라를 Point 위치에 정적으로 둔다
        [System.Serializable]
        public struct RegionCameraAnchor
        {
            [Tooltip("이 카메라 포인트가 담당할 영역을 고르는 대표 점(뷰포트 0~1) - 이 점이 들어가는 조각이 아래 Point를 비춘다")]
            public Vector2 SampleUV;
            [Tooltip("그 영역이 정적으로 비출 카메라 위치(씬에 빈 오브젝트로 배치). 돌진 지점에 두면 그 화면에 보스가 보인다")]
            public Transform Point;
        }

        /****************************************
        *                Fields
        ****************************************/

        [Header("참조")]
        [SerializeField] private Camera _sourceCamera;      // 복제 기준이 되는 게임플레이 카메라(줌도 그대로 상속)
        [SerializeField] private RectTransform _shardRoot;  // RectMask2D 달린 풀스크린 부모(Canvas 아래)

        [Header("절단선 (하나씩 순서대로 그어진다 - 총 5개 권장: 6조각)")]
        [SerializeField] private CutLine[] _cutLines;
        [SerializeField, Range(0.2f, 1f)] private float _rtResolutionScale = 0.75f; // RT 해상도 배율(성능)

        [Header("비-플레이어 조각 카메라 줌")]
        [Tooltip("플레이어 조각(PlayerRegionIndex)을 제외한 나머지(예비+돌진 슬롯) 조각 카메라의 orthographicSize 배율 - 1보다 크면 더 넓은 범위가 보인다(줌아웃)")]
        [SerializeField] private float _nonPlayerOrthoSizeMultiplier = 1.6f;

        [Header("분할 시 조각 어긋남")]
        [Tooltip("선이 지나 둘로 나뉜 조각을 서로 반대로 살짝 어긋내 분할을 눈에 보이게 한다(월드 유닛). 0이면 어긋남 없음(같은 시점)")]
        [SerializeField] private float _shatterOffset = 0.4f;

        [Header("영역별 카메라 포인트 (직접 지정)")]
        [Tooltip("최대 면적 조각은 플레이어를 추적한다. 그 외 조각은 여기서 지정한 지점(SampleUV로 매칭)을 정적으로 비춘다. 미지정 조각은 기본 시점 유지")]
        [SerializeField] private RegionCameraAnchor[] _cameraAnchors;

        [Header("균열 스트로크 (직선 절단 자국)")]
        [SerializeField] private Color _crackColor = new Color(0.05f, 0.05f, 0.05f, 0.85f);
        [SerializeField] private float _crackThickness = 3f; // px
        [SerializeField] private float _crackDrawDuration = 1.3f; // 선 하나가 A->B로 그어지는 시간(초) - 이 동안 그어지는 지점에서 파편이 튄다
        [SerializeField] private Color _cutHeadColor = new Color(0.85f, 0.97f, 1f, 1f); // 그어지는 선단(절단 헤드) 색 - 밝게
        [SerializeField] private float _cutHeadSize  = 26f; // 절단 헤드 크기(px)

        [Header("유리 파편 버스트 (그어지는 지점에서 튐)")]
        [SerializeField] private float _glassEmitInterval = 0.025f; // 그어지는 동안 파편을 방출하는 간격(초)
        [SerializeField] private int _glassShardsPerEmit   = 1;      // 한 방출당 큰 파편 수
        [SerializeField] private int _glassSparklesPerEmit = 2;      // 한 방출당 작은 스파클 수
        [SerializeField] private float _glassSpeedMin = 200f;
        [SerializeField] private float _glassSpeedMax = 550f; // 큰 파편이 화면 넓게 퍼지도록 상향
        [SerializeField] private float _glassLifeMin  = 0.6f;  // 큰 파편이 오래 보이도록 상향
        [SerializeField] private float _glassLifeMax  = 1.0f;

        [Header("흑백 와이프")]
        [SerializeField] private Material _shardMaterial;    // Minsung/ScreenTearGrayWipe - 비우면 Shader.Find로 런타임 생성
        [SerializeField] private float _wipeMaxRadius = 1.15f; // 흑백이 끝까지 번진 반경(uv 높이 단위)
        [SerializeField] private float _wipeFeather   = 0.12f; // 번짐 경계 부드러움
        [SerializeField] private float _wipeEdgeNoise = 0.06f; // 잉크 번짐 가장자리 노이즈

        [Header("연출 타이밍 (unscaled 초)")]
        [SerializeField] private float _grayInDuration = 0.7f;  // 흑백이 번지는 시간
        [SerializeField] private float _cutStaggerTime = 0.9f; // 절단선이 하나씩 그어지는 간격 - 화면이 서서히 갈라지는 느낌을 위해 넉넉하게(2026-07-21: 너무 빠르다는 피드백으로 0.5->0.9)

        [Header("프로토타입")]
        [SerializeField] private bool _activateOnStart; // 켜면 Start에서 바로 연출 시작(테스트용)

        private readonly List<Camera> _shardCameras = new List<Camera>();
        private readonly List<RenderTexture> _renderTextures = new List<RenderTexture>();
        private readonly List<ScreenTearShard> _shardGraphics = new List<ScreenTearShard>();
        private readonly List<Material> _runtimeMats = new List<Material>();
        private readonly List<List<Vector2>> _regions = new List<List<Vector2>>(); // 현재 살아있는 조각 폴리곤(선이 그어질 때마다 그 선이 지나는 조각이 분할된다)
        private List<Vector2> _fullRectPoly; // 화면 전체 사각형 - 균열선을 화면 끝까지 긋는 기준
        private float _currentWipeRadius;    // 흑백 진행도 - 새로 생기는 조각 머티리얼에 반영
        private bool[] _tracking;       // 조각별 - true면 LateUpdate가 매 프레임 target 위치를 따라간다
        private Transform[] _trackTargets;
        private int[] _dashRegionIndices; // 논리 돌진 슬롯(0..N-1) -> 실제 조각 인덱스
        private bool _active;

        private static readonly int WipeRadiusId = Shader.PropertyToID("_WipeRadius");
        private static readonly int GrayAmountId = Shader.PropertyToID("_GrayAmount");
        private static readonly int WipeFeatherId = Shader.PropertyToID("_WipeFeather");
        private static readonly int EdgeNoiseId = Shader.PropertyToID("_EdgeNoise");
        private static readonly int CenterUvId = Shader.PropertyToID("_CenterUV");

        public int ShardCount => _shardCameras.Count;
        public int PlayerRegionIndex { get; private set; } = -1;
        public int MiscRegionIndex { get; private set; } = -1;

        /// <summary> 흑백 물들기 + 절단선이 전부 그어지는 인트로 연출이 끝났는지 - 외부(패턴)가 이 시점까지 기다렸다가 다음 단계(돌진)를 시작하는 데 쓴다 </summary>
        public bool IsIntroComplete { get; private set; }

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            if (_activateOnStart)
            {
                Activate();
            }
        }

        private void OnDisable()
        {
            Deactivate();
        }

        // 추적 중인 조각 카메라를 매 프레임 target 위치로 갱신 - LateUpdate라 Cinemachine Brain(플레이어 카메라 갱신) 다음에 실행되어 한 프레임 밀리지 않는다
        private void LateUpdate()
        {
            if (!_active || (_tracking == null))
            {
                return;
            }
            for (int i = 0; i < _shardCameras.Count; ++i)
            {
                if (!_tracking[i] || (_trackTargets[i] == null) || (_shardCameras[i] == null))
                {
                    continue;
                }
                Vector3 pos = _shardCameras[i].transform.position;
                Vector3 targetPos = _trackTargets[i].position;
                _shardCameras[i].transform.position = new Vector3(targetPos.x, targetPos.y, pos.z);
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        // 연출 시작 - 절단선으로 조각을 계산하고 카메라/RT/조각을 생성한다. 처음엔 전부 소스 카메라와 동일한 시점(이음매 없음)
        public void Activate()
        {
            if (_active || _sourceCamera == null || _shardRoot == null || _cutLines == null || _cutLines.Length == 0)
            {
                return;
            }
            _active = true;
            Build();
        }

        // 연출 종료 - 생성물을 전부 정리한다(두 번 호출해도 안전)
        public void Deactivate()
        {
            if (!_active)
            {
                return;
            }
            _active = false;
            StopAllCoroutines();
            Teardown();
        }

        /// <summary> 플레이어 담당 조각(가장 큰 조각)의 카메라가 player를 매 프레임 실시간으로 따라가기 시작한다. Deactivate까지 계속 추적 </summary>
        public void BeginTrackPlayer(Transform player)
        {
            BeginTrackRegion(PlayerRegionIndex, player);
        }

        /// <summary> dashSlot(0..N-1)번 돌진 조각의 카메라가 target(보스)을 매 프레임 실시간으로 따라가기 시작한다 </summary>
        public void BeginTrackBoss(int dashSlot, Transform target)
        {
            BeginTrackRegion(DashRegionIndex(dashSlot), target);
        }

        /// <summary> dashSlot번 돌진 조각의 추적을 멈추고 현재 카메라 위치(보통 방금 도달한 End 지점)에 고정한다 </summary>
        public void FreezeShard(int dashSlot)
        {
            FreezeRegion(DashRegionIndex(dashSlot));
        }

        /// <summary> 실제 조각 인덱스(actualRegionIndex)의 흑백/컬러를 개별 전환한다(colored=true면 원색 복귀) </summary>
        public void SetShardColor(int actualRegionIndex, bool colored)
        {
            if (!IsValidIndex(actualRegionIndex) || (actualRegionIndex >= _runtimeMats.Count) || (_runtimeMats[actualRegionIndex] == null))
            {
                return;
            }
            _runtimeMats[actualRegionIndex].SetFloat(GrayAmountId, colored ? 0f : 1f);
        }

        private void BeginTrackRegion(int index, Transform target)
        {
            if (!IsValidIndex(index) || (target == null))
            {
                return;
            }
            _tracking[index] = true;
            _trackTargets[index] = target;
        }

        private void FreezeRegion(int index)
        {
            if (!IsValidIndex(index))
            {
                return;
            }
            _tracking[index] = false;
            _trackTargets[index] = null;
        }

        private int DashRegionIndex(int dashSlot)
        {
            if ((_dashRegionIndices == null) || (dashSlot < 0) || (dashSlot >= _dashRegionIndices.Length))
            {
                return -1;
            }
            return _dashRegionIndices[dashSlot];
        }

        private bool IsValidIndex(int index)
        {
            return _active && (_tracking != null) && (index >= 0) && (index < _tracking.Length);
        }

        private void Build()
        {
            IsIntroComplete = false;
            Rect rootRect = _shardRoot.rect;

            // 화면 전체 사각형(로컬 좌표) 1조각에서 시작 - 선이 그어질 때마다 이 조각이 하나씩 나뉜다
            _fullRectPoly = new List<Vector2>
            {
                new Vector2(rootRect.xMin, rootRect.yMin),
                new Vector2(rootRect.xMax, rootRect.yMin),
                new Vector2(rootRect.xMax, rootRect.yMax),
                new Vector2(rootRect.xMin, rootRect.yMax),
            };
            _regions.Clear();
            _regions.Add(new List<Vector2>(_fullRectPoly));
            _currentWipeRadius = 0f;

            CreateOneShard(_regions[0]); // 처음엔 화면 전체 1조각

            if (Application.isPlaying)
            {
                StartCoroutine(CoPlayIntro());
            }
        }

        // 흑백 물들기 -> 선을 하나씩 긋고(파편) 그을 때마다 그 선이 지나는 영역을 분할 -> 1.5초 대기 -> 다음 선
        private IEnumerator CoPlayIntro()
        {
            // 흑백 물들기 (아직 1조각)
            float elapsed = 0f;
            while (elapsed < _grayInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _grayInDuration);
                SetAllShardsWipeRadius(Mathf.Lerp(0f, _wipeMaxRadius, t));
                yield return null;
            }
            SetAllShardsWipeRadius(_wipeMaxRadius);

            WaitForSecondsRealtime waitStagger = new WaitForSecondsRealtime(_cutStaggerTime);
            Rect rootRect = _shardRoot.rect;
            for (int i = 0; i < _cutLines.Length; ++i)
            {
                Vector2 lineP   = UvToLocal(_cutLines[i].StartUV, rootRect);
                Vector2 lineEnd = UvToLocal(_cutLines[i].EndUV, rootRect);
                Vector2 lineDir = (lineEnd - lineP);
                if (lineDir.sqrMagnitude < 0.0001f)
                {
                    continue; // 시작/끝점이 같으면 무시(설정 실수 방지)
                }
                lineDir.Normalize();

                // 1) 화면 전체를 가로지르는 균열선을 그어지듯 애니메이션(파편 튐)
                if (ConvexPolygonSplitter.TryGetCrackSegment(_fullRectPoly, lineP, lineDir, out Vector2 a, out Vector2 b))
                {
                    yield return CoDrawCrackAnimated(a, b);
                }

                // 2) 선이 다 그어진 다음에 그 선이 지나는 영역을 분할
                ApplyCutLine(lineP, lineDir);

                // 3) 다음 선까지 대기(1.5초 등)
                yield return waitStagger;
            }

            // 모든 선 완료 - 역할 배정(면적 기준) + 추적 배열 준비 + 비-플레이어 줌 + 지정한 카메라 포인트 적용
            AssignRegionRoles(_regions);
            _tracking     = new bool[_regions.Count];
            _trackTargets = new Transform[_regions.Count];
            ApplyNonPlayerZoom();
            ApplyAuthoredCameraPoints();

            IsIntroComplete = true; // 외부(패턴)가 이 시점을 기다려 다음 단계(돌진)를 시작한다
        }

        // 최대 면적(플레이어) 조각을 제외한 각 조각을, SampleUV가 그 조각 안에 들어가는 지정 카메라 포인트 위치에 정적으로 둔다
        private void ApplyAuthoredCameraPoints()
        {
            if ((_cameraAnchors == null) || (_cameraAnchors.Length == 0))
            {
                return;
            }
            Rect rootRect = _shardRoot.rect;
            for (int i = 0; i < _shardCameras.Count; ++i)
            {
                if ((i == PlayerRegionIndex) || (_shardCameras[i] == null))
                {
                    continue; // 플레이어 조각은 플레이어를 추적하므로 건너뜀
                }
                for (int a = 0; a < _cameraAnchors.Length; ++a)
                {
                    if (_cameraAnchors[a].Point == null)
                    {
                        continue;
                    }
                    Vector2 sampleLocal = UvToLocal(_cameraAnchors[a].SampleUV, rootRect);
                    if (ConvexPolygonSplitter.ContainsPoint(_regions[i], sampleLocal))
                    {
                        Vector3 cur = _shardCameras[i].transform.position;
                        Vector3 target = _cameraAnchors[a].Point.position;
                        _shardCameras[i].transform.position = new Vector3(target.x, target.y, cur.z); // z(카메라 깊이)는 유지
                        break; // 첫 매칭만
                    }
                }
            }
        }

        // 한 선이 지나는 모든 조각을 둘로 나눈다(각 조각별로 개별 분할해 카메라/그래픽을 새로 만든다)
        // 나뉜 두 조각의 카메라를 절단선 수직 방향으로 서로 반대로 살짝 어긋내 분할이 눈에 보이게 한다
        private void ApplyCutLine(Vector2 lineP, Vector2 lineDir)
        {
            Vector2 normal = new Vector2(-lineDir.y, lineDir.x).normalized;

            List<List<Vector2>> newPolys = new List<List<Vector2>>();
            List<float> newSigns = new List<float>(); // +1 / -1 - 어긋내는 방향

            for (int r = _regions.Count - 1; r >= 0; --r)
            {
                List<List<Vector2>> single = new List<List<Vector2>> { _regions[r] };
                List<List<Vector2>> split = ConvexPolygonSplitter.SplitByLine(single, lineP, lineDir);
                if (split.Count == 2) // 이 조각을 실제로 가로지름 -> 둘로 분할
                {
                    DestroyShardAt(r);
                    newPolys.Add(split[0]); newSigns.Add(1f);
                    newPolys.Add(split[1]); newSigns.Add(-1f);
                }
            }

            for (int i = 0; i < newPolys.Count; ++i)
            {
                _regions.Add(newPolys[i]);
                CreateOneShard(newPolys[i]);

                // 방금 만든 조각의 카메라를 수직 방향으로 어긋냄
                Camera cam = _shardCameras[_shardCameras.Count - 1];
                if ((cam != null) && (_shatterOffset > 0f))
                {
                    cam.transform.position += new Vector3(normal.x, normal.y, 0f) * (_shatterOffset * newSigns[i]);
                }
            }
        }

        // 조각 하나 생성(카메라/RT/머티리얼/그래픽). _regions에는 호출 전에 이미 추가되어 있어야 한다
        private void CreateOneShard(List<Vector2> poly)
        {
            int index = _shardCameras.Count;

            Material mat = CreateRuntimeMaterial();
            mat.SetFloat(WipeRadiusId, _currentWipeRadius); // 현재 흑백 진행도 반영(이미 흑백이면 바로 흑백)
            _runtimeMats.Add(mat);

            Camera cam = CreateShardCamera(index); // CopyFrom(source) - 소스와 동일한 시점/줌으로 시작
            RenderTexture rt = CreateRenderTexture(index);
            cam.targetTexture = rt;
            _shardCameras.Add(cam);
            _renderTextures.Add(rt);

            ScreenTearShard shard = CreateShardGraphic(index);
            shard.material = mat;
            shard.Setup(rt, poly);
            _shardGraphics.Add(shard);
        }

        // 인덱스 r의 조각(카메라/RT/머티리얼/그래픽/폴리곤)을 제거한다
        private void DestroyShardAt(int r)
        {
            if (_shardCameras[r] != null)
            {
                _shardCameras[r].targetTexture = null;
                Destroy(_shardCameras[r].gameObject);
            }
            if (_renderTextures[r] != null)
            {
                _renderTextures[r].Release();
                Destroy(_renderTextures[r]);
            }
            if (_shardGraphics[r] != null)
            {
                Destroy(_shardGraphics[r].gameObject);
            }
            if (_runtimeMats[r] != null)
            {
                Destroy(_runtimeMats[r]);
            }
            _shardCameras.RemoveAt(r);
            _renderTextures.RemoveAt(r);
            _shardGraphics.RemoveAt(r);
            _runtimeMats.RemoveAt(r);
            _regions.RemoveAt(r);
        }

        // 플레이어 조각을 제외한 나머지(예비+돌진 슬롯) 카메라를 더 넓게 보이도록 줌아웃한다(orthographicSize 확대)
        private void ApplyNonPlayerZoom()
        {
            if (_nonPlayerOrthoSizeMultiplier <= 0f)
            {
                return;
            }
            for (int i = 0; i < _shardCameras.Count; ++i)
            {
                if ((i == PlayerRegionIndex) || (_shardCameras[i] == null) || !_shardCameras[i].orthographic)
                {
                    continue;
                }
                _shardCameras[i].orthographicSize *= _nonPlayerOrthoSizeMultiplier;
            }
        }

        // 뷰포트(0~1) 좌표를 shardRoot 로컬 좌표로 변환
        private Vector2 UvToLocal(Vector2 uv, Rect rootRect)
        {
            return new Vector2(
                (uv.x - _shardRoot.pivot.x) * rootRect.width + rootRect.x + (_shardRoot.pivot.x * rootRect.width),
                (uv.y - _shardRoot.pivot.y) * rootRect.height + rootRect.y + (_shardRoot.pivot.y * rootRect.height));
        }

        // 면적 가장 작은 조각=예비, 가장 큰 조각=플레이어, 나머지=돌진 슬롯(등장 순서대로)
        private void AssignRegionRoles(List<List<Vector2>> regions)
        {
            int minIdx = 0;
            int maxIdx = 0;
            float minArea = float.MaxValue;
            float maxArea = float.MinValue;

            for (int i = 0; i < regions.Count; ++i)
            {
                float area = ConvexPolygonSplitter.PolygonArea(regions[i]);
                if (area < minArea) { minArea = area; minIdx = i; }
                if (area > maxArea) { maxArea = area; maxIdx = i; }
            }

            MiscRegionIndex = minIdx;
            PlayerRegionIndex = maxIdx;

            List<int> dashIndices = new List<int>();
            for (int i = 0; i < regions.Count; ++i)
            {
                if ((i != minIdx) && (i != maxIdx))
                {
                    dashIndices.Add(i);
                }
            }
            _dashRegionIndices = dashIndices.ToArray();

#if UNITY_EDITOR
            if (_dashRegionIndices.Length != 4)
            {
                Debug.LogWarning($"[ScreenTearOverlay] 절단 결과 돌진 슬롯이 {_dashRegionIndices.Length}개입니다(기대 4개) - _cutLines 배치를 확인하세요.", this);
            }
#endif
        }

        private void SetAllShardsWipeRadius(float radius)
        {
            _currentWipeRadius = radius; // 새로 생기는 조각도 같은 흑백 진행도로 시작하도록 저장
            for (int i = 0; i < _runtimeMats.Count; ++i)
            {
                if (_runtimeMats[i] != null)
                {
                    _runtimeMats[i].SetFloat(WipeRadiusId, radius);
                }
            }
        }

        // 절단선 하나를 A->B로 그어지듯 애니메이션하고, 그어지는 지점(tip)에서 유리 파편을 튀긴다
        private IEnumerator CoDrawCrackAnimated(Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.001f)
            {
                yield break;
            }
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Vector2 dir = delta / length;

            // 균열선 - 시작점(A)을 피벗으로 두고 길이를 0에서 length로 늘려 A->B로 뻗어나가게 한다
            GameObject go = new GameObject("Crack", typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(_shardRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f); // 왼쪽 끝 기준 - 여기서부터 자라난다
            rt.anchoredPosition = a;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            rt.sizeDelta = new Vector2(0f, _crackThickness);
            rt.SetAsLastSibling();

            Image img = go.AddComponent<Image>();
            img.color = _crackColor;
            img.raycastTarget = false;

            // 그어지는 선단을 강조하는 밝은 절단 헤드 - tip을 따라 이동해 "여기가 지금 갈리는 지점"임을 명확히 보여준다
            GameObject headGo = new GameObject("CutHead", typeof(RectTransform));
            RectTransform headRt = headGo.GetComponent<RectTransform>();
            headRt.SetParent(_shardRoot, false);
            headRt.anchorMin = headRt.anchorMax = new Vector2(0.5f, 0.5f);
            headRt.pivot = new Vector2(0.5f, 0.5f);
            headRt.sizeDelta = new Vector2(_cutHeadSize, _cutHeadSize);
            headRt.anchoredPosition = a;
            headRt.SetAsLastSibling();
            Image headImg = headGo.AddComponent<Image>();
            headImg.color = _cutHeadColor;
            headImg.raycastTarget = false;

            // 그어지는 동안 tip에서 파편을 방출할 버스트 그래픽 하나(재사용)
            ScreenTearGlassBurst burst = CreateBurstGraphic();

            float duration = Mathf.Max(0.05f, _crackDrawDuration);
            float elapsed = 0f;
            float sinceEmit = _glassEmitInterval; // 첫 프레임에 바로 1회 방출
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curLen = length * t;
                rt.sizeDelta = new Vector2(curLen, _crackThickness);

                Vector2 tip = a + (dir * curLen);
                headRt.anchoredPosition = tip; // 절단 헤드를 tip으로 이동

                sinceEmit += Time.unscaledDeltaTime;
                if (sinceEmit >= _glassEmitInterval)
                {
                    sinceEmit = 0f;
                    // tip 주변 짧은 구간에 파편을 튀긴다 - 절단면을 따라 튀는 느낌
                    burst.Emit(tip - (dir * 6f), tip + (dir * 6f),
                        _glassShardsPerEmit, _glassSparklesPerEmit,
                        _glassSpeedMin, _glassSpeedMax, _glassLifeMin, _glassLifeMax);
                }
                yield return null;
            }
            rt.sizeDelta = new Vector2(length, _crackThickness);
            Destroy(headGo); // 다 그어지면 헤드 제거

            StartCoroutine(CoDestroyWhenFinished(burst));
        }

        // 풀스크린 유리 파편 버스트 그래픽 하나 생성(shardRoot 최상단)
        private ScreenTearGlassBurst CreateBurstGraphic()
        {
            GameObject go = new GameObject("GlassBurst", typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(_shardRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            ScreenTearGlassBurst burst = go.AddComponent<ScreenTearGlassBurst>();
            burst.raycastTarget = false;
            return burst;
        }

        // 파편이 전부 사라진 뒤 버스트 그래픽을 정리한다
        private IEnumerator CoDestroyWhenFinished(ScreenTearGlassBurst burst)
        {
            while ((burst != null) && !burst.IsFinished)
            {
                yield return null;
            }
            if (burst != null)
            {
                Destroy(burst.gameObject);
            }
        }

        // 흑백 와이프 셰이더 머티리얼을 런타임 인스턴스로 만든다(에셋 원본을 건드리지 않음) - 조각마다 독립 인스턴스
        private Material CreateRuntimeMaterial()
        {
            Material mat = _shardMaterial != null
                ? new Material(_shardMaterial)
                : new Material(Shader.Find("Minsung/ScreenTearGrayWipe"));

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            mat.SetVector(CenterUvId, new Vector4(0.5f, 0.5f, aspect, 0f));
            mat.SetFloat(WipeFeatherId, _wipeFeather);
            mat.SetFloat(EdgeNoiseId, _wipeEdgeNoise);
            mat.SetFloat(WipeRadiusId, 0f);
            mat.SetFloat(GrayAmountId, 1f); // 흑백 강도는 항상 최대, 번짐은 _WipeRadius로 제어(SetShardColor가 개별 예외 처리)
            return mat;
        }

        // 소스 카메라를 복제해 Base 카메라를 만든다 - 위치/줌 모두 CopyFrom으로 소스와 동일하게 시작(이음매 없음)
        private Camera CreateShardCamera(int index)
        {
            GameObject go = new GameObject("ShardCamera_" + index);
            go.transform.SetParent(_sourceCamera.transform.parent, false);

            Camera cam = go.AddComponent<Camera>();
            cam.CopyFrom(_sourceCamera);
            // 추적 중 보스가 레벨 밖(빈 공간)을 비추면 소스 배경색이 슬리버로 보인다 - 검정으로 덮어 어두운 씬에 묻히게 한다
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.rect = new Rect(0f, 0f, 1f, 1f);

            // URP - 단독 렌더하는 Base 카메라로 두고 포스트는 끈다(성능)
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderType = CameraRenderType.Base;
                data.renderPostProcessing = false;
            }
            return cam;
        }

        private RenderTexture CreateRenderTexture(int index)
        {
            int width  = Mathf.Max(1, Mathf.RoundToInt(Screen.width * _rtResolutionScale));
            int height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * _rtResolutionScale));

            RenderTexture rt = new RenderTexture(width, height, 16, RenderTextureFormat.Default);
            rt.name = "ScreenTearRT_" + index;
            rt.Create();
            return rt;
        }

        private ScreenTearShard CreateShardGraphic(int index)
        {
            GameObject go = new GameObject("Shard_" + index, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(_shardRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            ScreenTearShard shard = go.AddComponent<ScreenTearShard>();
            shard.raycastTarget = false;
            return shard;
        }

        private void Teardown()
        {
            for (int i = 0; i < _shardCameras.Count; ++i)
            {
                if (_shardCameras[i] != null)
                {
                    _shardCameras[i].targetTexture = null;
                    Destroy(_shardCameras[i].gameObject);
                }
            }
            for (int i = 0; i < _renderTextures.Count; ++i)
            {
                if (_renderTextures[i] != null)
                {
                    _renderTextures[i].Release();
                    Destroy(_renderTextures[i]);
                }
            }
            for (int i = 0; i < _shardGraphics.Count; ++i)
            {
                if (_shardGraphics[i] != null)
                {
                    Destroy(_shardGraphics[i].gameObject);
                }
            }
            for (int i = 0; i < _runtimeMats.Count; ++i)
            {
                if (_runtimeMats[i] != null)
                {
                    Destroy(_runtimeMats[i]);
                }
            }

            // Crack/GlassBurst 잔여 오브젝트 정리 - shardRoot 아래 이름으로 찾아 제거
            if (_shardRoot != null)
            {
                for (int i = _shardRoot.childCount - 1; i >= 0; --i)
                {
                    Transform child = _shardRoot.GetChild(i);
                    if (child.name.StartsWith("Crack") || child.name.StartsWith("GlassBurst") || child.name.StartsWith("CutHead"))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            _shardCameras.Clear();
            _renderTextures.Clear();
            _shardGraphics.Clear();
            _runtimeMats.Clear();
            _regions.Clear();
            _fullRectPoly = null;
            _tracking = null;
            _trackTargets = null;
            _dashRegionIndices = null;
            PlayerRegionIndex = -1;
            MiscRegionIndex = -1;
            IsIntroComplete = false;
        }

#if UNITY_EDITOR
        // 절단선을 Scene View에 표시 - 배치 확인용(실제 조각 모양은 런타임 분할 결과라 여기선 직선만 보여준다)
        private void OnDrawGizmosSelected()
        {
            if ((_cutLines == null) || (_shardRoot == null))
            {
                return;
            }
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _cutLines.Length; ++i)
            {
                Vector3 a = _shardRoot.TransformPoint(UvToLocal(_cutLines[i].StartUV, _shardRoot.rect));
                Vector3 b = _shardRoot.TransformPoint(UvToLocal(_cutLines[i].EndUV, _shardRoot.rect));
                Gizmos.DrawLine(a, b);
            }
        }
#endif
    }
}
