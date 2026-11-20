using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Logistics.Geo;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems.Geo;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.ReadModel
{
    /// <summary>
    /// Emir önizlemesi (yalnız okur): emir işleyicilerinin kullandığı kurallarla (<see cref="GeoMovementRules.CheckMove"/>,
    /// <see cref="GeoCombatRules.CheckAttack"/>) rota ve geçerlilik, günlük hareket bütçesiyle süre, P11 ile aynı
    /// kaynaklardan (ana şehirler + konvoy limanları) hedef düğümün ikmali, oyuncunun <see cref="ObservedWorld"/>
    /// temaslarından ve hedef şehrin garnizonundan düşman gücü. Gerçek dünyadaki görülmeyen birlikler
    /// önizlemeye girmez (savaş sisi). Temasların türü bilinmediğinden savunma gücü birliğin kendi türünün
    /// savunmasıyla tahmin edilir.
    /// </summary>
    public static class OrderPreviewProjection
    {
        public static OrderPreview Preview(GameState state, GameContent content, ulong unitId, TargetRef target, bool attack, List<GeoPoint> path)
        {
            path.Clear();
            CountryId player = state.PlayerCountry;
            GeoWarfareContext war = GeoWarfareContexts.From(state, content);
            var id = new LandUnitId(unitId);
            TransportNodeId destination;
            long totalCost = 0L;
            var route = new List<TransportEdgeId>();
            if (attack)
            {
                ValidationResult check = GeoCombatRules.CheckAttack(war, player, id, target, out destination);
                if (!check.IsValid)
                {
                    return OrderPreview.Invalid(check.ErrorKey);
                }
            }
            else
            {
                ValidationResult check = GeoMovementRules.CheckMove(war, new TransportPathfinder(state.World.Transport), player, id, target, route);
                if (!check.IsValid)
                {
                    return OrderPreview.Invalid(check.ErrorKey);
                }

                destination = TransportNodeId.None;
            }

            LandUnit unit = state.Warfare.Units[id];
            GeoWorld world = state.World;
            long[] perKm = war.Geo.LandCostPerKm;
            path.Add(ReadModelProjection.Position(world, unit.Node, unit.Route, unit.RouteProgress, perKm));
            if (attack)
            {
                world.Transport.TryGetNode(destination, out TransportNode node);
                path.Add(node.Location);
            }
            else
            {
                TransportNodeId at = unit.Node;
                for (int i = 0; i < route.Count; i++)
                {
                    world.Transport.TryGetEdge(route[i], out TransportEdge edge);
                    int kind = (int)edge.Kind;
                    totalCost += (edge.LengthMeters * (kind >= 0 && kind < perKm.Length ? perKm[kind] : 0L)) / 1000L;
                    at = edge.Other(at);
                    world.Transport.TryGetNode(at, out TransportNode next);
                    path.Add(next.Location);
                }

                if (unit.RouteProgress > 0L && unit.Route.Count > 0 && route.Count > 0 && route[0] == unit.Route[0])
                {
                    totalCost -= unit.RouteProgress;
                }

                destination = route.Count > 0 ? at : unit.Node;
            }

            UnitTypeDefinition type = war.Catalog.UnitTypes[unit.Type];
            long budget = GeoMovementRules.DailyBudget(war, type);
            int days = attack || totalCost <= 0L ? 0 : budget <= 0L ? int.MaxValue : (int)((totalCost + budget - 1L) / budget);

            Fixed64 own = CombatResolver.SidePower(war.Catalog, new[] { unit }, true);
            Fixed64 enemy = EnemyPower(state, war, player, destination, type, out bool estimated);
            return new OrderPreview(true, null, attack, days, SupplyAt(war, player, destination), own, enemy, estimated, Risk(own, enemy));
        }

        /// <summary>Güç oranına göre risk: düşman yoksa yok, ≥ 2 düşük, ≥ 1 orta, altı yüksek.</summary>
        public static int Risk(Fixed64 own, Fixed64 enemy)
        {
            if (!enemy.IsPositive)
            {
                return OrderPreview.RiskNone;
            }

            if (own >= enemy * 2)
            {
                return OrderPreview.RiskLow;
            }

            return own >= enemy ? OrderPreview.RiskMedium : OrderPreview.RiskHigh;
        }

        private static Fixed64 SupplyAt(GeoWarfareContext war, CountryId country, TransportNodeId node)
        {
            List<GeoSupplySource> sources = GeoSupplyRules.HomeSources(war.World, country);
            GeoConvoyRules.CollectSources(war, country, sources);
            GeoSupplyNetwork network = GeoSupplyRules.Compute(war.World, new EdgeKindCostModel(war.Geo.LandCostPerKm, false, war.Today),
                new ControlledRegionSupplyAccess(war.World), country, sources, war.Geo.SupplyRange);
            return network.NodeLevel(node);
        }

        private static Fixed64 EnemyPower(GameState state, GeoWarfareContext war, CountryId player, TransportNodeId node, UnitTypeDefinition proxy, out bool estimated)
        {
            estimated = false;
            Fixed64 power = Fixed64.Zero;
            if (state.Intelligence.Countries.TryGetValue(player, out ObservedWorld view))
            {
                for (int i = 0; i < view.Contacts.Count; i++)
                {
                    ContactReport c = view.Contacts[i];
                    if (c.Kind == SightingKind.LandUnit && c.LastNode == node && war.IsHostile(player, c.Owner))
                    {
                        power += c.EstimatedStrength * proxy.Defense;
                        estimated |= !c.IsExact;
                    }
                }
            }

            City city = GeoUnitQueries.CityAt(war, node);
            if (city != null && war.IsHostile(player, city.Controller))
            {
                power += war.Catalog.Parameters.GarrisonDefense * city.Fortification;
            }

            return power;
        }
    }
}
