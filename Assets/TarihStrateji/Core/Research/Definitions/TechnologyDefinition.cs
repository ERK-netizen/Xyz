using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;

namespace TarihStrateji.Core.Research.Definitions
{
    /// <summary>Teknoloji (içerik verisi): ön koşullar, çağ, maliyet, tarih penceresi ve kalıcı etkiler.</summary>
    public sealed class TechnologyDefinition
    {
        private readonly TechnologyId[] _prerequisites;
        private readonly EffectGrant[] _effects;

        public TechnologyDefinition(
            TechnologyId id,
            string key,
            int era,
            TechnologyId[] prerequisites,
            Fixed64 cost,
            GameDate availableFrom,
            EffectGrant[] effects)
        {
            Id = id;
            Key = key;
            Era = era;
            _prerequisites = prerequisites ?? new TechnologyId[0];
            Cost = cost;
            AvailableFrom = availableFrom;
            _effects = effects ?? new EffectGrant[0];
        }

        public TechnologyId Id { get; }

        public string Key { get; }

        /// <summary>Gereken en düşük çağ dizini.</summary>
        public int Era { get; }

        public IReadOnlyList<TechnologyId> Prerequisites => _prerequisites;

        /// <summary>Araştırma puanı.</summary>
        public Fixed64 Cost { get; }

        /// <summary>Bu tarihten önce araştırmaya başlanamaz.</summary>
        public GameDate AvailableFrom { get; }

        /// <summary>Tamamlanınca ülkeye kalıcı olarak uygulanır.</summary>
        public IReadOnlyList<EffectGrant> Effects => _effects;
    }
}
