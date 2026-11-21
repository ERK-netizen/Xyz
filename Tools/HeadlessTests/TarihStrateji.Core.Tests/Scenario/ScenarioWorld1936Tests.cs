using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Scenario
{
    /// <summary>
    /// "Dünya 1936" senaryosu (Adım 21): tam dünya paketi (71 devlet, CShapes birimlerinin egemenlere bağlanması), ±180°
    /// deniz dikişi, başkentler, Türkiye siyaseti, YZ'li 90 günde determinizm, değişmezler ve barış (YZ ≥ 07.07.1937).
    /// </summary>
    [TestFixture]
    public class ScenarioWorld1936Tests
    {
        private static readonly GameContent Content = BaselineContent.Create();
        private static readonly GeoPackage Package = Load();

        private static GeoPackage Load()
        {
            string path = Path.Combine(Path.GetDirectoryName(GeoPackageTests.PackagePath), "dunya1936.bytes");
            Result<GeoPackage> package = GeoPackageReader.Read(File.ReadAllBytes(path));
            Assert.That(package.IsSuccess, Is.True, package.ErrorKey);
            return package.Value;
        }

        public static SimulationSetup Setup(bool ai)
        {
            var setup = new SimulationSetup
            {
                HashIntervalDays = 1, GeoWorldTemplate = Package.BuildWorld, CountryNameKeys = Package.CountryNameKey,
                Capitals = CapitalTable.FromCityKeys(Package.BuildWorld(), ScenarioWorld1936.CapitalCityKeys).Of,
                AiEarliestWarDate = ScenarioWorld1936.AiEarliestWar,
            };
            StandardRules.Install(setup, Content);
            return ai ? StandardRules.InstallArtificialIntelligence(setup, Content) : setup;
        }

        public static SimulationKernel Kernel(ulong seed, GameMode mode, bool ai) =>
            TestSimulation.Kernel(Setup(ai), ScenarioFactory.CreateWorld1936(Content, Package, seed, mode, mode == GameMode.Observer ? CountryId.None : ScenarioWorld1936.Turkey));

        private static CountryId OwnerOf(SimulationKernel k, string cityKey)
        {
            k.State.World.TryGetCity(ScenarioFactory.CityOf(k.State, cityKey), out City city);
            return city.Owner;
        }

        [Test]
        public void Package_CoversTheWorld_WithSovereignsAndColonies()
        {
            Assert.That(Package.Date, Is.EqualTo(GameDate.FromYmd(1936, 3, 7)));
            Assert.That(Package.Countries.Count, Is.EqualTo(71));
            SimulationKernel k = Kernel(1936, GameMode.Normal, false);
            Assert.That(k.State.Economy.Countries.Count, Is.EqualTo(71));
            Assert.That(k.CountryNameKey(ScenarioWorld1936.Turkey), Is.EqualTo("ulke.d1936.turkiye"));
            Assert.That(k.State.World.Regions.Count, Is.GreaterThan(350));

            var britain = new CountryId(11);
            var france = new CountryId(13);
            Assert.That(OwnerOf(k, "sehir.d1936.yenidelhi"), Is.EqualTo(britain), "Hindistan İngiliz");
            Assert.That(OwnerOf(k, "sehir.d1936.cezayir"), Is.EqualTo(france), "Cezayir Fransız");
            Assert.That(OwnerOf(k, "sehir.d1936.batavya"), Is.EqualTo(new CountryId(19)), "Hollanda Doğu Hint Adaları");
            Assert.That(OwnerOf(k, "sehir.d1936.leopoldville"), Is.EqualTo(new CountryId(20)), "Belçika Kongosu");
            Assert.That(OwnerOf(k, "sehir.d1936.manila"), Is.EqualTo(new CountryId(30)), "Filipinler ABD'ye bağlı");
            Assert.That(OwnerOf(k, "sehir.d1936.asmara"), Is.EqualTo(new CountryId(9)), "Eritre İtalyan");

            var cities = new List<CityView>();
            k.GetCities(cities);
            int capitals = 0;
            for (int i = 0; i < cities.Count; i++)
            {
                capitals += cities[i].IsCapital ? 1 : 0;
            }

            Assert.That(capitals, Is.EqualTo(71), "her egemen devletin tek başkenti");
        }

        [Test]
        public void SeaGraph_CrossesTheAntimeridian()
        {
            GeoWorld world = Package.BuildWorld();
            int crossing = 0;
            IReadOnlyList<TransportNodeId> nodes = world.Transport.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                world.Transport.TryGetNode(nodes[i], out TransportNode a);
                IReadOnlyList<TransportEdge> edges = world.Transport.EdgesOf(nodes[i]);
                for (int e = 0; e < edges.Count; e++)
                {
                    TransportNodeId other = edges[e].A == nodes[i] ? edges[e].B : edges[e].A;
                    world.Transport.TryGetNode(other, out TransportNode b);
                    int lonA = a.Location.LongitudeE6;
                    int lonB = b.Location.LongitudeE6;
                    crossing += lonA > 170_000_000 && lonB < -170_000_000 ? 1 : 0;
                }
            }

            Assert.That(crossing, Is.GreaterThan(5), "Pasifik ±180°'de bölünmez");
        }

        [Test]
        public void NinetyDays_WithAi_Deterministic_AndAtPeace()
        {
            SimulationKernel a = Kernel(36, GameMode.Observer, true);
            SimulationKernel b = Kernel(36, GameMode.Observer, true);
            for (int d = 0; d < 90; d++)
            {
                Result<DayReport> ra = a.AdvanceDay();
                Result<DayReport> rb = b.AdvanceDay();
                Assert.That(ra.IsSuccess && !ra.Value.Halted, Is.True, ra.IsSuccess ? string.Join("\n", ra.Value.Violations) : ra.ErrorKey);
                Assert.That(Sha256.ToHex(ra.Value.StateHash), Is.EqualTo(Sha256.ToHex(rb.Value.StateHash)), "aynı tohum → aynı hash, gün " + d);
            }

            Assert.That(a.State.Diplomacy.Wars.Count, Is.EqualTo(0), "YZ 07.07.1937'den önce savaş ilan etmez");
        }
    }
}
