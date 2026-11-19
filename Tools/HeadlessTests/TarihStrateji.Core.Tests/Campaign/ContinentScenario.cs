using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.Campaign
{
    /// <summary>
    /// 365 günlük başsız kampanya senaryosu (Gözlemci modu, tüm ülkeler YZ): 720×24 ızgarada (50°–40° K,
    /// ≈38–46 km hücre)
    /// 40×20 hücrelik kıta dört eşit çeyreğe bölünür (A kuzeybatı, B kuzeydoğu, C güneybatı,
    /// D güneydoğu). Her ülkenin iki bölgesi, bir başkenti (tarım, maden, çelik, silah) ve bir
    /// kasabası vardır. A 6, diğerleri 3 piyade tümeniyle başlar (bilinçli dengesizlik: YZ'nin
    /// savaş kararlarını tetikler). Bazı hücreler tepe/orman.
    /// </summary>
    public sealed class ContinentScenario
    {
        public static readonly GameContent Content = BaselineContent.Create();
        public static readonly CountryId A = new CountryId(1);
        public static readonly CountryId B = new CountryId(2);
        public static readonly CountryId C = new CountryId(3);
        public static readonly CountryId D = new CountryId(4);
        public static readonly CountryId[] All = { A, B, C, D };
        public static readonly GameDate Start = GameDate.FromYmd(1925, 1, 1);

        public ContinentScenario(ulong seed)
        {
            var grid = new HexGrid(720, 24, Fixed64.FromInt(50), Fixed64.FromInt(40));
            var b = new WorldBuilder(grid);
            var regions = new List<HexCoord>[8];
            for (int i = 0; i < 8; i++)
            {
                regions[i] = new List<HexCoord>();
            }

            for (int row = 2; row <= 21; row++)
            {
                for (int col = 100; col <= 139; col++)
                {
                    var c = new HexCoord(col, row);
                    TerrainType t = (col + row) % 7 == 0 ? TerrainType.Hills : (col * 3 + row) % 11 == 0 ? TerrainType.Forest : TerrainType.Plains;
                    b.SetTerrain(c, t);
                    int country = (row <= 11 ? 0 : 2) + (col <= 119 ? 0 : 1);
                    int half = (col - 100) % 20 < 10 ? 0 : 1;
                    regions[(country * 2) + half].Add(c);
                }
            }

            for (int i = 0; i < 8; i++)
            {
                b.AddLandRegion("bolge.kita." + i.ToString(System.Globalization.CultureInfo.InvariantCulture), regions[i], All[i / 2]);
            }

            for (int k = 0; k < 4; k++)
            {
                int col0 = k % 2 == 0 ? 100 : 120;
                int row = k < 2 ? 6 : 17;
                b.AddCity("sehir.kita.baskent" + k.ToString(System.Globalization.CultureInfo.InvariantCulture), new HexCoord(col0 + 5, row), CityTier.SmallCity, HexDirection.East,
                    new[] { new SectorSpec(SectorKind.Administrative, 4), new SectorSpec(SectorKind.Industrial, 8), new SectorSpec(SectorKind.Residential, 12) });
                b.AddCity("sehir.kita.kasaba" + k.ToString(System.Globalization.CultureInfo.InvariantCulture), new HexCoord(col0 + 15, row), CityTier.Town, HexDirection.East,
                    new[] { new SectorSpec(SectorKind.Administrative, 2), new SectorSpec(SectorKind.Residential, 12) });
            }

            Result<WorldMap> map = b.Build();
            Assert.That(map.IsSuccess, Is.True, map.ErrorKey);
            State = GameState.CreateNew(seed, GameMode.Observer, Start, CountryId.None, map.Value);
            for (int i = 0; i < 8; i++)
            {
                State.Population.AddRegion(State.World.Regions.ValueAt(i).Id, BaselineContent.AgePyramid(i % 2 == 0 ? 300_000 : 200_000));
            }

            for (int k = 0; k < 4; k++)
            {
                CountryId country = All[k];
                State.Economy.AddCountry(country, Fixed64.FromInt(20_000), Fixed64.Parse("0.2"));
                State.Economy.SeedStock(country, BaselineGoods.Food, Fixed64.FromInt(20_000));
                State.Economy.SeedStock(country, BaselineGoods.InfantryEquipment, Fixed64.FromInt(k == 0 ? 2_000 : 1_200));
                State.Economy.SeedStock(country, BaselineGoods.Steel, Fixed64.FromInt(300));
                State.Research.AddCountry(country, Content.Technology, new TechnologyId[0], State.Effects, State.Today);
                State.Characters.AddCountry(country);

                City capital = State.World.Cities.ValueAt(k * 2);
                City town = State.World.Cities.ValueAt((k * 2) + 1);
                SectorId industrial = capital.Sectors[1];
                SectorId capitalResidential = capital.Sectors[2];
                SectorId townResidential = town.Sectors[1];
                for (int f = 0; f < 11; f++)
                {
                    Place(country, BaselineFacilities.Farm, capitalResidential);
                    Place(country, BaselineFacilities.Farm, townResidential);
                }

                Place(country, BaselineFacilities.CoalMine, industrial);
                Place(country, BaselineFacilities.IronMine, industrial);
                Place(country, BaselineFacilities.SteelWorks, industrial);
                Place(country, BaselineFacilities.ArmsFactory, industrial);
            }

            AddPolitics(A, BaselineStatecraft.RepublicanPeoples, BaselineStatecraft.Progressive, BaselineStatecraft.Republic);
            AddPolitics(B, BaselineStatecraft.GreekLiberal, BaselineStatecraft.GreekPeoples, BaselineStatecraft.ConstitutionalMonarchy);

            var war = WarfareContexts.From(State, Content);
            for (int k = 0; k < 4; k++)
            {
                HexCoord capital = State.World.Grid.FromId(State.World.Cities.ValueAt(k * 2).Hex);
                for (int u = 0; u < (k == 0 ? 6 : 3); u++)
                {
                    Assert.That(UnitRules.Recruit(war, All[k], BaselineUnits.InfantryDivision, capital).IsSuccess, Is.True);
                }
            }
        }

        public GameState State { get; }

        public static SimulationSetup Setup()
        {
            SimulationSetup setup = StandardRules.Install(new SimulationSetup { HashIntervalDays = 1 }, Content);
            return StandardRules.InstallArtificialIntelligence(setup, Content);
        }

        public SimulationKernel Kernel() => TestSimulation.Kernel(Setup(), State);

        private void Place(CountryId country, FacilityTypeId type, SectorId sector)
        {
            Result<FacilityId> placed = ConstructionRules.PlaceExisting(State.Economy, Content.Economy, State.World, country, type, sector);
            Assert.That(placed.IsSuccess, Is.True, placed.ErrorKey);
        }

        private void AddPolitics(CountryId country, PartyId ruling, PartyId other, GovernmentFormId form)
        {
            var support = new[] { new KeyValuePair<PartyId, Fixed64>(ruling, Fixed64.FromInt(6)), new KeyValuePair<PartyId, Fixed64>(other, Fixed64.FromInt(4)) };
            Assert.That(State.Politics.AddCountry(country, Content.Politics, form, ruling, support, Fixed64.Parse("0.55"), State.Today).IsSuccess, Is.True);
        }
    }
}
