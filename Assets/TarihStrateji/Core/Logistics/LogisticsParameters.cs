using System;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Logistics
{
    /// <summary>İkmal parametreleri (içerik verisi).</summary>
    public sealed class LogisticsParameters
    {
        public LogisticsParameters(Fixed64 baseSupplyRange)
        {
            if (!baseSupplyRange.IsPositive)
            {
                throw new ArgumentException(LogisticsErrorKeys.TanimGecersiz);
            }

            BaseSupplyRange = baseSupplyRange;
        }

        /// <summary>
        /// Kaynaktan itibaren ikmalin ulaştığı en büyük hareket maliyeti (km × arazi × altyapı).
        /// Ovada yolsuz ≈ km; demiryolu menzili uzatır.
        /// </summary>
        public Fixed64 BaseSupplyRange { get; }
    }
}
