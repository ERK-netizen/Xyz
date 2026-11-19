using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Logistics.Geo
{
    /// <summary>
    /// Bir ülkenin günlük ikmal ağı: düğüm başına seviye [0, 1] ve bölge başına en iyi düğüm seviyesi.
    /// Hesaplanır, kayda yazılmaz (aynı durumdan her zaman aynı ağ çıkar).
    /// </summary>
    public sealed class GeoSupplyNetwork
    {
        private readonly Dictionary<TransportNodeId, Fixed64> _nodes;
        private readonly Dictionary<RegionId, Fixed64> _regions;

        public GeoSupplyNetwork(CountryId country, Dictionary<TransportNodeId, Fixed64> nodes, Dictionary<RegionId, Fixed64> regions)
        {
            Country = country;
            _nodes = nodes;
            _regions = regions;
        }

        public CountryId Country { get; }

        public int ReachedNodeCount => _nodes.Count;

        public Fixed64 NodeLevel(TransportNodeId node) => _nodes.TryGetValue(node, out Fixed64 level) ? level : Fixed64.Zero;

        public Fixed64 RegionLevel(RegionId region) => _regions.TryGetValue(region, out Fixed64 level) ? level : Fixed64.Zero;
    }
}
