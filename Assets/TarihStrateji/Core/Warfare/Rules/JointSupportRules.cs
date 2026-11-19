using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Ortak destek: bir hücredeki muharebeye bir tarafın hava ve deniz katkısı (güç olarak
    /// eklenir, aynı <see cref="CombatResolver"/> çözer).
    /// <list type="bullet">
    /// <item>Hava: tarafın o hücreyi hedefleyen yakın hava desteği (kara hücresi) veya deniz taarruzu
    /// (deniz hücresi) kanatlarının yer saldırı gücü × hava üstünlüğü payı.</item>
    /// <item>Deniz: kara hücresine komşu deniz hücrelerindeki tarafın filolarının kıyı bombardımanı.</item>
    /// </list>
    /// Hava üstünlüğü payı = tarafın hücre üzerindeki hava gücü / (taraf + düşman); ikisi de yoksa 1.
    /// </summary>
    public static class JointSupportRules
    {
        public static Fixed64 Support(WarfareContext ctx, HexCell target, CountryId side)
        {
            if (!side.IsValid)
            {
                return Fixed64.Zero;
            }

            Fixed64 air = Fixed64.Zero;
            AirMission wanted = target.IsWater ? AirMission.NavalStrike : AirMission.GroundSupport;
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                if (w.Owner == side && w.Mission == wanted && w.MissionTarget == target.Id)
                {
                    AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[w.Type];
                    air += CombatResolver.AirWingPower(w, type, type.GroundAttack);
                }
            }

            Fixed64 total = air.IsPositive ? air * AirShare(ctx, target.Id, side) : Fixed64.Zero;
            if (target.IsWater)
            {
                return total;
            }

            for (int d = 0; d < HexDirections.Count; d++)
            {
                if (!ctx.World.TryGetNeighborCell(target, (HexDirection)d, out HexCell sea) || !sea.IsWater)
                {
                    continue;
                }

                for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
                {
                    Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                    if (f.Owner == side && f.Hex == sea.Id)
                    {
                        ShipTypeDefinition type = ctx.Catalog.ShipTypes[f.Type];
                        if (type.ShoreBombardment.IsPositive)
                        {
                            total += CombatResolver.FleetPower(f, type, type.ShoreBombardment);
                        }
                    }
                }
            }

            return total;
        }

        /// <summary>Tarafın <paramref name="hex"/> üzerindeki hava üstünlüğü payı [0, 1].</summary>
        public static Fixed64 AirShare(WarfareContext ctx, HexId hex, CountryId side)
        {
            Fixed64 own = Fixed64.Zero;
            Fixed64 enemy = Fixed64.Zero;
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                if (w.Mission == AirMission.None || w.MissionTarget != hex)
                {
                    continue;
                }

                AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[w.Type];
                if (w.Owner == side)
                {
                    own += CombatResolver.AirWingPower(w, type, type.AirAttack);
                }
                else if (UnitQueries.IsHostile(ctx, side, w.Owner))
                {
                    enemy += CombatResolver.AirWingPower(w, type, type.AirAttack);
                }
            }

            Fixed64 sum = own + enemy;
            return sum.IsPositive ? Fixed64.MulDiv(own, Fixed64.One, sum) : Fixed64.One;
        }
    }
}
