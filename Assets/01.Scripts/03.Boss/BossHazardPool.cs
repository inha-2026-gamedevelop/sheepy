// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.Boss
{
    // 보스 패턴 공용 해저드 풀 - 낙뢰/장풍/레이저/안전구역이 공유하는 사각 판정·연출 오브젝트
    public class BossHazardPool
    {
        /****************************************
        *             Inner Types
        ****************************************/

        // 슬롯 하나의 상태 스냅샷 (리와인드 기록용, 힙 할당 없음)
        public readonly struct HazardRecord
        {
            public readonly bool    Active;
            public readonly Vector2 Position;
            public readonly Vector2 Scale;
            public readonly float   RotationDeg;
            public readonly Color   Color;
            public readonly bool    HasCollider;
            public readonly int     DamageHalves;
            public readonly float   StunDuration;
            public readonly bool    InstantKill;

            public static readonly HazardRecord Inactive = new HazardRecord();

            public HazardRecord(Vector2 position, Vector2 scale, float rotationDeg, Color color,
                                bool hasCollider, int damageHalves, float stunDuration, bool instantKill)
            {
                Active       = true;
                Position     = position;
                Scale        = scale;
                RotationDeg  = rotationDeg;
                Color        = color;
                HasCollider  = hasCollider;
                DamageHalves = damageHalves;
                StunDuration = stunDuration;
                InstantKill  = instantKill;
            }
        }

        private struct PoolSlot
        {
            public GameObject     Go;
            public SpriteRenderer Renderer;
            public BoxCollider2D  Collider;
            public DamageHazard   Hazard;
        }

        /****************************************
        *                Fields
        ****************************************/

        private static Sprite _squareSprite; // 공용 1x1 흰 사각 스프라이트 (지연 생성 후 재사용)

        private PoolSlot[] _slots;

        public int Size => (_slots != null) ? _slots.Length : 0;

        /****************************************
        *              Constructor
        ****************************************/

        public BossHazardPool(int size, string namePrefix)
        {
            _slots = new PoolSlot[size];
            for (int i = 0; i < size; ++i)
            {
                GameObject go = new GameObject($"{namePrefix}_{i}");
                go.SetActive(false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SquareSprite();
                _slots[i] = new PoolSlot { Go = go, Renderer = sr };
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 비활성 슬롯 하나를 지정 상태로 활성화. 전부 사용 중이면 -1 </summary>
        public int Alloc(Vector2 pos, Vector2 scale, Color color, bool hasCollider,
                         int damageHalves = Constants.Player.HALVES_PER_HEART,
                         float stunDuration = 0f, bool instantKill = false, float rotationDeg = 0f)
        {
            if (_slots == null)
            {
                return -1;
            }
            for (int i = 0; i < _slots.Length; ++i)
            {
                if ((_slots[i].Go != null) && (!_slots[i].Go.activeSelf))
                {
                    Configure(i, pos, scale, rotationDeg, color, hasCollider, damageHalves, stunDuration, instantKill);
                    return i;
                }
            }
            return -1;
        }

        /// <summary> 슬롯이 살아 있고 활성 상태인지. 풀 해제 후 잔여 코루틴의 안전 가드용 </summary>
        public bool IsActive(int i)
        {
            if ((_slots == null) || (i < 0) || (i >= _slots.Length) || (_slots[i].Go == null))
            {
                return false;
            }
            return _slots[i].Go.activeSelf;
        }

        /// <summary> 활성 슬롯 위치 이동 (낙뢰 낙하/장풍 상승 등 이동 패턴용) </summary>
        public void SetPosition(int i, Vector2 pos)
        {
            if (IsActive(i))
            {
                _slots[i].Go.transform.position = pos;
            }
        }

        /// <summary> 렌더러만 켜고 끈다 (판정은 유지). 슬로우 중에만 보이는 안전구역/경고 깜빡임용 </summary>
        public void SetVisible(int i, bool visible)
        {
            if (IsActive(i))
            {
                _slots[i].Renderer.enabled = visible;
            }
        }

        /// <summary> 슬롯 비활성화(풀 반환). 잔여 코루틴 대비 널/범위 가드 </summary>
        public void Free(int i)
        {
            if ((_slots == null) || (i < 0) || (i >= _slots.Length) || (_slots[i].Go == null))
            {
                return;
            }
            _slots[i].Go.SetActive(false);
            if (_slots[i].Collider != null)
            {
                _slots[i].Collider.enabled = false;
            }
            if (_slots[i].Hazard != null)
            {
                _slots[i].Hazard.enabled = false;
            }
        }

        /// <summary> 전체 슬롯 반환 </summary>
        public void FreeAll()
        {
            if (_slots == null)
            {
                return;
            }
            for (int i = 0; i < _slots.Length; ++i)
            {
                Free(i);
            }
        }

        /// <summary> 풀 파괴. 페이즈 Exit 등 소유자가 수명을 끝낼 때 호출 </summary>
        public void Dispose()
        {
            if (_slots == null)
            {
                return;
            }
            for (int i = 0; i < _slots.Length; ++i)
            {
                if (_slots[i].Go != null)
                {
                    Object.Destroy(_slots[i].Go);
                }
            }
            _slots = null;
        }

        /// <summary> 슬롯 상태를 스냅샷으로 캡처 (리와인드 기록) </summary>
        public HazardRecord Capture(int i)
        {
            if (!IsActive(i))
            {
                return HazardRecord.Inactive;
            }

            Transform tr = _slots[i].Go.transform;
            Vector3   ls = tr.localScale;
            bool  hasCollider   = (_slots[i].Collider != null) && (_slots[i].Collider.enabled);
            int   damageHalves  = (_slots[i].Hazard != null) ? _slots[i].Hazard.DamageHalves : 0;
            float stunDuration  = (_slots[i].Hazard != null) ? _slots[i].Hazard.StunDuration : 0f;
            bool  instantKill   = (_slots[i].Hazard != null) && (_slots[i].Hazard.InstantKill);

            return new HazardRecord(tr.position, new Vector2(ls.x, ls.y), tr.eulerAngles.z,
                                    _slots[i].Renderer.color, hasCollider, damageHalves, stunDuration, instantKill);
        }

        /// <summary> 스냅샷 내용대로 슬롯을 복원 (리와인드 역재생) </summary>
        public void Apply(int i, HazardRecord rec)
        {
            if ((_slots == null) || (i < 0) || (i >= _slots.Length) || (_slots[i].Go == null))
            {
                return;
            }
            if (rec.Active)
            {
                Configure(i, rec.Position, rec.Scale, rec.RotationDeg, rec.Color,
                          rec.HasCollider, rec.DamageHalves, rec.StunDuration, rec.InstantKill);
            }
            else
            {
                Free(i);
            }
        }

        // 슬롯을 지정 상태로 활성화. 콜라이더/해저드는 처음 필요할 때 1회만 추가(지연 생성)
        private void Configure(int i, Vector2 pos, Vector2 scale, float rotationDeg, Color color,
                               bool hasCollider, int damageHalves, float stunDuration, bool instantKill)
        {
            Transform tr = _slots[i].Go.transform;
            tr.position   = pos;
            tr.localScale = new Vector3(scale.x, scale.y, 1f);
            tr.rotation   = Quaternion.Euler(0f, 0f, rotationDeg);

            _slots[i].Renderer.color   = color;
            _slots[i].Renderer.enabled = true;
            _slots[i].Go.SetActive(true);

            if (hasCollider)
            {
                if (_slots[i].Collider == null)
                {
                    _slots[i].Collider           = _slots[i].Go.AddComponent<BoxCollider2D>();
                    _slots[i].Collider.isTrigger = true;
                }
                _slots[i].Collider.enabled = true;

                if (_slots[i].Hazard == null)
                {
                    _slots[i].Hazard = _slots[i].Go.AddComponent<DamageHazard>();
                }
                _slots[i].Hazard.enabled = true;
                _slots[i].Hazard.Configure(damageHalves, stunDuration, instantKill);
            }
            else
            {
                // 텔레그래프/안전구역은 보이기만 하고 피해 판정이 없다
                if (_slots[i].Collider != null)
                {
                    _slots[i].Collider.enabled = false;
                }
                if (_slots[i].Hazard != null)
                {
                    _slots[i].Hazard.enabled = false;
                }
            }
        }

        private static Sprite SquareSprite()
        {
            if (_squareSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                _squareSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            return _squareSprite;
        }
    }
}
