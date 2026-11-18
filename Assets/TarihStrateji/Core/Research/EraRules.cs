using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.Research.State;

namespace TarihStrateji.Core.Research
{
    /// <summary>
    /// Çağ kuralları (CLAUDE.md §2.6): teknolojiler ve karakter atamaları ülkenin ulaştığı çağa
    /// ve tarih penceresine bağlıdır.
    /// </summary>
    public static class EraRules
    {
        /// <summary>Ülke bir sonraki çağa geçebilir mi (tarih + kapı teknolojileri).</summary>
        public static bool CanAdvance(TechCatalog catalog, CountryResearch research, GameDate today)
        {
            int next = research.Era + 1;
            if (next >= catalog.Eras.Count)
            {
                return false;
            }

            EraDefinition era = catalog.Eras[next];
            if (today < era.StartDate)
            {
                return false;
            }

            for (int i = 0; i < era.GateTechnologies.Count; i++)
            {
                if (!research.Has(era.GateTechnologies[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Teknolojiye bugün başlanabilir mi (araştırılmamış, ön koşullar, çağ, tarih).</summary>
        public static ValidationResult CheckAvailable(TechCatalog catalog, CountryResearch research, TechnologyId tech, GameDate today)
        {
            if (!catalog.Technologies.TryGetValue(tech, out TechnologyDefinition t))
            {
                return ValidationResult.Fail(ResearchErrorKeys.TeknolojiBilinmiyor);
            }

            if (research.Has(tech))
            {
                return ValidationResult.Fail(ResearchErrorKeys.ZatenArastirildi);
            }

            if (t.Era > research.Era)
            {
                return ValidationResult.Fail(ResearchErrorKeys.CagaUlasilmadi);
            }

            if (today < t.AvailableFrom)
            {
                return ValidationResult.Fail(ResearchErrorKeys.TarihPenceresiKapali);
            }

            for (int i = 0; i < t.Prerequisites.Count; i++)
            {
                if (!research.Has(t.Prerequisites[i]))
                {
                    return ValidationResult.Fail(ResearchErrorKeys.OnkosulEksik);
                }
            }

            return ValidationResult.Ok;
        }

        /// <summary>Çağa bağlı içerik (karakter vb.) bu ülkede kullanılabilir mi.</summary>
        public static bool AllowsEra(CountryResearch research, int minimumEra)
        {
            return research != null && research.Era >= minimumEra;
        }
    }
}
