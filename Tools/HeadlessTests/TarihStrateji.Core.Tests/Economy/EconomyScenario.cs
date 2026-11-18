using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;

namespace TarihStrateji.Core.Tests.Economy
{
    /// <summary>
    /// Kıyı haritası üzerinde iki ülkeli ekonomi: A (batı, liman şehri) çiftçi; B (doğu, iç şehir)
    /// çiftlik + maden + çelikhane. Nüfus: batı 60 bin, doğu 80 bin.
    /// </summary>
    public sealed class EconomyScenario
    {
        public static readonly GameContent Content = BaselineContent.Create();

        public EconomyScenario()
            : this(TestWorlds.Coastal(out _, out _), new PopulationState(), new EconomyState())
        {
        }

        /// <summary>Var olan bir oyun durumunun haritası, nüfusu ve ekonomisi üzerine kurar.</summary>
        public EconomyScenario(WorldMap map, PopulationState population, EconomyState economy)
        {
            Map = map;
            Port = Map.Cities.ValueAt(0);
            Inland = Map.Cities.ValueAt(1);
            Economy = economy;
            Population = population;
            West = Map.Regions.ValueAt(0).Id;
            East = Map.Regions.ValueAt(1).Id;
            Population.AddRegion(West, BaselineContent.AgePyramid(60_000));
            Population.AddRegion(East, BaselineContent.AgePyramid(80_000));
            Economy.AddCountry(TestWorlds.CountryA, Fixed64.FromInt(10_000), Fixed64.Parse("0.2"));
            Economy.AddCountry(TestWorlds.CountryB, Fixed64.FromInt(10_000), Fixed64.Parse("0.2"));

            SectorId westResidential = Port.Sectors[2];
            SectorId eastIndustrial = Inland.Sectors[1];
            SectorId eastResidential = Inland.Sectors[2];
            for (int i = 0; i < 3; i++)
            {
                Place(TestWorlds.CountryA, BaselineFacilities.Farm, westResidential);
                Place(TestWorlds.CountryB, BaselineFacilities.Farm, eastResidential);
            }

            Place(TestWorlds.CountryB, BaselineFacilities.Farm, eastIndustrial);
            Place(TestWorlds.CountryB, BaselineFacilities.CoalMine, eastIndustrial);
            Place(TestWorlds.CountryB, BaselineFacilities.IronMine, eastIndustrial);
            SteelWorks = Place(TestWorlds.CountryB, BaselineFacilities.SteelWorks, eastIndustrial);
            Economy.SeedStock(TestWorlds.CountryA, BaselineGoods.Food, Fixed64.FromInt(300));
            Economy.SeedStock(TestWorlds.CountryB, BaselineGoods.Food, Fixed64.FromInt(300));
            Economy.SeedStock(TestWorlds.CountryB, BaselineGoods.Steel, Fixed64.FromInt(200));
        }

        public WorldMap Map { get; }

        public City Port { get; }

        public City Inland { get; }

        public EconomyState Economy { get; }

        public PopulationState Population { get; }

        public RegionId West { get; }

        public RegionId East { get; }

        public FacilityId SteelWorks { get; }

        public CountryEconomy A => Economy.Countries[TestWorlds.CountryA];

        public CountryEconomy B => Economy.Countries[TestWorlds.CountryB];

        public FacilityId Place(CountryId country, FacilityTypeId type, SectorId sector)
        {
            var result = ConstructionRules.PlaceExisting(Economy, Content.Economy, Map, country, type, sector);
            Assert.That(result.IsSuccess, Is.True, result.ErrorKey);
            return result.Value;
        }
    }
}
