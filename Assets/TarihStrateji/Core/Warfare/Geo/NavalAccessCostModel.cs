using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Filo rotası: yalnız deniz yolu kenarları; deniz düğümleri serbest, liman düğümüne yalnız kontrolü
    /// filonun ülkesinde olan liman şehrine girilir (düşman limanına giriş yok).
    /// </summary>
    public sealed class NavalAccessCostModel : ITransportCostModel
    {
        private readonly GeoWarfareContext _ctx;
        private readonly CountryId _country;
        private readonly EdgeKindCostModel _inner;

        public NavalAccessCostModel(GeoWarfareContext ctx, CountryId country)
        {
            _ctx = ctx;
            _country = country;
            _inner = new EdgeKindCostModel(ctx.Geo.SeaCostPerKm, true, ctx.Today);
        }

        public long MinCostPerKilometer => _inner.MinCostPerKilometer;

        public bool CanEnter(TransportNode node)
        {
            if (node.Kind == TransportNodeKind.Sea)
            {
                return true;
            }

            if (node.Kind != TransportNodeKind.Port)
            {
                return false;
            }

            City city = GeoUnitQueries.CityAt(_ctx, node.Id);
            return city != null && city.Controller == _country;
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
