using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Scenario
{
    /// <summary>"1936" senaryosu (Adım 19): paket, 15 ülke, Hatay/Arnavutluk/Avusturya/Onikiadalar, başkentler, Türkiye siyaseti, YZ'nin 07.04.1939'dan önce savaş açmaması (bir yıl tam barış), determinizm ve bant.</summary>
    [TestFixture]
    public class Scenario1936Tests
    {
        private static readonly GameContent Content = BaselineContent.Create();
        private static readonly GeoPackage Package = Load();

        private static GeoPackage Load()
        {
            string path = Path.Combine(Path.GetDirectoryName(GeoPackageTests.PackagePath), "1936_mvp.bytes");
            Result<GeoPackage> package = GeoPackageReader.Read(File.ReadAllBytes(path));
            Assert.That(package.IsSuccess, Is.True, package.ErrorKey);
            return package.Value;
        }

        public static SimulationSetup Setup(bool ai)
        {
            var setup = new SimulationSetup
            {
                HashIntervalDays = 1, GeoWorldTemplate = Package.BuildWorld, CountryNameKeys = Package.CountryNameKey,
                Capitals = CapitalTable.FromCityKeys(Package.BuildWorld(), Scenario1936.CapitalCityKeys).Of,
                AiEarliestWarDate = Scenario1936.AiEarliestWar,
            };
            StandardRules.Install(setup, Content);
            return ai ? StandardRules.InstallArtificialIntelligence(setup, Content) : setup;
        }

        public static SimulationKernel Kernel(ulong seed, GameMode mode, bool ai) =>
            TestSimulation.Kernel(Setup(ai), ScenarioFactory.Create1936(Content, Package, seed, mode, mode == GameMode.Observer ? CountryId.None : Scenario1936.Turkey));

        private static CountryId OwnerOf(SimulationKernel k, string cityKey)
        {
            k.State.World.TryGetCity(ScenarioFactory.CityOf(k.State, cityKey), out City city);
            return city.Owner;
        }

        [Test]
        public void Package_And_Setup_FollowMarch1936()
        {
            Assert.That(Package.Date, Is.EqualTo(GameDate.FromYmd(1936, 3, 7)));
            Assert.That(Package.Countries.Count, Is.EqualTo(15));
            SimulationKernel k = Kernel(1936, GameMode.Normal, false);
            Assert.That(k.State.Economy.Countries.Count, Is.EqualTo(15));
            Assert.That(k.CountryNameKey(Scenario1936.Turkey), Is.EqualTo("ulke.1936.turkiye"));

            var france = new CountryId(13);
            Assert.That(OwnerOf(k, "sehir.1936.tiran"), Is.EqualTo(new CountryId(15)), "Arnavutluk bağımsız (Zogu krallığı)");
            Assert.That(OwnerOf(k, "sehir.1936.rodos"), Is.EqualTo(new CountryId(9)), "Onikiadalar İtalyan");
            Assert.That(OwnerOf(k, "sehir.1936.antakya"), Is.EqualTo(france), "Hatay 1936'da Fransız mandası Suriye'de");
            Assert.That(OwnerOf(k, "sehir.1936.iskenderun"), Is.EqualTo(france), "İskenderun Sancağı");
            Assert.That(OwnerOf(k, "sehir.1936.adana"), Is.EqualTo(Scenario1936.Turkey), "Hatay düzeltmesi Adana'ya taşmaz");
            Assert.That(OwnerOf(k, "sehir.1936.graz"), Is.EqualTo(new CountryId(8)), "Avusturya bağımsız");
            Assert.That(OwnerOf(k, "sehir.1936.lefkosa"), Is.EqualTo(new CountryId(11)), "Kıbrıs İngiliz");

            Assert.That(k.State.Politics.Countries.TryGetValue(Scenario1936.Turkey, out var politics), Is.True);
            Assert.That(politics.Form, Is.EqualTo(BaselineStatecraft.SingleParty));
            Assert.That(politics.RulingParty, Is.EqualTo(BaselineStatecraft.RepublicanPeoples));

            var cities = new List<CityView>();
            k.GetCities(cities);
            int capitals = 0;
            string turkishCapital = null;
            for (int i = 0; i < cities.Count; i++)
            {
                if (cities[i].IsCapital)
                {
                    capitals++;
                    turkishCapital = cities[i].Owner == Scenario1936.Turkey ? cities[i].NameKey : turkishCapital;
                }
            }

            Assert.That(capitals, Is.EqualTo(10));
            Assert.That(turkishCapital, Is.EqualTo("sehir.1936.ankara"));
            Assert.That(k.TryGetCountry(Scenario1936.Turkey, out CountryView turkey), Is.True);
            Assert.That(turkey.Divisions, Is.GreaterThan(0));
        }

        [Test]
        public void ScenarioView_PairsCountriesWithNameKeys()
        {
            var names = new string[Package.Countries.Count];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = Package.CountryNameKey(Package.Countries[i]);
            }

            var view = new ScenarioView("1936", "senaryo.1936", Package.Date, Scenario1936.Turkey, Package.Countries, names);
            Assert.That(view.Countries.Count, Is.EqualTo(15));
            Assert.That(view.CountryNameKeys[0], Is.EqualTo("ulke.1936.turkiye"));
            Assert.That(() => new ScenarioView("x", "y", Package.Date, Scenario1936.Turkey, Package.Countries, new string[1]), Throws.ArgumentException);
        }

        [Test]
        public void Year_WithAi_StaysDeterministic_InsideBands_AndAtPeace()
        {
            SimulationKernel a = Kernel(41, GameMode.Observer, true);
            SimulationKernel b = Kernel(41, GameMode.Observer, true);
            var treasury = new Dictionary<CountryId, Fixed64>();
            for (int i = 0; i < a.State.Economy.Countries.Count; i++)
            {
                treasury[a.State.Economy.Countries.KeyAt(i)] = a.State.Economy.Countries.ValueAt(i).Treasury;
            }

            GameDate firstWar = GameDate.MaxValue;
            for (int d = 0; d < 365; d++)
            {
                Result<DayReport> ra = a.AdvanceDay();
                Result<DayReport> rb = b.AdvanceDay();
                Assert.That(ra.IsSuccess && !ra.Value.Halted, Is.True, ra.IsSuccess ? string.Join("\n", ra.Value.Violations) : ra.ErrorKey);
                Assert.That(Sha256.ToHex(ra.Value.StateHash), Is.EqualTo(Sha256.ToHex(rb.Value.StateHash)), "aynı tohum → aynı hash, gün " + d);
                if (a.State.Diplomacy.Wars.Count > 0 && firstWar == GameDate.MaxValue)
                {
                    firstWar = a.State.Today;
                }
            }

            Assert.That(firstWar, Is.EqualTo(GameDate.MaxValue), "YZ 07.04.1939'dan önce savaş ilan etmez: ilk yıl tam barış");
            // Barış ekonomisi bandı (Docs/DENGE_1914.md, "1939 barış ekonomisi", çözüldü): hazine oranı P10 ≥ 0,25, P90 ≤ 3,0.
            var ratios = new List<Fixed64>();
            for (int i = 0; i < a.State.Economy.Countries.Count; i++)
            {
                CountryId c = a.State.Economy.Countries.KeyAt(i);
                ratios.Add(Fixed64.MulDiv(a.State.Economy.Countries.ValueAt(i).Treasury, Fixed64.One, treasury[c]));
            }

            ratios.Sort();
            Fixed64 p10 = ratios[((ratios.Count - 1) * 10 + 50) / 100];
            Fixed64 p90 = ratios[((ratios.Count - 1) * 90 + 50) / 100];
            Assert.That(p10 >= Fixed64.Parse("0.25") && p90 <= Fixed64.FromInt(3), Is.True, "hazine oranı P10 " + p10 + ", P90 " + p90);
            Assert.That(ratios[ratios.Count - 1] <= Fixed64.FromInt(4), Is.True, "aykırı ülke bile kaçak büyümez: en çok " + ratios[ratios.Count - 1]);
        }
    }
}
