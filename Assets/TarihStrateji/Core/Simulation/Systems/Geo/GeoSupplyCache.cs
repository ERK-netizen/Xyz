using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Logistics.Geo;

namespace TarihStrateji.Core.Simulation.Systems.Geo
{
    /// <summary>
    /// Günün ikmal ağları (P11'de hesaplanır, P14 bölge kontrolünde okunur). Türetilmiş veridir: kayda
    /// yazılmaz, her gün aynı durumdan aynı ağ çıkar (ADR: ikmal ızgarası kalıcı durum değildir).
    /// </summary>
    public sealed class GeoSupplyCache
    {
        public Dictionary<CountryId, GeoSupplyNetwork> Networks { get; } = new Dictionary<CountryId, GeoSupplyNetwork>();
    }
}
