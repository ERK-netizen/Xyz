using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.Definitions
{
    /// <summary>Mal + miktar (tanım verisi).</summary>
    [Serializable]
    public readonly struct GoodAmount
    {
        public GoodAmount(GoodId good, Fixed64 amount)
        {
            Good = good;
            Amount = amount;
        }

        public GoodId Good { get; }

        public Fixed64 Amount { get; }
    }
}
