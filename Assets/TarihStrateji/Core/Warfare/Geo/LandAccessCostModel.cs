using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Kara kuvveti rotası: kenar türü maliyeti + giriş izni. Birlik yalnız kendi kontrolündeki veya
    /// savaşta olduğu ülkenin kontrolündeki bölgelere girer; tarafsız ülke toprağı (geçiş hakkı yok)
    /// kapalıdır. Deniz düğümü kara kuvvetine kapalıdır (sudan yürüyüş yok, WAR-L12).
    /// </summary>
    public sealed class LandAccessCostModel : ITransportCostModel
    {
        private readonly GeoWarfareContext _ctx;
        private readonly CountryId _country;
        private readonly EdgeKindCostModel _inner;

        public LandAccessCostModel(GeoWarfareContext ctx, CountryId country)
        {
            _ctx = ctx;
            _country = country;
            _inner = new EdgeKindCostModel(ctx.Geo.LandCostPerKm, false, ctx.Today);
        }

        public long MinCostPerKilometer => _inner.MinCostPerKilometer;

        public bool CanEnter(TransportNode node)
        {
            if (!node.IsLand || !_ctx.World.TryGetControl(node.Region, out RegionControl control))
            {
                return false;
            }

            return control.Controller == _country || !control.Controller.IsValid || _ctx.IsHostile(_country, control.Controller);
        }

        public bool TryGetCost(TransportEdge edge, TransportNode from, TransportNode to, out long cost)
        {
            if (!CanEnter(to))
            {
                cost = 0L;
                return false;
            }

            return _inner.TryGetCost(edge, from, to, out cost);
        }
    }
}
