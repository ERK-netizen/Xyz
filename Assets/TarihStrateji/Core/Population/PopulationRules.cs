using TarihStrateji.Core.Domain.Commands;

namespace TarihStrateji.Core.Population
{
    /// <summary>Seferberlik ve kayıplar (Warfare, Adım 6, bu API'yi kullanır).</summary>
    public static class PopulationRules
    {
        /// <summary>Günlük istihdamı ayarlar (P7); iş gücünü aşamaz.</summary>
        public static ValidationResult SetEmployed(RegionPopulation p, long employed, DemographyParameters parameters)
        {
            if (employed < 0L || employed > p.LaborForce(parameters.LaborParticipation))
            {
                return ValidationResult.Fail(PopulationErrorKeys.IstihdamTutarsiz);
            }

            p.Employed = employed;
            return ValidationResult.Ok;
        }

        public static ValidationResult CheckMobilize(RegionPopulation p, long count)
        {
            if (count <= 0L)
            {
                return ValidationResult.Fail(PopulationErrorKeys.MiktarGecersiz);
            }

            return count > p.AvailableManpower ? ValidationResult.Fail(PopulationErrorKeys.InsanGucuYetersiz) : ValidationResult.Ok;
        }

        public static ValidationResult Mobilize(RegionPopulation p, long count)
        {
            ValidationResult check = CheckMobilize(p, count);
            if (check.IsValid)
            {
                // Askere alınanlar iş gücünden gelir: istihdam aynı anda düşer. Böylece P7'den sonra
                // (P11 takviyesi) yapılan seferberlik de "istihdam ≤ iş gücü" değişmezini bozmaz.
                p.Mobilized += count;
                p.Employed = p.Employed > count ? p.Employed - count : 0L;
            }

            return check;
        }

        public static ValidationResult Demobilize(RegionPopulation p, long count)
        {
            if (count <= 0L || count > p.Mobilized)
            {
                return ValidationResult.Fail(PopulationErrorKeys.MiktarGecersiz);
            }

            p.Mobilized -= count;
            return ValidationResult.Ok;
        }

        /// <summary>
        /// Askerî kayıp: seferberlerden düşülür ve çalışma çağı bantlarından, bant büyüklüğüyle
        /// orantılı (en büyük kalan yöntemi, eşitlikte küçük bant önce) kişi eksilir.
        /// </summary>
        public static ValidationResult ApplyMilitaryCasualties(RegionPopulation p, long count)
        {
            if (count <= 0L || count > p.Mobilized)
            {
                return ValidationResult.Fail(PopulationErrorKeys.MiktarGecersiz);
            }

            long working = p.WorkingAge;
            const int first = AgeBands.WorkingFirst;
            int n = AgeBands.WorkingLast - first + 1;
            var take = new long[n];
            var remainder = new long[n];
            long assigned = 0L;
            for (int i = 0; i < n; i++)
            {
                long people = p.Cohorts[first + i];
                take[i] = (count * people) / working;
                remainder[i] = (count * people) % working;
                assigned += take[i];
            }

            for (long left = count - assigned; left > 0L; left--)
            {
                int best = -1;
                for (int i = 0; i < n; i++)
                {
                    if (take[i] < p.Cohorts[first + i] && (best < 0 || remainder[i] > remainder[best]))
                    {
                        best = i;
                    }
                }

                take[best]++;
                remainder[best] = -1L;
            }

            for (int i = 0; i < n; i++)
            {
                p.Cohorts[first + i] -= take[i];
            }

            p.Mobilized -= count;
            p.Total -= count;
            return ValidationResult.Ok;
        }
    }
}
