using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Population
{
    /// <summary>
    /// Bir bölgenin nüfusu (düz veri): yaş bandı kohortları (tam kişi), akışların kesirli
    /// birikimleri (carry), seferber edilenler ve istihdam edilenler. <see cref="Total"/> her
    /// değişimde güncellenir ve günlük olarak kohort toplamıyla doğrulanır (CLAUDE.md §2.6).
    /// </summary>
    public sealed class RegionPopulation
    {
        internal readonly long[] Cohorts = new long[AgeBands.Count];
        internal readonly Fixed64[] DeathCarry = new Fixed64[AgeBands.Count];
        internal readonly Fixed64[] AgingCarry = new Fixed64[AgeBands.Count];

        internal RegionPopulation(RegionId region)
        {
            Region = region;
        }

        public RegionId Region { get; }

        public long Total { get; internal set; }

        /// <summary>Seferber edilmiş (ordudaki) çalışma çağı nüfusu; kohortların içindedir.</summary>
        public long Mobilized { get; internal set; }

        /// <summary>Bugün tesislerde istihdam edilen (P7'de atanır).</summary>
        public long Employed { get; internal set; }

        internal Fixed64 BirthCarry { get; set; }

        public long Cohort(int band) => Cohorts[band];

        public long SumCohorts()
        {
            long sum = 0L;
            for (int b = 0; b < AgeBands.Count; b++)
            {
                sum = checked(sum + Cohorts[b]);
            }

            return sum;
        }

        public long WorkingAge
        {
            get
            {
                long sum = 0L;
                for (int b = AgeBands.WorkingFirst; b <= AgeBands.WorkingLast; b++)
                {
                    sum = checked(sum + Cohorts[b]);
                }

                return sum;
            }
        }

        /// <summary>Seferber edilmemiş çalışma çağı nüfusu (yeni seferberlik için üst sınır).</summary>
        public long AvailableManpower => Math.Max(0L, WorkingAge - Mobilized);

        /// <summary>İş gücü = (çalışma çağı − seferber) × katılım; seferberlik iş gücünü azaltır.</summary>
        public long LaborForce(Fixed64 participation)
        {
            return (Fixed64.FromInt(AvailableManpower) * participation).FloorToLong();
        }
    }
}
