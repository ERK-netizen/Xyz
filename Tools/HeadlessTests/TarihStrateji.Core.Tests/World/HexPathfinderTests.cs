using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Tests.Support;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Tests.World
{
    [TestFixture]
    public class HexPathfinderTests
    {
        private static readonly LandMovementCostModel Land = new LandMovementCostModel(TerrainMovementTable.CreateBaseline());

        /// <summary>Bağımsız kâhin: .NET PriorityQueue ile Dijkstra (h = 0). Erişilemezse -1.</summary>
        private static long Dijkstra(WorldMap map, IMovementCostModel model, HexCoord start, HexCoord goal)
        {
            HexGrid grid = map.Grid;
            if (!model.CanEnter(map.Cell(start)) || !model.CanEnter(map.Cell(goal)))
            {
                return -1;
            }

            var dist = new Dictionary<int, long> { [grid.IndexOf(start)] = 0 };
            var done = new HashSet<int>();
            var queue = new PriorityQueue<int, long>();
            queue.Enqueue(grid.IndexOf(start), 0);
            while (queue.TryDequeue(out int node, out long d))
            {
                if (!done.Add(node))
                {
                    continue;
                }

                if (node == grid.IndexOf(goal))
                {
                    return d;
                }

                HexCoord c = grid.FromIndex(node);
                for (int dir = 0; dir < 6; dir++)
                {
                    if (!grid.TryGetNeighbor(c, (HexDirection)dir, out HexCoord n))
                    {
                        continue;
                    }

                    map.Distances.TryGetNeighborKm(c, (HexDirection)dir, out Fixed64 km);
                    if (!model.TryGetStepCost(map.Cell(c), map.Cell(n), km, out Fixed64 cost))
                    {
                        continue;
                    }

                    int ni = grid.IndexOf(n);
                    long nd = d + cost.Raw;
                    if (!dist.TryGetValue(ni, out long old) || nd < old)
                    {
                        dist[ni] = nd;
                        queue.Enqueue(ni, nd);
                    }
                }
            }

            return -1;
        }

        private static void AssertValidPath(WorldMap map, PathResult result, HexCoord start, HexCoord goal)
        {
            Assert.That(result.Path[0], Is.EqualTo(start));
            Assert.That(result.Path[result.Path.Count - 1], Is.EqualTo(goal));
            long cost = 0, km = 0;
            for (int i = 1; i < result.Path.Count; i++)
            {
                HexCoord a = result.Path[i - 1], b = result.Path[i];
                bool adjacent = false;
                for (int d = 0; d < 6; d++)
                {
                    if (map.Grid.TryGetNeighbor(a, (HexDirection)d, out HexCoord n) && n == b)
                    {
                        adjacent = true;
                        map.Distances.TryGetNeighborKm(a, (HexDirection)d, out Fixed64 stepKm);
                        Assert.That(Land.TryGetStepCost(map.Cell(a), map.Cell(b), stepKm, out Fixed64 stepCost), Is.True);
                        cost += stepCost.Raw;
                        km += stepKm.Raw;
                    }
                }

                Assert.That(adjacent, Is.True, $"{a} ile {b} komşu değil");
            }

            Assert.That(result.TotalCost.Raw, Is.EqualTo(cost));
            Assert.That(result.TotalKm.Raw, Is.EqualTo(km));
        }

        [Test]
        public void AStar_EqualsDijkstra_On50RandomMaps()
        {
            int compared = 0, unreachable = 0;
            for (ulong seed = 1; seed <= 50; seed++)
            {
                WorldMap map = TestWorlds.Random(seed, 24, 16, 65);
                var finder = new HexPathfinder(map, Land);
                var rng = new TestRng(seed * 7919);
                for (int q = 0; q < 20; q++)
                {
                    HexCoord a = map.Grid.FromIndex(rng.Next(map.CellCount));
                    HexCoord b = map.Grid.FromIndex(rng.Next(map.CellCount));
                    if (!Land.CanEnter(map.Cell(a)))
                    {
                        continue;
                    }

                    long oracle = Dijkstra(map, Land, a, b);
                    PathResult result = finder.FindPath(a, b);
                    if (oracle < 0)
                    {
                        unreachable++;
                        Assert.That(result.Found, Is.False);
                        Assert.That(result.ErrorKey, Is.EqualTo(WorldErrorKeys.RotaUlasilamaz));
                        Assert.That(result.ExpandedNodes, Is.EqualTo(0), "bileşen etiketi aramadan önce reddetmeli");
                        continue;
                    }

                    compared++;
                    Assert.That(result.Found, Is.True, $"tohum {seed}: {a} → {b}");
                    Assert.That(result.TotalCost.Raw, Is.EqualTo(oracle), $"tohum {seed}: {a} → {b} (sezgisel kabul edilebilir değil)");
                    AssertValidPath(map, result, a, b);
                }
            }

            Assert.That(compared, Is.GreaterThan(300));
            Assert.That(unreachable, Is.GreaterThan(0), "test ulaşılamaz durumları da kapsamalı");
        }

        [Test]
        public void Path_CrossesTheAntimeridian()
        {
            var builder = new WorldBuilder(TestWorlds.SmallGrid());
            for (int col = 0; col < 12; col++)
            {
                builder.SetTerrain(new HexCoord(col, 3), TerrainType.Plains);
            }

            Result<WorldMap> built = builder.Build();
            WorldMap map = built.Value;
            PathResult result = new HexPathfinder(map, Land).FindPath(new HexCoord(10, 3), new HexCoord(1, 3));
            Assert.That(result.Found, Is.True);
            Assert.That(result.Steps, Is.EqualTo(3));
            Assert.That(result.Path, Is.EqualTo(new[] { new HexCoord(10, 3), new HexCoord(11, 3), new HexCoord(0, 3), new HexCoord(1, 3) }));
        }

        [Test]
        public void Unreachable_Start_Goal_AndLimits()
        {
            var builder = new WorldBuilder(TestWorlds.SmallGrid());
            builder.SetTerrain(new HexCoord(2, 2), TerrainType.Plains);
            builder.SetTerrain(new HexCoord(3, 2), TerrainType.Plains);
            builder.SetTerrain(new HexCoord(8, 5), TerrainType.Plains);
            WorldMap map = builder.Build().Value;
            var finder = new HexPathfinder(map, Land);

            PathResult island = finder.FindPath(new HexCoord(2, 2), new HexCoord(8, 5));
            Assert.That(island.ErrorKey, Is.EqualTo(WorldErrorKeys.RotaUlasilamaz));
            Assert.That(island.ExpandedNodes, Is.EqualTo(0));
            Assert.That(finder.ComponentOf(new HexCoord(2, 2)), Is.EqualTo(finder.ComponentOf(new HexCoord(3, 2))));
            Assert.That(finder.ComponentOf(new HexCoord(0, 0)), Is.EqualTo(-1));

            Assert.That(finder.FindPath(new HexCoord(0, 0), new HexCoord(2, 2)).ErrorKey, Is.EqualTo(WorldErrorKeys.RotaBaslangicGecersiz));
            Assert.That(finder.FindPath(new HexCoord(2, 2), new HexCoord(0, 0)).ErrorKey, Is.EqualTo(WorldErrorKeys.RotaUlasilamaz));
            Assert.That(finder.FindPath(new HexCoord(2, 2), new HexCoord(50, 0)).ErrorKey, Is.EqualTo(WorldErrorKeys.RotaBaslangicGecersiz));

            PathResult same = finder.FindPath(new HexCoord(2, 2), new HexCoord(2, 2));
            Assert.That(same.Found && same.Steps == 0 && same.TotalCost.IsZero, Is.True);

            WorldMap big = TestWorlds.Random(99, 40, 20, 100);
            PathResult capped = new HexPathfinder(big, Land).FindPath(new HexCoord(0, 0), new HexCoord(20, 19), maxExpansions: 3);
            Assert.That(capped.ErrorKey, Is.EqualTo(WorldErrorKeys.RotaAramaSiniri));
        }

        [Test]
        public void Railway_IsPreferred_AndCheaper()
        {
            var plain = new WorldBuilder(new HexGrid(20, 6, Fixed64.FromInt(45), Fixed64.FromInt(35)));
            var railed = new WorldBuilder(new HexGrid(20, 6, Fixed64.FromInt(45), Fixed64.FromInt(35)));
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 20; col++)
                {
                    plain.SetTerrain(new HexCoord(col, row), TerrainType.Plains);
                    railed.SetTerrain(new HexCoord(col, row), TerrainType.Plains);
                }
            }

            for (int col = 2; col <= 14; col++)
            {
                railed.SetInfrastructure(new HexCoord(col, 4), new HexInfrastructure(0, 2, false));
            }

            WorldMap a = plain.Build().Value, b = railed.Build().Value;
            HexCoord from = new HexCoord(2, 2), to = new HexCoord(14, 2);
            PathResult slow = new HexPathfinder(a, Land).FindPath(from, to);
            PathResult fast = new HexPathfinder(b, Land).FindPath(from, to);
            Assert.That(fast.TotalCost < slow.TotalCost, Is.True);
            Assert.That(fast.TotalKm > slow.TotalKm, Is.True, "demiryolu için daha uzun ama ucuz yol seçilmeli");
            Assert.That(fast.Path, Has.Some.EqualTo(new HexCoord(8, 4)));
            Assert.That(fast.TotalCost.Raw, Is.EqualTo(Dijkstra(b, Land, from, to)));
        }

        [Test]
        public void ReusedBuffers_GiveSameResultsAsFreshInstances()
        {
            WorldMap map = TestWorlds.Random(7, 30, 18, 70);
            var shared = new HexPathfinder(map, Land);
            var rng = new TestRng(5);
            for (int q = 0; q < 100; q++)
            {
                HexCoord a = map.Grid.FromIndex(rng.Next(map.CellCount));
                HexCoord b = map.Grid.FromIndex(rng.Next(map.CellCount));
                PathResult reused = shared.FindPath(a, b);
                PathResult fresh = new HexPathfinder(map, Land).FindPath(a, b);
                Assert.That(reused.Found, Is.EqualTo(fresh.Found));
                Assert.That(reused.Path, Is.EqualTo(fresh.Path));
                Assert.That(reused.TotalCost, Is.EqualTo(fresh.TotalCost));
            }
        }

        /// <summary>
        /// Altın determinizm: harita kurulumu (km tablosu, kıyı, bağlantı) ve rota seçimi her
        /// platformda bit düzeyinde aynı olmalıdır. Maliyetler aynı testte Dijkstra kâhiniyle
        /// doğrulanır; hash yolların kendisini de (eşit maliyetli seçim sırası) sabitler.
        /// </summary>
        [Test]
        public void GoldenHash_MapAndPaths_AreStable()
        {
            WorldMap map = TestWorlds.Random(2026, 36, 20, 68);
            var hash = new Fnv1a64();
            for (int i = 0; i < map.CellCount; i++)
            {
                HexCell cell = map.CellAt(i);
                hash.Add((long)cell.Terrain);
                hash.Add(cell.IsCoastal ? 1 : 0);
                for (int d = 0; d < 6; d++)
                {
                    map.Distances.TryGetNeighborKm(cell.Coord, (HexDirection)d, out Fixed64 km);
                    hash.Add(km.Raw);
                }
            }

            var finder = new HexPathfinder(map, Land);
            var rng = new TestRng(1923);
            for (int q = 0; q < 150; q++)
            {
                HexCoord a = map.Grid.FromIndex(rng.Next(map.CellCount));
                HexCoord b = map.Grid.FromIndex(rng.Next(map.CellCount));
                PathResult r = finder.FindPath(a, b);
                hash.Add(r.Found ? r.TotalCost.Raw : -1);
                foreach (HexCoord c in r.Path)
                {
                    hash.Add(map.Grid.IndexOf(c));
                }

                if (r.Found && q % 10 == 0)
                {
                    Assert.That(r.TotalCost.Raw, Is.EqualTo(Dijkstra(map, Land, a, b)));
                }
            }

            Assert.That(hash.Value, Is.EqualTo(GoldenValues.WorldMapAndPaths), $"yeni hash: 0x{hash.Value:X16}UL");
        }
    }
}
