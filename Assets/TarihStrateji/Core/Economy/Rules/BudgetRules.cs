using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>
    /// Kamu bütçesi ve gıda tüketimi (P10, pazardan sonra):
    /// <list type="bullet">
    /// <item>Vergi = bugünkü üretim değeri × vergi oranı × vergi verimi (etkiler).</item>
    /// <item>Bakım = çalışan tesislerin günlük bakım gideri (ölçekle büyüyen gider).</item>
    /// <item>Gıda: bölgenin fiili kontrolcüsü o bölgenin nüfusunu besler; karşılanamayan oran
    /// <see cref="CountryEconomy.FoodShortage"/> olur ve ertesi gün P6'da ölümü artırır.</item>
    /// </list>
    /// </summary>
    public static class BudgetRules
    {
        public static void SettleDay(EconomyState economy, EconomyCatalog catalog, PopulationState population, GeoWorld world, EffectEngine effects, GameDate today)
        {
            var targets = new EffectTarget[2];
            targets[0] = EffectTarget.Global;
            for (int i = 0; i < economy.Countries.Count; i++)
            {
                CountryEconomy country = economy.Countries.ValueAt(i);
                targets[1] = EffectTarget.Of(country.Country);
                Fixed64 efficiency = Fixed64.Max(Fixed64.Zero, effects.Evaluate(targets, EconomyStatKeys.TaxEfficiency, Fixed64.One, today));
                Fixed64 tax = country.OutputValueToday * country.TaxRate * efficiency;
                country.Treasury += tax;
                country.Ledger.TaxIncome += tax;
            }

            for (int i = 0; i < economy.Facilities.Count; i++)
            {
                Facility facility = economy.Facilities.ValueAt(i);
                if (!EconomyGeography.IsOperational(world, facility.City, facility.Owner)
                    || !economy.Countries.TryGetValue(facility.Owner, out CountryEconomy owner))
                {
                    continue;
                }

                Fixed64 upkeep = catalog.FacilityTypes[facility.Type].UpkeepPerDay;
                owner.Treasury -= upkeep;
                owner.Ledger.FacilityUpkeep += upkeep;
            }

            var people = new StableIdMap<CountryId, long>(economy.Countries.Count);
            for (int i = 0; i < population.Regions.Count; i++)
            {
                RegionPopulation p = population.Regions.ValueAt(i);
                if (!world.TryGetControl(p.Region, out RegionControl region) || !region.Controller.IsValid)
                {
                    continue;
                }

                people.TryGetValue(region.Controller, out long current);
                people[region.Controller] = current + p.Total;
            }

            GoodId food = catalog.Parameters.FoodGood;
            for (int i = 0; i < economy.Countries.Count; i++)
            {
                CountryEconomy country = economy.Countries.ValueAt(i);
                people.TryGetValue(country.Country, out long fed);
                Fixed64 need = Fixed64.MulDiv(Fixed64.FromInt(fed), catalog.Parameters.FoodPerThousandPerDay, Fixed64.FromInt(1000));
                if (!need.IsPositive)
                {
                    country.FoodShortage = Fixed64.Zero;
                    continue;
                }

                Fixed64 eaten = Fixed64.Min(need, country.Stock(food));
                country.RemoveStock(food, eaten);
                country.FoodShortage = Fixed64.One - Fixed64.MulDiv(eaten, Fixed64.One, need);
            }
        }

        public static void ResetLedgers(EconomyState economy)
        {
            for (int i = 0; i < economy.Countries.Count; i++)
            {
                economy.Countries.ValueAt(i).Ledger.Reset();
            }
        }
    }
}
