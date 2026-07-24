// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace Minsung.Player
{
    // 사망/RetireZone 복귀 시 캐릭터가 빛 덩어리 여러 개로 흩어지며 위로 떠오르다 사라지는 연출
    // CharaGlow와 동일한 관례 - 순수 비주얼 튜닝값이라 GameDB가 아닌 컴포넌트 로컬 SerializeField로 관리한다
    [AddComponentMenu("Minsung/Player/Death Burst Effect")]
    public class PlayerDeathBurstEffect : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("빛 덩어리 스프라이트")]
        [SerializeField] private Sprite _blobSprite; // Orb.png 등 원형 글로우 스프라이트 - 비우면 연출 생략

        [Header("개수/크기")]
        [SerializeField] private int   _lightCount       = 7;    // 빛 덩어리 개수
        [SerializeField] private float _blobScale        = 0.05f; // 스프라이트 크기 배율(오브와 동일 규모)
        [SerializeField] private float _startJitterRadius = 0.15f; // 시작 위치 랜덤 흩뿌림 반경(유닛)

        [Header("이동 속도")]
        [SerializeField] private float _riseSpeed   = 0.9f; // 위로 떠오르는 속도(유닛/초)
        [SerializeField] private float _spreadSpeed = 0.5f; // 옆으로 퍼지는 속도(유닛/초)

        [Header("지속시간 / 색상")]
        [SerializeField] private float _duration      = 1.5f; // 연출 유지 시간(초, 실시간)
        [SerializeField] private float _glowIntensity = 3.5f; // HDR 발광 강도(오브 발광과 동일 범위 0~8)
        [SerializeField] private Color _color         = new Color(1f, 0.97f, 0.85f, 1f); // 빛 덩어리 색(따뜻한 흰색)

        private static readonly int GLOW_COLOR     = Shader.PropertyToID("_GlowColor");
        private static readonly int GLOW_INTENSITY = Shader.PropertyToID("_GlowIntensity");

        private Material _blobMaterial; // 모든 블롭이 공유하는 런타임 머티리얼 인스턴스 (CharaGlow와 동일한 관례)
        private SpriteRenderer _bodyRenderer; // 정렬 레이어/순서 참고용 - 같은 오브젝트의 캐릭터 스프라이트
        private MaterialPropertyBlock _propBlock;
        private readonly List<Transform> _activeBlobs    = new List<Transform>();
        private readonly List<Vector3>   _blobVelocities = new List<Vector3>();

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            TryGetComponent(out _bodyRenderer);

            Shader shader = Shader.Find("Minsung/OrbGlow");
            if (shader != null)
            {
                _blobMaterial = new Material(shader);
            }
            _propBlock = new MaterialPropertyBlock();
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 빛 분해 연출 재생 - 완료까지 대기 가능한 코루틴. 필수 참조가 없으면 즉시 반환(연출 생략, 안전망). </summary>
        public IEnumerator PlayRoutine()
        {
            ClearBlobs(); // 직전 연출이 비정상 종료돼 남아있을 수 있는 블롭 정리(안전망)

            if ((_blobMaterial == null) || (_blobSprite == null))
            {
                yield break;
            }

            Vector3 origin = transform.position;

            for (int i = 0; i < _lightCount; ++i)
            {
                Transform blob = CreateBlob();
                blob.position = origin + (Vector3)(Random.insideUnitCircle * _startJitterRadius);

                // 방사형으로 퍼지되 위쪽 성분을 더해 전체적으로 위로 떠오르는 인상을 준다
                float angle = (((float)i / _lightCount) * Mathf.PI * 2f) + Random.Range(-0.3f, 0.3f);
                Vector3 velocity = new Vector3(
                    Mathf.Cos(angle) * _spreadSpeed,
                    _riseSpeed + (Mathf.Abs(Mathf.Sin(angle)) * _spreadSpeed * 0.4f),
                    0f);
                _blobVelocities.Add(velocity);
            }

            float elapsed = 0f;
            while (elapsed < _duration)
            {
                float dt = Time.unscaledDeltaTime; // 히트스톱/슬로우와 무관하게 일정한 길이로 재생
                elapsed += dt;
                float fade = 1f - Mathf.Clamp01(elapsed / _duration);

                for (int i = 0; i < _activeBlobs.Count; ++i)
                {
                    Transform blob = _activeBlobs[i];
                    if (blob == null)
                    {
                        continue;
                    }

                    blob.position += _blobVelocities[i] * dt;
                    SetBlobAlpha(blob, fade);
                }

                yield return null;
            }

            ClearBlobs();
        }

        // 블롭 1개 생성 - SpriteRenderer + 공유 OrbGlow 머티리얼, MaterialPropertyBlock으로 색/강도만 개별 지정
        private Transform CreateBlob()
        {
            GameObject go = new GameObject("DeathBurstBlob");
            go.transform.localScale = new Vector3(_blobScale, _blobScale, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite         = _blobSprite;
            sr.sharedMaterial = _blobMaterial;
            sr.color          = Color.white; // 실제 색/강도는 _GlowColor/_GlowIntensity가 담당(OrbController와 동일 관례) - alpha만 페이드에 사용

            if (_bodyRenderer != null)
            {
                sr.sortingLayerID = _bodyRenderer.sortingLayerID;
                sr.sortingOrder   = _bodyRenderer.sortingOrder + 1;
            }

            _propBlock.SetColor(GLOW_COLOR, _color);
            _propBlock.SetFloat(GLOW_INTENSITY, _glowIntensity);
            sr.SetPropertyBlock(_propBlock);

            _activeBlobs.Add(go.transform);
            return go.transform;
        }

        private void SetBlobAlpha(Transform blob, float alpha)
        {
            if (!blob.TryGetComponent(out SpriteRenderer sr))
            {
                return;
            }
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        private void ClearBlobs()
        {
            for (int i = 0; i < _activeBlobs.Count; ++i)
            {
                if (_activeBlobs[i] != null)
                {
                    Destroy(_activeBlobs[i].gameObject);
                }
            }
            _activeBlobs.Clear();
            _blobVelocities.Clear();
        }
    }
}
