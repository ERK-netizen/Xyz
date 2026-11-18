using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Population
{
    /// <summary>
    /// Günlük kohort akışı (P6). Tüm akışlar günün başlangıç değerlerinden hesaplanır ve sonra
    /// birlikte uygulanır (sıra bağımsız). Kesirli kişi kalanı carry alanlarında birikir; kişi
    /// asla yoktan var olmaz veya kaybolmaz: Σ değişim = doğum − ölüm.
    /// </summary>
    public static class DemographyRules
    {
        public readonly struct DayResult
        {
            public DayResult(long births, long deaths)
            {
                Births = births;
                Deaths = deaths;
            }

            public long Births { get; }

            public long Deaths { get; }
        }

        /// <param name="mortalityMultiplier">Kıtlık vb. için ölüm hazardı çarpanı (≥ 0; 1 = normal).</param>
        public static DayResult AdvanceDay(RegionPopulation p, DemographyParameters parameters, Fixed64 mortalityMultiplier)
        {
            Fixed64 multiplier = Fixed64.Max(Fixed64.Zero, mortalityMultiplier);
            Fixed64 days = DemographyParameters.DaysPerYear;

            // Doğumlar: Σ kişi × doğurganlık / 365 (+ carry).
            Fixed64 birthsExact = p.BirthCarry;
            for (int b = AgeBands.FertileFirst; b <= AgeBands.FertileLast; b++)
            {
                birthsExact += Fixed64.MulDiv(Fixed64.FromInt(p.Cohorts[b]), parameters.Fertility(b), days);
            }

            long births = Take(ref birthsExact, long.MaxValue);
            p.BirthCarry = birthsExact;

            System.Span<long> deaths = stackalloc long[AgeBands.Count];
            System.Span<long> aging = stackalloc long[AgeBands.Count];
            long totalDeaths = 0L;
            for (int b = 0; b < AgeBands.Count; b++)
            {
                long people = p.Cohorts[b];
                Fixed64 hazard = parameters.MortalityHazard(b) * multiplier;
                Fixed64 deathExact = p.DeathCarry[b] + Fixed64.MulDiv(Fixed64.FromInt(people), hazard, days);
                deaths[b] = Take(ref deathExact, people);
                p.DeathCarry[b] = deathExact;
                totalDeaths += deaths[b];

                if (b < AgeBands.Last)
                {
                    Fixed64 agingExact = p.AgingCarry[b] + Fixed64.MulDiv(Fixed64.FromInt(people), DemographyParameters.AgingPerYear, days);
                    aging[b] = Take(ref agingExact, people - deaths[b]);
                    p.AgingCarry[b] = agingExact;
                }
            }

            for (int b = AgeBands.Last; b >= 0; b--)
            {
                long incoming = b == 0 ? births : aging[b - 1];
                p.Cohorts[b] = p.Cohorts[b] - deaths[b] - aging[b] + incoming;
            }

            p.Total = checked(p.Total + births - totalDeaths);
            if (p.Mobilized > p.WorkingAge)
            {
                p.Mobilized = p.WorkingAge;
            }

            return new DayResult(births, totalDeaths);
        }

        /// <summary>
        /// Tam kısmı çeker (üst sınırla); çekilemeyen ve kesirli kısım carry'de kalır. Sınıra
        /// takılan fazla, kaybolmasın diye carry'de bekler ama bir günlük akışı aşmaz.
        /// </summary>
        private static long Take(ref Fixed64 exact, long cap)
        {
            long whole = exact.FloorToLong();
            if (whole < 0L)
            {
                whole = 0L;
            }

            if (whole > cap)
            {
                whole = cap;
            }

            exact -= Fixed64.FromInt(whole);
            if (exact > Fixed64.One)
            {
                exact = Fixed64.One;
            }

            return whole;
        }
    }
}
