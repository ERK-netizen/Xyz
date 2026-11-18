using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.State
{
    /// <summary>Dünya pazarında bir malın fiyatı ve son günkü işlem hacmi.</summary>
    public sealed class MarketGood
    {
        internal MarketGood(GoodId good, Fixed64 price)
        {
            Good = good;
            Price = price;
        }

        public GoodId Good { get; }

        public Fixed64 Price { get; internal set; }

        public Fixed64 VolumeToday { get; internal set; }
    }
}
