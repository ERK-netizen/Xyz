using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Population
{
    /// <summary>Tüm bölgelerin nüfusu, bölge kimliği sırasında.</summary>
    public sealed class PopulationState
    {
        public StableIdMap<RegionId, RegionPopulation> Regions { get; } = new StableIdMap<RegionId, RegionPopulation>();

        /// <summary>Senaryo kurulumu: bölgeye başlangıç kohortlarını yerleştirir.</summary>
        public Result<RegionPopulation> AddRegion(RegionId region, long[] cohorts)
        {
            if (!region.IsValid || Regions.ContainsKey(region))
            {
                return Result<RegionPopulation>.Failure(PopulationErrorKeys.BolgeZatenVar);
            }

            if (cohorts == null || cohorts.Length != AgeBands.Count)
            {
                return Result<RegionPopulation>.Failure(PopulationErrorKeys.KohortSayisi);
            }

            var population = new RegionPopulation(region);
            long total = 0L;
            for (int b = 0; b < AgeBands.Count; b++)
            {
                if (cohorts[b] < 0L)
                {
                    return Result<RegionPopulation>.Failure(PopulationErrorKeys.NegatifNufus);
                }

                population.Cohorts[b] = cohorts[b];
                total = checked(total + cohorts[b]);
            }

            population.Total = total;
            Regions.Add(region, population);
            return Result<RegionPopulation>.Success(population);
        }

        public long WorldTotal()
        {
            long sum = 0L;
            for (int i = 0; i < Regions.Count; i++)
            {
                sum = checked(sum + Regions.ValueAt(i).Total);
            }

            return sum;
        }
    }
}
