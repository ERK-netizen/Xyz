using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems;
using TarihStrateji.Core.Tests.Military;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.Naval
{
    /// <summary>
    /// Boğaz senaryosu (720×10 ızgara, ≈42 km hücre): A kıyısı sütun 100–109, deniz 110–112,
    /// B kıyısı 113–122 (satır 1–8). A: iç şehir (103,4), liman (109,4) doğuya bakar. B: liman
    /// (113,4) batıya bakar, iç şehir (118,4). A ile B savaşta; her ülkede gemi, uçak ve teçhizat stoku.
    /// </summary>
    public sealed class StraitScenario
    {
        public static readonly GameContent Content = FrontScenario.Content;

        public StraitScenario(ulong seed = 1922)
        {
            var grid = new HexGrid(720, 10, Fixed64.FromInt(42), Fixed64.FromInt(38));
            var b = new WorldBuilder(grid);
            var west = new List<HexCoord>();
            var east = new List<HexCoord>();
            for (int row = 1; row <= 8; row++)
            {
                for (int col = 100; col <= 122; col++)
                {
                    if (col >= 110 && col <= 112)
                    {
                        continue;
                    }

                    var c = new HexCoord(col, row);
                    b.SetTerrain(c, TerrainType.Plains);
                    (col <= 109 ? west : east).Add(c);
                }
            }

            b.AddLandRegion("bolge.test.batiKiyi", west, TestWorlds.CountryA);
            b.AddLandRegion("bolge.test.doguKiyi", east, TestWorlds.CountryB);
            b.AddCity("sehir.test.aIc", new HexCoord(103, 4), CityTier.SmallCity, HexDirection.East,
                new[] { new SectorSpec(SectorKind.Administrative, 4), new SectorSpec(SectorKind.Industrial, 4), new SectorSpec(SectorKind.Residential, 3) });
            b.AddCity("sehir.test.aLiman", new HexCoord(109, 4), CityTier.SmallCity, HexDirection.East,
                new[] { new SectorSpec(SectorKind.Administrative, 2), new SectorSpec(SectorKind.Port, 3), new SectorSpec(SectorKind.Residential, 2) });
            b.AddCity("sehir.test.bLiman", new HexCoord(113, 4), CityTier.SmallCity, HexDirection.West,
                new[] { new SectorSpec(SectorKind.Administrative, 2), new SectorSpec(SectorKind.Port, 3), new SectorSpec(SectorKind.Residential, 2) });
            b.AddCity("sehir.test.bIc", new HexCoord(118, 4), CityTier.SmallCity, HexDirection.West,
                new[] { new SectorSpec(SectorKind.Administrative, 4), new SectorSpec(SectorKind.Industrial, 4), new SectorSpec(SectorKind.Residential, 3) });
            Result<WorldMap> map = b.Build();
            Assert.That(map.IsSuccess, Is.True, map.ErrorKey);

            State = GameState.CreateNew(seed, GameMode.Normal, GameDate.FromYmd(1922, 3, 1), TestWorlds.CountryA, map.Value);
            West = State.World.Regions.ValueAt(0).Id;
            East = State.World.Regions.ValueAt(1).Id;
            State.Population.AddRegion(West, BaselineContent.AgePyramid(400_000));
            State.Population.AddRegion(East, BaselineContent.AgePyramid(400_000));
            foreach (CountryId c in new[] { TestWorlds.CountryA, TestWorlds.CountryB })
            {
                State.Economy.AddCountry(c, Fixed64.FromInt(50_000), Fixed64.Parse("0.2"));
                State.Economy.SeedStock(c, BaselineGoods.InfantryEquipment, Fixed64.FromInt(2_000));
                State.Economy.SeedStock(c, BaselineGoods.Ship, Fixed64.FromInt(40));
                State.Economy.SeedStock(c, BaselineGoods.Aircraft, Fixed64.FromInt(500));
                State.Economy.SeedStock(c, BaselineGoods.Food, Fixed64.FromInt(10_000));
            }

            Core.Diplomacy.DiplomacyRules.StartWar(State.Diplomacy, TestWorlds.CountryA, TestWorlds.CountryB, State.Today);
            InlandA = State.World.Cities.ValueAt(0);
            PortA = State.World.Cities.ValueAt(1);
            PortB = State.World.Cities.ValueAt(2);
            InlandB = State.World.Cities.ValueAt(3);
        }

        public GameState State { get; }

        public RegionId West { get; }

        public RegionId East { get; }

        public City InlandA { get; }

        public City PortA { get; }

        public City PortB { get; }

        public City InlandB { get; }

        public WarfareContext War => WarfareContexts.From(State, Content);

        public HexCell Cell(int col, int row) => State.World.Cell(new HexCoord(col, row));

        public HexId Hex(int col, int row) => State.World.Grid.ToId(new HexCoord(col, row));

        public static HexCoord At(City city, WorldMap map) => map.Grid.FromId(city.Hex);

        /// <summary>Ülkenin ana limanında filo kurar, tam teşkilat verir ve istenen hücreye taşır.</summary>
        public Fleet Fleet(CountryId country, UnitTypeId type, int col, int row)
        {
            City port = country == TestWorlds.CountryA ? PortA : PortB;
            Result<FleetId> id = FleetRules.Build(War, country, type, At(port, State.World));
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            Fleet fleet = State.Warfare.Fleets[id.Value];
            fleet.Hex = Hex(col, row);
            fleet.Organization = Content.Warfare.ShipTypes[type].MaxOrganization;
            return fleet;
        }

        public LandUnit Unit(CountryId country, int col, int row)
        {
            City city = country == TestWorlds.CountryA ? InlandA : InlandB;
            Result<LandUnitId> id = UnitRules.Recruit(War, country, BaselineUnits.InfantryDivision, At(city, State.World));
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            LandUnit unit = State.Warfare.Units[id.Value];
            unit.Hex = Hex(col, row);
            unit.Organization = Content.Warfare.UnitTypes[BaselineUnits.InfantryDivision].MaxOrganization;
            unit.Supply = Fixed64.One;
            return unit;
        }

        public AirWing Wing(CountryId country, UnitTypeId type, City baseCity)
        {
            Result<AirWingId> id = AirRules.Recruit(War, country, type, At(baseCity, State.World));
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            AirWing wing = State.Warfare.AirWings[id.Value];
            wing.Organization = Content.Warfare.AirWingTypes[type].MaxOrganization;
            return wing;
        }

        /// <summary>Şehrin hücresini ve tüm sektörlerini <paramref name="country"/> kontrolüne verir (işgal kurulumu).</summary>
        public void Occupy(City city, CountryId country)
        {
            foreach (SectorId id in city.Sectors)
            {
                State.World.SetSectorController(id, country);
            }
        }
    }
}
