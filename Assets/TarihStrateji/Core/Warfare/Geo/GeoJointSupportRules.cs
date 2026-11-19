using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Ortak destek (§7, güç olarak eklenir): hedef düğüme yakın hava desteği / deniz taarruzu görevindeki
    /// kanatların kara saldırı gücü × hava üstünlüğü payı (taraf / (taraf + düşman) hava gücü, aynı düğüme
    /// görevli kanatlar) ve hedef düğüme deniz yoluyla bitişik düğümlerdeki filoların kıyı bombardımanı.
    /// Ülke çapında bonus yoktur; destek gerçek konumlu varlıklardan gelir.
    /// </summary>
    public static class GeoJointSupportRules
    {
        public static Fixed64 Support(GeoWarfareContext ctx, TransportNodeId node, CountryId side)
        {
            if (!side.IsValid)
            {
                return Fixed64.Zero;
            }

            Fixed64 total = Fixed64.Zero;
            Fixed64 strike = Fixed64.Zero;
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                if (w.Owner == side && (w.Mission == AirMission.GroundSupport || w.Mission == AirMission.NavalStrike)
                    && GeoAirRules.TargetNode(ctx, w.GeoMissionTarget) == node)
                {
                    AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[w.Type];
                    strike += CombatResolver.AirWingPower(w, type, type.GroundAttack);
                }
            }

            if (strike.IsPositive)
            {
                total += strike * AirShare(ctx, node, side);
            }

            IReadOnlyList<TransportEdge> edges = ctx.World.Transport.EdgesOf(node);
            for (int e = 0; e < edges.Count; e++)
            {
                if (edges[e].Kind != TransportEdgeKind.SeaLane)
                {
                    continue;
                }

                TransportNodeId sea = edges[e].Other(node);
                for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
                {
                    Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                    if (f.Owner == side && f.Node == sea && f.RouteProgress == 0L)
                    {
                        ShipTypeDefinition type = ctx.Catalog.ShipTypes[f.Type];
                        total += CombatResolver.FleetPower(f, type, type.ShoreBombardment);
                    }
                }
            }

            return total;
        }

        /// <summary>Düğüm üzerindeki hava gücü payı; iki taraf da yoksa 1.</summary>
        public static Fixed64 AirShare(GeoWarfareContext ctx, TransportNodeId node, CountryId side)
        {
            Fixed64 own = Fixed64.Zero;
            Fixed64 enemy = Fixed64.Zero;
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                if (w.Mission == AirMission.None || GeoAirRules.TargetNode(ctx, w.GeoMissionTarget) != node)
                {
                    continue;
                }

                AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[w.Type];
                Fixed64 power = CombatResolver.AirWingPower(w, type, type.AirAttack);
                if (w.Owner == side)
                {
                    own += power;
                }
                else if (ctx.IsHostile(side, w.Owner))
                {
                    enemy += power;
                }
            }

            Fixed64 sum = own + enemy;
            return sum.IsPositive ? Fixed64.MulDiv(own, Fixed64.One, sum) : Fixed64.One;
        }
    }
}
