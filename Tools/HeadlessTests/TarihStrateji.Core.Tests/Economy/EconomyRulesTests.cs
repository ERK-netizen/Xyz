using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Tests.Support;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.World.Cities;

namespace TarihStrateji.Core.Tests.Economy
{
    [TestFixture]
    public class EconomyRulesTests
    {
        private static readonly GameDate Day = GameDate.FromYmd(1925, 1, 1);
        private static EconomyCatalog Catalog => EconomyScenario.Content.Economy;

        private static Fixed64 F(string s) => Fixed64.Parse(s);

        private static void Workday(EconomyScenario s, EffectEngine effects = null)
        {
            WorkforceRules.AssignDay(s.Economy, s.Population, s.Map, Catalog, EconomyScenario.Content.Demography);
            ProductionRules.RunDay(s.Economy, Catalog, s.Map, effects ?? new EffectEngine(), Day);
        }

        private static void AssertInvariants(EconomyScenario s)
        {
            var violations = new List<string>();
            Assert.That(EconomyInvariants.Check(s.Economy, Catalog, s.Map, violations), Is.EqualTo(0), string.Join("\n", violations));
        }

        [Test]
        public void Catalog_RejectsMilitaryGoodsFromNothing_AndBadData()
        {
            var parameters = new EconomyParameters(new GoodId(1), F("1"), F("0.5"), F("0.5"), F("0.1"), F("0.25"), F("4"));
            var food = new GoodDefinition(new GoodId(1), "mal.gida", GoodCategory.Food, F("2"));
            var rifle = new GoodDefinition(new GoodId(2), "mal.tufek", GoodCategory.MilitaryEquipment, F("20"));
            SectorKind[] any = { SectorKind.Industrial };
            var magic = new FacilityTypeDefinition(new FacilityTypeId(1), "tesis.sihir", null, new[] { new GoodAmount(rifle.Id, F("1")) }, 100, 1, F("1"), null, 1, F("0"), any);
            Assert.That(EconomyCatalog.Create(new[] { food, rifle }, new[] { magic }, parameters).ErrorKey, Is.EqualTo(EconomyErrorKeys.YoktanUretim));

            var noOutput = new FacilityTypeDefinition(new FacilityTypeId(1), "tesis.bos", null, null, 100, 1, F("1"), null, 1, F("0"), any);
            Assert.That(EconomyCatalog.Create(new[] { food }, new[] { noOutput }, parameters).ErrorKey, Is.EqualTo(EconomyErrorKeys.TanimGecersiz));
            Assert.That(EconomyCatalog.Create(new[] { food, food }, new FacilityTypeDefinition[0], parameters).ErrorKey, Is.EqualTo(EconomyErrorKeys.TanimGecersiz));
            var badFood = new EconomyParameters(new GoodId(2), F("1"), F("0.5"), F("0.5"), F("0.1"), F("0.25"), F("4"));
            Assert.That(EconomyCatalog.Create(new[] { food, rifle }, new FacilityTypeDefinition[0], badFood).ErrorKey, Is.EqualTo(EconomyErrorKeys.TanimGecersiz));
            var adminSector = new FacilityTypeDefinition(new FacilityTypeId(1), "tesis.x", null, new[] { new GoodAmount(food.Id, F("1")) }, 100, 1, F("1"), null, 1, F("0"), new[] { SectorKind.Administrative });
            Assert.That(EconomyCatalog.Create(new[] { food }, new[] { adminSector }, parameters).ErrorKey, Is.EqualTo(EconomyErrorKeys.TanimGecersiz));
            Assert.That(BaselineContent.CreateEconomy().FacilityTypes.Count, Is.EqualTo(9));
        }

        [Test]
        public void Workforce_FillsFacilitiesInIdOrder_WithinRegionalLabor()
        {
            var s = new EconomyScenario();
            Workday(s);
            long eastLabor = s.Population.Regions[s.East].LaborForce(EconomyScenario.Content.Demography.LaborParticipation);
            long required = 4 * 5000 + 4000 + 4000 + 3000;
            Assert.That(eastLabor, Is.LessThan(required), "senaryo bilerek az kadrolu");
            Facility steel = s.Economy.Facilities[s.SteelWorks];
            Assert.That(steel.Workers, Is.EqualTo(eastLabor - (4 * 5000 + 8000)), "en son kimlik kalan işçiyi alır");
            Assert.That(s.Population.Regions[s.East].Employed, Is.EqualTo(eastLabor));
            Assert.That(s.Population.Regions[s.West].Employed, Is.EqualTo(15_000));
        }

