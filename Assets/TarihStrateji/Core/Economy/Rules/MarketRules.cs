using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>
    /// Dünya pazarı (P10). Her mal için, kimlik sırasıyla:
    /// <list type="number">
    /// <item>Bugünkü fiyatta istenen alış (B) ve satış (S) hacmine göre fiyat bir adım ayarlanır:
    /// <c>fiyat × (1 + adım × (B − S)/(B + S))</c>, temel fiyatın [min, max] katı içinde.</item>
    /// <item>Yeni fiyatta uygun emirler eşleşir; alıcı hazinesi, satıcı stoğuyla sınırlıdır.
    /// Hacim = min(ΣB, ΣS) iki tarafa orantılı ve tam bölüştürülür.</item>
    /// <item>Alıcıların ödediği toplam para satıcılara miktarla orantılı ve tam dağıtılır: ticaret
    /// para veya mal yaratmaz, yok etmez.</item>
    /// </list>
    /// </summary>
    public static class MarketRules
    {
        public static void ClearDay(EconomyState economy, EconomyCatalog catalog)
        {
            var orders = new List<TradeOrder>();
            for (int g = 0; g < catalog.Goods.Count; g++)
            {
                GoodDefinition good = catalog.Goods.ValueAt(g);
                orders.Clear();
                for (int i = 0; i < economy.TradeOrders.Count; i++)
                {
                    TradeOrder order = economy.TradeOrders.ValueAt(i);
                    order.FilledToday = order.Good == good.Id ? Fixed64.Zero : order.FilledToday;
                    if (order.Good == good.Id)
                    {
                        orders.Add(order);
                    }
                }

                if (!economy.Market.TryGetValue(good.Id, out MarketGood market))
                {
                    if (orders.Count == 0)
                    {
                        continue;
                    }

                    market = new MarketGood(good.Id, good.BasePrice);
                    economy.Market.Add(good.Id, market);
                }

                market.VolumeToday = Fixed64.Zero;
                if (orders.Count == 0)
                {
                    continue;
                }

                Fixed64 demand = Fixed64.Zero;
                Fixed64 supply = Fixed64.Zero;
                for (int i = 0; i < orders.Count; i++)
                {
                    Fixed64 cap = Capacity(economy, orders[i], market.Price);
                    if (orders[i].Side == TradeSide.Buy)
                    {
                        demand += cap;
                    }
                    else
                    {
                        supply += cap;
                    }
                }

                if ((demand + supply).IsPositive)
                {
                    Fixed64 imbalance = Fixed64.MulDiv(demand - supply, Fixed64.One, demand + supply);
                    Fixed64 next = market.Price * (Fixed64.One + (catalog.Parameters.MarketPriceStep * imbalance));
                    Fixed64 min = good.BasePrice * catalog.Parameters.MinPriceFactor;
                    Fixed64 max = good.BasePrice * catalog.Parameters.MaxPriceFactor;
                    market.Price = Fixed64.Clamp(next, min, max);
                }

                Execute(economy, orders, market);
            }
        }

        private static void Execute(EconomyState economy, List<TradeOrder> orders, MarketGood market)
        {
            int n = orders.Count;
            var caps = new Fixed64[n];
            var buyCaps = new Fixed64[n];
            var sellCaps = new Fixed64[n];
            Fixed64 totalBuy = Fixed64.Zero;
            Fixed64 totalSell = Fixed64.Zero;
            for (int i = 0; i < n; i++)
            {
                caps[i] = Capacity(economy, orders[i], market.Price);
                if (orders[i].Side == TradeSide.Buy)
                {
                    buyCaps[i] = caps[i];
                    totalBuy += caps[i];
                }
                else
                {
                    sellCaps[i] = caps[i];
                    totalSell += caps[i];
                }
            }

            Fixed64 volume = Fixed64.Min(totalBuy, totalSell);
            if (!volume.IsPositive)
            {
                return;
            }

            var bought = new Fixed64[n];
            var sold = new Fixed64[n];
            ProRata.Allocate(volume, buyCaps, n, bought);
            ProRata.Allocate(volume, sellCaps, n, sold);

            Fixed64 money = Fixed64.Zero;
            for (int i = 0; i < n; i++)
            {
                if (!bought[i].IsPositive)
                {
                    continue;
                }

                CountryEconomy buyer = economy.Countries[orders[i].Country];
                Fixed64 cost = Fixed64.Min(bought[i] * market.Price, Fixed64.Max(Fixed64.Zero, buyer.Treasury));
                buyer.Treasury -= cost;
                buyer.Ledger.TradeExpense += cost;
                buyer.AddStock(market.Good, bought[i]);
                orders[i].FilledToday = bought[i];
                money += cost;
            }

            var proceeds = new Fixed64[n];
            ProRata.AllocateByWeight(money, sold, n, proceeds);
            for (int i = 0; i < n; i++)
            {
                if (!sold[i].IsPositive)
                {
                    continue;
                }

                CountryEconomy seller = economy.Countries[orders[i].Country];
                seller.RemoveStock(market.Good, sold[i]);
                seller.Treasury += proceeds[i];
                seller.Ledger.TradeIncome += proceeds[i];
                orders[i].FilledToday = sold[i];
            }

            market.VolumeToday = volume;
        }

        /// <summary>Emrin bu fiyattaki günlük kapasitesi (fiyat sınırı, hazine ve stokla).</summary>
        private static Fixed64 Capacity(EconomyState economy, TradeOrder order, Fixed64 price)
        {
            if (!economy.Countries.TryGetValue(order.Country, out CountryEconomy country) || !price.IsPositive)
            {
                return Fixed64.Zero;
            }

            if (order.Side == TradeSide.Buy)
            {
                if (price > order.LimitPrice || !country.Treasury.IsPositive)
                {
                    return Fixed64.Zero;
                }

                Fixed64 affordable = Fixed64.FromRaw((country.Treasury / price).Raw);
                while ((affordable * price) > country.Treasury && affordable.IsPositive)
                {
                    affordable = Fixed64.FromRaw(affordable.Raw - 1L);
                }

                return Fixed64.Min(order.QuantityPerDay, affordable);
            }

            return price < order.LimitPrice ? Fixed64.Zero : Fixed64.Min(order.QuantityPerDay, country.Stock(order.Good));
        }
    }
}
