using System;

namespace TzspServer
{
    public class CircularBuffer<T>
    {
        private readonly object _locker;
        private readonly Item[] _items;
        private int _readIndex;
        private int _writeIndex;

        public CircularBuffer(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            _locker = new object();
            _items = new Item[size];
            _readIndex = 0;
            _writeIndex = 0;
        }

        public void Add(T item)
        {
            lock (_locker)
            {
                _items[_writeIndex] = Item.Create(item);
                _writeIndex += 1;
                if (_writeIndex == _items.Length)
                    _writeIndex = 0;
            }
        }

        public bool TryTake(out T item)
        {
            item = default;
            lock (_locker)
            {
                var arrayItem = _items[_readIndex];
                if (!arrayItem.IsSet)
                    return false;

                _items[_readIndex] = default;
                _readIndex += 1;
                if (_readIndex == _items.Length)
                    _readIndex = 0;

                item = arrayItem.Value;
                return true;
            }
        }

        private readonly struct Item
        {
            public readonly bool IsSet;
            public readonly T Value;

            private Item(bool isSet, T value)
            {
                IsSet = isSet;
                Value = value;
            }

            public static Item Create(T value) => new Item(true, value);
        }
    }
}
