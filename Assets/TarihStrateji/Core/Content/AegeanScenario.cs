using System;
using System.Collections.Generic;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Başlangıç senaryosu "Ege 1925" (ADR-011, altıgensiz): Türkiye ve Yunanistan, barış içinde. Harita
    /// sadeleştirilmiş gerçek koordinatlıdır: 14 idari bölge çokgeni (Türkiye 9, Yunanistan 5; ülke alanı
    /// bölgelerin birleşimidir), her bölgede bir şehir, kavşak düğümleri, boğaz geçişleri (İstanbul,
    /// Rio–Antirio) ve deniz yolları. Komşu bölgeler köşe noktalarını paylaşır; ortak sınırlar birebir
    /// aynıdır. Kenar uzunluğu büyük çember × kıvrım katsayısıdır (kara %15, demiryolu %10, deniz %20,
    /// boğaz %10). Nüfus gelişim kademesine göre (5: 600 bin … 2: 150 bin); çiftlik sayısı nüfusu
    /// besleyecek kadar; kademe ≥ 3 şehirlerde maden, çelik ve silah; büyük limanlarda tersane. Sayılar
    /// temsilîdir (1925 nüfusunun ≈ beşte biri ölçeğinde). Gerçek 1914 verisi Adım 14'te gelir.
    /// </summary>
    public static class AegeanScenario
    {
        public static readonly GameDate Start = GameDate.FromYmd(1925, 1, 1);

        /// <summary>Başkentler (1925): Türkiye Ankara (1923'ten beri), Yunanistan Atina.</summary>
        public static readonly string[] CapitalCityKeys = { "sehir.ege.ankara", "sehir.ege.atina" };
        public static readonly CountryId[] Countries = { BaselineStatecraft.Turkey, BaselineStatecraft.Greece };

        private static readonly GameDate Opened = GameDate.FromYmd(1900, 1, 1);

        private const byte Plains = (byte)TerrainType.Plains;
        private const byte Hills = (byte)TerrainType.Hills;
        private const byte Mountains = (byte)TerrainType.Mountains;

        /// <summary>Bölge = şehir sırası + 1. Köşeler (enlem, boylam) yüzde bir derece.</summary>
        private static readonly int[][] RegionRings =
        {
            new[] { 4196, 2760, 4198, 2800, 4160, 2810, 4125, 2890, 4120, 2905, 4100, 2898, 4097, 2755 },
            new[] { 3930, 2850, 4000, 3100, 4110, 3100, 4190, 3350, 4200, 3520, 4130, 3640, 4000, 3700, 3860, 3700, 3840, 3450, 3880, 3200, 3820, 3000 },
            new[] { 3670, 2800, 3730, 2730, 3840, 2670, 3930, 2660, 3930, 2850, 3820, 3000, 3620, 2960 },
            new[] { 3930, 2660, 4000, 2620, 4035, 2668, 4040, 2730, 4040, 2900, 4075, 2990, 4100, 2910, 4120, 2910, 4110, 3100, 4000, 3100, 3930, 2850 },
            new[] { 3620, 2960, 3820, 3000, 3880, 3200, 3840, 3450, 3680, 3460, 3610, 3300, 3690, 3070 },
            new[] { 4130, 3640, 4110, 3800, 4105, 3972, 4150, 4160, 4060, 4160, 4000, 3700 },
            new[] { 3680, 3460, 3840, 3450, 3860, 3700, 3700, 3950, 3670, 3800, 3675, 3660, 3675, 3580 },
            new[] { 4000, 3700, 4060, 4160, 4150, 4160, 4120, 4300, 4000, 4440, 3960, 4480, 3890, 4430, 3720, 4480, 3710, 4240, 3700, 3950, 3860, 3700 },
            new[] { 4085, 2605, 4175, 2635, 4195, 2720, 4196, 2760, 4097, 2755, 4042, 2668, 4010, 2625, 4050, 2620, 4070, 2640 },
            new[] { 3890, 2260, 3860, 2330, 3830, 2390, 3765, 2402, 3793, 2365, 3795, 2340, 3795, 2300, 3793, 2292, 3835, 2230, 3835, 2180, 3830, 2120, 3895, 2075 },
            new[] { 4085, 2605, 4093, 2440, 4025, 2390, 4000, 2350, 4060, 2295, 4025, 2260, 4000, 2200, 4030, 2080, 4090, 2095, 4110, 2260, 4135, 2300, 4140, 2400, 4130, 2520, 4160, 2620, 4175, 2635 },
            new[] { 3795, 2300, 3755, 2320, 3645, 2310, 3645, 2245, 3675, 2170, 3770, 2130, 3815, 2135, 3830, 2178, 3805, 2260, 3793, 2292 },
            new[] { 4025, 2260, 3990, 2270, 3930, 2310, 3890, 2260, 3895, 2075, 3965, 2005, 4030, 2080, 4000, 2200 },
            new[] { 3535, 2355, 3560, 2360, 3550, 2420, 3540, 2500, 3530, 2630, 3500, 2620, 3495, 2480, 3520, 2350 },
        };

        private static readonly CitySeed[] Cities =
        {
            new CitySeed("sehir.ege.istanbul", 4101, 2897, BaselineStatecraft.Turkey, 5, true),
            new CitySeed("sehir.ege.ankara", 3993, 3286, BaselineStatecraft.Turkey, 4, false),
            new CitySeed("sehir.ege.izmir", 3842, 2714, BaselineStatecraft.Turkey, 4, true),
            new CitySeed("sehir.ege.bursa", 4019, 2906, BaselineStatecraft.Turkey, 3, false),
            new CitySeed("sehir.ege.konya", 3787, 3248, BaselineStatecraft.Turkey, 3, false),
            new CitySeed("sehir.ege.trabzon", 4100, 3972, BaselineStatecraft.Turkey, 3, true),
            new CitySeed("sehir.ege.adana", 3700, 3532, BaselineStatecraft.Turkey, 3, false),
            new CitySeed("sehir.ege.erzurum", 3990, 4127, BaselineStatecraft.Turkey, 2, false),
            new CitySeed("sehir.ege.edirne", 4168, 2656, BaselineStatecraft.Turkey, 2, false),
            new CitySeed("sehir.ege.atina", 3798, 2373, BaselineStatecraft.Greece, 4, true),
            new CitySeed("sehir.ege.selanik", 4064, 2294, BaselineStatecraft.Greece, 4, true),
            new CitySeed("sehir.ege.patras", 3825, 2174, BaselineStatecraft.Greece, 3, true),
            new CitySeed("sehir.ege.larisa", 3964, 2242, BaselineStatecraft.Greece, 2, false),
            new CitySeed("sehir.ege.kandiye", 3534, 2513, BaselineStatecraft.Greece, 3, true),
        };

        /// <summary>Kavşak düğümleri: kimlik, bölge (şehir sırası + 1), enlem, boylam (yüzde bir derece).</summary>
        private static readonly int[][] Junctions =
        {
            new[] { 101, 1, 4115, 2845 },  // Çatalca
            new[] { 102, 2, 3978, 3052 },  // Eskişehir
            new[] { 103, 2, 3872, 3548 },  // Kayseri
            new[] { 104, 3, 3785, 2785 },  // Aydın
            new[] { 105, 4, 4103, 2920 },  // Üsküdar (boğaz)
            new[] { 106, 4, 4014, 2998 },  // Bilecik
            new[] { 107, 5, 3718, 3322 },  // Karaman
            new[] { 108, 6, 4098, 3788 },  // Ordu
            new[] { 109, 7, 3707, 3738 },  // Antep
            new[] { 110, 8, 3975, 3702 },  // Sivas
            new[] { 111, 9, 4140, 2736 },  // Lüleburgaz
            new[] { 112, 10, 3832, 2332 }, // Thiva
            new[] { 113, 10, 3839, 2183 }, // Nafpaktos (boğaz)
            new[] { 114, 11, 4115, 2415 }, // Drama
            new[] { 115, 12, 3751, 2237 }, // Tripoli
            new[] { 116, 13, 3966, 2085 }, // Yanya
        };

        /// <summary>Deniz düğümleri: kimlik, enlem, boylam (yüzde bir derece); hiçbir bölgede değildir.</summary>
        private static readonly int[][] SeaNodes =
        {
            new[] { 201, 4075, 2800 }, // Marmara
            new[] { 202, 3980, 2530 }, // Kuzey Ege
            new[] { 203, 3830, 2530 }, // Orta Ege
            new[] { 204, 3670, 2500 }, // Güney Ege
            new[] { 205, 3600, 2850 }, // Rodos
            new[] { 206, 3600, 3300 }, // Doğu Akdeniz
            new[] { 207, 4230, 3000 }, // Batı Karadeniz
            new[] { 208, 4180, 3800 }, // Doğu Karadeniz
            new[] { 209, 3800, 2060 }, // İyon
        };

        /// <summary>Kenarlar: düğüm A, düğüm B, tür (<see cref="TransportEdgeKind"/>), arazi (<see cref="TerrainType"/>).</summary>
        private static readonly int[][] Edges =
        {
            new[] { 9, 111, (int)TransportEdgeKind.Rail, Plains },
            new[] { 111, 101, (int)TransportEdgeKind.Rail, Plains },
            new[] { 101, 1, (int)TransportEdgeKind.Rail, Plains },
            new[] { 1, 105, (int)TransportEdgeKind.StraitCrossing, Plains },
            new[] { 105, 4, (int)TransportEdgeKind.Road, Hills },
            new[] { 105, 106, (int)TransportEdgeKind.Rail, Hills },
            new[] { 4, 106, (int)TransportEdgeKind.Road, Hills },
            new[] { 106, 102, (int)TransportEdgeKind.Rail, Plains },
            new[] { 102, 2, (int)TransportEdgeKind.Rail, Plains },
            new[] { 102, 5, (int)TransportEdgeKind.Rail, Plains },
            new[] { 2, 103, (int)TransportEdgeKind.Road, Plains },
            new[] { 103, 110, (int)TransportEdgeKind.Road, Hills },
            new[] { 110, 8, (int)TransportEdgeKind.Road, Mountains },
            new[] { 103, 7, (int)TransportEdgeKind.Road, Mountains },
            new[] { 5, 107, (int)TransportEdgeKind.Road, Plains },
            new[] { 107, 7, (int)TransportEdgeKind.Rail, Mountains },
            new[] { 3, 104, (int)TransportEdgeKind.Rail, Plains },
            new[] { 104, 5, (int)TransportEdgeKind.Rail, Hills },
            new[] { 3, 4, (int)TransportEdgeKind.Road, Hills },
            new[] { 3, 102, (int)TransportEdgeKind.Road, Hills },
            new[] { 7, 109, (int)TransportEdgeKind.Road, Plains },
            new[] { 109, 110, (int)TransportEdgeKind.Road, Mountains },
            new[] { 110, 108, (int)TransportEdgeKind.Road, Mountains },
            new[] { 108, 6, (int)TransportEdgeKind.Road, Hills },
            new[] { 6, 8, (int)TransportEdgeKind.Road, Mountains },
            new[] { 2, 108, (int)TransportEdgeKind.Road, Hills },
            new[] { 9, 114, (int)TransportEdgeKind.Road, Hills },
            new[] { 114, 11, (int)TransportEdgeKind.Rail, Plains },
            new[] { 11, 13, (int)TransportEdgeKind.Rail, Hills },
            new[] { 13, 116, (int)TransportEdgeKind.Road, Mountains },
            new[] { 13, 112, (int)TransportEdgeKind.Rail, Hills },
            new[] { 112, 10, (int)TransportEdgeKind.Rail, Plains },
            new[] { 10, 115, (int)TransportEdgeKind.Road, Hills },
            new[] { 115, 12, (int)TransportEdgeKind.Road, Hills },
            new[] { 12, 113, (int)TransportEdgeKind.StraitCrossing, Plains },
            new[] { 113, 116, (int)TransportEdgeKind.Road, Mountains },
            new[] { 113, 112, (int)TransportEdgeKind.Road, Mountains },
            new[] { 1, 201, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 201, 202, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 202, 203, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 203, 204, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 204, 205, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 205, 206, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 3, 203, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 11, 202, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 10, 203, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 14, 204, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 12, 209, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 209, 204, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 1, 207, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 207, 208, (int)TransportEdgeKind.SeaLane, 0 },
            new[] { 6, 208, (int)TransportEdgeKind.SeaLane, 0 },
        };

        /// <summary>Kara sınırları (bölge, bölge, ortak uzunluk km).</summary>
        private static readonly int[][] Borders =
        {
            new[] { 9, 1, 110 }, new[] { 9, 11, 100 }, new[] { 4, 3, 190 }, new[] { 4, 2, 360 }, new[] { 3, 2, 200 },
            new[] { 3, 5, 230 }, new[] { 5, 2, 470 }, new[] { 5, 7, 180 }, new[] { 2, 7, 230 }, new[] { 2, 6, 150 },
            new[] { 2, 8, 150 }, new[] { 6, 8, 400 }, new[] { 7, 8, 280 }, new[] { 11, 13, 240 }, new[] { 13, 10, 160 },
            new[] { 10, 12, 10 },
        };

        public static IReadOnlyList<CitySeed> CitySeeds => Cities;

        public static RegionId RegionOf(int cityIndex) => new RegionId((ulong)(cityIndex + 1));

        public static CityId CityOf(int cityIndex) => new CityId((ulong)(cityIndex + 1));

        /// <summary>Değişmez geometri + başlangıç kontrolüyle dünyayı kurar (kayıttan yüklemede şablon olarak da kullanılır).</summary>
        public static GeoWorld BuildWorld()
        {
            var world = new GeoWorld();
            for (int i = 0; i < RegionRings.Length; i++)
            {
                var ring = new GeoRing(Points(RegionRings[i], 0, RegionRings[i].Length));
                Require(world.AddRegion(RegionOf(i), new GeoMultiPolygon(new GeoPolygon(ring)), Cities[i].Country), "bolge " + i);
            }

            for (int i = 0; i < Borders.Length; i++)
            {
                int[] b = Borders[i];
                Require(world.Borders.AddBorder(new RegionId((ulong)b[0]), new RegionId((ulong)b[1]), b[2] * 1000L, true), "sinir " + i);
            }

            var nodes = new Dictionary<int, TransportNode>();
            for (int i = 0; i < Cities.Length; i++)
            {
                CitySeed s = Cities[i];
                var node = new TransportNode(new TransportNodeId((ulong)(i + 1)), Point(s.Latitude, s.Longitude), RegionOf(i),
                    s.IsPort ? TransportNodeKind.Port : TransportNodeKind.City);
                AddNode(world, nodes, node);
            }

            for (int i = 0; i < Junctions.Length; i++)
            {
                int[] j = Junctions[i];
                AddNode(world, nodes, new TransportNode(new TransportNodeId((ulong)j[0]), Point(j[2], j[3]), new RegionId((ulong)j[1]), TransportNodeKind.Junction));
            }

            for (int i = 0; i < SeaNodes.Length; i++)
            {
                int[] s = SeaNodes[i];
                AddNode(world, nodes, new TransportNode(new TransportNodeId((ulong)s[0]), Point(s[1], s[2]), RegionId.None, TransportNodeKind.Sea));
            }

            for (int i = 0; i < Edges.Length; i++)
            {
                int[] e = Edges[i];
                TransportNode a = nodes[e[0]];
                TransportNode b = nodes[e[1]];
                var kind = (TransportEdgeKind)e[2];
                long length = GeoMath.DistanceMeters(a.Location, b.Location) * Winding(kind) / 100L;
                var edge = new TransportEdge(new TransportEdgeId((ulong)(i + 1)), a.Id, b.Id, kind, length, Capacity(kind), Opened, (byte)e[3]);
                Require(world.Transport.AddEdge(edge), "kenar " + i);
            }

            for (int i = 0; i < Cities.Length; i++)
            {
                CitySeed s = Cities[i];
                var city = new City(CityOf(i), s.Key, Point(s.Latitude, s.Longitude), RegionOf(i), new TransportNodeId((ulong)(i + 1)),
                    Development(s.Tier), s.Country, Fortified(s.Key) ? Fixed64.Parse("1.5") : Fixed64.One);
                Require(world.AddCity(city), "sehir " + s.Key);
            }

            Require(world.Validate(), "dogrulama");
            return world;
        }

        /// <summary>Nüfus, ekonomi (tesisler, stoklar), araştırma, siyaset ve karakter durumlarını kurar.</summary>
        public static void Populate(GeoWorld world, PopulationState population, EconomyState economy, ResearchState research, PoliticsState politics,
            CharacterState characters, EffectEngine effects, GameContent content, GameDate today)
        {
            for (int i = 0; i < Countries.Length; i++)
            {
                CountryId c = Countries[i];
                economy.AddCountry(c, Fixed64.FromInt(40_000), Fixed64.Parse("0.2"));
                economy.SeedStock(c, BaselineGoods.Food, Fixed64.FromInt(60_000));
                economy.SeedStock(c, BaselineGoods.InfantryEquipment, Fixed64.FromInt(i == 0 ? 1_200 : 1_000));
                economy.SeedStock(c, BaselineGoods.Steel, Fixed64.FromInt(400));
                economy.SeedStock(c, BaselineGoods.Ship, Fixed64.FromInt(8));
                Require(research.AddCountry(c, content.Technology, new TechnologyId[0], effects, today).IsSuccess);
                Require(characters.AddCountry(c).IsSuccess);
            }

            Require(politics.AddCountry(BaselineStatecraft.Turkey, content.Politics, BaselineStatecraft.Republic, BaselineStatecraft.RepublicanPeoples,
                new[] { Pair(BaselineStatecraft.RepublicanPeoples, 7), Pair(BaselineStatecraft.Progressive, 3) }, Fixed64.Parse("0.5"), today).IsSuccess);
            Require(politics.AddCountry(BaselineStatecraft.Greece, content.Politics, BaselineStatecraft.ConstitutionalMonarchy, BaselineStatecraft.GreekLiberal,
                new[] { Pair(BaselineStatecraft.GreekLiberal, 5), Pair(BaselineStatecraft.GreekPeoples, 5) }, Fixed64.Parse("0.5"), today).IsSuccess);

            for (int i = 0; i < Cities.Length; i++)
            {
                CitySeed s = Cities[i];
                population.AddRegion(RegionOf(i), BaselineContent.AgePyramid(Population(s.Tier)));
                PlaceFacilities(world, economy, content, s, CityOf(i));
            }
        }

        public static long Population(int tier) => tier >= 5 ? 600_000 : tier == 4 ? 400_000 : tier == 3 ? 250_000 : 150_000;

        /// <summary>Nüfusu besleyen çiftlik sayısı (çiftlik 25 gıda/gün, 1000 kişi 1 gıda/gün, %8 pay).</summary>
        public static int Farms(int tier) => (int)((Population(tier) * 108 / 100 + 24_999) / 25_000);

        /// <summary>Kademe → gelişim seviyesi (5 → 7, 4 → 6, 3 → 4, 2 → 3).</summary>
        public static int Development(int tier) => tier >= 5 ? 7 : tier == 4 ? 6 : tier == 3 ? 4 : 3;

        private static bool Fortified(string key) =>
            string.Equals(key, "sehir.ege.istanbul", StringComparison.Ordinal) || string.Equals(key, "sehir.ege.ankara", StringComparison.Ordinal)
            || string.Equals(key, "sehir.ege.edirne", StringComparison.Ordinal) || string.Equals(key, "sehir.ege.erzurum", StringComparison.Ordinal)
            || string.Equals(key, "sehir.ege.atina", StringComparison.Ordinal);

        private static void PlaceFacilities(GeoWorld world, EconomyState economy, GameContent content, CitySeed s, CityId city)
        {
            for (int f = 0; f < Farms(s.Tier); f++)
            {
                Place(economy, content, world, s.Country, BaselineFacilities.Farm, city);
            }

            if (s.Tier >= 3)
            {
                Place(economy, content, world, s.Country, BaselineFacilities.CoalMine, city);
                Place(economy, content, world, s.Country, BaselineFacilities.IronMine, city);
                Place(economy, content, world, s.Country, BaselineFacilities.SteelWorks, city);
                Place(economy, content, world, s.Country, BaselineFacilities.ArmsFactory, city);
            }

            if (s.Tier >= 5)
            {
                Place(economy, content, world, s.Country, BaselineFacilities.ArtilleryFactory, city);
            }

            if (s.IsPort && s.Tier >= 4)
            {
                Place(economy, content, world, s.Country, BaselineFacilities.Shipyard, city);
            }
        }

        private static void Place(EconomyState economy, GameContent content, GeoWorld world, CountryId country, FacilityTypeId type, CityId city)
        {
            Result<FacilityId> placed = ConstructionRules.PlaceExisting(economy, content.Economy, world, country, type, city);
            if (!placed.IsSuccess)
            {
                throw new InvalidOperationException("Ege senaryosu tesisi yerleştirilemedi: " + placed.ErrorKey);
            }
        }

        private static void AddNode(GeoWorld world, Dictionary<int, TransportNode> nodes, TransportNode node)
        {
            Require(world.Transport.AddNode(node), "dugum " + node.Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            nodes.Add((int)node.Id.Value, node);
        }

        private static long Winding(TransportEdgeKind kind)
        {
            switch (kind)
            {
                case TransportEdgeKind.Rail: return 110L;
                case TransportEdgeKind.SeaLane: return 120L;
                case TransportEdgeKind.StraitCrossing: return 110L;
                default: return 115L;
            }
        }

        private static int Capacity(TransportEdgeKind kind)
        {
            switch (kind)
            {
                case TransportEdgeKind.Rail: return 30;
                case TransportEdgeKind.SeaLane: return 20;
                case TransportEdgeKind.Road: return 10;
                default: return 5;
            }
        }

        private static GeoPoint[] Points(int[] values, int start, int end)
        {
            var points = new GeoPoint[(end - start) / 2];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = Point(values[start + (2 * i)], values[start + (2 * i) + 1]);
            }

            return points;
        }

        private static GeoPoint Point(int latHundredths, int lonHundredths) => new GeoPoint(latHundredths * 10_000, lonHundredths * 10_000);

        private static KeyValuePair<PartyId, Fixed64> Pair(PartyId party, int weight) => new KeyValuePair<PartyId, Fixed64>(party, Fixed64.FromInt(weight));

        private static void Require(ValidationResult result, string what)
        {
            if (!result.IsValid)
            {
                throw new InvalidOperationException("Ege senaryosu haritası geçersiz (" + what + "): " + result.ErrorKey);
            }
        }

        private static void Require(bool ok)
        {
            if (!ok)
            {
                throw new InvalidOperationException("Ege senaryosu kurulamadı.");
            }
        }
    }
}
