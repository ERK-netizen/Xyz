using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Hareket (P12). Rota emir anında A* ile hesaplanır. Her gün bütçe = hız × (½ + ½ × ikmal)
    /// eklenir; sıradaki adımın maliyeti bütçeye sığdıkça ilerlenir. Girilemeyen hücreye (düşman
    /// birliği, düşman şehri, tarafsız toprak) gelince rota iptal olur; saldırı ayrı emirdir.
    /// </summary>
    public static class MovementRules
    {
        public const string BlockedNote = "bilgi.savas.hareketEngellendi";

        public static ValidationResult CheckMove(WarfareContext ctx, HexPathfinder pathfinder, CountryId country, LandUnitId unitId, HexCoord target, List<HexId> path)
        {
            ValidationResult owned = UnitRules.CheckOwned(ctx, country, unitId, out LandUnit unit);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (unit.IsEmbarked)
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikGemide);
            }

            if (!ctx.World.Grid.Contains(target))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
            }

            PathResult result = pathfinder.FindPath(ctx.World.Grid.FromId(unit.Hex), target);
            if (!result.Found || result.Steps == 0)
            {
                return ValidationResult.Fail(WarfareErrorKeys.RotaYok);
            }

            path.Clear();
            for (int i = 1; i < result.Path.Count; i++)
            {
                path.Add(ctx.World.Grid.ToId(result.Path[i]));
            }

            return ValidationResult.Ok;
        }

        /// <summary>Rotayı birliğe yazar (saldırı emrini kaldırır).</summary>
        public static void SetPath(LandUnit unit, IReadOnlyList<HexId> path)
        {
            unit.PathList.Clear();
            for (int i = 0; i < path.Count; i++)
            {
                unit.PathList.Add(path[i]);
            }

            unit.AttackTarget = HexId.None;
        }

        /// <summary>Günlük saldırı emri verir (hareket emrini kaldırır).</summary>
        public static void SetAttackTarget(LandUnit unit, HexId target)
        {
            ClearOrders(unit);
            unit.AttackTarget = target;
        }

        public static void ClearOrders(LandUnit unit)
        {
            unit.PathList.Clear();
            unit.MoveBudget = Fixed64.Zero;
            unit.AttackTarget = HexId.None;
        }

        public static void AdvanceDay(WarfareContext ctx, IMovementCostModel movement, List<string> notes)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit unit = ctx.Warfare.Units.ValueAt(i);
                if (unit.PathList.Count == 0 || unit.IsEmbarked)
                {
                    unit.MoveBudget = Fixed64.Zero;
                    continue;
                }

                UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
                Fixed64 speed = type.SpeedPerDay * (Fixed64.Half + (Fixed64.Half * unit.Supply));
                unit.MoveBudget = Fixed64.Min(unit.MoveBudget + speed, type.SpeedPerDay * 2);
                while (unit.PathList.Count > 0)
                {
                    HexCell from = ctx.World.Cell(unit.Hex);
                    HexCell to = ctx.World.Cell(unit.PathList[0]);
                    if (!TryStepCost(ctx, movement, from, to, out Fixed64 cost) || !UnitQueries.CanEnter(ctx, unit.Owner, to))
                    {
                        ClearOrders(unit);
                        notes.Add(BlockedNote + " @ " + unit.Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    }

                    if (cost > unit.MoveBudget)
                    {
                        break;
                    }

                    unit.MoveBudget -= cost;
                    unit.Hex = to.Id;
                    unit.PathList.RemoveAt(0);
                }

                if (unit.PathList.Count == 0)
                {
                    unit.MoveBudget = Fixed64.Zero;
                }
            }
        }

        private static bool TryStepCost(WarfareContext ctx, IMovementCostModel movement, HexCell from, HexCell to, out Fixed64 cost)
        {
            for (int d = 0; d < HexDirections.Count; d++)
            {
                if (ctx.World.Grid.TryGetNeighbor(from.Coord, (HexDirection)d, out HexCoord n) && n == to.Coord)
                {
                    ctx.World.Distances.TryGetNeighborKm(from.Coord, (HexDirection)d, out Fixed64 km);
                    return movement.TryGetStepCost(from, to, km, out cost);
                }
            }

            cost = Fixed64.Zero;
            return false;
        }
    }
}
