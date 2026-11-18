using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Effects;

namespace TarihStrateji.Core.Politics.Definitions
{
    /// <summary>
    /// Kriz türü (içerik verisi). İstikrar <see cref="TriggerBelowStability"/>'nin altındayken her
    /// gün <see cref="DailyChance"/> olasılıkla başlar; <see cref="DurationDays"/> sürer, başlarken
    /// istikrarı <see cref="StabilityShock"/> kadar sarsar ve süresince etkileri uygular.
    /// </summary>
    public sealed class CrisisDefinition
    {
        private readonly EffectGrant[] _effects;

        public CrisisDefinition(CrisisId id, string key, Fixed64 triggerBelowStability, Fixed64 dailyChance, int durationDays, Fixed64 stabilityShock, EffectGrant[] effects)
        {
            Id = id;
            Key = key;
            TriggerBelowStability = triggerBelowStability;
            DailyChance = dailyChance;
            DurationDays = durationDays;
            StabilityShock = stabilityShock;
            _effects = effects ?? new EffectGrant[0];
        }

        public CrisisId Id { get; }

        public string Key { get; }

        public Fixed64 TriggerBelowStability { get; }

        public Fixed64 DailyChance { get; }

        public int DurationDays { get; }

        /// <summary>Başlangıçta istikrara eklenir (genelde negatif).</summary>
        public Fixed64 StabilityShock { get; }

        public IReadOnlyList<EffectGrant> Effects => _effects;
    }
}
