using System;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Politics.Definitions
{
    /// <summary>Siyaset genel parametreleri (içerik verisi).</summary>
    public sealed class PoliticsParameters
    {
        public PoliticsParameters(Fixed64 stabilityDriftPerDay, Fixed64 rulingSupportDrift, Fixed64 institutionDecayPerDay, Fixed64 famineStabilityPenalty)
        {
            if (stabilityDriftPerDay.IsNegative || stabilityDriftPerDay > Fixed64.One || rulingSupportDrift.IsNegative
                || institutionDecayPerDay.IsNegative || famineStabilityPenalty.IsNegative)
            {
                throw new ArgumentException(PoliticsErrorKeys.TanimGecersiz);
            }

            StabilityDriftPerDay = stabilityDriftPerDay;
            RulingSupportDrift = rulingSupportDrift;
            InstitutionDecayPerDay = institutionDecayPerDay;
            FamineStabilityPenalty = famineStabilityPenalty;
        }

        /// <summary>İstikrar, hedefle arasındaki farkın bu oranı kadar her gün yaklaşır.</summary>
        public Fixed64 StabilityDriftPerDay { get; }

        /// <summary>İktidar desteği her gün × (1 + kayma × (istikrar − 0.5)).</summary>
        public Fixed64 RulingSupportDrift { get; }

        public Fixed64 InstitutionDecayPerDay { get; }

        /// <summary>Tam kıtlıkta istikrar hedefinden düşülen miktar.</summary>
        public Fixed64 FamineStabilityPenalty { get; }
    }
}
