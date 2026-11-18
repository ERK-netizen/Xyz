using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Commands.Economy;
using TarihStrateji.Core.Simulation.Scenario;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems;
using TarihStrateji.Core.Tests.Economy;
using TarihStrateji.Core.Tests.Support;
using TarihStrateji.Core.Tests.World;

namespace TarihStrateji.Core.Tests.Simulation
{
    /// <summary>Nüfus + ekonomi, çekirdekle uçtan uca (P0, P6–P10, P15).</summary>
    [TestFixture]
    public class EconomySimulationTests
    {
        private static readonly GameContent Content = EconomyScenario.Content;

        private static SimulationSetup Setup() => StandardRules.Install(new SimulationSetup(), EconomyScenario.Standard);

        private static SimulationKernel NewKernel(CountryId player, ulong seed = 1920)
        {
            GameState state = GameState.CreateNew(seed, GameMode.Normal, TestSimulation.Start, player, TestWorlds.Coastal(out _, out _));
            _ = new EconomyScenario(state.World, state.Population, state.Economy);
            return TestSimulation.Kernel(Setup(), state);
        }

        private static CountryEconomy Country(SimulationKernel k, CountryId id) => k.State.Economy.Countries[id];

        [Test]
        public void FullYear_RunsClean_AndLedgerExplainsEveryTreasuryChange()
        {
            SimulationKernel k = NewKernel(TestWorlds.CountryA);
            long startPopulation = k.State.Population.WorldTotal();
            for (int day = 0; day < 365; day++)
            {
                Fixed64 a = Country(k, TestWorlds.CountryA).Treasury;
                Fixed64 b = Country(k, TestWorlds.CountryB).Treasury;
                TestSimulation.AdvanceOk(k, 1);
                Assert.That(Country(k, TestWorlds.CountryA).Treasury - a, Is.EqualTo(Country(k, TestWorlds.CountryA).Ledger.Net), $"gün {day}");
                Assert.That(Country(k, TestWorlds.CountryB).Treasury - b, Is.EqualTo(Country(k, TestWorlds.CountryB).Ledger.Net), $"gün {day}");
            }

            long endPopulation = k.State.Population.WorldTotal();
            Assert.That(endPopulation, Is.GreaterThan(startPopulation));
            Assert.That(Country(k, TestWorlds.CountryA).FoodShortage, Is.EqualTo(Fixed64.Zero), "A kendi gıdasını üretir");
            Assert.That(Country(k, TestWorlds.CountryB).Stock(BaselineGoods.Steel), Is.GreaterThan(Fixed64.FromInt(200)), "B çelik üretir");
            Assert.That(Country(k, TestWorlds.CountryA).Treasury, Is.GreaterThan(Fixed64.FromInt(10_000)), "vergi > bakım");
        }

        [Test]
        public void BuildCommand_PaysAtP1_AndFactoryAppearsAfterBuildDays()
        {
            SimulationKernel k = NewKernel(TestWorlds.CountryB);
            var cmd = new BuildFacilityCommand(TestWorlds.CountryB, BaselineFacilities.ConsumerFactory, k.State.World.Cities.ValueAt(1).Sectors[1]);
            Assert.That(k.Submit(cmd).Accepted, Is.True);
            Assert.That(k.Submit(cmd).Accepted, Is.True, "ikincisi de gönderildiği an geçerli");

            DayReport first = k.AdvanceDay().Value;
            Assert.That(first.AppliedCommands, Is.EqualTo(1));
            Assert.That(first.RejectedCommands, Has.Count.EqualTo(1).And.Some.Contains(EconomyErrorKeys.SlotYetersiz), "son slotu ilk emir aldı");
            Assert.That(Country(k, TestWorlds.CountryB).Ledger.ConstructionSpending, Is.EqualTo(Fixed64.FromInt(900)));
            Assert.That(k.State.Economy.Constructions.Count, Is.EqualTo(1));

            var notes = new List<string>();
            for (int d = 1; d < 75; d++)
            {
                notes.AddRange(k.AdvanceDay().Value.Notes);
            }

            Assert.That(notes, Has.Some.StartsWith(ConstructionSystem.CompletedNote));
            Assert.That(k.State.Economy.Constructions.Count, Is.EqualTo(0));
            Assert.That(k.State.Economy.Facilities.Count, Is.EqualTo(11));
        }

