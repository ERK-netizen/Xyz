using System;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.World.Geo
{
    /// <summary>
    /// Komşu hücreler arası km tablosu (CLAUDE.md §2.3). Komşu mesafesi yalnız satıra ve yöne
    /// bağlıdır (bir satırdaki tüm hücreler boylamca eşdeğerdir), bu yüzden tablo
    /// <c>Height × 6</c> boyutundadır ve harita kurulurken bir kez, deterministik tamsayı
    /// trigonometrisiyle hesaplanır. Kutup dışına çıkan yönlerin değeri yoktur.
    /// </summary>
    public sealed class HexDistanceTable
    {
        private readonly HexGrid _grid;
        private readonly long[] _kmRaw; // -1 = komşu yok

        public HexDistanceTable(HexGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _kmRaw = new long[grid.Height * HexDirections.Count];

            long minAny = long.MaxValue;
            long minRowChange = long.MaxValue;
            for (int row = 0; row < grid.Height; row++)
            {
                // Satırın temsilcisi: sütun 0 (tüm sütunlar boylamca eşdeğer).
                var origin = new HexCoord(0, row);
                Fixed64 lat = grid.CenterLatitude(row);
                Fixed64 lon = grid.CenterLongitude(origin);
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    HexDirection direction = (HexDirection)d;
                    long raw = -1L;
                    if (grid.TryGetNeighbor(origin, direction, out HexCoord n))
                    {
                        raw = GeoConstants.GreatCircleKm(lat, lon, grid.CenterLatitude(n.Row), grid.CenterLongitude(n)).Raw;
                        if (raw < minAny)
                        {
                            minAny = raw;
                        }

                        if (HexDirections.ChangesRow(direction) && raw < minRowChange)
                        {
                            minRowChange = raw;
                        }
                    }

                    _kmRaw[(row * HexDirections.Count) + d] = raw;
                }
            }

            MinimumNeighborKm = Fixed64.FromRaw(minAny);
            MinimumRowChangeKm = Fixed64.FromRaw(minRowChange == long.MaxValue ? 0L : minRowChange);
        }

        /// <summary>Haritadaki en kısa komşu mesafesi (A* alt sınırı için).</summary>
        public Fixed64 MinimumNeighborKm { get; }

        /// <summary>Satır değiştiren (çapraz) komşuların en kısası (A* enlem alt sınırı için).</summary>
        public Fixed64 MinimumRowChangeKm { get; }

        public HexGrid Grid => _grid;

        public bool TryGetNeighborKm(HexCoord from, HexDirection direction, out Fixed64 km)
        {
            if (!_grid.Contains(from))
            {
                throw new ArgumentOutOfRangeException(nameof(from), WorldErrorKeys.HucreDisinda);
            }

            long raw = _kmRaw[(from.Row * HexDirections.Count) + (int)direction];
            km = Fixed64.FromRaw(raw < 0L ? 0L : raw);
            return raw >= 0L;
        }

        /// <summary>İki hücre merkezi arası büyük çember mesafesi (hava menzili vb.).</summary>
        public Fixed64 GreatCircleKm(HexCoord a, HexCoord b)
        {
            return GeoConstants.GreatCircleKm(
                _grid.CenterLatitude(a.Row), _grid.CenterLongitude(a),
                _grid.CenterLatitude(b.Row), _grid.CenterLongitude(b));
        }
    }
}
