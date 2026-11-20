using System;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// A* açık listesi: (f, sıra) anahtarlı ikili yığın. Eşit f değerlerinde ekleme sırası
    /// belirleyicidir; böylece sonuç platformdan ve koleksiyon sırasından bağımsızdır.
    /// Diziler aramalar arasında yeniden kullanılır (sıcak yolda tahsis yok).
    /// </summary>
    internal sealed class PathNodeHeap
    {
        private long[] _priority = new long[64];
        private long[] _sequence = new long[64];
        private int[] _node = new int[64];
        private int _count;
        private long _nextSequence;

        public int Count => _count;

        public void Clear()
        {
            _count = 0;
            _nextSequence = 0L;
        }

        public void Push(long priority, int node)
        {
            if (_count == _node.Length)
            {
                int capacity = checked(_node.Length * 2);
                Array.Resize(ref _priority, capacity);
                Array.Resize(ref _sequence, capacity);
                Array.Resize(ref _node, capacity);
            }

            int i = _count++;
            long sequence = _nextSequence++;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (!Less(priority, sequence, _priority[parent], _sequence[parent]))
                {
                    break;
                }

                Move(parent, i);
                i = parent;
            }

            _priority[i] = priority;
            _sequence[i] = sequence;
            _node[i] = node;
        }

        public int Pop()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("Boş yığından çekilemez.");
            }

            int top = _node[0];
            _count--;
            if (_count > 0)
            {
                long priority = _priority[_count];
                long sequence = _sequence[_count];
                int node = _node[_count];
                int i = 0;
                while (true)
                {
                    int left = (2 * i) + 1;
                    if (left >= _count)
                    {
                        break;
                    }

                    int child = left;
                    int right = left + 1;
                    if (right < _count && Less(_priority[right], _sequence[right], _priority[left], _sequence[left]))
                    {
                        child = right;
                    }

                    if (!Less(_priority[child], _sequence[child], priority, sequence))
                    {
                        break;
                    }

                    Move(child, i);
                    i = child;
                }

                _priority[i] = priority;
                _sequence[i] = sequence;
                _node[i] = node;
            }

            return top;
        }

        private static bool Less(long p1, long s1, long p2, long s2)
        {
            return p1 < p2 || (p1 == p2 && s1 < s2);
        }

        private void Move(int from, int to)
        {
            _priority[to] = _priority[from];
            _sequence[to] = _sequence[from];
            _node[to] = _node[from];
        }
    }
}
