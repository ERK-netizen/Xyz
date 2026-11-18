using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Effects;

namespace TarihStrateji.Core.Politics.Definitions
{
    /// <summary>Doğrulanmış siyaset tanımları.</summary>
    public sealed class PoliticsCatalog
    {
        private PoliticsCatalog(
            StableIdMap<GovernmentFormId, GovernmentFormDefinition> forms,
            StableIdMap<PartyId, PartyDefinition> parties,
            StableIdMap<InstitutionId, InstitutionDefinition> institutions,
            StableIdMap<CrisisId, CrisisDefinition> crises,
            PoliticsParameters parameters)
        {
            Forms = forms;
            Parties = parties;
            Institutions = institutions;
            Crises = crises;
            Parameters = parameters;
        }

        public StableIdMap<GovernmentFormId, GovernmentFormDefinition> Forms { get; }

        public StableIdMap<PartyId, PartyDefinition> Parties { get; }

        public StableIdMap<InstitutionId, InstitutionDefinition> Institutions { get; }

        public StableIdMap<CrisisId, CrisisDefinition> Crises { get; }

        public PoliticsParameters Parameters { get; }

        public static Result<PoliticsCatalog> Create(
            IReadOnlyList<GovernmentFormDefinition> forms,
            IReadOnlyList<PartyDefinition> parties,
            IReadOnlyList<InstitutionDefinition> institutions,
            IReadOnlyList<CrisisDefinition> crises,
            PoliticsParameters parameters)
        {
            if (forms == null || parties == null || institutions == null || crises == null || parameters == null)
            {
                throw new ArgumentNullException(nameof(forms));
            }

            var formMap = new StableIdMap<GovernmentFormId, GovernmentFormDefinition>(forms.Count);
            for (int i = 0; i < forms.Count; i++)
            {
                GovernmentFormDefinition f = forms[i];
                if (f == null || !f.Id.IsValid || string.IsNullOrEmpty(f.Key) || f.ElectionIntervalDays < 0 || !InUnit(f.BaseStability) || !formMap.TryAdd(f.Id, f))
                {
                    return Result<PoliticsCatalog>.Failure(PoliticsErrorKeys.TanimGecersiz);
                }
            }

            var partyMap = new StableIdMap<PartyId, PartyDefinition>(parties.Count);
            for (int i = 0; i < parties.Count; i++)
            {
                PartyDefinition p = parties[i];
                if (p == null || !p.Id.IsValid || !p.Country.IsValid || string.IsNullOrEmpty(p.Key) || p.Ideology > Ideology.Monarchist || !partyMap.TryAdd(p.Id, p))
                {
                    return Result<PoliticsCatalog>.Failure(PoliticsErrorKeys.TanimGecersiz);
                }
            }

            var institutionMap = new StableIdMap<InstitutionId, InstitutionDefinition>(institutions.Count);
            for (int i = 0; i < institutions.Count; i++)
            {
                InstitutionDefinition d = institutions[i];
                if (d == null || !d.Id.IsValid || string.IsNullOrEmpty(d.Key) || !InUnit(d.StabilityContribution) || !d.MoneyPerStrength.IsPositive || !institutionMap.TryAdd(d.Id, d))
                {
                    return Result<PoliticsCatalog>.Failure(PoliticsErrorKeys.TanimGecersiz);
                }
            }

            var crisisMap = new StableIdMap<CrisisId, CrisisDefinition>(crises.Count);
            for (int i = 0; i < crises.Count; i++)
            {
                CrisisDefinition c = crises[i];
                if (c == null || !c.Id.IsValid || string.IsNullOrEmpty(c.Key) || !InUnit(c.TriggerBelowStability) || !InUnit(c.DailyChance)
                    || c.DurationDays <= 0 || !EffectGrants.AllValid(c.Effects) || !crisisMap.TryAdd(c.Id, c))
                {
                    return Result<PoliticsCatalog>.Failure(PoliticsErrorKeys.TanimGecersiz);
                }
            }

            return Result<PoliticsCatalog>.Success(new PoliticsCatalog(formMap, partyMap, institutionMap, crisisMap, parameters));
        }

        private static bool InUnit(Fixed64 v) => !v.IsNegative && v <= Fixed64.One;
    }
}
