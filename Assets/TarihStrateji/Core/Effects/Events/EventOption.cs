using System.Collections.Generic;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>
    /// Olay seçeneği: <see cref="Grants"/> ülkeye <see cref="DurationDays"/> gün (0 = kalıcı)
    /// uygulanır, hazineye <see cref="TreasuryChange"/> eklenir (eksi = harcama).
    /// </summary>
    public sealed class EventOption
    {
        private readonly EffectGrant[] _grants;

        public EventOption(string key, EffectGrant[] grants, int durationDays, Fixed64 treasuryChange)
        {
            Key = key;
            _grants = grants ?? new EffectGrant[0];
            DurationDays = durationDays;
            TreasuryChange = treasuryChange;
        }

        public string Key { get; }

        public IReadOnlyList<EffectGrant> Grants => _grants;

        public int DurationDays { get; }

        public Fixed64 TreasuryChange { get; }
    }
}
