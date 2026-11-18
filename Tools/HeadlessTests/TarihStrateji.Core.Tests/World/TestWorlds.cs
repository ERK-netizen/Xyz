using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Tests.Support;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.World
{
    /// <summary>Test haritaları.</summary>
    public static class TestWorlds
    {
        public static readonly CountryId CountryA = new CountryId(1);
        public static readonly CountryId CountryB = new CountryId(2);
        public static readonly CountryId CountryC = new CountryId(3);

        public static HexGrid SmallGrid() => new HexGrid(12, 8, Fixed64.FromInt(48), Fixed64.FromInt(32));

        /// <summary>
        /// 12×8 ızgara; sütun 2–9, satır 2–5 kara (ova), geri kalanı okyanus. Batı yarısı A,
        /// doğu yarısı B; (2,3) kıyıdaki bir liman şehri (batıya bakan liman sektörü), (6,3)
        /// iç kesimde 4 sektörlü şehir.
        /// </summary>
        public static WorldBuilder CoastalBuilder(out CityId portCity, out CityId inlandCity)
        {
            var builder = new WorldBuilder(SmallGrid());
            var west = new List<HexCoord>();
            var east = new List<HexCoord>();
            var sea = new List<HexCoord>();
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 12; col++)
                {
                    var c = new HexCoord(col, row);
                    bool land = col >= 2 && col <= 9 && row >= 2 && row <= 5;
                    if (land)
                    {
                        builder.SetTerrain(c, TerrainType.Plains);
                        (col <= 5 ? west : east).Add(c);
                    }
                    else if (row == 3 && col <= 1)
                    {
                        sea.Add(c);
                    }
                }
            }

            builder.AddLandRegion("bolge.test.bati", west, CountryA);
            builder.AddLandRegion("bolge.test.dogu", east, CountryB);
            builder.AddSeaRegion("bolge.test.deniz", sea);
            portCity = builder.AddCity("sehir.test.liman", new HexCoord(2, 3), CityTier.SmallCity, HexDirection.West, new[]
            {
                new SectorSpec(SectorKind.Administrative, 4),
                new SectorSpec(SectorKind.Port, 2),
                new SectorSpec(SectorKind.Residential, 3),
            });
            inlandCity = builder.AddCity("sehir.test.ic", new HexCoord(6, 3), CityTier.City, HexDirection.East, new[]
            {
                new SectorSpec(SectorKind.Administrative, 4),
                new SectorSpec(SectorKind.Industrial, 6),
                new SectorSpec(SectorKind.Residential, 3),
                new SectorSpec(SectorKind.Fortress, 1),
            });
            return builder;
        }

        public static WorldMap Coastal(out CityId portCity, out CityId inlandCity)
        {
            Result<WorldMap> result = CoastalBuilder(out portCity, out inlandCity).Build();
            NUnit.Framework.Assert.That(result.IsSuccess, NUnit.Framework.Is.True, result.ErrorKey);
            return result.Value;
        }

        /// <summary>Rastgele arazi ve altyapılı, bölgesiz harita (rota testleri için).</summary>
        public static WorldMap Random(ulong seed, int width, int height, int landPercent)
        {
            var rng = new TestRng(seed);
            var grid = new HexGrid(width, height, Fixed64.FromInt(70), Fixed64.FromInt(-70));
            var builder = new WorldBuilder(grid);
            TerrainType[] land = { TerrainType.Plains, TerrainType.Forest, TerrainType.Hills, TerrainType.Mountains, TerrainType.Desert, TerrainType.Marsh, TerrainType.Tundra };
            for (int i = 0; i < grid.CellCount; i++)
            {
                HexCoord c = grid.FromIndex(i);
                if (rng.Chance(landPercent))
                {
                    builder.SetTerrain(c, land[rng.Next(land.Length)]);
                    if (rng.Chance(30))
                    {
                        builder.SetInfrastructure(c, new HexInfrastructure(rng.Next(4), rng.Chance(30) ? rng.Next(3) : 0, false));
                    }
                }
                else if (rng.Chance(10))
                {
                    builder.SetTerrain(c, TerrainType.Lake);
                }
            }

            Result<WorldMap> result = builder.Build();
            NUnit.Framework.Assert.That(result.IsSuccess, NUnit.Framework.Is.True, result.ErrorKey);
            return result.Value;
        }
    }
}
