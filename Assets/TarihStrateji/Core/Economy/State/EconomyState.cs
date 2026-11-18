using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.State
{
    /// <summary>Tüm ekonomi durumu (düz veri, kimlik sırasında).</summary>
    public sealed class EconomyState
    {
        public StableIdMap<CountryId, CountryEconomy> Countries { get; } = new StableIdMap<CountryId, CountryEconomy>();

        public StableIdMap<FacilityId, Facility> Facilities { get; } = new StableIdMap<FacilityId, Facility>();

        public StableIdMap<ConstructionId, ConstructionOrder> Constructions { get; } = new StableIdMap<ConstructionId, ConstructionOrder>();

        public StableIdMap<TradeOrderId, TradeOrder> TradeOrders { get; } = new StableIdMap<TradeOrderId, TradeOrder>();

        public StableIdMap<GoodId, MarketGood> Market { get; } = new StableIdMap<GoodId, MarketGood>();

        public StableIdAllocator FacilityIds { get; internal set; } = new StableIdAllocator();

        public StableIdAllocator ConstructionIds { get; internal set; } = new StableIdAllocator();

        public StableIdAllocator TradeOrderIds { get; internal set; } = new StableIdAllocator();

        /// <summary>Senaryo kurulumu: ülke ekonomisi ekler.</summary>
        public Result<CountryEconomy> AddCountry(CountryId country, Fixed64 treasury, Fixed64 taxRate)
        {
            if (!country.IsValid || Countries.ContainsKey(country))
            {
                return Result<CountryEconomy>.Failure(EconomyErrorKeys.UlkeZatenVar);
            }

            if (taxRate.IsNegative || taxRate > Fixed64.One)
            {
                return Result<CountryEconomy>.Failure(EconomyErrorKeys.VergiOraniGecersiz);
            }

            var economy = new CountryEconomy(country) { Treasury = treasury, TaxRate = taxRate };
            Countries.Add(country, economy);
            return Result<CountryEconomy>.Success(economy);
        }

        /// <summary>Senaryo kurulumu: başlangıç stoku (yalnız kurulumda; oyun içinde mal yalnız üretimle gelir).</summary>
        public void SeedStock(CountryId country, GoodId good, Fixed64 amount)
        {
            Countries[country].AddStock(good, amount);
        }
    }
}
