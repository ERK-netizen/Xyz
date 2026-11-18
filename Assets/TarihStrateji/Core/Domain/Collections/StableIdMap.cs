using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Domain.Collections
{
    /// <summary>
    /// Anahtarlarını daima artan <see cref="IStableId.Value"/> sırasında tutan sözlük.
    /// Simülasyonda sırası önemli her döngü bu yapı üzerinden yürür; <c>Dictionary</c>
    /// sırasına güvenilmez (CLAUDE.md §2.4). Numaralandırma tahsis yapmaz (struct enumerator).
    /// Okuma O(log n), sona ekleme O(1), araya ekleme/silme O(n).
    /// </summary>
    public sealed class StableIdMap<TKey, TValue>
        where TKey : struct, IStableId, IEquatable<TKey>, IComparable<TKey>
    {
        private TKey[] _keys;
        private TValue[] _values;
        private int _count;
        private int _version;

        public StableIdMap()
            : this(4)
        {
        }

        public StableIdMap(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _keys = new TKey[capacity];
            _values = new TValue[capacity];
        }

        public int Count => _count;

        /// <summary>Mevcut anahtarın değerini okur/yazar; yazmada anahtar yoksa ekler.</summary>
        public TValue this[TKey key]
        {
            get
            {
                int index = IndexOf(key);
                if (index < 0)
                {
                    throw new KeyNotFoundException(DomainErrorKeys.KimlikBulunamadi);
                }

                return _values[index];
            }

            set
            {
                RequireValid(key);
                int index = BinarySearch(key);
                if (index >= 0)
                {
                    _values[index] = value; // sıra değişmez: numaralandırma geçerli kalır
                }
                else
                {
                    InsertAt(~index, key, value);
                }
            }
        }

        /// <summary>Sıradaki konuma göre anahtar (0 = en küçük kimlik).</summary>
        public TKey KeyAt(int index)
        {
            CheckIndex(index);
            return _keys[index];
        }

        /// <summary>Sıradaki konuma göre değer (0 = en küçük kimlik).</summary>
        public TValue ValueAt(int index)
        {
            CheckIndex(index);
            return _values[index];
        }

        /// <summary>Sıradaki konumdaki değeri yerinde günceller; numaralandırmayı bozmaz.</summary>
        public void SetValueAt(int index, TValue value)
        {
            CheckIndex(index);
            _values[index] = value;
        }

        public void Add(TKey key, TValue value)
        {
            RequireValid(key);
            int index = BinarySearch(key);
            if (index >= 0)
            {
                throw new ArgumentException(DomainErrorKeys.KimlikZatenVar, nameof(key));
            }

            InsertAt(~index, key, value);
        }

        public bool TryAdd(TKey key, TValue value)
        {
            RequireValid(key);
            int index = BinarySearch(key);
            if (index >= 0)
            {
                return false;
            }

            InsertAt(~index, key, value);
            return true;
        }

        public bool ContainsKey(TKey key)
        {
            return IndexOf(key) >= 0;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = IndexOf(key);
            if (index < 0)
            {
                value = default;
                return false;
            }

            value = _values[index];
            return true;
        }

        public bool Remove(TKey key)
        {
            int index = IndexOf(key);
            if (index < 0)
            {
                return false;
            }

            _count--;
            if (index < _count)
            {
                Array.Copy(_keys, index + 1, _keys, index, _count - index);
                Array.Copy(_values, index + 1, _values, index, _count - index);
            }

            _keys[_count] = default;
            _values[_count] = default;
            _version++;
            return true;
        }

        public void Clear()
        {
            Array.Clear(_keys, 0, _count);
            Array.Clear(_values, 0, _count);
            _count = 0;
            _version++;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        private int IndexOf(TKey key)
        {
            return key.IsValid ? BinarySearch(key) : -1;
        }

        private int BinarySearch(TKey key)
        {
            int low = 0;
            int high = _count - 1;
            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                int cmp = _keys[mid].CompareTo(key);
                if (cmp == 0)
                {
                    return mid;
                }

                if (cmp < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return ~low;
        }

        private void InsertAt(int index, TKey key, TValue value)
        {
            if (_count == _keys.Length)
            {
                int newCapacity = _keys.Length == 0 ? 4 : checked(_keys.Length * 2);
                Array.Resize(ref _keys, newCapacity);
                Array.Resize(ref _values, newCapacity);
            }

            if (index < _count)
            {
                Array.Copy(_keys, index, _keys, index + 1, _count - index);
                Array.Copy(_values, index, _values, index + 1, _count - index);
            }

            _keys[index] = key;
            _values[index] = value;
            _count++;
            _version++;
        }

        private void CheckIndex(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static void RequireValid(TKey key)
        {
            if (!key.IsValid)
            {
                throw new ArgumentException(DomainErrorKeys.KimlikGecersiz, nameof(key));
            }
        }

        /// <summary>Artan kimlik sırasında, tahsissiz numaralandırıcı.</summary>
        public struct Enumerator
        {
            private readonly StableIdMap<TKey, TValue> _map;
            private readonly int _version;
            private int _index;

            internal Enumerator(StableIdMap<TKey, TValue> map)
            {
                _map = map;
                _version = map._version;
                _index = -1;
            }

            public KeyValuePair<TKey, TValue> Current =>
                new KeyValuePair<TKey, TValue>(_map._keys[_index], _map._values[_index]);

            public bool MoveNext()
            {
                if (_version != _map._version)
                {
                    throw new InvalidOperationException("StableIdMap numaralandırılırken değiştirildi.");
                }

                _index++;
                return _index < _map._count;
            }
        }
    }
}
