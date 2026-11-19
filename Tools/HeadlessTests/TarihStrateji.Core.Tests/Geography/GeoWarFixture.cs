using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;
using static TarihStrateji.Core.Tests.Geography.GeoFixtures;

namespace TarihStrateji.Core.Tests.Geography
{
    /// <summary>
    /// <see cref="GeoFixtures"/> dünyası + şehirler (a1 Kızıl, aP Kızıl liman, b1 Mavi, cP Kızıl ada limanı),
    /// nüfus, envanter ve savaş durumu. Kızıl–Mavi savaşta; 3 numaralı ülke tarafsız.
    /// Yürüyüş: piyade 30 hız × 20 = günde 600 maliyet (yolda 30 km).
    /// </summary>
    internal sealed class GeoWarFixture
    {
        public static readonly GameContent Content = BaselineContent.Create();
        public static readonly CountryId Neutral = new CountryId(3UL);
        public static readonly CityId CityA1 = new CityId(1UL);
        public static readonly CityId CityAP = new CityId(2UL);
        public static readonly CityId CityB1 = new CityId(3UL);
        public static readonly CityId CityCP = new CityId(4UL);

        public GeoWarFixture(GameDate? today = null)
        {
            World = Build();
            Ok(World.AddCity(new City(CityA1, "sehir.test.a1", P("40.5", "28.5"), A, A1, 5, Red, Fixed64.One)));
            Ok(World.AddCity(new City(CityAP, "sehir.test.ap", P("40.1", "28.5"), A, AP, 3, Red, Fixed64.One)));
            Ok(World.AddCity(new City(CityB1, "sehir.test.b1", P("40.5", "29.5"), B, B1, 5, Blue, Fixed64.One)));
            Ok(World.AddCity(new City(CityCP, "sehir.test.cp", P("38.9", "28.5"), C, CP, 2, Red, Fixed64.One)));
            Ok(World.Validate());

            Population.AddRegion(A, BaselineContent.AgePyramid(400_000));
            Population.AddRegion(B, BaselineContent.AgePyramid(400_000));
            Population.AddRegion(C, BaselineContent.AgePyramid(50_000));
            foreach (CountryId c in new[] { Red, Blue, Neutral })
            {
                Economy.AddCountry(c, Fixed64.FromInt(50_000), Fixed64.Parse("0.2"));
                Economy.SeedStock(c, BaselineGoods.InfantryEquipment, Fixed64.FromInt(2_000));
            }

            Hostility.Wars.Add((Red, Blue));
            Today = today ?? Day1925;
        }

        public GeoWorld World { get; }

        public WarfareState Warfare { get; } = new WarfareState();

        public PopulationState Population { get; } = new PopulationState();

        public EconomyState Economy { get; } = new EconomyState();

        public EffectEngine Effects { get; } = new EffectEngine();

        public TestHostility Hostility { get; } = new TestHostility();

        public GameDate Today { get; set; }

        public static GeoWarfareParameters Parameters { get; } =
            new GeoWarfareParameters(LandCosts(), SeaCosts(), Fixed64.FromInt(20), new RegionControlParameters(3, 5), 4);

        public GeoWarfareContext Ctx => new GeoWarfareContext(World, Warfare, Content.Warfare, Parameters, Population, Economy, Effects, Hostility, Today);

        public LandUnit Recruit(CountryId country, CityId city)
        {
            Result<LandUnitId> id = GeoUnitRules.Recruit(Ctx, country, BaselineUnits.InfantryDivision, city);
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            return Warfare.Units[id.Value];
        }

        public void SetController(RegionId region, CountryId controller)
        {
            World.TryGetControl(region, out RegionControl control);
            control.Controller = controller;
        }

        internal sealed class TestHostility : IHostility
        {
            public List<(CountryId, CountryId)> Wars { get; } = new List<(CountryId, CountryId)>();

            public bool AtWar(CountryId a, CountryId b) => Wars.Contains((a, b)) || Wars.Contains((b, a));
        }
    }
}
