using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Logistics.Geo
{
    /// <summary>
    /// İkmal yalnız ülkenin fiilen kontrol ettiği bölgelerdeki kara düğümlerinden geçer; deniz düğümleri
    /// (konvoy) ayrıca konvoy kurallarıyla açılır, bu erişimde kapalıdır.
    /// </summary>
    public sealed class ControlledRegionSupplyAccess : IGeoSupplyAccess
    {
        private readonly GeoWorld _world;

        public ControlledRegionSupplyAccess(GeoWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public bool CanPass(CountryId country, TransportNode node) =>
            node.IsLand && _world.TryGetControl(node.Region, out RegionControl control) && control.Controller == country;
    }
}
