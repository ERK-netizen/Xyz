using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// İkmal sonuçları (P11, ikmal ağından sonra): ikmalsiz birlik teşkilat ve güç kaybeder (asker
    /// ölür); ikmalli birlik teşkilat toplar; kendi toprağında ve ikmali yarıdan fazlaysa takviye
    /// alır — asker bölge nüfusundan seferber edilir, ekipman ortak envanterden çekilir (yoksa
    /// takviye olmaz). Gücü eşiğin altına düşen birlik dağılır, sağ kalanlar eve döner.
    /// </summary>
    public static class SustainmentRules
    {
        public static void ApplyDay(WarfareContext ctx, StableIdMap<CountryId, SupplyGrid> grids, List<string> notes)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            var dissolved = new List<LandUnit>();
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit unit = ctx.Warfare.Units.ValueAt(i);
                if (unit.IsEmbarked)
                {
                    // Gemideki birlik gemi erzakıyla yaşar: ne yıpranır ne toparlanır.
                    continue;
                }

                UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
                int cellIndex = ctx.World.Grid.IndexOf(ctx.World.Grid.FromId(unit.Hex));
                unit.Supply = grids.TryGetValue(unit.Owner, out SupplyGrid grid) ? grid.Level(cellIndex) : Fixed64.Zero;

                if (!unit.Supply.IsPositive)
                {
                    unit.Organization = Fixed64.Max(Fixed64.Zero, unit.Organization - (type.MaxOrganization * prm.OutOfSupplyOrganizationLoss));
                    CasualtyRules.ApplyStrengthLoss(ctx, unit, type, prm.OutOfSupplyStrengthLoss);
                }
                else
                {
                    Fixed64 recovery = type.MaxOrganization * prm.OrganizationRecovery * unit.Supply;
                    unit.Organization = Fixed64.Min(type.MaxOrganization, unit.Organization + recovery);
                    if (unit.Supply >= Fixed64.Half && ctx.World.Cell(unit.Hex).Controller == unit.Owner)
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
                notes.Add(CombatRules.UnitDestroyedNote + " @ " + dissolved[i].Id.Value.ToString(CultureInfo.InvariantCulture));
                UnitRules.Remove(ctx, dissolved[i], true);
            }
        }

        private static void Reinforce(WarfareContext ctx, LandUnit unit, UnitTypeDefinition type, Fixed64 rate)
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
    }
}