        [Test]
        public void Production_ConsumesInputs_ScalesWithStaffing_AndNeverGoesNegative()
        {
            var s = new EconomyScenario();
            Workday(s);
            Facility steel = s.Economy.Facilities[s.SteelWorks];
            Fixed64 staffing = Fixed64.FromRatio(steel.Workers, 3000);
            Assert.That(staffing > Fixed64.Zero && staffing < Fixed64.Parse("0.2"), Is.True, "az kadrolu");

            // Madenlerin kimliği küçük: aynı gün önce onlar üretir, çelikhane sonra tüketir.
            Assert.That(steel.OutputScale, Is.EqualTo(staffing));
            Assert.That(s.B.Stock(BaselineGoods.Steel), Is.EqualTo(Fixed64.FromInt(200) + F("6") * staffing));
            Assert.That(s.B.Stock(BaselineGoods.Coal), Is.EqualTo(F("20") - F("10") * staffing));
            Assert.That(s.A.Stock(BaselineGoods.Food), Is.EqualTo(Fixed64.FromInt(375)), "A: 300 + 3 çiftlik × 25");

            // Girdi kısıtı: demir madeni durur, stokta 0.5 demir kalır → ölçek 0.05.
            s.Economy.Facilities[s.Economy.Facilities.KeyAt(8)].Workers = 0;
            s.B.RemoveStock(BaselineGoods.Iron, s.B.Stock(BaselineGoods.Iron) - F("0.5"));
            ProductionRules.RunDay(s.Economy, Catalog, s.Map, new EffectEngine(), Day);
            Assert.That(steel.OutputScale, Is.EqualTo(F("0.05")));
            Assert.That(s.B.Stock(BaselineGoods.Iron), Is.EqualTo(Fixed64.Zero));
            AssertInvariants(s);
        }

        [Test]
        public void Production_UsesEffectsEfficiency_AndStopsUnderOccupation()
        {
            var s = new EconomyScenario();
            var effects = new EffectEngine();
            effects.Add(EffectTarget.Of(TestWorlds.CountryA), EconomyStatKeys.ProductionEfficiency, EffectOperation.Percent, F("0.2"), Day, 0, "olay.test");
            Workday(s, effects);
            Assert.That(s.A.Stock(BaselineGoods.Food), Is.EqualTo(Fixed64.FromInt(300) + F("90")), "3 × 25 × 1.2");

            s.Map.SetSectorController(s.Port.Sectors[2], TestWorlds.CountryB); // çiftliklerin sektörü işgal
            Workday(s, effects);
            Assert.That(s.A.Stock(BaselineGoods.Food), Is.EqualTo(Fixed64.FromInt(300) + F("90")), "işgal altında üretim yok");
            Assert.That(s.Population.Regions[s.West].Employed, Is.EqualTo(0));
        }

