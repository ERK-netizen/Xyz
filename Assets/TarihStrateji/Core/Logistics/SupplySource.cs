using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Logistics
{
    /// <summary>İkmal kaynağı: hücre ve kaynaktaki ikmal seviyesi (0, 1].</summary>
    public readonly struct SupplySource
    {
        public SupplySource(HexCoord coord, Fixed64 level)
        {
            Coord = coord;
            Level = level;
        }

        public HexCoord Coord { get; }

        public Fixed64 Level { get; }
    }
}
