using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>
    /// Tesis üretimi (P8). Ölçek = kadro oranı × üretim verimi (etkiler), en kıt girdiyle
    /// sınırlı. Girdiler ortak envanterden çekilir, çıktılar oraya girer; envanter negatife
    /// düşmez ve hiçbir mal girdisiz bir askerî üretimden gelmez (katalog doğrulaması).
    /// Girdi kıtlığında tesis kimliği küçük olan önce alır (deterministik öncelik).
    /// </summary>
    public static class ProductionRules
    {
        public static void RunDay(EconomyState economy, EconomyCatalog catalog, GeoWorld world, EffectEngine effects, GameDate today)
        {
            for (int i = 0; i < economy.Countries.Count; i++)
            {
                economy.Countries.ValueAt(i).OutputValueToday = Fixed64.Zero;
            }

            var targets = new EffectTarget[2];
            targets[0] = EffectTarget.Global;
            for (int i = 0; i < economy.Facilities.Count; i++)
            {
                Facility facility = economy.Facilities.ValueAt(i);
                facility.OutputScale = Fixed64.Zero;
                if (facility.Workers <= 0L || !EconomyGeography.IsOperational(world, facility.City, facility.Owner)
                    || !economy.Countries.TryGetValue(facility.Owner, out CountryEconomy country))
                {
                    continue;
                }

                FacilityTypeDefinition type = catalog.FacilityTypes[facility.Type];
                Fixed64 staffing = Fixed64.Min(Fixed64.One, Fixed64.FromRatio(facility.Workers, type.WorkersRequired));
                targets[1] = EffectTarget.Of(facility.Owner);
                Fixed64 efficiency = Fixed64.Max(Fixed64.Zero, effects.Evaluate(targets, EconomyStatKeys.ProductionEfficiency, Fixed64.One, today));
                Fixed64 scale = staffing * efficiency;

                for (int k = 0; k < type.Inputs.Count && scale.IsPositive; k++)
                {
                    GoodAmount input = type.Inputs[k];
                    Fixed64 stock = country.Stock(input.Good);
                    if (stock < input.Amount * scale)
                    {
                        scale = Fixed64.Min(scale, stock / input.Amount);
                    }
                }

                if (!scale.IsPositive)
                {
                    continue;
                }

                for (int k = 0; k < type.Inputs.Count; k++)
                {
                    GoodAmount input = type.Inputs[k];
                    country.RemoveStock(input.Good, Fixed64.Min(input.Amount * scale, country.Stock(input.Good)));
                }

                for (int k = 0; k < type.Outputs.Count; k++)
                {
                    GoodAmount output = type.Outputs[k];
                    Fixed64 produced = output.Amount * scale;
                    country.AddStock(output.Good, produced);
                    country.OutputValueToday += produced * catalog.Goods[output.Good].BasePrice;
                }

                facility.OutputScale = scale;
            }
        }
    }
}
