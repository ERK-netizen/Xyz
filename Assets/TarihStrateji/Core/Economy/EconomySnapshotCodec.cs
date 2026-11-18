using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Economy.State;

namespace TarihStrateji.Core.Economy
{
    /// <summary>
    /// <see cref="EconomyState"/>'in kanonik anlık görüntüsü. Gün defteri ve günlük ara değerler
    /// de yazılır: P10'da yazılıp ertesi gün okunan alanlar (gıda açığı) yeniden oynatma için gerekir.
    /// </summary>
    public static class EconomySnapshotCodec
    {
        public const uint Tag = 0x4E4F4345; // "ECON"

        public static void Write(EconomyState s, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteUInt64(s.FacilityIds.NextValue);
            w.WriteUInt64(s.ConstructionIds.NextValue);
            w.WriteUInt64(s.TradeOrderIds.NextValue);

            w.WriteInt32(s.Countries.Count);
            for (int i = 0; i < s.Countries.Count; i++)
            {
                CountryEconomy c = s.Countries.ValueAt(i);
                w.WriteId(c.Country);
                w.WriteFixed64(c.Treasury);
                w.WriteFixed64(c.TaxRate);
                w.WriteFixed64(c.FoodShortage);
                w.WriteFixed64(c.OutputValueToday);
                BudgetLedger l = c.Ledger;
                w.WriteFixed64(l.TaxIncome);
                w.WriteFixed64(l.TradeIncome);
                w.WriteFixed64(l.ConstructionRefunds);
                w.WriteFixed64(l.TradeExpense);
                w.WriteFixed64(l.FacilityUpkeep);
                w.WriteFixed64(l.ConstructionSpending);
                w.WriteFixed64(l.OtherSpending);
                w.WriteInt32(c.Inventory.Count);
                for (int g = 0; g < c.Inventory.Count; g++)
                {
                    w.WriteId(c.Inventory.KeyAt(g));
                    w.WriteFixed64(c.Inventory.ValueAt(g));
                }
            }

            w.WriteInt32(s.Facilities.Count);
            for (int i = 0; i < s.Facilities.Count; i++)
            {
                Facility f = s.Facilities.ValueAt(i);
                w.WriteId(f.Id);
                w.WriteId(f.Type);
                w.WriteId(f.Sector);
                w.WriteId(f.Owner);
                w.WriteInt64(f.Workers);
                w.WriteFixed64(f.OutputScale);
            }

            w.WriteInt32(s.Constructions.Count);
            for (int i = 0; i < s.Constructions.Count; i++)
            {
                ConstructionOrder o = s.Constructions.ValueAt(i);
                w.WriteId(o.Id);
                w.WriteId(o.Country);
                w.WriteId(o.Type);
                w.WriteId(o.Sector);
                w.WriteFixed64(o.Progress);
            }

            w.WriteInt32(s.TradeOrders.Count);
            for (int i = 0; i < s.TradeOrders.Count; i++)
            {
                TradeOrder t = s.TradeOrders.ValueAt(i);
                w.WriteId(t.Id);
                w.WriteId(t.Country);
                w.WriteId(t.Good);
                w.WriteByte((byte)t.Side);
                w.WriteFixed64(t.QuantityPerDay);
                w.WriteFixed64(t.LimitPrice);
                w.WriteFixed64(t.FilledToday);
            }

            w.WriteInt32(s.Market.Count);
            for (int i = 0; i < s.Market.Count; i++)
            {
                MarketGood m = s.Market.ValueAt(i);
                w.WriteId(m.Good);
                w.WriteFixed64(m.Price);
                w.WriteFixed64(m.VolumeToday);
            }
        }

