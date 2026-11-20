using TarihStrateji.Core.Domain.Ids;
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
        public static long ApplyStrengthLoss(PopulationState population, LandUnit unit, UnitTypeDefinition type, Fixed64 fraction)
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
            RecordDeaths(population, unit.HomeRegion, men);
            return men;
        }

        /// <summary>Filo/kanat için: azami güce göre <paramref name="fraction"/> kadar insan ve teçhizat kaybı.</summary>
        public static long ApplyLoss(PopulationState population, RegionId home, long fullCrew, ref long manpower, ref Fixed64 equipment, Fixed64 fraction)
        {
            if (!fraction.IsPositive)
            {
                return 0L;
            }

            long men = (Fixed64.FromInt(fullCrew) * fraction).RoundToLong();
            if (men > manpower)
            {
                men = manpower;
            }

            manpower -= men;
            equipment = Fixed64.Max(Fixed64.Zero, equipment - fraction);
            RecordDeaths(population, home, men);
            return men;
        }

        public static void RecordDeaths(PopulationState population, RegionId home, long men)
        {
            if (men > 0L && population.Regions.TryGetValue(home, out RegionPopulation region))
            {
                long count = men < region.Mobilized ? men : region.Mobilized;
                if (count > 0L)
                {
                    PopulationRules.ApplyMilitaryCasualties(region, count);
                }
            }
        }


        public static void SendHome(PopulationState population, RegionId home, long men)
        {
            if (men > 0L && population.Regions.TryGetValue(home, out RegionPopulation region))
            {
                long count = men < region.Mobilized ? men : region.Mobilized;
                if (count > 0L)
                {
                    PopulationRules.Demobilize(region, count);
                }
            }
        }
    }
}
