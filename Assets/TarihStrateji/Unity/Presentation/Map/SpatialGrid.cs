using System.Collections.Generic;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Dokunma seçimi için düz kovalı mekânsal indeks (yalnız sunum). Noktalar sabit boyutlu hücrelere
    /// dağıtılır; en yakın sorgusu yalnız arama dairesinin değdiği hücreleri tarar. Eşit uzaklıkta küçük
    /// öğe dizini kazanır (sonuç ekleme sırasından bağımsız ve kararlıdır).
    /// </summary>
    public sealed class SpatialGrid
    {
        private readonly float _cellSize;
        private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();
        private readonly List<Vector2> _points = new List<Vector2>();

        public SpatialGrid(float cellSize)
        {
            _cellSize = cellSize > 0f ? cellSize : 1f;
        }

        public int Count => _points.Count;

        public void Clear()
        {
            _points.Clear();
            foreach (List<int> bucket in _cells.Values)
            {
                bucket.Clear();
            }
        }

        /// <summary>Noktayı ekler; dönüş öğe dizinidir (ekleme sırası).</summary>
        public int Add(Vector2 point)
        {
            int index = _points.Count;
            _points.Add(point);
            long key = Key(Cell(point.x), Cell(point.y));
            if (!_cells.TryGetValue(key, out List<int> bucket))
            {
                bucket = new List<int>(4);
                _cells.Add(key, bucket);
            }

            bucket.Add(index);
            return index;
        }

        /// <summary>Yarıçap içindeki en yakın öğenin dizini; yoksa −1.</summary>
        public int Nearest(Vector2 at, float radius)
        {
            int best = -1;
            float bestDistance = radius * radius;
            int x0 = Cell(at.x - radius);
            int x1 = Cell(at.x + radius);
            int y0 = Cell(at.y - radius);
            int y1 = Cell(at.y + radius);
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    if (!_cells.TryGetValue(Key(x, y), out List<int> bucket))
                    {
                        continue;
                    }

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        int index = bucket[i];
                        float d = (_points[index] - at).sqrMagnitude;
                        if (d < bestDistance || (d == bestDistance && (best < 0 || index < best)))
                        {
                            bestDistance = d;
                            best = index;
                        }
                    }
                }
            }

            return best;
        }

        private int Cell(float v) => Mathf.FloorToInt(v / _cellSize);

        private static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;
    }
}
