using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.Research.State;

namespace TarihStrateji.Core.Research
{
    /// <summary>
    /// Araştırma (P5). Günlük puan = taban × araştırma hızı (etkiler). İlerleme projeye yazılır;
    /// tamamlanınca etkiler kalıcı uygulanır ve kuyruktaki ilk uygun teknolojiye geçilir. Ardından
    /// çağ geçişi denetlenir.
    /// </summary>
    public static class ResearchRules
    {
        public const int MaxQueue = 16;
        public const string CompletedNote = "bilgi.arastirma.tamamlandi";
        public const string EraNote = "bilgi.arastirma.yeniCag";

        public static ValidationResult CheckSetCurrent(ResearchState state, TechCatalog catalog, CountryId country, TechnologyId tech, GameDate today)
        {
            return state.Countries.TryGetValue(country, out CountryResearch r)
                ? EraRules.CheckAvailable(catalog, r, tech, today)
                : ValidationResult.Fail(ResearchErrorKeys.UlkeYok);
        }

        public static ValidationResult SetCurrent(ResearchState state, TechCatalog catalog, CountryId country, TechnologyId tech, GameDate today)
        {
            ValidationResult check = CheckSetCurrent(state, catalog, country, tech, today);
            if (check.IsValid)
            {
                CountryResearch r = state.Countries[country];
                r.Current = tech;
                r.QueueList.Remove(tech);
            }

            return check;
        }

        /// <summary>
        /// Kuyruğa ekler. Ön koşul ve tarih denetimi başlama anında yapılır; burada yalnız tanım,
        /// tekrar ve sınır denetlenir (ön koşulları kuyrukta önde olan bir teknoloji eklenebilir).
        /// </summary>
        public static ValidationResult CheckEnqueue(ResearchState state, TechCatalog catalog, CountryId country, TechnologyId tech)
        {
            if (!state.Countries.TryGetValue(country, out CountryResearch r))
            {
                return ValidationResult.Fail(ResearchErrorKeys.UlkeYok);
            }

            if (!catalog.Technologies.ContainsKey(tech))
            {
                return ValidationResult.Fail(ResearchErrorKeys.TeknolojiBilinmiyor);
            }

            if (r.Has(tech))
            {
                return ValidationResult.Fail(ResearchErrorKeys.ZatenArastirildi);
            }

            if (r.Current == tech || r.QueueList.Contains(tech))
            {
                return ValidationResult.Fail(ResearchErrorKeys.ZatenKuyrukta);
            }

            return r.QueueList.Count >= MaxQueue ? ValidationResult.Fail(ResearchErrorKeys.KuyrukDolu) : ValidationResult.Ok;
        }

        public static ValidationResult Enqueue(ResearchState state, TechCatalog catalog, CountryId country, TechnologyId tech)
        {
            ValidationResult check = CheckEnqueue(state, catalog, country, tech);
            if (check.IsValid)
            {
                state.Countries[country].QueueList.Add(tech);
            }

            return check;
        }

        public static void AdvanceDay(ResearchState state, TechCatalog catalog, EffectEngine effects, GameDate today, List<string> notes)
        {
            var targets = new EffectTarget[2];
            targets[0] = EffectTarget.Global;
            var applied = new List<EffectId>();
            for (int i = 0; i < state.Countries.Count; i++)
            {
                CountryResearch r = state.Countries.ValueAt(i);
                if (!r.Current.IsValid || !EraRules.CheckAvailable(catalog, r, r.Current, today).IsValid)
                {
                    PickNext(r, catalog, today);
                }

                if (r.Current.IsValid)
                {
                    targets[1] = EffectTarget.Of(r.Country);
                    Fixed64 speed = Fixed64.Max(Fixed64.Zero, effects.Evaluate(targets, ResearchStatKeys.ResearchSpeed, Fixed64.One, today));
                    Fixed64 progress = r.ProgressOf(r.Current) + (catalog.BasePointsPerDay * speed);
                    TechnologyDefinition tech = catalog.Technologies[r.Current];
                    if (progress >= tech.Cost)
                    {
                        r.Progress.Remove(tech.Id);
                        r.Researched.Add(tech.Id, today);
                        applied.Clear();
                        EffectGrants.Apply(effects, EffectTarget.Of(r.Country), tech.Effects, today, 0, tech.Key, applied);
                        notes.Add(CompletedNote + " @ " + r.Country.Value.ToString(CultureInfo.InvariantCulture) + ":" + tech.Key);
                        r.Current = TechnologyId.None;
                        PickNext(r, catalog, today);
                    }
                    else
                    {
                        r.Progress[tech.Id] = progress;
                    }
                }

                while (EraRules.CanAdvance(catalog, r, today))
                {
                    r.Era++;
                    notes.Add(EraNote + " @ " + r.Country.Value.ToString(CultureInfo.InvariantCulture) + ":" + catalog.Eras[r.Era].Key);
                }
            }
        }

        /// <summary>Kuyruktan bugün başlanabilen ilk teknolojiyi güncel proje yapar (başlanamayanlar kuyrukta kalır).</summary>
        private static void PickNext(CountryResearch r, TechCatalog catalog, GameDate today)
        {
            r.Current = TechnologyId.None;
            for (int q = 0; q < r.QueueList.Count; q++)
            {
                if (r.Has(r.QueueList[q]))
                {
                    r.QueueList.RemoveAt(q);
                    q--;
                    continue;
                }

                if (EraRules.CheckAvailable(catalog, r, r.QueueList[q], today).IsValid)
                {
                    r.Current = r.QueueList[q];
                    r.QueueList.RemoveAt(q);
                    return;
                }
            }
        }
    }
}
