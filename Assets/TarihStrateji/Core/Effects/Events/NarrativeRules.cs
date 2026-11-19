using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>
    /// Olay ve odak kuralları (P2). Günlük sıra: (1) süresi dolan bekleyen olaylarda ilk seçenek,
    /// (2) yeni olay tetiklemeleri, (3) odak ilerlemesi. Sonuçların etkileri
    /// <see cref="ApplyGrants"/> ile, hazine değişimi Simulation'da uygulanır.
    /// </summary>
    public static class NarrativeRules
    {
        public const string FiredNote = "bilgi.olay.tetiklendi";
        public const string FocusCompletedNote = "bilgi.odak.tamamlandi";

        public static void ExpireDay(NarrativeState state, GameDate today, List<NarrativeOutcome> outcomes)
        {
            for (int c = 0; c < state.Countries.Count; c++)
            {
                CountryNarrative n = state.Countries.ValueAt(c);
                for (int i = 0; i < n.PendingList.Count; i++)
                {
                    if (today > n.PendingList[i].LastDay)
                    {
                        outcomes.Add(new NarrativeOutcome(n.Country, n.PendingList[i].EventId, 0, FocusId.None));
                        n.PendingList.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// Koşulları sağlanan olaylar <c>1 / MeanDays</c> olasılıkla tetiklenir (ülke ve olay kimliği
        /// sırasıyla; PRNG anahtarı: <see cref="RngSubsystem.Events"/>, gün, olay, ülke). Bekleyen
        /// veya bir kez olup tetiklenmiş olay yeniden tetiklenmez.
        /// </summary>
        public static void FireDay(NarrativeState state, NarrativeCatalog catalog, ICountryConditions conditions, IReadOnlyList<CountryId> countries, ulong worldSeed, GameDate today, List<string> notes)
        {
            for (int c = 0; c < countries.Count; c++)
            {
                CountryId country = countries[c];
                for (int e = 0; e < catalog.Events.Count; e++)
                {
                    EventDefinition def = catalog.Events.ValueAt(e);
                    if ((def.Country.IsValid && def.Country != country) || !AllHold(conditions, country, def.Conditions))
                    {
                        continue;
                    }

                    CountryNarrative n = state.For(country);
                    if ((def.FiresOnce && n.Fired.ContainsKey(def.Id)) || n.IndexOfPending(def.Id) >= 0)
                    {
                        continue;
                    }

                    var rng = new RngStream(worldSeed, RngSubsystem.Events, today.DayNumber, def.Id.Value, country.Value);
                    if (!rng.Chance(Fixed64.FromRatio(1, def.MeanDays)))
                    {
                        continue;
                    }

                    n.Fired[def.Id] = today;
                    int at = 0;
                    while (at < n.PendingList.Count && n.PendingList[at].EventId < def.Id)
                    {
                        at++;
                    }

                    n.PendingList.Insert(at, new PendingEvent(def.Id, today, today.AddDays(def.ResponseDays - 1)));
                    notes.Add(FiredNote + " @ " + def.Key + " / " + country.Value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        public static ValidationResult CheckChoose(NarrativeState state, NarrativeCatalog catalog, CountryId country, EventId eventId, int option)
        {
            if (!state.Countries.TryGetValue(country, out CountryNarrative n) || n.IndexOfPending(eventId) < 0)
            {
                return ValidationResult.Fail(NarrativeErrorKeys.OlayBulunamadi);
            }

            return option < 0 || option >= catalog.Events[eventId].Options.Count ? ValidationResult.Fail(NarrativeErrorKeys.SecenekGecersiz) : ValidationResult.Ok;
        }

        public static NarrativeOutcome Choose(NarrativeState state, CountryId country, EventId eventId, int option)
        {
            CountryNarrative n = state.Countries[country];
            n.PendingList.RemoveAt(n.IndexOfPending(eventId));
            return new NarrativeOutcome(country, eventId, option, FocusId.None);
        }

        public static ValidationResult CheckSelectFocus(NarrativeState state, NarrativeCatalog catalog, ICountryConditions conditions, CountryId country, FocusId focus)
        {
            if (!catalog.Focuses.TryGetValue(focus, out FocusDefinition def))
            {
                return ValidationResult.Fail(NarrativeErrorKeys.OdakBilinmiyor);
            }

            if (def.Country.IsValid && def.Country != country)
            {
                return ValidationResult.Fail(NarrativeErrorKeys.UlkeyeAitDegil);
            }

            state.Countries.TryGetValue(country, out CountryNarrative n);
            if (n != null && n.CompletedFocuses.ContainsKey(focus))
            {
                return ValidationResult.Fail(NarrativeErrorKeys.OdakTamamlandi);
            }

            if (n != null && n.CurrentFocus == focus)
            {
                return ValidationResult.Fail(NarrativeErrorKeys.OdakSurmekte);
            }

            for (int i = 0; i < def.Prerequisites.Count; i++)
            {
                if (n == null || !n.CompletedFocuses.ContainsKey(def.Prerequisites[i]))
                {
                    return ValidationResult.Fail(NarrativeErrorKeys.OnKosulEksik);
                }
            }

            for (int i = 0; n != null && i < n.CompletedFocuses.Count; i++)
            {
                if (catalog.AreExclusive(focus, n.CompletedFocuses.KeyAt(i)))
                {
                    return ValidationResult.Fail(NarrativeErrorKeys.Dislanmis);
                }
            }

            return AllHold(conditions, country, def.Conditions) ? ValidationResult.Ok : ValidationResult.Fail(NarrativeErrorKeys.KosulSaglanmiyor);
        }

        /// <summary>Odağı seçer; önceki odağın ilerlemesi sıfırlanır.</summary>
        public static void SelectFocus(NarrativeState state, CountryId country, FocusId focus)
        {
            CountryNarrative n = state.For(country);
            n.CurrentFocus = focus;
            n.FocusProgressDays = 0;
        }

        public static void AdvanceFocusDay(NarrativeState state, NarrativeCatalog catalog, GameDate today, List<NarrativeOutcome> outcomes, List<string> notes)
        {
            for (int c = 0; c < state.Countries.Count; c++)
            {
                CountryNarrative n = state.Countries.ValueAt(c);
                if (!n.CurrentFocus.IsValid)
                {
                    continue;
                }

                n.FocusProgressDays++;
                FocusDefinition def = catalog.Focuses[n.CurrentFocus];
                if (n.FocusProgressDays < def.Days)
                {
                    continue;
                }

                n.CompletedFocuses.Add(def.Id, today);
                n.CurrentFocus = FocusId.None;
                n.FocusProgressDays = 0;
                outcomes.Add(new NarrativeOutcome(n.Country, EventId.None, -1, def.Id));
                notes.Add(FocusCompletedNote + " @ " + def.Key + " / " + n.Country.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Sonucun etkilerini ülkeye uygular (olay seçeneği süreli, odak kalıcı).</summary>
        public static Result<int> ApplyGrants(EffectEngine effects, NarrativeCatalog catalog, NarrativeOutcome outcome, GameDate today, List<EffectId> applied)
        {
            if (outcome.IsFocus)
            {
                FocusDefinition focus = catalog.Focuses[outcome.Focus];
                return EffectGrants.Apply(effects, EffectTarget.Of(outcome.Country), focus.Grants, today, 0, focus.Key, applied);
            }

            EventDefinition def = catalog.Events[outcome.EventId];
            EventOption option = def.Options[outcome.Option];
            return EffectGrants.Apply(effects, EffectTarget.Of(outcome.Country), option.Grants, today, option.DurationDays, def.Key + "." + option.Key, applied);
        }

        /// <summary>Sonucun hazine değişimi.</summary>
        public static Fixed64 TreasuryChange(NarrativeCatalog catalog, NarrativeOutcome outcome)
        {
            return outcome.IsFocus ? catalog.Focuses[outcome.Focus].TreasuryChange : catalog.Events[outcome.EventId].Options[outcome.Option].TreasuryChange;
        }

        public static bool AllHold(ICountryConditions conditions, CountryId country, IReadOnlyList<Condition> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!conditions.Holds(country, list[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
