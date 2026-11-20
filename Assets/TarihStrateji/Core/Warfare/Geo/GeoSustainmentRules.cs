using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Logistics.Geo;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// İkmalin birliğe etkisi (P11, WAR-L05): ikmal = ağın birliğin düğümündeki seviyesi. İkmalsiz birlik
    /// teşkilat ve güç kaybeder; ikmalli toparlanır, kendi kontrolündeki bölgede ve ikmal ≥ ½ ise nüfus ve
    /// envanterden takviye alır (yoktan asker/teçhizat yok). Gücü eşiğin altına düşen birlik dağılır.
    /// Gemideki birlik ne yıpranır ne toparlanır.
    /// </summary>
    public static class GeoSustainmentRules
    {
        public static void ApplyDay(GeoWarfareContext ctx, IReadOnlyDictionary<CountryId, GeoSupplyNetwork> supply, List<string> notes)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            var dissolved = new List<LandUnit>();
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit unit = ctx.Warfare.Units.ValueAt(i);
                if (unit.IsEmbarked)
                {
                    continue;
                }

                UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
                unit.Supply = supply.TryGetValue(unit.Owner, out GeoSupplyNetwork net) ? net.NodeLevel(unit.Node) : Fixed64.Zero;
                if (!unit.Supply.IsPositive)
                {
                    unit.Organization = Fixed64.Max(Fixed64.Zero, unit.Organization - (type.MaxOrganization * prm.OutOfSupplyOrganizationLoss));
                    CasualtyRules.ApplyStrengthLoss(ctx.Population, unit, type, prm.OutOfSupplyStrengthLoss);
                }
                else
                {
                    unit.Organization = Fixed64.Min(type.MaxOrganization, unit.Organization + (type.MaxOrganization * prm.OrganizationRecovery * unit.Supply));
                    if (unit.Supply >= Fixed64.Half && OwnTerritory(ctx, unit))
                    {
                        Reinforce(ctx, unit, type, prm.ReinforcementRate);
                    }
                }

                if (unit.Strength(type) < prm.DestroyedBelowStrength)
                {
                    dissolved.Add(unit);
                }
            }

            for (int i = 0; i < dissolved.Count; i++)
            {
                notes.Add(WarfareNotes.UnitDestroyed + " @ " + dissolved[i].Id.Value.ToString(CultureInfo.InvariantCulture));
                GeoCombatRules.Remove(ctx, dissolved[i], true);
            }
        }

        private static bool OwnTerritory(GeoWarfareContext ctx, LandUnit unit) =>
            ctx.World.Transport.TryGetNode(unit.Node, out TransportNode node)
            && ctx.World.TryGetControl(node.Region, out RegionControl control) && control.Controller == unit.Owner;

        private static void Reinforce(GeoWarfareContext ctx, LandUnit unit, UnitTypeDefinition type, Fixed64 rate)
        {
            long missingMen = type.Manpower - unit.Manpower;
            if (missingMen > 0L && ctx.Population.Regions.TryGetValue(unit.HomeRegion, out RegionPopulation region))
            {
                long wanted = (Fixed64.FromInt(type.Manpower) * rate).RoundToLong();
                long men = wanted < missingMen ? wanted : missingMen;
                if (men > region.AvailableManpower)
                {
                    men = region.AvailableManpower;
                }

                if (men > 0L && PopulationRules.Mobilize(region, men).IsValid)
                {
                    unit.Manpower += men;
                }
            }

            Fixed64 missingEquipment = Fixed64.One - unit.Equipment;
            if (missingEquipment.IsPositive)
            {
                Fixed64 gain = Fixed64.Min(rate, missingEquipment);
                if (InventoryRules.Consume(ctx.Economy, unit.Owner, type.Equipment, gain).IsValid)
                {
                    unit.Equipment += gain;
                }
            }
        }

        /// <summary>P0: aksiyon puanları günlük değere yenilenir (birikmez).</summary>
        public static void RefreshActionPoints(GeoWarfareContext ctx)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                u.ActionPoints = ctx.Catalog.UnitTypes[u.Type].ActionPointsPerDay;
            }
        }
    }
}