        [Test]
        public void Construction_PaysUpfront_ProgressesDaily_AndCompletesExactly()
        {
            var s = new EconomyScenario();
            SectorId res = s.Port.Sectors[2];
            Assert.That(ConstructionRules.Validate(s.Economy, Catalog, s.Map, TestWorlds.CountryA, BaselineFacilities.Farm, res).ErrorKey,
                Is.EqualTo(EconomyErrorKeys.SlotYetersiz), "3 çiftlik slotları doldurdu");
            Assert.That(ConstructionRules.Validate(s.Economy, Catalog, s.Map, TestWorlds.CountryA, BaselineFacilities.Farm, s.Inland.Sectors[2]).ErrorKey,
                Is.EqualTo(EconomyErrorKeys.SektorBizimDegil));
            Assert.That(ConstructionRules.Validate(s.Economy, Catalog, s.Map, TestWorlds.CountryA, BaselineFacilities.SteelWorks, s.Port.Sectors[1]).ErrorKey,
                Is.EqualTo(EconomyErrorKeys.SektorTuruUygunDegil));
            Assert.That(ConstructionRules.Validate(s.Economy, Catalog, s.Map, TestWorlds.CountryB, BaselineFacilities.ArmsFactory, s.Inland.Sectors[1]).ErrorKey,
                Is.EqualTo(EconomyErrorKeys.SlotYetersiz));

            SectorId port = s.Port.Sectors[1]; // liman, 2 slot
            Assert.That(ConstructionRules.Validate(s.Economy, Catalog, s.Map, TestWorlds.CountryA, BaselineFacilities.Shipyard, port).ErrorKey,
                Is.EqualTo(EconomyErrorKeys.SlotYetersiz), "tersane 3 slot ister");

            s.A.Treasury = Fixed64.FromInt(100);
            SectorId admin = s.Port.AdministrativeSector;
            Assert.That(ConstructionRules.Validate(s.Economy, Catalog, s.Map, TestWorlds.CountryA, BaselineFacilities.Farm, admin).ErrorKey,
                Is.EqualTo(EconomyErrorKeys.SektorTuruUygunDegil));

            var b = new EconomyScenario();
            SectorId fortress = b.Inland.Sectors[3];
            Assert.That(ConstructionRules.Validate(b.Economy, Catalog, b.Map, TestWorlds.CountryB, BaselineFacilities.Farm, fortress).ErrorKey,
                Is.EqualTo(EconomyErrorKeys.SektorTuruUygunDegil));
            b.Map.ReleaseBuildingSlots(b.Inland.Sectors[2], 1);
            b.Economy.Facilities.Remove(b.Economy.Facilities.KeyAt(1)); // doğudaki bir çiftliği kaldır (slot boşalt)
            Fixed64 before = b.B.Treasury;
            Result<ConstructionId> order = ConstructionRules.Order(b.Economy, Catalog, b.Map, TestWorlds.CountryB, BaselineFacilities.Farm, b.Inland.Sectors[2]);
            Assert.That(order.IsSuccess, Is.True, order.ErrorKey);
            Assert.That(b.B.Treasury, Is.EqualTo(before - F("400")));
            Assert.That(b.B.Ledger.ConstructionSpending, Is.EqualTo(F("400")));
            AssertInvariants(b);

            var completed = new List<FacilityId>();
            for (int d = 1; d < 45; d++)
            {
                ConstructionRules.AdvanceDay(b.Economy, Catalog, b.Map, new EffectEngine(), Day, completed);
            }

            Assert.That(completed, Is.Empty);
            ConstructionRules.AdvanceDay(b.Economy, Catalog, b.Map, new EffectEngine(), Day, completed);
            Assert.That(completed.Count, Is.EqualTo(1), "tam 45. gün");
            Assert.That(b.Economy.Facilities[completed[0]].Type, Is.EqualTo(BaselineFacilities.Farm));
            AssertInvariants(b);
        }

        [Test]
        public void Construction_PausesUnderOccupation_AndCancelRefundsHalf()
        {
            var s = new EconomyScenario();
            s.Economy.SeedStock(TestWorlds.CountryB, BaselineGoods.Steel, F("100"));
            SectorId target = s.Inland.Sectors[1];
            s.Map.ReleaseBuildingSlots(target, 1);
            s.Economy.Facilities.Remove(s.Economy.Facilities.KeyAt(6)); // doğu sanayideki çiftlik
            ConstructionId id = ConstructionRules.Order(s.Economy, Catalog, s.Map, TestWorlds.CountryB, BaselineFacilities.ConsumerFactory, target).Value;
            var completed = new List<FacilityId>();
            ConstructionRules.AdvanceDay(s.Economy, Catalog, s.Map, new EffectEngine(), Day, completed);
            s.Map.SetSectorController(target, TestWorlds.CountryA);
            ConstructionRules.AdvanceDay(s.Economy, Catalog, s.Map, new EffectEngine(), Day, completed);
            Assert.That(s.Economy.Constructions[id].Progress, Is.EqualTo(Fixed64.One), "işgalde ilerleme durur");

            Assert.That(ConstructionRules.Cancel(s.Economy, Catalog, s.Map, TestWorlds.CountryA, id).ErrorKey, Is.EqualTo(EconomyErrorKeys.InsaatBizimDegil));
            Fixed64 before = s.B.Treasury;
            int freeBefore = s.Map.Sectors[target].FreeBuildingSlots;
            Assert.That(ConstructionRules.Cancel(s.Economy, Catalog, s.Map, TestWorlds.CountryB, id).IsValid, Is.True);
            Assert.That(s.B.Treasury, Is.EqualTo(before + F("450")));
            Assert.That(s.Map.Sectors[target].FreeBuildingSlots, Is.EqualTo(freeBefore + 1));
            AssertInvariants(s);
        }

