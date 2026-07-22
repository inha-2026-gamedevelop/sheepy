// Unity
using UnityEngine;

using Minsung.Common;

namespace Minsung.Item
{
    // 포션 드랍 오브젝트 풀. 생성/파괴 대신 슬롯 활성/비활성으로 관리한다.
    public class PotionPickupPool
    {
        /****************************************
        *             Inner Types
        ****************************************/

        private struct PoolSlot
        {
            public GameObject     Go;
            public SpriteRenderer Renderer;
        }

        /****************************************
        *                Fields
        ****************************************/

        private const string POTION_SPRITE_PATH = "Item/Potion";

        private static Sprite _potionSprite;

        private static Sprite _placeholderSprite; // 전용 아트 확보 전 임시 스프라이트 (지연 생성 후 재사용)

        private readonly Color _color = new Color(1f, 0.35f, 0.45f); // 포션 전용 아트 확보 전 임시 색상 (붉은빛)

        private PoolSlot[] _slots;

        public int Size => (_slots != null) ? _slots.Length : 0;

        /****************************************
        *              Constructor
        ****************************************/

        public PotionPickupPool(int size)
        {
            _slots = new PoolSlot[size];
            for (int i = 0; i < size; ++i)
            {
                GameObject go = new GameObject($"PotionPickup_{i}");
                go.SetActive(false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PotionSprite();
                sr.color  = Color.white;
                ManagedObjectManager.Register(EManagedObjectType.PotionPickup, sr);
                _slots[i] = new PoolSlot { Go = go, Renderer = sr };
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 비활성 슬롯 하나를 찾아 지정 위치에 활성화. 전부 사용 중이면 -1 </summary>
        public int TryAlloc(Vector2 position)
        {
            if (_slots == null)
            {
                return -1;
            }
            for (int i = 0; i < _slots.Length; ++i)
            {
                if ((_slots[i].Go != null) && (!_slots[i].Go.activeSelf))
                {
                    ActivateAt(i, position);
                    return i;
                }
            }
            return -1;
        }

        /// <summary> 지정 슬롯 인덱스를 직접 활성화 (리와인드 복원용 - 어느 슬롯인지 이미 알고 있는 경우) </summary>
        public void ActivateAt(int i, Vector2 position)
        {
            if (!IsValidIndex(i))
            {
                return;
            }
            _slots[i].Go.transform.position = position;
            _slots[i].Go.SetActive(true);
        }

        /// <summary> 슬롯이 살아 있고 활성 상태인지 </summary>
        public bool IsActive(int i)
        {
            return IsValidIndex(i) && _slots[i].Go.activeSelf;
        }

        public Vector2 GetPosition(int i)
        {
            return IsActive(i) ? (Vector2)_slots[i].Go.transform.position : Vector2.zero;
        }

        public void SetPosition(int i, Vector2 position)
        {
            if (IsActive(i))
            {
                _slots[i].Go.transform.position = position;
            }
        }

        /// <summary> 슬롯 비활성화(풀 반환) </summary>
        public void Free(int i)
        {
            if (IsValidIndex(i))
            {
                _slots[i].Go.SetActive(false);
            }
        }

        /// <summary> 풀 파괴. 소유자(PotionManager) 파괴 시 호출 </summary>
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
                    ManagedObjectManager.Unregister(_slots[i].Renderer);
                    Object.Destroy(_slots[i].Go);
                }
            }
            _slots = null;
        }

        private bool IsValidIndex(int i)
        {
            return (_slots != null) && (i >= 0) && (i < _slots.Length) && (_slots[i].Go != null);
        }

        private static Sprite PotionSprite()
        {
            if (_potionSprite == null)
            {
                _potionSprite = Resources.Load<Sprite>(POTION_SPRITE_PATH);
            }

            return (_potionSprite != null) ? _potionSprite : PlaceholderSprite();
        }

        private static Sprite PlaceholderSprite()
        {
            if (_placeholderSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                _placeholderSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            return _placeholderSprite;
        }
    }
}
