using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// İdari/operasyonel bölgelerin değişmez geometrisi ve mekânsal indeksi. Indeks 1°×1° kova
    /// ızgarasıdır; her kova bölge kimliklerini artan sırada tutar. Noktayı içeren bölge, içeren en
    /// küçük <see cref="RegionId"/>'dir (sınırdaki nokta deterministik olarak küçük kimliğe düşer).
    /// Hiçbir bölge içermiyorsa (deniz, sahipsiz alan) <see cref="RegionId.None"/>.
    /// </summary>
    public sealed class RegionAtlas
    {
        private const int CellE6 = 1_000_000;
        private const int LonCells = 360;

        private readonly Dictionary<RegionId, GeoMultiPolygon> _shapes = new Dictionary<RegionId, GeoMultiPolygon>();
        private readonly List<RegionId> _ordered = new List<RegionId>();
        private readonly Dictionary<int, List<RegionId>> _cells = new Dictionary<int, List<RegionId>>();

        public int Count => _ordered.Count;

        /// <summary>Kimlik sırasıyla bölgeler.</summary>
        public IReadOnlyList<RegionId> Regions => _ordered;

        public ValidationResult AddRegion(RegionId id, GeoMultiPolygon shape)
        {
            if (!id.IsValid || shape == null)
            {
                return ValidationResult.Fail(GeographyErrorKeys.BolgeGeometrisiYok);
            }

            if (_shapes.ContainsKey(id))
            {
                return ValidationResult.Fail(GeographyErrorKeys.BolgeTekrar);
            }

            ValidationResult valid = GeometryValidator.ValidateMultiPolygon(shape);
            if (!valid.IsValid)
            {
                return valid;
            }

            _shapes.Add(id, shape);
            InsertSorted(_ordered, id);
            GeoBounds b = shape.Bounds;
            for (int lat = CellIndex(b.MinLatE6, 90); lat <= CellIndex(b.MaxLatE6, 90); lat++)
            {
                for (int lon = CellIndex(b.MinLonE6, 180); lon <= CellIndex(b.MaxLonE6, 180); lon++)
                {
                    int key = (lat * LonCells) + lon;
                    if (!_cells.TryGetValue(key, out List<RegionId> bucket))
                    {
                        bucket = new List<RegionId>(2);
                        _cells.Add(key, bucket);
                    }

                    InsertSorted(bucket, id);
                }
            }

            return ValidationResult.Ok;
        }

        public bool TryGetShape(RegionId id, out GeoMultiPolygon shape) => _shapes.TryGetValue(id, out shape);

        public RegionId FindRegion(GeoPoint p)
        {
            int key = (CellIndex(p.LatitudeE6, 90) * LonCells) + CellIndex(p.LongitudeE6, 180);
            if (!_cells.TryGetValue(key, out List<RegionId> bucket))
            {
                return RegionId.None;
            }

            for (int i = 0; i < bucket.Count; i++)
            {
                if (_shapes[bucket[i]].Contains(p))
                {
                    return bucket[i];
                }
            }

            return RegionId.None;
        }

        public bool Contains(RegionId region, GeoPoint p) => _shapes.TryGetValue(region, out GeoMultiPolygon shape) && shape.Contains(p);

        private static int CellIndex(int valueE6, int offsetDegrees)
        {
            // Değer + kaydırma ≥ 0 olduğundan tamsayı bölme taban (floor) verir; +90° ve +180° uçları
            // son kovaya katılır.
            int cell = (valueE6 + (offsetDegrees * CellE6)) / CellE6;
            int max = (2 * offsetDegrees) - 1;
            return cell > max ? max : cell;
        }

        private static void InsertSorted(List<RegionId> list, RegionId id)
        {
            int index = list.BinarySearch(id);
            if (index < 0)
            {
                list.Insert(~index, id);
            }
        }
    }
}
