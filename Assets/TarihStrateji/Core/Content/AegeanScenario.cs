using System;
using System.Collections.Generic;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Başlangıç senaryosu "Ege 1925": Türkiye ve Yunanistan, barış içinde. Stilize harita 720×18
    /// küresel ızgaranın 42,5°–35° K ve ≈19,5°–45° D penceresidir (sütun 399–450); karakterler:
    /// '.' deniz; küçük harf Türk, büyük harf Yunan kara; p/P ova, h/H tepe, m/M dağ, f orman,
    /// d çöl. Her şehir kendi bölgesinin merkezidir (aynı ülkenin hücrelerinde altıgen mesafesine
    /// göre en yakın şehir). Nüfus kademe başına (5: 600 bin … 2: 150 bin), çiftlik sayısı nüfusu
    /// besleyecek kadar; kademe ≥ 3 şehirlerde maden, çelik ve silah; büyük limanlarda tersane.
    /// Sayılar temsilîdir (1925 nüfusunun ≈ beşte biri ölçeğinde).
    /// </summary>
    public static class AegeanScenario
    {
        public const int GridWidth = 720;
        public const int GridHeight = 18;
        public const int WindowColumn = 399;
        public static readonly GameDate Start = GameDate.FromYmd(1925, 1, 1);
        public static readonly CountryId[] Countries = { BaselineStatecraft.Turkey, BaselineStatecraft.Greece };

        private static readonly string[] TerrainRows =
        {
            "....................................................",
            ".............pppppp.................................",
            "...HHHHHHHHHHppppppp................................",
            "..HHPPPPPPPPPpppppphffffffffffffffffffmmmmmmmmmmmmm.",
            "..HMHPPPPPPPP......hhhhhhhmmmmmmmmmmmmmmmmmmmmmmmmm.",
            ".HMMHHHH......hhhhhhhhhhhhhhhhhhhhhhhhmmmmmmmmmmmmm.",
            "..HMHPPH......hhhhhhhhhhhhhhhhhhhhhhhhmmmmmmmmmmmmm.",
            ".HMMPPHH.....hppppphhhhhhhhhhhhhhhhhhhmmmmmmmmmmmmm.",
            "..HMHHHH......hpppphhhhhhhhhhhhhhhhhhhmmmmmmmmmmmmm.",
            ".HHHHHHHH....hppppphhhhhhhhhhhhhhhhhhhmmmmmmmmmmmmm.",
            "...HHHHHH.....hpppphhhhhhpppphhhhhhhhhmmmmmmmmmmmmm.",
            "...HHHHHH......pppphhhhhppppphhdddddddddddddddddddd.",
            "...HHHH........hhhhhhmmmmmmmmmmmmmmmmmddddddddddddd.",
            "...HHHH...........hhmmmmmmmmmmmmmm..................",
            "...................hhhhhhhhhhhhpp...................",
            "....................................................",
            "........HHHHHH......................................",
            ".......HHHHHH.......................................",
        };

        private static readonly string[] RegionRows =
        {
            "....................................................",
            ".............iiiaaa.................................",
            "...mmkkkkkkkkiiiaaaa................................",
            "..mmmkkkkkkkkiiaaaaaaaabbbbbbbbbfffffffffffhhhhhhhh.",
            "..mmmmkkkkkkk......aaaabbbbbbbbbbfffffffffhhhhhhhhh.",
            ".mmmmmkk......ddddddddbbbbbbbbbbbffffffffhhhhhhhhhh.",
            "..mmmmmk......ccddddddbbbbbbbbbbbbfffffffhhhhhhhhhh.",
            ".llmmmkk.....ccccdddddbbbbbbbbbbgggfffffhhhhhhhhhhh.",
            "..lllmmj......ccccddddbbbbbbbbbgggggffffhhhhhhhhhhh.",
            ".lllljjjj....ccccccdddeeeeebbgggggggfffhhhhhhhhhhhh.",
            "...llljjj.....cccccccdeeeeeeeegggggggffhhhhhhhhhhhh.",
            "...lljjjj......ccccceeeeeeeeeggggggggghhhhhhhhhhhhh.",
            "...lllj........cccccceeeeeeeegggggggggghhhhhhhhhhhh.",
            "...lljj...........cceeeeeeeegggggg..................",
            "...................cceeeeeeeegggg...................",
            "....................................................",
            "........nnnnnn......................................",
            ".......nnnnnn.......................................",
        };

        private static readonly CitySeed[] Cities =
        {
            new CitySeed("sehir.ege.istanbul", 18, 3, BaselineStatecraft.Turkey, 5, true, 4),
            new CitySeed("sehir.ege.ankara", 26, 6, BaselineStatecraft.Turkey, 4, false, 0),
            new CitySeed("sehir.ege.izmir", 15, 10, BaselineStatecraft.Turkey, 4, true, 4),
            new CitySeed("sehir.ege.bursa", 18, 5, BaselineStatecraft.Turkey, 3, false, 0),
            new CitySeed("sehir.ege.konya", 25, 11, BaselineStatecraft.Turkey, 3, false, 0),
            new CitySeed("sehir.ege.trabzon", 39, 3, BaselineStatecraft.Turkey, 3, true, 1),
            new CitySeed("sehir.ege.adana", 31, 13, BaselineStatecraft.Turkey, 3, false, 0),
            new CitySeed("sehir.ege.erzurum", 43, 6, BaselineStatecraft.Turkey, 2, false, 0),
            new CitySeed("sehir.ege.edirne", 13, 1, BaselineStatecraft.Turkey, 2, false, 0),
            new CitySeed("sehir.ege.atina", 8, 10, BaselineStatecraft.Greece, 4, true, 0),
            new CitySeed("sehir.ege.selanik", 7, 5, BaselineStatecraft.Greece, 4, true, 0),
            new CitySeed("sehir.ege.patras", 3, 10, BaselineStatecraft.Greece, 3, true, 3),
            new CitySeed("sehir.ege.larisa", 5, 6, BaselineStatecraft.Greece, 2, false, 0),
            new CitySeed("sehir.ege.kandiye", 7, 17, BaselineStatecraft.Greece, 3, true, 3),
        };

        public static IReadOnlyList<CitySeed> CitySeeds => Cities;

        public static WorldMap BuildMap()
        {
            var grid = new HexGrid(GridWidth, GridHeight, Fixed64.Parse("42.5"), Fixed64.FromInt(35));
            var b = new WorldBuilder(grid);
            var regions = new List<HexCoord>[Cities.Length];
            for (int i = 0; i < regions.Length; i++)
            {
                regions[i] = new List<HexCoord>();
            }

            for (int r = 0; r < TerrainRows.Length; r++)
            {
                for (int c = 0; c < TerrainRows[r].Length; c++)
                {
                    char t = TerrainRows[r][c];
                    if (t == '.')
                    {
                        continue;
                    }

                    var at = new HexCoord(WindowColumn + c, r);
                    b.SetTerrain(at, Terrain(t));
                    regions[RegionRows[r][c] - 'a'].Add(at);
                }
            }

            for (int i = 0; i < Cities.Length; i++)
            {
                CitySeed s = Cities[i];
                b.AddLandRegion("bolge.ege." + s.Key.Substring("sehir.ege.".Length), regions[i], s.Country);
            }

            for (int i = 0; i < Cities.Length; i++)
            {
                CitySeed s = Cities[i];
                b.AddCity(s.Key, new HexCoord(WindowColumn + s.Column, s.Row), (CityTier)s.Tier, (HexDirection)s.Facing, Sectors(s));
            }

            Result<WorldMap> map = b.Build();
            if (!map.IsSuccess)
            {
                throw new InvalidOperationException("Ege senaryosu haritası geçersiz: " + map.ErrorKey);
            }

            return map.Value;
        }

        /// <summary>Nüfus, ekonomi (tesisler, stoklar), araştırma, siyaset ve karakter durumlarını kurar.</summary>
        public static void Populate(WorldMap map, PopulationState population, EconomyState economy, ResearchState research, PoliticsState politics,
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
                City city = map.Cities.ValueAt(i);
                RegionId region = map.Cell(city.Hex).Region;
                population.AddRegion(region, BaselineContent.AgePyramid(Population(s.Tier)));
                PlaceFacilities(map, economy, content, s, city);
            }
        }

        public static long Population(int tier) => tier >= 5 ? 600_000 : tier == 4 ? 400_000 : tier == 3 ? 250_000 : 150_000;

        /// <summary>Nüfusu besleyen çiftlik sayısı (çiftlik 25 gıda/gün, 1000 kişi 1 gıda/gün, %8 pay).</summary>
        public static int Farms(int tier) => (int)((Population(tier) * 108 / 100 + 24_999) / 25_000);

        private static SectorSpec[] Sectors(CitySeed s)
        {
            var specs = new List<SectorSpec> { new SectorSpec(SectorKind.Administrative, 2) };
            if (s.IsPort)
            {
                specs.Add(new SectorSpec(SectorKind.Port, 4));
            }

            if (s.Tier >= 3 && specs.Count < s.Tier - 1)
            {
                specs.Add(new SectorSpec(SectorKind.Industrial, s.Tier >= 5 ? 10 : 8));
            }

            specs.Add(new SectorSpec(SectorKind.Residential, Farms(s.Tier) + 2));
            while (specs.Count < s.Tier)
            {
                specs.Add(new SectorSpec(SectorKind.Fortress, 1));
            }

            return specs.ToArray();
        }

        private static void PlaceFacilities(WorldMap map, EconomyState economy, GameContent content, CitySeed s, City city)
        {
            for (int i = 0; i < city.Sectors.Count; i++)
            {
                CitySector sector = map.Sectors[city.Sectors[i]];
                switch (sector.Kind)
                {
                    case SectorKind.Residential:
                        for (int f = 0; f < Farms(s.Tier); f++)
                        {
                            Place(economy, content, map, s.Country, BaselineFacilities.Farm, sector.Id);
                        }

                        break;
                    case SectorKind.Industrial:
                        Place(economy, content, map, s.Country, BaselineFacilities.CoalMine, sector.Id);
                        Place(economy, content, map, s.Country, BaselineFacilities.IronMine, sector.Id);
                        Place(economy, content, map, s.Country, BaselineFacilities.SteelWorks, sector.Id);
                        Place(economy, content, map, s.Country, BaselineFacilities.ArmsFactory, sector.Id);
                        if (s.Tier >= 5)
                        {
                            Place(economy, content, map, s.Country, BaselineFacilities.ArtilleryFactory, sector.Id);
                        }

                        break;
                    case SectorKind.Port:
                        if (s.Tier >= 4)
                        {
                            Place(economy, content, map, s.Country, BaselineFacilities.Shipyard, sector.Id);
                        }

                        break;
                }
            }
        }

        private static void Place(EconomyState economy, GameContent content, WorldMap map, CountryId country, FacilityTypeId type, SectorId sector)
        {
            Result<FacilityId> placed = ConstructionRules.PlaceExisting(economy, content.Economy, map, country, type, sector);
            if (!placed.IsSuccess)
            {
                throw new InvalidOperationException("Ege senaryosu tesisi yerleştirilemedi: " + placed.ErrorKey);
            }
        }

        private static TerrainType Terrain(char c)
        {
            switch (char.ToLowerInvariant(c))
            {
                case 'p': return TerrainType.Plains;
                case 'h': return TerrainType.Hills;
                case 'm': return TerrainType.Mountains;
                case 'f': return TerrainType.Forest;
                case 'd': return TerrainType.Desert;
                default: throw new InvalidOperationException("Ege senaryosu: bilinmeyen arazi '" + c + "'");
            }
        }

        private static KeyValuePair<PartyId, Fixed64> Pair(PartyId party, int weight) => new KeyValuePair<PartyId, Fixed64>(party, Fixed64.FromInt(weight));

        private static void Require(bool ok)
        {
            if (!ok)
            {
                throw new InvalidOperationException("Ege senaryosu kurulamadı.");
            }
        }
    }
}
