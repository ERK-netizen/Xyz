using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Tests.Simulation;

namespace TarihStrateji.Core.Tests.Scenario
{
    /// <summary>Başlangıç senaryosu "Ege 1925": kurulum, okuma modeli (savaş sisi) ve YZ'li koşu.</summary>
    [TestFixture]
    public class AegeanScenarioTests
    {
        private static readonly GameContent Content = BaselineContent.Create();

        private static SimulationKernel Kernel(ulong seed, GameMode mode, CountryId player)
        {
            SimulationSetup setup = StandardRules.InstallArtificialIntelligence(StandardRules.Install(new SimulationSetup { HashIntervalDays = 1 }, Content), Content);
            return TestSimulation.Kernel(setup, ScenarioFactory.CreateAegean(Content, seed, mode, player));
        }

        [Test]
        public void Scenario_BuildsTheAegeanWithConsistentState()
        {
            SimulationKernel k = Kernel(1925, GameMode.Normal, BaselineStatecraft.Turkey);
            GameState s = k.State;
            Assert.That(s.World.Cities.Count, Is.EqualTo(14));
            Assert.That(s.World.Regions.Count, Is.EqualTo(14));
            Assert.That(s.Warfare.Units.Count, Is.EqualTo(11));
            Assert.That(s.Warfare.Fleets.Count, Is.EqualTo(2));
            Assert.That(k.MapWidth, Is.EqualTo(720));
            var cities = new List<CityView>();
            k.GetCities(cities);
            Assert.That(cities.Exists(c => c.NameKey == "sehir.ege.istanbul" && c.Owner == BaselineStatecraft.Turkey), Is.True);
            Assert.That(cities.Exists(c => c.NameKey == "sehir.ege.atina" && c.Owner == BaselineStatecraft.Greece), Is.True);
            Assert.That(k.TryGetCountry(BaselineStatecraft.Turkey, out CountryView tr), Is.True);
            Assert.That(tr.Divisions, Is.EqualTo(6));
            Assert.That(tr.Population, Is.GreaterThan(2_000_000));
            TestSimulation.AdvanceOk(k, 1);
        }

        [Test]
        public void ReadModel_AppliesFogOfWar_ForThePlayer_AndShowsAllToTheObserver()
        {
            SimulationKernel player = Kernel(7, GameMode.Normal, BaselineStatecraft.Turkey);
            TestSimulation.AdvanceOk(player, 1);
            var units = new List<UnitView>();
            player.GetVisibleUnits(units);
            Assert.That(units.FindAll(u => u.IsOwn).Count, Is.EqualTo(7), "6 tümen + 1 filo");
            Assert.That(units.Exists(u => u.Owner == BaselineStatecraft.Greece && u.Kind == UnitViewKind.Land), Is.False, "Atina'daki Yunan tümenleri İstanbul'dan görülmez");
            int visible = 0;
            for (int i = 0; i < player.MapWidth * player.MapHeight; i++)
            {
                if (player.IsCellVisible(i))
                {
                    visible++;
                }
            }

            Assert.That(visible, Is.InRange(50, 2000), "yalnız sensör çevresi görünür");

            SimulationKernel observer = Kernel(7, GameMode.Observer, CountryId.None);
            TestSimulation.AdvanceOk(observer, 1);
            units.Clear();
            observer.GetVisibleUnits(units);
            Assert.That(units.Count, Is.EqualTo(13), "gözlemci her şeyi görür");
            Assert.That(observer.IsCellVisible(0), Is.True);
        }

        [Test]
        public void Scenario_RunsWithAi_Deterministically()
        {
            var hashes = new List<string>();
            for (int run = 0; run < 2; run++)
            {
                SimulationKernel k = Kernel(42, GameMode.Normal, BaselineStatecraft.Turkey);
                for (int d = 0; d < 120; d++)
                {
                    Result<DayReport> r = k.AdvanceDay();
                    Assert.That(r.IsSuccess && !r.Value.Halted, Is.True, r.IsSuccess ? string.Join("\n", r.Value.Violations) : r.ErrorKey);
                    if (d == 119)
                    {
                        hashes.Add(Sha256.ToHex(r.Value.StateHash));
                    }
                }
            }

            Assert.That(hashes[1], Is.EqualTo(hashes[0]));
        }
    }
}
