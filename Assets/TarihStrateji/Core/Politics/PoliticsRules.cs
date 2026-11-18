using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Politics.Definitions;
using TarihStrateji.Core.Politics.State;

namespace TarihStrateji.Core.Politics
{
    /// <summary>
    /// Günlük siyaset (P4), sırasıyla: istikrar hedefe yaklaşır; kurumlar aşınır; iktidar desteği
    /// istikrarla değişir; süresi biten krizler kapanır, eşiğin altındaysa yeni kriz çıkabilir;
    /// seçim günüyse destekle ağırlıklı deterministik çekilişle iktidar belirlenir.
    /// </summary>
    public static class PoliticsRules
    {
        public const string CrisisStartedNote = "bilgi.siyaset.krizBasladi";
        public const string CrisisEndedNote = "bilgi.siyaset.krizBitti";
        public const string ElectionNote = "bilgi.siyaset.secim";

        private static readonly Fixed64 MinSupport = Fixed64.FromRaw(1_000L); // 0.001: hiçbir parti yok olmaz

        /// <summary>Hedef = yönetim tabanı + Σ kurum gücü × katkı + etkiler − kıtlık cezası, [0, 1].</summary>
        public static Fixed64 StabilityTarget(CountryPolitics p, PoliticsCatalog catalog, EffectEngine effects, GameDate today, Fixed64 penalty)
        {
            Fixed64 target = catalog.Forms[p.Form].BaseStability;
            for (int i = 0; i < p.Institutions.Count; i++)
            {
                target += p.Institutions.ValueAt(i) * catalog.Institutions[p.Institutions.KeyAt(i)].StabilityContribution;
            }

            target = effects.Evaluate(EffectTarget.Of(p.Country), PoliticsStatKeys.StabilityTarget, target, today);
            return Fixed64.Clamp01(target - penalty);
        }

        public static void AdvanceDay(CountryPolitics p, PoliticsCatalog catalog, EffectEngine effects, GameDate today, ulong worldSeed, Fixed64 penalty, List<string> notes)
        {
            PoliticsParameters prm = catalog.Parameters;
            Fixed64 target = StabilityTarget(p, catalog, effects, today, penalty);
            p.Stability = Fixed64.Clamp01(p.Stability + ((target - p.Stability) * prm.StabilityDriftPerDay));

            for (int i = 0; i < p.Institutions.Count; i++)
            {
                p.Institutions.SetValueAt(i, Fixed64.Max(Fixed64.Zero, p.Institutions.ValueAt(i) - prm.InstitutionDecayPerDay));
            }

            if (p.RulingParty.IsValid && p.PartySupport.TryGetValue(p.RulingParty, out Fixed64 ruling))
            {
                Fixed64 factor = Fixed64.One + (prm.RulingSupportDrift * (p.Stability - Fixed64.Half));
                p.PartySupport[p.RulingParty] = Fixed64.Max(MinSupport, ruling * factor);
            }

            string at = " @ " + p.Country.Value.ToString(CultureInfo.InvariantCulture) + ":";
            var ended = new List<CrisisId>();
            for (int i = 0; i < p.ActiveCrises.Count; i++)
            {
                if (p.ActiveCrises.ValueAt(i).LastDay < today)
                {
                    ended.Add(p.ActiveCrises.KeyAt(i));
                }
            }

            for (int i = 0; i < ended.Count; i++)
            {
                EffectGrants.Remove(effects, p.ActiveCrises[ended[i]].Effects);
                p.ActiveCrises.Remove(ended[i]);
                notes.Add(CrisisEndedNote + at + catalog.Crises[ended[i]].Key);
            }

            for (int i = 0; i < catalog.Crises.Count; i++)
            {
                CrisisDefinition crisis = catalog.Crises.ValueAt(i);
                if (p.ActiveCrises.ContainsKey(crisis.Id) || p.Stability >= crisis.TriggerBelowStability)
                {
                    continue;
                }

                var rng = new RngStream(worldSeed, RngSubsystem.Politics, today.DayNumber, crisis.Id.Value, p.Country.Value);
                if (!rng.Chance(crisis.DailyChance))
                {
                    continue;
                }

                var applied = new List<EffectId>();
                EffectGrants.Apply(effects, EffectTarget.Of(p.Country), crisis.Effects, today, crisis.DurationDays, crisis.Key, applied);
                p.ActiveCrises.Add(crisis.Id, new ActiveCrisis(crisis.Id, today, today.AddDays(crisis.DurationDays - 1), applied));
                p.Stability = Fixed64.Clamp01(p.Stability + crisis.StabilityShock);
                notes.Add(CrisisStartedNote + at + crisis.Key);
            }

            GovernmentFormDefinition form = catalog.Forms[p.Form];
            if (form.HoldsElections && today >= p.NextElection && p.PartySupport.Count > 0)
            {
                var rng = new RngStream(worldSeed, RngSubsystem.Politics, today.DayNumber, 0UL, p.Country.Value);
                p.RulingParty = WeightedPick(p.PartySupport, rng.NextFraction());
                p.NextElection = today.AddDays(form.ElectionIntervalDays);
                notes.Add(ElectionNote + at + catalog.Parties[p.RulingParty].Key);
            }
        }

        /// <summary>Kesir ∈ [0, 1) → ağırlıklı seçim (kimlik sırasında birikimli).</summary>
        public static PartyId WeightedPick(TarihStrateji.Core.Domain.Collections.StableIdMap<PartyId, Fixed64> weights, Fixed64 fraction)
        {
            Fixed64 total = Fixed64.Zero;
            for (int i = 0; i < weights.Count; i++)
            {
                total += weights.ValueAt(i);
            }

            Fixed64 point = total * fraction;
            Fixed64 cumulative = Fixed64.Zero;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights.ValueAt(i);
                if (point < cumulative)
                {
                    return weights.KeyAt(i);
                }
            }

            return weights.KeyAt(weights.Count - 1);
        }

        public static ValidationResult CheckInvest(PoliticsState state, PoliticsCatalog catalog, CountryId country, InstitutionId institution, Fixed64 money)
        {
            if (!state.Countries.TryGetValue(country, out CountryPolitics p))
            {
                return ValidationResult.Fail(PoliticsErrorKeys.UlkeYok);
            }

            if (!catalog.Institutions.ContainsKey(institution))
            {
                return ValidationResult.Fail(PoliticsErrorKeys.KurumBilinmiyor);
            }

            if (!money.IsPositive)
            {
                return ValidationResult.Fail(PoliticsErrorKeys.MiktarGecersiz);
            }

            return p.InstitutionStrength(institution) >= Fixed64.One ? ValidationResult.Fail(PoliticsErrorKeys.KurumDolu) : ValidationResult.Ok;
        }

        /// <summary>Kurumu güçlendirir (para düşümü çağıranın işidir); güç 1'i aşmaz.</summary>
        public static ValidationResult Invest(PoliticsState state, PoliticsCatalog catalog, CountryId country, InstitutionId institution, Fixed64 money)
        {
            ValidationResult check = CheckInvest(state, catalog, country, institution, money);
            if (check.IsValid)
            {
                CountryPolitics p = state.Countries[country];
                Fixed64 gain = money / catalog.Institutions[institution].MoneyPerStrength;
                p.Institutions[institution] = Fixed64.Min(Fixed64.One, p.InstitutionStrength(institution) + gain);
            }

            return check;
        }
    }
}