        [Test]
        public void Commands_AreValidatedAgainstOwnershipAndLimits()
        {
            SimulationKernel k = NewKernel(TestWorlds.CountryA);
            SectorId bSector = k.State.World.Cities.ValueAt(1).Sectors[1];
            Assert.That(k.Validate(new BuildFacilityCommand(TestWorlds.CountryA, BaselineFacilities.Farm, bSector)).ErrorKey, Is.EqualTo(EconomyErrorKeys.SektorBizimDegil));
            Assert.That(k.Validate(new BuildFacilityCommand(TestWorlds.CountryA, new FacilityTypeId(99), bSector)).ErrorKey, Is.EqualTo(EconomyErrorKeys.TesisTuruBilinmiyor));
            Assert.That(k.Validate(new SetTaxRateCommand(TestWorlds.CountryA, Fixed64.Parse("0.9"))).ErrorKey, Is.EqualTo(EconomyErrorKeys.VergiOraniGecersiz));
            Assert.That(k.Validate(new PlaceTradeOrderCommand(TestWorlds.CountryA, new GoodId(99), TradeSide.Buy, Fixed64.One, Fixed64.One)).ErrorKey, Is.EqualTo(EconomyErrorKeys.MalBilinmiyor));
            Assert.That(k.Validate(new CancelConstructionCommand(TestWorlds.CountryA, new ConstructionId(1))).ErrorKey, Is.EqualTo(EconomyErrorKeys.InsaatBulunamadi));
            Assert.That(k.Validate(new BuildFacilityCommand(TestWorlds.CountryB, BaselineFacilities.Farm, bSector)).ErrorKey, Is.EqualTo(SimulationErrorKeys.YabanciUlkeEmri));

            Assert.That(k.Submit(new SetTaxRateCommand(TestWorlds.CountryA, Fixed64.Parse("0.35"))).Accepted, Is.True);
            Assert.That(k.Submit(new PlaceTradeOrderCommand(TestWorlds.CountryA, BaselineGoods.Food, TradeSide.Sell, Fixed64.FromInt(5), Fixed64.One)).Accepted, Is.True);
            TestSimulation.AdvanceOk(k, 1);
            Assert.That(Country(k, TestWorlds.CountryA).TaxRate, Is.EqualTo(Fixed64.Parse("0.35")));
            TradeOrderId order = k.State.Economy.TradeOrders.KeyAt(0);
            Assert.That(k.Submit(new CancelTradeOrderCommand(TestWorlds.CountryA, order)).Accepted, Is.True);
            TestSimulation.AdvanceOk(k, 1);
            Assert.That(k.State.Economy.TradeOrders.Count, Is.EqualTo(0));
        }

        [Test]
        public void Famine_KillsMoreThanPlenty()
        {
            SimulationKernel fed = NewKernel(TestWorlds.CountryA);
            SimulationKernel starved = NewKernel(TestWorlds.CountryA);
            CountryEconomy a = Country(starved, TestWorlds.CountryA);
            a.RemoveStock(BaselineGoods.Food, a.Stock(BaselineGoods.Food));
            starved.State.World.SetSectorController(starved.State.World.Cities.ValueAt(0).Sectors[2], TestWorlds.CountryB); // çiftlikler işgalde
            TestSimulation.AdvanceOk(fed, 120);
            TestSimulation.AdvanceOk(starved, 120);
            Assert.That(Country(starved, TestWorlds.CountryA).FoodShortage, Is.EqualTo(Fixed64.One));
            RegionId west = starved.State.World.Regions.ValueAt(0).Id;
            long fedWest = fed.State.Population.Regions[west].Total;
            long starvedWest = starved.State.Population.Regions[west].Total;
            Assert.That(starvedWest, Is.LessThan(fedWest - 500), $"tok {fedWest}, aç {starvedWest}");
        }

        private static List<string> Run(SimulationKernel k, int days)
        {
            var hashes = new List<string>();
            for (int d = 0; d < days; d++)
            {
                if (d == 30)
                {
                    k.Submit(new PlaceTradeOrderCommand(TestWorlds.CountryA, BaselineGoods.Food, TradeSide.Sell, Fixed64.FromInt(8), Fixed64.One));
                }

                Result<DayReport> r = k.AdvanceDay();
                Assert.That(r.IsSuccess && !r.Value.Halted, Is.True);
                hashes.Add(Sha256.ToHex(r.Value.StateHash));
            }

            return hashes;
        }

        [Test]
        public void SaveMidRun_ContinuesIdentically_WithEconomyAndPopulation()
        {
            List<string> expected = Run(NewKernel(TestWorlds.CountryA), 200);
            SimulationKernel first = NewKernel(TestWorlds.CountryA);
            Run(first, 100);
            Result<SimulationKernel> restored = SimulationKernel.Restore(Setup(), first.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            var resumed = new List<string>();
            for (int d = 100; d < 200; d++)
            {
                resumed.Add(Sha256.ToHex(restored.Value.AdvanceDay().Value.StateHash));
            }

            Assert.That(resumed, Is.EqualTo(expected.GetRange(100, 100)));
        }

        /// <summary>Altın: 120 günlük nüfus + ekonomi senaryosunun son durum hash'i.</summary>
        [Test]
        public void GoldenHash_EconomyScenario_Day120()
        {
            List<string> hashes = Run(NewKernel(TestWorlds.CountryA), 120);
            Assert.That(hashes[119], Is.EqualTo(GoldenValues.EconomyDay120), "yeni hash: " + hashes[119]);
        }
    }
}
