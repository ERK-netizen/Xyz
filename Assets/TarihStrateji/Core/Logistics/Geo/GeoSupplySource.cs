using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Logistics.Geo
{
    /// <summary>İkmal kaynağı: ulaşım düğümü ve kaynaktaki seviye (0, 1].</summary>
    public readonly struct GeoSupplySource
    {
        public GeoSupplySource(TransportNodeId node, Fixed64 level)
        {
            Node = node;
            Level = level;
        }

        public TransportNodeId Node { get; }

        public Fixed64 Level { get; }
    }
}