        [Test]
        public void ProRata_IsExact_AndRespectsCaps()
        {
            var rng = new TestRng(77);
            for (int trial = 0; trial < 2_000; trial++)
            {
                int n = rng.Next(1, 9);
                var caps = new Fixed64[n];
                Fixed64 sum = Fixed64.Zero;
                for (int i = 0; i < n; i++)
                {
                    caps[i] = Fixed64.FromRaw(rng.Next(0, 50_000_000));
                    sum += caps[i];
                }

                Fixed64 total = Fixed64.FromRaw((long)(sum.Raw * (long)rng.Next(0, 101) / 100));
                var result = new Fixed64[n];
                ProRata.Allocate(total, caps, n, result);
                Fixed64 got = Fixed64.Zero;
                for (int i = 0; i < n; i++)
                {
                    Assert.That(result[i] >= Fixed64.Zero && result[i] <= caps[i], Is.True);
                    got += result[i];
                }

                Assert.That(got, Is.EqualTo(total));
            }
        }

        [Test]
        public void ProRataByWeight_IsExact_WithoutCaps()
        {
            var rng = new TestRng(78);
            for (int trial = 0; trial < 2_000; trial++)
            {
                int n = rng.Next(1, 9);
                var weights = new Fixed64[n];
                for (int i = 0; i < n; i++)
                {
                    weights[i] = Fixed64.FromRaw(rng.Chance(20) ? 0 : rng.Next(1, 50_000_000));
                }

                Fixed64 total = Fixed64.FromRaw(rng.Next(0, 900_000_000));
                var result = new Fixed64[n];
                ProRata.AllocateByWeight(total, weights, n, result);
                Fixed64 got = Fixed64.Zero;
                bool anyWeight = false;
                for (int i = 0; i < n; i++)
                {
                    Assert.That(result[i].IsNegative, Is.False);
                    Assert.That(weights[i].IsZero ? result[i].IsZero : true, Is.True, "ağırlıksız pay almaz");
                    anyWeight |= weights[i].IsPositive;
                    got += result[i];
                }

                Assert.That(got, Is.EqualTo(anyWeight ? total : Fixed64.Zero));
            }
        }

        [Test]
        public void Market_ConservesMoneyAndGoods_AndMovesPrice()
        {
            var s = new EconomyScenario();
            TradeOrderRules.Place(s.Economy, Catalog, TestWorlds.CountryA, BaselineGoods.Steel, TradeSide.Buy, F("30"), F("20"));
            TradeOrderRules.Place(s.Economy, Catalog, TestWorlds.CountryB, BaselineGoods.Steel, TradeSide.Sell, F("10"), F("5"));
            TradeOrderRules.Place(s.Economy, Catalog, TestWorlds.CountryB, BaselineGoods.Food, TradeSide.Sell, F("50"), F("50"));

            Fixed64 money = s.A.Treasury + s.B.Treasury;
            Fixed64 steel = s.A.Stock(BaselineGoods.Steel) + s.B.Stock(BaselineGoods.Steel);
            MarketRules.ClearDay(s.Economy, Catalog);

            Assert.That(s.A.Treasury + s.B.Treasury, Is.EqualTo(money), "ticaret para yaratmaz/yok etmez");
            Assert.That(s.A.Stock(BaselineGoods.Steel) + s.B.Stock(BaselineGoods.Steel), Is.EqualTo(steel));
            MarketGood m = s.Economy.Market[BaselineGoods.Steel];
            Assert.That(m.Price, Is.EqualTo(F("6") * F("1.05")), "B=30, S=10 → dengesizlik 0.5 → +%5");
            Assert.That(m.VolumeToday, Is.EqualTo(F("10")));
            Assert.That(s.A.Stock(BaselineGoods.Steel), Is.EqualTo(F("10")));
            Assert.That(s.A.Ledger.TradeExpense, Is.EqualTo(s.B.Ledger.TradeIncome));
            Assert.That(s.Economy.Market.ContainsKey(BaselineGoods.Food), Is.True);
            Assert.That(s.Economy.Market[BaselineGoods.Food].VolumeToday, Is.EqualTo(Fixed64.Zero), "alıcı yok");
            Assert.That(s.Economy.Market[BaselineGoods.Food].Price, Is.EqualTo(F("2")), "fiyat sınırı uygun değil: arz/talep yok, fiyat değişmez");
        }

