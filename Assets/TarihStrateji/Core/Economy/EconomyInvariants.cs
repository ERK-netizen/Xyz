using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;

namespace TarihStrateji.Core.Economy
{
    /// <summary>
    /// Ekonomi değişmezleri (P15): negatif stok yok; tesis/inşaat/ticaret emri referansları
    /// geçerli; her sektörde (tesis slotları + süren inşaat slotları) == dünyanın kullanılan
    /// slot sayısı (slot muhasebesi).
    /// </summary>
    public static class EconomyInvariants
    {
        public static int Check(EconomyState economy, EconomyCatalog catalog, WorldMap map, List<string> violations)
        {
            int before = violations.Count;
            for (int i = 0; i < economy.Countries.Count; i++)
            {
                CountryEconomy c = economy.Countries.ValueAt(i);
                for (int g = 0; g < c.Inventory.Count; g++)
                {
                    if (c.Inventory.ValueAt(g).IsNegative || !catalog.Goods.ContainsKey(c.Inventory.KeyAt(g)))
                    {
                        violations.Add(EconomyErrorKeys.NegatifStok + At(c.Country));
                    }
                }

                if (c.TaxRate.IsNegative || c.TaxRate > catalog.Parameters.MaxTaxRate)
                {
                    violations.Add(EconomyErrorKeys.VergiOraniGecersiz + At(c.Country));
                }
            }

            var slots = new StableIdMap<SectorId, int>();
            for (int i = 0; i < economy.Facilities.Count; i++)
            {
                Facility f = economy.Facilities.ValueAt(i);
                if (!catalog.FacilityTypes.TryGetValue(f.Type, out FacilityTypeDefinition type) || !map.Sectors.ContainsKey(f.Sector)
                    || !economy.Countries.ContainsKey(f.Owner))
                {
                    violations.Add(EconomyErrorKeys.KayitTutarsiz + At(f.Id));
                    continue;
                }

                slots.TryGetValue(f.Sector, out int used);
                slots[f.Sector] = used + type.BuildingSlots;
            }

            for (int i = 0; i < economy.Constructions.Count; i++)
            {
                ConstructionOrder o = economy.Constructions.ValueAt(i);
                if (!catalog.FacilityTypes.TryGetValue(o.Type, out FacilityTypeDefinition type) || !map.Sectors.ContainsKey(o.Sector)
                    || !economy.Countries.ContainsKey(o.Country) || o.Progress.IsNegative)
                {
                    violations.Add(EconomyErrorKeys.KayitTutarsiz + At(o.Id));
                    continue;
                }

                slots.TryGetValue(o.Sector, out int used);
                slots[o.Sector] = used + type.BuildingSlots;
            }

            for (int i = 0; i < map.Sectors.Count; i++)
            {
                CitySector sector = map.Sectors.ValueAt(i);
                slots.TryGetValue(sector.Id, out int expected);
                if (expected != sector.BuildingSlotsUsed)
                {
                    violations.Add(EconomyErrorKeys.SlotMuhasebesiTutarsiz + At(sector.Id));
                }
            }

            for (int i = 0; i < economy.TradeOrders.Count; i++)
            {
                TradeOrder t = economy.TradeOrders.ValueAt(i);
                if (!economy.Countries.ContainsKey(t.Country) || !catalog.Goods.ContainsKey(t.Good)
                    || !t.QuantityPerDay.IsPositive || !t.LimitPrice.IsPositive)
                {
                    violations.Add(EconomyErrorKeys.TicaretEmriGecersiz + At(t.Id));
                }
            }

            for (int i = 0; i < economy.Market.Count; i++)
            {
                if (!economy.Market.ValueAt(i).Price.IsPositive)
                {
                    violations.Add(EconomyErrorKeys.KayitTutarsiz + At(economy.Market.KeyAt(i)));
                }
            }

            return violations.Count - before;
        }

        private static string At<TId>(TId id)
            where TId : struct, IStableId
        {
            return " @ " + id.Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
