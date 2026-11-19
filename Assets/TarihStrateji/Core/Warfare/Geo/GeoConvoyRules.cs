using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Logistics.Geo;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Konvoy ikmali (§5.6, §6): ülkenin işgal ettiği (kontrol ≠ sahip), ablukasız liman şehirleri, kendi ana
    /// limanlarından düşman filosu bulunmayan deniz yoluyla ulaşılabiliyorsa ve boşta nakliye kapasitesi
    /// varsa (liman başına 1 birim, şehir kimliği sırasıyla) <see cref="JointWarfareParameters.ConvoySupplyLevel"/>
    /// seviyesinde ikmal kaynağı olur. Boşta kapasite = ana limanda demirli, yüksüz filoların nakliye kapasitesi.
    /// </summary>
    public static class GeoConvoyRules
    {
        public static int IdleCapacity(GeoWarfareContext ctx, CountryId country)
        {
            int capacity = 0;
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.Owner != country || f.RouteProgress != 0L || f.RouteList.Count > 0 || GeoAmphibiousRules.CarriedBy(ctx, f.Id) > 0)
                {
                    continue;
                }

                City port = GeoUnitQueries.CityAt(ctx, f.Node);
                if (port != null && GeoNavalRules.IsHomePort(ctx, country, port))
                {
                    capacity += ctx.Catalog.ShipTypes[f.Type].TransportCapacity;
                }
            }

            return capacity;
        }

        public static void CollectSources(GeoWarfareContext ctx, CountryId country, List<GeoSupplySource> into)
        {
            int capacity = IdleCapacity(ctx, country);
            if (capacity <= 0)
            {
                return;
            }

            var homePorts = new List<TransportNodeId>();
            for (int i = 0; i < ctx.World.Cities.Count; i++)
            {
                ctx.World.TryGetCity(ctx.World.Cities[i], out City city);
                if (GeoNavalRules.IsHomePort(ctx, country, city))
                {
                    homePorts.Add(city.Node);
                }
            }

            var finder = new TransportPathfinder(ctx.World.Transport);
            var lanes = new ConvoyLaneModel(ctx, country);
            for (int i = 0; i < ctx.World.Cities.Count && capacity > 0; i++)
            {
                ctx.World.TryGetCity(ctx.World.Cities[i], out City city);
                if (!ctx.World.IsPort(city) || city.Controller != country || city.Owner == country || ctx.Warfare.IsBlockaded(city.Id))
                {
                    continue;
                }

                for (int p = 0; p < homePorts.Count; p++)
                {
                    Result<TransportPath> path = finder.FindPath(homePorts[p], city.Node, lanes);
                    if (path.IsSuccess)
                    {
                        into.Add(new GeoSupplySource(city.Node, ctx.Catalog.Joint.ConvoySupplyLevel));
                        capacity--;
                        break;
                    }
                }
            }
        }

        /// <summary>Konvoy rotası: deniz yolu, düşman filosu bulunan düğümler kapalı.</summary>
        private sealed class ConvoyLaneModel : ITransportCostModel
        {
            private readonly GeoWarfareContext _ctx;
            private readonly CountryId _country;
            private readonly NavalAccessCostModel _naval;

            public ConvoyLaneModel(GeoWarfareContext ctx, CountryId country)
            {
                _ctx = ctx;
                _country = country;
                _naval = new NavalAccessCostModel(ctx, country);
            }

            public long MinCostPerKilometer => _naval.MinCostPerKilometer;

            public bool TryGetCost(TransportEdge edge, TransportNode from, TransportNode to, out long cost)
            {
                if (GeoNavalRules.HasHostileFleetAt(_ctx, to.Id, _country))
                {
                    cost = 0L;
                    return false;
                }

                return _naval.TryGetCost(edge, from, to, out cost);
            }
        }
    }
}
