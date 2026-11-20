using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Content.Packages;
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
    /// <summary>"1914 MVP" senaryosu: kurulum, okuma modeli (ülke adları), kural sistemleriyle ve YZ'yle koşu.</summary>
    [TestFixture]
    public class Scenario1914Tests
    {
        private static readonly GameContent Content = BaselineContent.Create();
        private static readonly GeoPackage Package = GeoPackageTests.Load();

        public static SimulationSetup Setup(bool ai)
        {
            var setup = new SimulationSetup { HashIntervalDays = 1, GeoWorldTemplate = Package.BuildWorld, CountryNameKeys = Package.CountryNameKey };
            StandardRules.Install(setup, Content);
            return ai ? StandardRules.InstallArtificialIntelligence(setup, Content) : setup;
        }

        public static SimulationKernel Kernel(ulong seed, GameMode mode, bool ai) =>
            TestSimulation.Kernel(Setup(ai), ScenarioFactory.Create1914(Content, Package, seed, mode, mode == GameMode.Observer ? CountryId.None : Scenario1914.Ottoman));

        [Test]
        public void Scenario_BuildsAllCountries_WithArmiesFleetsAndNames()
        {
            SimulationKernel k = Kernel(1914, GameMode.Normal, false);
            GameState s = k.State;
            Assert.That(s.Today, Is.EqualTo(Package.Date));
            Assert.That(s.Economy.Countries.Count, Is.EqualTo(12));
            Assert.That(s.World.Cities.Count, Is.EqualTo(Package.Cities.Count));
            Assert.That(k.TryGetCountry(Scenario1914.Ottoman, out CountryView ottoman), Is.True);
            Assert.That(ottoman.Divisions, Is.EqualTo(10));
            Assert.That(ottoman.Population, Is.GreaterThan(3_000_000));
            Assert.That(k.CountryNameKey(Scenario1914.Ottoman), Is.EqualTo("ulke.1914.osmanli"));
            Assert.That(k.CountryNameKey(new CountryId(99)), Is.EqualTo("ulke.99"));
            int fleets = s.Warfare.Fleets.Count;
            Assert.That(fleets, Is.InRange(4, 12), "büyük limanı olan güçlerin filoları");
            for (int i = 0; i < Package.Countries.Count; i++)
            {
                Assert.That(k.TryGetCountry(Package.Countries[i], out CountryView c) && c.Divisions >= 1, Is.True, "ülke " + Package.Countries[i]);
            }

            TestSimulation.AdvanceOk(k, 30);
        }

        [Test]
        public void PeacefulYear_KeepsEconomiesSolvent_WithoutAi()
        {
            SimulationKernel k = Kernel(7, GameMode.Observer, false);
            TestSimulation.AdvanceOk(k, 120);
            for (int i = 0; i < k.State.Economy.Countries.Count; i++)
            {
                var c = k.State.Economy.Countries.ValueAt(i);
                Assert.That(c.Treasury.IsPositive, Is.True, "ülke " + c.Country + " hazine " + c.Treasury);
            }
        }

        [Test]
        public void ObserverCampaign_WithAi_IsDeterministic_AndSurvivesSaveLoad()
        {
            List<string> Run(SimulationKernel k, int from, int to)
            {
                var hashes = new List<string>();
                for (int d = from; d < to; d++)
                {
                    Result<DayReport> r = k.AdvanceDay();
                    Assert.That(r.IsSuccess && !r.Value.Halted, Is.True, r.IsSuccess ? string.Join("\n", r.Value.Violations) : r.ErrorKey);
                    hashes.Add(Sha256.ToHex(r.Value.StateHash));
                }

                return hashes;
            }

            List<string> full = Run(Kernel(11, GameMode.Observer, true), 0, 180);
            SimulationKernel b = Kernel(11, GameMode.Observer, true);
            Run(b, 0, 90);
            Result<SimulationKernel> restored = SimulationKernel.Restore(Setup(true), b.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            Assert.That(Run(restored.Value, 90, 180), Is.EqualTo(full.GetRange(90, 90)));
        }
    }
}
