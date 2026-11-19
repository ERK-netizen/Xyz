using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>Güç kaybını askere (nüfustan düşülür) ve ekipmana uygular.</summary>
    public static class CasualtyRules
    {
        /// <summary>Tam güce oranla <paramref name="fraction"/> kadar kayıp; ölen asker sayısını döner.</summary>
        public static long ApplyStrengthLoss(WarfareContext ctx, LandUnit unit, UnitTypeDefinition type, Fixed64 fraction)
        {
            if (!fraction.IsPositive)
            {
                return 0L;
            }

            long men = (Fixed64.FromInt(type.Manpower) * fraction).RoundToLong();
            if (men > unit.Manpower)
            {
                men = unit.Manpower;
            }

            unit.Manpower -= men;
            unit.Equipment = Fixed64.Max(Fixed64.Zero, unit.Equipment - fraction);
            RecordDeaths(ctx, unit, men);
            return men;
        }

        /// <summary>Ölen askerleri bölge nüfusundan düşer (seferberlik sınırıyla).</summary>
        public static void RecordDeaths(WarfareContext ctx, LandUnit unit, long men)
        {
            if (men > 0L && ctx.Population.Regions.TryGetValue(unit.HomeRegion, out RegionPopulation region))
            {
                long count = men < region.Mobilized ? men : region.Mobilized;
                if (count > 0L)
                {
                    PopulationRules.ApplyMilitaryCasualties(region, count);
                }
            }
        }

        /// <summary>Kalan askerleri terhis eder (sağ kalanlar eve döner).</summary>
        public static void SendHome(WarfareContext ctx, LandUnit unit)
        {
            if (unit.Manpower > 0L && ctx.Population.Regions.TryGetValue(unit.HomeRegion, out RegionPopulation region))
            {
                long count = unit.Manpower < region.Mobilized ? unit.Manpower : region.Mobilized;
                if (count > 0L)
                {
                    PopulationRules.Demobilize(region, count);
                }
            }

            unit.Manpower = 0L;
        }
    }
}