        public static Result<EconomyState> Read(CanonicalReader r)
        {
            try
            {
                return ReadCore(r);
            }
            catch (ArgumentException)
            {
                return Result<EconomyState>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }

        private static Result<EconomyState> ReadCore(CanonicalReader r)
        {
            r.ExpectTag(Tag);
            var s = new EconomyState
            {
                FacilityIds = new StableIdAllocator(r.ReadUInt64()),
                ConstructionIds = new StableIdAllocator(r.ReadUInt64()),
                TradeOrderIds = new StableIdAllocator(r.ReadUInt64()),
            };

            int countries = r.ReadCount(r.Remaining);
            for (int i = 0; i < countries; i++)
            {
                var c = new CountryEconomy(new CountryId(r.ReadUInt64()))
                {
                    Treasury = r.ReadFixed64(),
                    TaxRate = r.ReadFixed64(),
                    FoodShortage = r.ReadFixed64(),
                    OutputValueToday = r.ReadFixed64(),
                };
                c.Ledger.TaxIncome = r.ReadFixed64();
                c.Ledger.TradeIncome = r.ReadFixed64();
                c.Ledger.ConstructionRefunds = r.ReadFixed64();
                c.Ledger.TradeExpense = r.ReadFixed64();
                c.Ledger.FacilityUpkeep = r.ReadFixed64();
                c.Ledger.ConstructionSpending = r.ReadFixed64();
                c.Ledger.OtherSpending = r.ReadFixed64();
                int goods = r.ReadCount(r.Remaining);
                for (int g = 0; g < goods; g++)
                {
                    c.Inventory.Add(new GoodId(r.ReadUInt64()), r.ReadFixed64());
                }

                s.Countries.Add(c.Country, c);
            }

            int facilities = r.ReadCount(r.Remaining);
            for (int i = 0; i < facilities; i++)
            {
                var f = new Facility(new FacilityId(r.ReadUInt64()), new FacilityTypeId(r.ReadUInt64()), new SectorId(r.ReadUInt64()), new CountryId(r.ReadUInt64()))
                {
                    Workers = r.ReadInt64(),
                    OutputScale = r.ReadFixed64(),
                };
                if (f.Id.Value >= s.FacilityIds.NextValue)
                {
                    return Result<EconomyState>.Failure(DomainErrorKeys.KayitBozuk);
                }

                s.Facilities.Add(f.Id, f);
            }

            int constructions = r.ReadCount(r.Remaining);
            for (int i = 0; i < constructions; i++)
            {
                var o = new ConstructionOrder(new ConstructionId(r.ReadUInt64()), new CountryId(r.ReadUInt64()), new FacilityTypeId(r.ReadUInt64()), new SectorId(r.ReadUInt64()))
                {
                    Progress = r.ReadFixed64(),
                };
                if (o.Id.Value >= s.ConstructionIds.NextValue)
                {
                    return Result<EconomyState>.Failure(DomainErrorKeys.KayitBozuk);
                }

                s.Constructions.Add(o.Id, o);
            }

            int trades = r.ReadCount(r.Remaining);
            for (int i = 0; i < trades; i++)
            {
                var id = new TradeOrderId(r.ReadUInt64());
                var country = new CountryId(r.ReadUInt64());
                var good = new GoodId(r.ReadUInt64());
                var side = (TradeSide)r.ReadByte();
                var t = new TradeOrder(id, country, good, side, r.ReadFixed64(), r.ReadFixed64()) { FilledToday = r.ReadFixed64() };
                if (side > TradeSide.Sell || id.Value >= s.TradeOrderIds.NextValue)
                {
                    return Result<EconomyState>.Failure(DomainErrorKeys.KayitBozuk);
                }

                s.TradeOrders.Add(id, t);
            }

            int market = r.ReadCount(r.Remaining);
            for (int i = 0; i < market; i++)
            {
                var m = new MarketGood(new GoodId(r.ReadUInt64()), r.ReadFixed64()) { VolumeToday = r.ReadFixed64() };
                s.Market.Add(m.Good, m);
            }

            return Result<EconomyState>.Success(s);
        }
    }
}
