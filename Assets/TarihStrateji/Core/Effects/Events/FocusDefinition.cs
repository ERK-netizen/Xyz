using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>
    /// Ulusal odak: ön koşulları tamamlanmış, dışlayıcılarından hiçbiri tamamlanmamış ve
    /// koşulları sağlanan ülke seçer; <see cref="Days"/> gün sonra tamamlanır, <see cref="Grants"/>
    /// kalıcı uygulanır ve hazineye <see cref="TreasuryChange"/> eklenir. Dışlama simetriktir.
    /// </summary>
    public sealed class FocusDefinition
    {
        private readonly FocusId[] _prerequisites;
        private readonly FocusId[] _exclusive;
        private readonly Condition[] _conditions;
        private readonly EffectGrant[] _grants;

        public FocusDefinition(FocusId id, string key, CountryId country, int days, FocusId[] prerequisites, FocusId[] exclusive, Condition[] conditions, EffectGrant[] grants, Fixed64 treasuryChange)
        {
            Id = id;
            Key = key;
            Country = country;
            Days = days;
            _prerequisites = prerequisites ?? new FocusId[0];
            _exclusive = exclusive ?? new FocusId[0];
            _conditions = conditions ?? new Condition[0];
            _grants = grants ?? new EffectGrant[0];
            TreasuryChange = treasuryChange;
        }

        public FocusId Id { get; }

        public string Key { get; }

        public CountryId Country { get; }

        public int Days { get; }

        public IReadOnlyList<FocusId> Prerequisites => _prerequisites;

        public IReadOnlyList<FocusId> Exclusive => _exclusive;

        public IReadOnlyList<Condition> Conditions => _conditions;

        public IReadOnlyList<EffectGrant> Grants => _grants;

        public Fixed64 TreasuryChange { get; }
    }
}
