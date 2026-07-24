// System
using System.Collections;

// Unity
using UnityEngine;
using TMPro;

namespace Minsung.Boss
{
    // 피격 데미지 숫자를 띄운다 - 보스 피격은 보스 근처(노랑), 감정 반사 피해는 플레이어 근처(빨강)에 표시
    [AddComponentMenu("Minsung/Boss Damage Numbers")]
    public class BossDamageNumbers : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("참조 (비우면 부모에서 자동 탐색)")]
        [SerializeField] private MonoBehaviour  _healthSource; // IBossHittable 구현체(BossController/Boss2Health)
        [SerializeField] private Transform      _center;       // 보스 숫자가 뜨는 중심 - 비우면 이 오브젝트
        [SerializeField] private TMP_FontAsset  _font;         // 비우면 TMP 기본 폰트

        [Header("배치")]
        [Tooltip("데미지 숫자가 뜨는 영역 반경(월드 유닛) - 몬스터가 작으면 이 값을 줄인다. 플레이 중 변경도 즉시 반영")]
        [SerializeField] private float _spawnRadius  = 0.6f;  // 중심에서 이 반경 안 랜덤 위치
        [SerializeField] private float _riseDistance = 1.0f;  // 떠오르는 거리(월드 유닛)
        [SerializeField] private float _lifetime     = 0.7f;  // 표시 시간(초)

        [Header("모양")]
        [Tooltip("글자 폰트 크기 - 매 스폰마다 적용돼 플레이 중 변경도 즉시 반영")]
        [SerializeField] private float _fontSize     = 8f;
        [Tooltip("월드 스페이스 캔버스 스케일 - 폰트*스케일 ≈ 월드 높이. 전체 숫자 크기를 한 번에 줄일 때 사용. 플레이 중 변경도 즉시 반영")]
        [SerializeField] private float _canvasScale  = 0.03f;
        [SerializeField] private Color _color        = new Color(1f, 0.95f, 0.6f, 1f); // 보스 피격 - 밝은 노랑
        [SerializeField] private Color _reflectColor = new Color(1f, 0.35f, 0.3f, 1f); // 반사 피해(플레이어) - 빨강
        [SerializeField] private int   _poolSize     = 16;

        private IBossHittable _health;
        private Canvas        _canvas;
        private TMP_Text[]    _pool;
        private Coroutine[]   _running;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            ResolveHealth();
            if (_center == null)
            {
                _center = transform;
            }
            BuildPool();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDamaged        += HandleDamaged;
                _health.OnDamageReflected += HandleReflected;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDamaged        -= HandleDamaged;
                _health.OnDamageReflected -= HandleReflected;
            }
            ClearActiveNumbers(); // 주체가 죽어 비활성화되면 씬 루트 캔버스에 떠 있던 숫자가 얼어붙어 남는다 - 즉시 정리
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject); // 씬 루트로 분리했으므로 직접 정리
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        private void ResolveHealth()
        {
            if (_healthSource != null)
            {
                _health = _healthSource as IBossHittable;
            }
            if (_health == null)
            {
                _health = GetComponentInParent<IBossHittable>();
            }
        }

        // 보스 피격 - 보스 근처 랜덤 위치에 노랑 숫자
        private void HandleDamaged(float applied)
        {
            Vector2 offset = Random.insideUnitCircle * _spawnRadius;
            Spawn(_center.position + (Vector3)offset, Mathf.RoundToInt(applied).ToString(), _color);
        }

        // 감정 반사 - 플레이어가 대신 피해를 입은 위치에 빨강으로 하트 손실량 표시
        private void HandleReflected(Vector3 playerPos, int halves)
        {
            float hearts = halves * 0.5f;
            string text = "-" + (Mathf.Approximately(hearts % 1f, 0f) ? ((int)hearts).ToString() : hearts.ToString("0.0"));
            Vector2 offset = Random.insideUnitCircle * (_spawnRadius * 0.5f);
            Spawn(playerPos + (Vector3)offset + (Vector3.up * 0.5f), text, _reflectColor);
        }

        private void Spawn(Vector3 pos, string text, Color color)
        {
            int slot = FindFreeSlot();
            if (slot < 0)
            {
                return; // 풀 고갈 - 이번 숫자는 생략
            }
            _running[slot] = StartCoroutine(CoFloat(slot, pos, text, color));
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < _pool.Length; ++i)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    return i;
                }
            }
            return -1;
        }

        // 떠 있는 숫자를 모두 즉시 감추고 코루틴을 멈춘다 - 주체 비활성화(사망)로 CoFloat가 중단돼 숫자가 얼어붙는 잔여물을 막는다
        private void ClearActiveNumbers()
        {
            if (_pool == null)
            {
                return;
            }
            StopAllCoroutines();
            for (int i = 0; i < _pool.Length; ++i)
            {
                if (_pool[i] != null)
                {
                    _pool[i].gameObject.SetActive(false);
                }
                if (_running != null)
                {
                    _running[i] = null;
                }
            }
        }

        private IEnumerator CoFloat(int slot, Vector3 startPos, string text, Color color)
        {
            TMP_Text tmp = _pool[slot];
            if ((_font != null) && (tmp.font != _font))
            {
                tmp.font = _font; // 인스펙터에서 폰트를 바꾸면 즉시 반영된다
            }
            tmp.fontSize = _fontSize; // 매 스폰마다 적용 - 인스펙터에서 플레이 중 바꿔도 즉시 반영된다
            if (_canvas != null)
            {
                _canvas.transform.localScale = Vector3.one * _canvasScale; // 캔버스 스케일도 매 스폰마다 반영해 플레이 중 조절이 먹히게 한다
            }
            tmp.text = text;
            tmp.gameObject.SetActive(true);
            tmp.transform.position      = startPos;
            tmp.transform.localRotation = Quaternion.identity; // 보스 뒤집힘과 무관하게 항상 정방향

            float elapsed = 0f;
            while (elapsed < _lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _lifetime);

                // 떠오르기
                tmp.transform.position = startPos + (Vector3.up * (_riseDistance * t));

                // 팝(초반 살짝 커졌다) + 페이드아웃(후반)
                float pop = 1f + (0.25f * Mathf.Clamp01(1f - (t * 4f)));
                tmp.transform.localScale = Vector3.one * pop;

                Color c = color;
                c.a = 1f - Mathf.Clamp01((t - 0.5f) / 0.5f); // 후반 절반 동안 페이드
                tmp.color = c;

                yield return null;
            }

            tmp.gameObject.SetActive(false);
            _running[slot] = null;
        }

        private void BuildPool()
        {
            GameObject canvasGo = new GameObject("BossDamageNumbersCanvas");
            canvasGo.transform.SetParent(null, false); // 씬 루트 - 보스 스케일 뒤집힘을 상속받지 않는다
            canvasGo.transform.position   = Vector3.zero;
            canvasGo.transform.rotation   = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * _canvasScale;

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.WorldSpace;
            _canvas.sortingOrder = 60;

            _pool    = new TMP_Text[_poolSize];
            _running = new Coroutine[_poolSize];
            for (int i = 0; i < _poolSize; ++i)
            {
                GameObject numberGo = new GameObject("DamageNumber_" + i);
                numberGo.transform.SetParent(canvasGo.transform, false);

                TextMeshProUGUI tmp = numberGo.AddComponent<TextMeshProUGUI>();
                if (_font != null)
                {
                    tmp.font = _font;
                }
                tmp.fontSize           = _fontSize;
                tmp.alignment          = TextAlignmentOptions.Center;
                tmp.color              = _color;
                tmp.raycastTarget      = false;
                tmp.enableWordWrapping = false;

                RectTransform rt = tmp.rectTransform;
                rt.sizeDelta = new Vector2(6f, 2f); // 캔버스 로컬 단위 - 넉넉히

                numberGo.SetActive(false);
                _pool[i] = tmp;
            }
        }
    }
}