        [Test]
        public void Market_RandomScenarios_AlwaysConserve()
        {
            var rng = new TestRng(1453);
            for (int trial = 0; trial < 200; trial++)
            {
                var s = new EconomyScenario();
                s.A.Treasury = Fixed64.FromInt(rng.Next(0, 500));
                for (int k = 0; k < 6; k++)
                {
                    CountryId c = rng.Chance(50) ? TestWorlds.CountryA : TestWorlds.CountryB;
                    GoodId g = rng.Chance(50) ? BaselineGoods.Steel : BaselineGoods.Food;
                    TradeSide side = rng.Chance(50) ? TradeSide.Buy : TradeSide.Sell;
                    TradeOrderRules.Place(s.Economy, Catalog, c, g, side, Fixed64.FromRaw(rng.Next(1, 80_000_000)), Fixed64.FromRaw(rng.Next(500_000, 15_000_000)));
                }

                for (int day = 0; day < 5; day++)
                {
                    Fixed64 money = s.A.Treasury + s.B.Treasury;
                    Fixed64 steel = s.A.Stock(BaselineGoods.Steel) + s.B.Stock(BaselineGoods.Steel);
                    Fixed64 food = s.A.Stock(BaselineGoods.Food) + s.B.Stock(BaselineGoods.Food);
                    MarketRules.ClearDay(s.Economy, Catalog);
                    Assert.That(s.A.Treasury + s.B.Treasury, Is.EqualTo(money));
                    Assert.That(s.A.Stock(BaselineGoods.Steel) + s.B.Stock(BaselineGoods.Steel), Is.EqualTo(steel));
                    Assert.That(s.A.Stock(BaselineGoods.Food) + s.B.Stock(BaselineGoods.Food), Is.EqualTo(food));
                    Assert.That(s.A.Treasury.IsNegative || s.B.Treasury.IsNegative, Is.False, "alıcı bütçesini aşmaz");
                    AssertInvariants(s);
                }
            }
        }

        [Test]
        public void Budget_TaxUpkeepFood_AndLedgerMatchesTreasury()
        {
            var s = new EconomyScenario();
            BudgetRules.ResetLedgers(s.Economy);
            Workday(s);
            Fixed64 before = s.A.Treasury;
            Fixed64 output = s.A.OutputValueToday;
            Assert.That(output, Is.EqualTo(F("150")), "75 gıda × 2");
            BudgetRules.SettleDay(s.Economy, Catalog, s.Population, s.Map, new EffectEngine(), Day);
            Assert.That(s.A.Ledger.TaxIncome, Is.EqualTo(F("30")));
            Assert.That(s.A.Ledger.FacilityUpkeep, Is.EqualTo(F("9")));
            Assert.That(s.A.Treasury - before, Is.EqualTo(s.A.Ledger.Net));
            Assert.That(s.A.Stock(BaselineGoods.Food), Is.EqualTo(F("375") - F("60")), "60 bin kişi × 1/1000");
            Assert.That(s.A.FoodShortage, Is.EqualTo(Fixed64.Zero));

            s.A.RemoveStock(BaselineGoods.Food, s.A.Stock(BaselineGoods.Food) - F("15"));
            BudgetRules.SettleDay(s.Economy, Catalog, s.Population, s.Map, new EffectEngine(), Day);
            Assert.That(s.A.FoodShortage, Is.EqualTo(F("0.75")), "15 / 60 karşılandı");
            Assert.That(TradeOrderRules.CheckTaxRate(s.Economy, Catalog, TestWorlds.CountryA, F("0.51")).ErrorKey, Is.EqualTo(EconomyErrorKeys.VergiOraniGecersiz));
        }

        [Test]
        public void Invariants_DetectSlotMismatch_AndSnapshotRoundTrips()
        {
            var s = new EconomyScenario();
            TradeOrderRules.Place(s.Economy, Catalog, TestWorlds.CountryA, BaselineGoods.Steel, TradeSide.Buy, F("3"), F("9"));
            Workday(s);
            MarketRules.ClearDay(s.Economy, Catalog);
            var w = new CanonicalWriter();
            EconomySnapshotCodec.Write(s.Economy, w);
            Result<EconomyState> back = EconomySnapshotCodec.Read(new CanonicalReader(w.ToArray()));
            Assert.That(back.IsSuccess, Is.True, back.ErrorKey);
            var w2 = new CanonicalWriter();
            EconomySnapshotCodec.Write(back.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));

            s.Economy.Facilities.Remove(s.Economy.Facilities.KeyAt(0)); // slot bırakılmadan tesis silindi
            var violations = new List<string>();
            Assert.That(EconomyInvariants.Check(s.Economy, Catalog, s.Map, violations), Is.GreaterThan(0));
            Assert.That(violations, Has.Some.StartsWith(EconomyErrorKeys.SlotMuhasebesiTutarsiz));
        }
    }
}
