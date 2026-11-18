using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Research.Definitions;

namespace TarihStrateji.Core.Research.State
{
    /// <summary>Tüm ülkelerin araştırma durumu.</summary>
    public sealed class ResearchState
    {
        public StableIdMap<CountryId, CountryResearch> Countries { get; } = new StableIdMap<CountryId, CountryResearch>();

        /// <summary>
        /// Senaryo kurulumu: başlangıç teknolojileri (ön koşullarıyla kapalı küme olmalı) bugünden
        /// önce araştırılmış sayılır ve etkileri kalıcı uygulanır. Çağ, tarih ve kapılara göre
        /// ulaşılabilecek en yüksek çağdır.
        /// </summary>
        public Result<CountryResearch> AddCountry(CountryId country, TechCatalog catalog, IReadOnlyList<TechnologyId> initial, EffectEngine effects, GameDate today)
        {
            if (!country.IsValid || Countries.ContainsKey(country))
            {
                return Result<CountryResearch>.Failure(ResearchErrorKeys.UlkeZatenVar);
            }

            var research = new CountryResearch(country, 0);
            for (int i = 0; i < initial.Count; i++)
            {
                if (!catalog.Technologies.ContainsKey(initial[i]) || !research.Researched.TryAdd(initial[i], today))
                {
                    return Result<CountryResearch>.Failure(ResearchErrorKeys.BaslangicTutarsiz);
                }
            }

            for (int i = 0; i < research.Researched.Count; i++)
            {
                TechnologyDefinition t = catalog.Technologies[research.Researched.KeyAt(i)];
                for (int p = 0; p < t.Prerequisites.Count; p++)
                {
                    if (!research.Has(t.Prerequisites[p]))
                    {
                        return Result<CountryResearch>.Failure(ResearchErrorKeys.BaslangicTutarsiz);
                    }
                }
            }

            while (EraRules.CanAdvance(catalog, research, today))
            {
                research.Era++;
            }

            for (int i = 0; i < research.Researched.Count; i++)
            {
                TechnologyDefinition t = catalog.Technologies[research.Researched.KeyAt(i)];
                if (t.Era > research.Era)
                {
                    return Result<CountryResearch>.Failure(ResearchErrorKeys.BaslangicTutarsiz);
                }
            }

            var applied = new List<EffectId>();
            for (int i = 0; i < research.Researched.Count; i++)
            {
                TechnologyDefinition t = catalog.Technologies[research.Researched.KeyAt(i)];
                Result<int> granted = EffectGrants.Apply(effects, EffectTarget.Of(country), t.Effects, today, 0, t.Key, applied);
                if (!granted.IsSuccess)
                {
                    EffectGrants.Remove(effects, applied);
                    return Result<CountryResearch>.Failure(granted.ErrorKey);
                }
            }

            Countries.Add(country, research);
            return Result<CountryResearch>.Success(research);
        }
    }
}
