using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Coğrafi kara hareketi (§3.4, §5.4, WAR-L11/L12, REG-002): hedef (<see cref="TargetRef"/>) bir
    /// ulaşım düğümüne çözülür, rota <see cref="LandAccessCostModel"/> ile A*'dan gelir (tarafsız toprak,
    /// deniz ve açılmamış demiryolu kapalı). Hareket çok günlüdür: birlik her gün tür hızı × parametre
    /// kadar maliyet harcar, kenar ortasında kalabilir (ara konum kayıtlıdır). Düşman birliği veya düşman
    /// kontrolündeki şehir bulunan düğüme yürüyerek girilmez; birlik bir önceki düğümde durur (temas) ve
    /// saldırı emri gerekir. Rota her gün yeniden denetlenir; kapanan kenarda rota iptal olur.
    /// </summary>
    public static class GeoMovementRules
    {
        public const string ContactNote = "bilgi.savas.temas";

        public static ValidationResult CheckMove(GeoWarfareContext ctx, TransportPathfinder finder, CountryId country, LandUnitId unitId, TargetRef target, List<TransportEdgeId> route)
        {
            route.Clear();
            ValidationResult owned = GeoUnitRules.CheckOwned(ctx, country, unitId, out LandUnit unit);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (unit.IsEmbarked)
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikGemide);
            }

            if (target.Kind == TargetKind.LandUnit && ctx.Warfare.Units.TryGetValue(new LandUnitId(target.EntityId), out LandUnit other)
                && ctx.IsHostile(country, other.Owner))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
            }

            TransportNodeId destination = GeoUnitQueries.ResolveNode(ctx, target);
            if (!destination.IsValid)
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
            }

            if (GeoUnitQueries.IsContested(ctx, destination, country))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
            }

            var cost = new LandAccessCostModel(ctx, country);
            TransportNodeId start = unit.Node;
            if (unit.RouteProgress > 0L && unit.RouteList.Count > 0)
            {
                // Kenar ortasındaki birlik önce bulunduğu kenarı tamamlar.
                ctx.World.Transport.TryGetEdge(unit.RouteList[0], out TransportEdge current);
                route.Add(current.Id);
                start = current.Other(unit.Node);
            }

            if (start == destination)
            {
                return route.Count > 0 ? ValidationResult.Ok : ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
            }

            Result<TransportPath> path = finder.FindPath(start, destination, cost);
            if (!path.IsSuccess)
            {
                route.Clear();
                return ValidationResult.Fail(WarfareErrorKeys.RotaYok);
            }

            for (int i = 0; i < path.Value.Edges.Count; i++)
            {
                route.Add(path.Value.Edges[i]);
            }

            return ValidationResult.Ok;
        }

        /// <summary>Doğrulanmış rotayı uygular (kenar ortasındaysa ilerleme korunur).</summary>
        public static void SetRoute(LandUnit unit, IReadOnlyList<TransportEdgeId> route)
        {
            bool keepProgress = unit.RouteProgress > 0L && unit.RouteList.Count > 0 && route.Count > 0 && route[0] == unit.RouteList[0];
            unit.RouteList.Clear();
            for (int i = 0; i < route.Count; i++)
            {
                unit.RouteList.Add(route[i]);
            }

            if (!keepProgress)
            {
                unit.RouteProgress = 0L;
            }

            unit.AttackTarget = TargetRef.None;
        }

        public static void Stop(LandUnit unit)
        {
            unit.RouteList.Clear();
            unit.RouteProgress = 0L;
            unit.AttackTarget = TargetRef.None;
        }

        /// <summary>Barış yerleşimi: birlik emirsiz olarak doğrudan düğüme konur (yol yürünmez).</summary>
        public static void Redeploy(LandUnit unit, TransportNodeId node)
        {
            Stop(unit);
            unit.Node = node;
        }

        /// <summary>Birliğin günlük hareket bütçesi (maliyet birimi).</summary>
        public static long DailyBudget(GeoWarfareContext ctx, UnitTypeDefinition type) =>
            (type.SpeedPerDay * ctx.Geo.MarchCostPerSpeedUnit).RoundToLong();

        /// <summary>P12: rotası olan kara birlikleri kimlik sırasıyla ilerler.</summary>
        public static void AdvanceDay(GeoWarfareContext ctx, List<string> notes)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit unit = ctx.Warfare.Units.ValueAt(i);
                if (unit.RouteList.Count == 0 || unit.IsEmbarked)
                {
                    continue;
                }

                Advance(ctx, unit, DailyBudget(ctx, ctx.Catalog.UnitTypes[unit.Type]), notes);
            }
        }

        private static void Advance(GeoWarfareContext ctx, LandUnit unit, long budget, List<string> notes)
        {
            var cost = new LandAccessCostModel(ctx, unit.Owner);
            while (unit.RouteList.Count > 0 && budget > 0L)
            {
                if (!ctx.World.Transport.TryGetEdge(unit.RouteList[0], out TransportEdge edge))
                {
                    Stop(unit);
                    return;
                }

                TransportNodeId nextId = edge.Other(unit.Node);
                ctx.World.Transport.TryGetNode(unit.Node, out TransportNode from);
                ctx.World.Transport.TryGetNode(nextId, out TransportNode next);
                if (!cost.TryGetCost(edge, from, next, out long step))
                {
                    Stop(unit);
                    return;
                }

                if (GeoUnitQueries.IsContested(ctx, nextId, unit.Owner))
                {
                    Stop(unit);
                    notes.Add(ContactNote + " @ " + unit.Id.Value.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                long need = step - unit.RouteProgress;
                if (budget < need)
                {
                    unit.RouteProgress += budget;
                    return;
                }

                budget -= need;
                unit.Node = nextId;
                unit.RouteProgress = 0L;
                unit.RouteList.RemoveAt(0);
            }
        }
    }
}
