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
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.Military
{
    /// <summary>
    /// 720×10 ızgarada (≈42–44 km hücre) kara cephesi: sütun 100–109 A, 110–119 B, 120–124 tarafsız
    /// C (savaşta değil); satır 1–8 kara (ova), gerisi deniz. A şehirleri (102,4) ve (106,2); B
    /// şehri (114,4) 4 sektörlü, batıya bakan yuvada kale. A ile B savaşta.
    /// </summary>
    public sealed class FrontScenario
    {
        public static readonly GameContent Content = BaselineContent.Create();
        public static readonly GameDate Start = GameDate.FromYmd(1921, 7, 1);

        public FrontScenario(GameMode mode = GameMode.Normal, ulong seed = 1921)
        {
            var grid = new HexGrid(720, 10, Fixed64.FromInt(42), Fixed64.FromInt(38));
            var b = new WorldBuilder(grid);
            var west = new List<HexCoord>();
            var east = new List<HexCoord>();
            var neutral = new List<HexCoord>();
            for (int row = 1; row <= 8; row++)
            {
                for (int col = 100; col <= 124; col++)
                {
                    var c = new HexCoord(col, row);
                    b.SetTerrain(c, TerrainType.Plains);
                    (col <= 109 ? west : col <= 119 ? east : neutral).Add(c);
                }
            }

            b.AddLandRegion("bolge.test.bati", west, TestWorlds.CountryA);
            b.AddLandRegion("bolge.test.dogu", east, TestWorlds.CountryB);
            b.AddLandRegion("bolge.test.tarafsiz", neutral, TestWorlds.CountryC);
            b.AddCity("sehir.test.a1", new HexCoord(102, 4), CityTier.SmallCity, HexDirection.East,
                new[] { new SectorSpec(SectorKind.Administrative, 4), new SectorSpec(SectorKind.Industrial, 4), new SectorSpec(SectorKind.Residential, 3) });
            b.AddCity("sehir.test.a2", new HexCoord(106, 2), CityTier.Town, HexDirection.East,
                new[] { new SectorSpec(SectorKind.Administrative, 2), new SectorSpec(SectorKind.Residential, 2) });
            b.AddCity("sehir.test.b1", new HexCoord(114, 4), CityTier.City, HexDirection.West,
                new[] { new SectorSpec(SectorKind.Administrative, 4), new SectorSpec(SectorKind.Fortress, 1), new SectorSpec(SectorKind.Residential, 3), new SectorSpec(SectorKind.Industrial, 4) });
            Result<WorldMap> map = b.Build();
            Assert.That(map.IsSuccess, Is.True, map.ErrorKey);

            State = GameState.CreateNew(seed, mode, Start, mode == GameMode.Observer ? CountryId.None : TestWorlds.CountryA, map.Value);
            West = State.World.Regions.ValueAt(0).Id;
            East = State.World.Regions.ValueAt(1).Id;
            State.Population.AddRegion(West, BaselineContent.AgePyramid(400_000));
            State.Population.AddRegion(East, BaselineContent.AgePyramid(400_000));
            State.Population.AddRegion(State.World.Regions.ValueAt(2).Id, BaselineContent.AgePyramid(100_000));
            foreach (CountryId c in new[] { TestWorlds.CountryA, TestWorlds.CountryB, TestWorlds.CountryC })
            {
                State.Economy.AddCountry(c, Fixed64.FromInt(50_000), Fixed64.Parse("0.2"));
                State.Economy.SeedStock(c, BaselineGoods.InfantryEquipment, Fixed64.FromInt(2_000));
                State.Economy.SeedStock(c, BaselineGoods.Artillery, Fixed64.FromInt(200));
                State.Economy.SeedStock(c, BaselineGoods.Food, Fixed64.FromInt(10_000));
            }

            State.Wars.SetAtWar(TestWorlds.CountryA, TestWorlds.CountryB, true);
            CityA = State.World.Cities.ValueAt(0);
            CityB = State.World.Cities.ValueAt(2);
        }

        public GameState State { get; }

        public RegionId West { get; }

        public RegionId East { get; }

        public City CityA { get; }

        public City CityB { get; }

        public WarfareContext War => WarfareContexts.From(State, Content);

        public HexCell Cell(int col, int row) => State.World.Cell(new HexCoord(col, row));

        /// <summary>Şehirde birlik toplar ve istenen hücreye ışınlar (yalnız test kurulumu).</summary>
        public LandUnit Place(CountryId country, UnitTypeId type, int col, int row)
        {
            City city = country == TestWorlds.CountryA ? CityA : CityB;
            Result<LandUnitId> id = UnitRules.Recruit(War, country, type, State.World.Grid.FromId(city.Hex));
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            LandUnit unit = State.Warfare.Units[id.Value];
            unit.Hex = State.World.Grid.ToId(new HexCoord(col, row));
            unit.Organization = Content.Warfare.UnitTypes[type].MaxOrganization;
            unit.Supply = Fixed64.One;
            return unit;
        }
    }
}
