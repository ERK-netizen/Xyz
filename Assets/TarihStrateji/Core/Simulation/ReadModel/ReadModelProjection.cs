using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.ReadModel
{
    /// <summary>
    /// <see cref="GameState"/>'ten sunum görünümleri (Domain türleri). Harita altıgensizdir: bölgeler
    /// (sahip/kontrolcü + değişmez halkalar), şehirler (nokta) ve birlik konumları (düğüm ya da yol üstündeki
    /// ara nokta). Savaş sisi burada uygulanır: oyuncu kendi birliklerini ve kendi <see cref="ObservedWorld"/>
    /// temaslarını görür; Gözlemci modunda (oyuncu ülkesi yok) her şey görünür ve güçler kesindir.
    /// </summary>
    public static class ReadModelProjection
    {
        public static void Regions(GameState state, List<RegionView> into)
        {
            GeoWorld world = state.World;
            for (int i = 0; i < world.Regions.Count; i++)
            {
                world.TryGetControl(world.Regions[i], out RegionControl c);
                into.Add(new RegionView(c.Region, c.Owner, c.Controller, (byte)c.Stage));
            }
        }

        public static void RegionRings(GameState state, RegionId region, List<RegionRingView> into)
        {
            if (!state.World.Atlas.TryGetShape(region, out GeoMultiPolygon shape))
            {
                return;
            }

            for (int p = 0; p < shape.Count; p++)
            {
                GeoPolygon polygon = shape[p];
                into.Add(new RegionRingView(p, false, Copy(polygon.Outer)));
                for (int h = 0; h < polygon.HoleCount; h++)
                {
                    into.Add(new RegionRingView(p, true, Copy(polygon.Hole(h))));
                }
            }
        }

        public static void Cities(GameState state, List<CityView> into)
        {
            GeoWorld world = state.World;
            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City city);
                into.Add(new CityView(city.Id, city.Location, city.NameKey, city.DevelopmentLevel, city.Owner, city.Controller,
                    city.Siege == SiegeState.Besieged, world.IsPort(city)));
            }
        }

        public static bool TryCountry(GameState state, CountryId country, out CountryView view)
        {
            if (!state.Economy.Countries.TryGetValue(country, out CountryEconomy economy))
            {
                view = default;
                return false;
            }

            Fixed64 stability = state.Politics.Countries.TryGetValue(country, out CountryPolitics politics) ? politics.Stability : Fixed64.One;
            long population = 0L;
            GeoWorld world = state.World;
            for (int i = 0; i < world.Regions.Count; i++)
            {
                world.TryGetControl(world.Regions[i], out RegionControl region);
                if (region.Owner == country && state.Population.Regions.TryGetValue(region.Region, out RegionPopulation p))
                {
                    population += p.Total;
                }
            }

            int divisions = 0;
            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                if (state.Warfare.Units.ValueAt(i).Owner == country)
                {
                    divisions++;
                }
            }

            view = new CountryView(country, economy.Treasury, economy.TaxRate, stability, population, divisions, state.Diplomacy.IsAtAnyWar(country));
            return true;
        }

        public static void Units(GameState state, GameContent content, List<UnitView> into)
        {
            CountryId player = state.PlayerCountry;
            bool observer = !player.IsValid;
            GeoWorld world = state.World;
            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                LandUnit u = state.Warfare.Units.ValueAt(i);
                if (observer || u.Owner == player)
                {
                    GeoPoint at = Position(world, u.Node, u.Route, u.RouteProgress, content.GeoWarfare.LandCostPerKm);
                    into.Add(new UnitView(u.Id.Value, UnitViewKind.Land, u.Owner, at, u.Strength(content.Warfare.UnitTypes[u.Type]), true, u.Owner == player));
                }
            }

            for (int i = 0; i < state.Warfare.Fleets.Count; i++)
            {
                Fleet f = state.Warfare.Fleets.ValueAt(i);
                if (observer || f.Owner == player)
                {
                    GeoPoint at = Position(world, f.Node, f.Route, f.RouteProgress, content.GeoWarfare.SeaCostPerKm);
                    into.Add(new UnitView(f.Id.Value, UnitViewKind.Fleet, f.Owner, at, f.Strength(content.Warfare.ShipTypes[f.Type]), true, f.Owner == player));
                }
            }

            for (int i = 0; i < state.Warfare.AirWings.Count; i++)
            {
                AirWing w = state.Warfare.AirWings.ValueAt(i);
                if ((observer || w.Owner == player) && world.TryGetCity(w.Base, out City baseCity))
                {
                    into.Add(new UnitView(w.Id.Value, UnitViewKind.AirWing, w.Owner, baseCity.Location, w.Strength(content.Warfare.AirWingTypes[w.Type]), true, w.Owner == player));
                }
            }

            if (observer || !state.Intelligence.Countries.TryGetValue(player, out ObservedWorld view))
            {
                return;
            }

            for (int i = 0; i < view.Contacts.Count; i++)
            {
                ContactReport c = view.Contacts[i];
                if (!world.Transport.TryGetNode(c.LastNode, out TransportNode node))
                {
                    continue;
                }

                UnitViewKind kind = c.Kind == SightingKind.Fleet ? UnitViewKind.Fleet : c.Kind == SightingKind.AirWing ? UnitViewKind.AirWing : UnitViewKind.Land;
                into.Add(new UnitView(c.EntityId, kind, c.Owner, node.Location, c.EstimatedStrength, c.IsExact, false));
            }
        }

        /// <summary>
        /// Düğümdeki varlığın konumu; yol üstündeyse ilk kenarın üzerinde, harcanan maliyetin kenar maliyetine
        /// oranıyla doğrusal ara nokta (yalnız görüntü içindir; kurallar düğüm + ilerlemeyi kullanır).
        /// </summary>
        public static GeoPoint Position(GeoWorld world, TransportNodeId nodeId, IReadOnlyList<TransportEdgeId> route, long progress, long[] costPerKm)
        {
            if (!world.Transport.TryGetNode(nodeId, out TransportNode node))
            {
                return default;
            }

            if (progress <= 0L || route.Count == 0 || !world.Transport.TryGetEdge(route[0], out TransportEdge edge)
                || !world.Transport.TryGetNode(edge.Other(nodeId), out TransportNode next))
            {
                return node.Location;
            }

            int kind = (int)edge.Kind;
            long perKm = kind >= 0 && kind < costPerKm.Length ? costPerKm[kind] : 0L;
            long total = (edge.LengthMeters * perKm) / 1000L;
            if (total <= 0L)
            {
                return node.Location;
            }

            long done = progress < total ? progress : total;
            long lat = node.Location.LatitudeE6 + ((next.Location.LatitudeE6 - (long)node.Location.LatitudeE6) * done / total);
            long dLon = next.Location.LongitudeE6 - (long)node.Location.LongitudeE6;
            if (dLon > GeoPoint.HalfTurnE6)
            {
                dLon -= GeoPoint.FullTurnE6;
            }
            else if (dLon < -GeoPoint.HalfTurnE6)
            {
                dLon += GeoPoint.FullTurnE6;
            }

            long lon = node.Location.LongitudeE6 + (dLon * done / total);
            return new GeoPoint((int)lat, GeoPoint.NormalizeLongitude(lon));
        }

        private static GeoPoint[] Copy(GeoRing ring)
        {
            var points = new GeoPoint[ring.Count];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = ring[i];
            }

            return points;
        }
    }
}
