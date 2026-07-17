// System
using System.Collections.Generic;

namespace Minsung.TimeSystem
{
    // 고정 용량 순환 버퍼. 가득 차면 가장 오래된 걸 덮어쓴다.
    public class RingBuffer<T>
    {
        /****************************************
        *                Fields
        ****************************************/

        private readonly T[] _items;
        private int _head;
        private int _count;

        public int Count    => _count;
        public int Capacity => _items.Length;

        /****************************************
        *                Methods
        ****************************************/

        public RingBuffer(int capacity)
        {
            _items = new T[capacity];
        }

        /// <summary> 항목 추가. 가득 찼으면 가장 오래된 항목을 덮어쓴다. </summary>
        public void Push(T item)
        {
            _items[_head] = item;
            _head = (_head + 1) % _items.Length;
            if (_count < _items.Length)
            {
                ++_count;
            }
        }

        // 오래된 것 -> 최신 순서로 dest에 전체 복사 (분신 클립 추출용).
        // 호출자가 재사용하는 리스트를 넘겨 리와인드마다 새 리스트 할당이 생기지 않게 한다.
        public void CopyOrderedTo(List<T> dest)
        {
            dest.Clear();
            if (dest.Capacity < _count)
            {
                dest.Capacity = _count;
            }

            int start = ((_head - _count + _items.Length) % _items.Length);
            for (int i = 0; i < _count; ++i)
            {
                dest.Add(_items[(start + i) % _items.Length]);
            }
        }

        // 오래된 것 -> 최신 순서로 orderedIndex번째 항목을 반환 (할당 없음).
        public bool TryGetOrdered(int orderedIndex, out T item)
        {
            if (orderedIndex < 0 || orderedIndex >= _count)
            {
                item = default;
                return false;
            }
            int start = ((_head - _count + _items.Length) % _items.Length);
            item = _items[(start + orderedIndex) % _items.Length];
            return true;
        }

        /// <summary> 버퍼 비우기. 배열은 유지하므로 추가 할당이 없다. </summary>
        public void Clear()
        {
            _head  = 0;
            _count = 0;
        }
    }
}
