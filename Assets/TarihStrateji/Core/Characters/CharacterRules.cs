using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Research;
using TarihStrateji.Core.Research.State;

namespace TarihStrateji.Core.Characters
{
    /// <summary>
    /// Atama kuralları: karakter ülkenin olmalı, rolü pozisyona uymalı, hizmet penceresi içinde
    /// olmalı, ülke karakterin çağına ulaşmış olmalı ve başka bir pozisyonda olmamalı. Atama
    /// özellikleri ülkeye uygular; görevden alma onları kaldırır.
    /// </summary>
    public static class CharacterRules
    {
        public const string ServiceEndedNote = "bilgi.karakter.hizmetSonaErdi";

        public static ValidationResult CheckAppoint(CharacterState state, CharacterCatalog catalog, ResearchState research, CountryId country, PositionKind position, CharacterId character, GameDate today)
        {
            if (!state.Countries.TryGetValue(country, out CountryCharacters cc))
            {
                return ValidationResult.Fail(CharacterErrorKeys.UlkeYok);
            }

            if (!Positions.IsDefined(position))
            {
                return ValidationResult.Fail(CharacterErrorKeys.PozisyonGecersiz);
            }

            if (!catalog.Characters.TryGetValue(character, out CharacterDefinition def))
            {
                return ValidationResult.Fail(CharacterErrorKeys.KarakterBilinmiyor);
            }

            if (def.Country != country)
            {
                return ValidationResult.Fail(CharacterErrorKeys.BaskaUlkenin);
            }

            if (!def.Has(Positions.RequiredRole(position)))
            {
                return ValidationResult.Fail(CharacterErrorKeys.RolUygunDegil);
            }

            if (today < def.ServiceFrom)
            {
                return ValidationResult.Fail(CharacterErrorKeys.HenuzHizmetteDegil);
            }

            if (today > def.ServiceUntil)
            {
                return ValidationResult.Fail(CharacterErrorKeys.HizmetSona);
            }

            research.Countries.TryGetValue(country, out CountryResearch cr);
            if (!EraRules.AllowsEra(cr, def.MinimumEra))
            {
                return ValidationResult.Fail(CharacterErrorKeys.CagUygunDegil);
            }

            return cc.TryFindPosition(character, out _) ? ValidationResult.Fail(CharacterErrorKeys.ZatenGorevde) : ValidationResult.Ok;
        }

        public static ValidationResult Appoint(CharacterState state, CharacterCatalog catalog, ResearchState research, EffectEngine effects, CountryId country, PositionKind position, CharacterId character, GameDate today)
        {
            ValidationResult check = CheckAppoint(state, catalog, research, country, position, character, today);
            if (!check.IsValid)
            {
                return check;
            }

            CountryCharacters cc = state.Countries[country];
            Vacate(cc, effects, position);
            CharacterDefinition def = catalog.Characters[character];
            List<EffectId> applied = cc.AppliedEffects[(int)position];
            EffectGrants.Apply(effects, EffectTarget.Of(country), def.Traits, today, 0, def.Key, applied);
            cc.Holders[(int)position] = character;
            return ValidationResult.Ok;
        }

        public static ValidationResult CheckDismiss(CharacterState state, CountryId country, PositionKind position)
        {
            if (!state.Countries.TryGetValue(country, out CountryCharacters cc))
            {
                return ValidationResult.Fail(CharacterErrorKeys.UlkeYok);
            }

            if (!Positions.IsDefined(position))
            {
                return ValidationResult.Fail(CharacterErrorKeys.PozisyonGecersiz);
            }

            return cc.Holder(position).IsValid ? ValidationResult.Ok : ValidationResult.Fail(CharacterErrorKeys.PozisyonBos);
        }

        public static ValidationResult Dismiss(CharacterState state, EffectEngine effects, CountryId country, PositionKind position)
        {
            ValidationResult check = CheckDismiss(state, country, position);
            if (check.IsValid)
            {
                Vacate(state.Countries[country], effects, position);
            }

            return check;
        }

        /// <summary>P4: hizmet penceresi bitenleri görevden alır.</summary>
        public static void ExpireDay(CharacterState state, CharacterCatalog catalog, EffectEngine effects, GameDate today, List<string> notes)
        {
            for (int i = 0; i < state.Countries.Count; i++)
            {
                CountryCharacters cc = state.Countries.ValueAt(i);
                for (int p = 0; p < Positions.Count; p++)
                {
                    CharacterId holder = cc.Holders[p];
                    if (holder.IsValid && !catalog.Characters[holder].InService(today))
                    {
                        Vacate(cc, effects, (PositionKind)p);
                        notes.Add(ServiceEndedNote + " @ " + cc.Country.Value.ToString(CultureInfo.InvariantCulture) + ":" + catalog.Characters[holder].Key);
                    }
                }
            }
        }

        private static void Vacate(CountryCharacters cc, EffectEngine effects, PositionKind position)
        {
            List<EffectId> applied = cc.AppliedEffects[(int)position];
            EffectGrants.Remove(effects, applied);
            applied.Clear();
            cc.Holders[(int)position] = CharacterId.None;
        }
    }
}
