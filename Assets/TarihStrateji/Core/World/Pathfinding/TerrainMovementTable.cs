using System;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.World.Pathfinding
{
    /// <summary>
    /// Kara hareketi çarpanları (veri). Değerler içerik tanımından (Content) gelir;
    /// <see cref="CreateBaseline"/> yalnız içerik yüklenmeden önceki temel değerlerdir ve denge
    /// geçişinde içerikle değiştirilir (game-balance: sayılar kodda değil veride).
    /// Çarpanı sıfır olan arazi geçilemez.
    /// </summary>
    public sealed class TerrainMovementTable
    {
        private readonly Fixed64[] _terrain;
        private readonly Fixed64[] _road;
        private readonly Fixed64[] _rail;

        public TerrainMovementTable(Fixed64[] terrainMultipliers, Fixed64[] roadMultipliers, Fixed64[] railMultipliers)
        {
            if (terrainMultipliers == null || terrainMultipliers.Length != TerrainRules.TerrainTypeCount)
            {
                throw new ArgumentException("Arazi çarpanı sayısı TerrainType sayısına eşit olmalı.", nameof(terrainMultipliers));
            }

            ValidateInfrastructure(roadMultipliers, HexInfrastructure.MaxRoadLevel + 1, nameof(roadMultipliers));
            ValidateInfrastructure(railMultipliers, HexInfrastructure.MaxRailLevel + 1, nameof(railMultipliers));

            Fixed64 minTerrain = Fixed64.MaxValue;
            bool anyPassable = false;
            for (int i = 0; i < terrainMultipliers.Length; i++)
            {
                if (terrainMultipliers[i].IsNegative)
                {
                    throw new ArgumentException("Arazi çarpanı negatif olamaz.", nameof(terrainMultipliers));
                }

                if (terrainMultipliers[i].IsPositive)
                {
                    anyPassable = true;
                    minTerrain = Fixed64.Min(minTerrain, terrainMultipliers[i]);
                }
            }

            if (!anyPassable)
            {
                throw new ArgumentException("En az bir arazi geçilebilir olmalı.", nameof(terrainMultipliers));
            }

            _terrain = (Fixed64[])terrainMultipliers.Clone();
            _road = (Fixed64[])roadMultipliers.Clone();
            _rail = (Fixed64[])railMultipliers.Clone();

            Fixed64 minInfra = Fixed64.One;
            for (int i = 0; i < _road.Length; i++)
            {
                minInfra = Fixed64.Min(minInfra, _road[i]);
            }

            for (int i = 0; i < _rail.Length; i++)
            {
                minInfra = Fixed64.Min(minInfra, _rail[i]);
            }

            MinimumMultiplier = minTerrain * minInfra;
            if (!MinimumMultiplier.IsPositive)
            {
                throw new ArgumentException("En küçük çarpan pozitif olmalı.", nameof(terrainMultipliers));
            }
        }

        /// <summary>Herhangi bir adımın alabileceği en küçük çarpan (arazi × altyapı).</summary>
        public Fixed64 MinimumMultiplier { get; }

        public bool IsPassable(TerrainType terrain) => _terrain[(int)terrain].IsPositive;

        public Fixed64 TerrainMultiplier(TerrainType terrain) => _terrain[(int)terrain];

        /// <summary>
        /// İki hücre arası altyapı çarpanı: her iki uçta ortak olan en yüksek yol ve demiryolu
        /// seviyesinin çarpanlarından küçüğü.
        /// </summary>
        public Fixed64 InfrastructureMultiplier(HexInfrastructure a, HexInfrastructure b)
        {
            int road = System.Math.Min(a.RoadLevel, b.RoadLevel);
            int rail = System.Math.Min(a.RailLevel, b.RailLevel);
            return Fixed64.Min(_road[road], _rail[rail]);
        }

        /// <summary>
        /// İçerik yüklenmeden önceki temel değerler (20. yüzyıl başı kara harekâtı): ova 1,
        /// orman 1.5, tepe 1.75, dağ 3, çöl 1.6, bataklık 2.5, tundra 1.8; yol 0.8/0.65/0.5;
        /// demiryolu 0.35/0.25. Su geçilemez.
        /// </summary>
        public static TerrainMovementTable CreateBaseline()
        {
            var terrain = new Fixed64[TerrainRules.TerrainTypeCount];
            terrain[(int)TerrainType.Ocean] = Fixed64.Zero;
            terrain[(int)TerrainType.Lake] = Fixed64.Zero;
            terrain[(int)TerrainType.Plains] = Fixed64.One;
            terrain[(int)TerrainType.Forest] = Fixed64.Parse("1.5");
            terrain[(int)TerrainType.Hills] = Fixed64.Parse("1.75");
            terrain[(int)TerrainType.Mountains] = Fixed64.FromInt(3);
            terrain[(int)TerrainType.Desert] = Fixed64.Parse("1.6");
            terrain[(int)TerrainType.Marsh] = Fixed64.Parse("2.5");
            terrain[(int)TerrainType.Tundra] = Fixed64.Parse("1.8");
            Fixed64[] road = { Fixed64.One, Fixed64.Parse("0.8"), Fixed64.Parse("0.65"), Fixed64.Parse("0.5") };
            Fixed64[] rail = { Fixed64.One, Fixed64.Parse("0.35"), Fixed64.Parse("0.25") };
            return new TerrainMovementTable(terrain, road, rail);
        }

        private static void ValidateInfrastructure(Fixed64[] values, int expected, string name)
        {
            if (values == null || values.Length != expected)
            {
                throw new ArgumentException("Altyapı çarpanı sayısı seviye sayısına eşit olmalı.", name);
            }

            if (values[0] != Fixed64.One)
            {
                throw new ArgumentException("Seviye 0 çarpanı 1 olmalı.", name);
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (!values[i].IsPositive || values[i] > Fixed64.One)
                {
                    throw new ArgumentException("Altyapı çarpanı (0, 1] aralığında olmalı.", name);
                }
            }
        }
    }
}
