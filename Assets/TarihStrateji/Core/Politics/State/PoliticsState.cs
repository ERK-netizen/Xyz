using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Politics.Definitions;

namespace TarihStrateji.Core.Politics.State
{
    /// <summary>Tüm ülkelerin siyaseti.</summary>
    public sealed class PoliticsState
    {
        public StableIdMap<CountryId, CountryPolitics> Countries { get; } = new StableIdMap<CountryId, CountryPolitics>();

        /// <summary>
        /// Senaryo kurulumu. Destek ağırlıkları ülkenin partilerine ait ve pozitif olmalı; iktidar
        /// partisi aralarında olmalı (seçimsiz/partisiz rejimde <see cref="PartyId.None"/> olabilir).
        /// </summary>
        public Result<CountryPolitics> AddCountry(
            CountryId country,
            PoliticsCatalog catalog,
            GovernmentFormId form,
            PartyId rulingParty,
            IReadOnlyList<KeyValuePair<PartyId, Fixed64>> support,
            Fixed64 stability,
            GameDate today)
        {
            if (!country.IsValid || Countries.ContainsKey(country))
            {
                return Result<CountryPolitics>.Failure(PoliticsErrorKeys.UlkeZatenVar);
            }

            if (!catalog.Forms.TryGetValue(form, out GovernmentFormDefinition formDef))
            {
                return Result<CountryPolitics>.Failure(PoliticsErrorKeys.YonetimBilinmiyor);
            }

            if (stability.IsNegative || stability > Fixed64.One)
            {
                return Result<CountryPolitics>.Failure(PoliticsErrorKeys.IstikrarAraligi);
            }

            var politics = new CountryPolitics(country) { Form = form, RulingParty = rulingParty, Stability = stability };
            for (int i = 0; i < support.Count; i++)
            {
                if (!catalog.Parties.TryGetValue(support[i].Key, out PartyDefinition party))
                {
                    return Result<CountryPolitics>.Failure(PoliticsErrorKeys.PartiBilinmiyor);
                }

                if (party.Country != country || !support[i].Value.IsPositive || !politics.PartySupport.TryAdd(party.Id, support[i].Value))
                {
                    return Result<CountryPolitics>.Failure(PoliticsErrorKeys.PartiBuUlkeninDegil);
                }
            }

            if (rulingParty.IsValid ? !politics.PartySupport.ContainsKey(rulingParty) : formDef.HoldsElections)
            {
                return Result<CountryPolitics>.Failure(PoliticsErrorKeys.PartiBilinmiyor);
            }

            for (int i = 0; i < catalog.Institutions.Count; i++)
            {
                politics.Institutions.Add(catalog.Institutions.KeyAt(i), Fixed64.Half);
            }

            politics.NextElection = formDef.HoldsElections ? today.AddDays(formDef.ElectionIntervalDays) : GameDate.MinValue;
            Countries.Add(country, politics);
            return Result<CountryPolitics>.Success(politics);
        }
    }
}
