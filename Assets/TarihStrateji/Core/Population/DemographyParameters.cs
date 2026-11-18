using System;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Population
{
    /// <summary>
    /// Demografi parametreleri (içerik verisi). Tasarımcılar yıllık değer girer; günlük akış
    /// yükleme anında açıkça türetilir (game-balance: günlük ↔ yıllık dönüşümü):
    /// <list type="bullet">
    /// <item>Doğurganlık: kişi-yıl başına doğum (her iki cinsiyet, bant ortalaması); günlük akış
    /// <c>kişi × oran / 365</c>.</item>
    /// <item>Ölüm: yıllık ölüm olasılığı <c>a</c> → sürekli hazard <c>H = −ln(1 − a)</c>; günlük
    /// akış <c>kişi × H / 365</c>. Böylece bir yıl sonunda hayatta kalma tam <c>1 − a</c> olur.</item>
    /// <item>Yaşlanma: her bant yılda 1/5 oranında bir sonrakine geçer (son bant hariç).</item>
    /// </list>
    /// </summary>
    public sealed class DemographyParameters
    {
        public static readonly Fixed64 DaysPerYear = Fixed64.FromInt(365);
        public static readonly Fixed64 AgingPerYear = Fixed64.FromRatio(1, AgeBands.YearsPerBand);

        private readonly Fixed64[] _fertility;
        private readonly Fixed64[] _mortalityProbability;
        private readonly Fixed64[] _mortalityHazard;

        public DemographyParameters(
            Fixed64[] annualFertilityPerPerson,
            Fixed64[] annualMortalityProbability,
            Fixed64 laborParticipation,
            Fixed64 famineMortalityFactor)
        {
            if (annualFertilityPerPerson == null || annualFertilityPerPerson.Length != AgeBands.Count
                || annualMortalityProbability == null || annualMortalityProbability.Length != AgeBands.Count)
            {
                throw new ArgumentException(PopulationErrorKeys.KohortSayisi);
            }

            _fertility = (Fixed64[])annualFertilityPerPerson.Clone();
            _mortalityProbability = (Fixed64[])annualMortalityProbability.Clone();
            _mortalityHazard = new Fixed64[AgeBands.Count];
            for (int b = 0; b < AgeBands.Count; b++)
            {
                if (_fertility[b].IsNegative || _fertility[b] > Fixed64.One
                    || (!_fertility[b].IsZero && (b < AgeBands.FertileFirst || b > AgeBands.FertileLast)))
                {
                    throw new ArgumentException(PopulationErrorKeys.OranGecersiz, nameof(annualFertilityPerPerson));
                }

                if (_mortalityProbability[b].IsNegative || _mortalityProbability[b] >= Fixed64.FromRatio(99, 100))
                {
                    throw new ArgumentException(PopulationErrorKeys.OranGecersiz, nameof(annualMortalityProbability));
                }

                _mortalityHazard[b] = HazardFromAnnualProbability(_mortalityProbability[b]);
            }

            if (laborParticipation.IsNegative || laborParticipation > Fixed64.One || famineMortalityFactor.IsNegative)
            {
                throw new ArgumentException(PopulationErrorKeys.OranGecersiz);
            }

            LaborParticipation = laborParticipation;
            FamineMortalityFactor = famineMortalityFactor;
        }

        /// <summary>Çalışma çağındakilerin iş gücüne katılım oranı.</summary>
        public Fixed64 LaborParticipation { get; }

        /// <summary>Tam kıtlıkta (gıda karşılanma 0) ölüm hazardının artış katsayısı: çarpan = 1 + kıtlık × katsayı.</summary>
        public Fixed64 FamineMortalityFactor { get; }

        public Fixed64 Fertility(int band) => _fertility[band];

        public Fixed64 MortalityProbability(int band) => _mortalityProbability[band];

        /// <summary>Türetilmiş yıllık sürekli ölüm hazardı.</summary>
        public Fixed64 MortalityHazard(int band) => _mortalityHazard[band];

        /// <summary>
        /// <c>−ln(1 − a) = Σ aᵏ / k</c>; a &lt; 0.99 için tamsayı serisi, terim 10⁻⁶ altına inince durur.
        /// </summary>
        public static Fixed64 HazardFromAnnualProbability(Fixed64 annualProbability)
        {
            if (annualProbability.IsNegative || annualProbability >= Fixed64.One)
            {
                throw new ArgumentOutOfRangeException(nameof(annualProbability));
            }

            Fixed64 sum = Fixed64.Zero;
            Fixed64 power = annualProbability;
            for (int k = 1; k <= 2000 && power.Raw > 0L; k++)
            {
                sum += power / k;
                power *= annualProbability;
            }

            return sum;
        }
    }
}
