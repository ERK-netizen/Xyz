using System;
using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Tests.Support;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Tests.World
{
    [TestFixture]
    public class HexGridTests
    {
        private static HexGrid Grid(int w, int h) => new HexGrid(w, h, Fixed64.FromInt(80), Fixed64.FromInt(-80));

        [Test]
        public void Ids_RoundTrip_AndFollowRowColumnOrder()
        {
            HexGrid grid = Grid(13, 7);
            HexId previous = HexId.None;
            for (int i = 0; i < grid.CellCount; i++)
            {
                HexCoord c = grid.FromIndex(i);
                HexId id = grid.ToId(c);
                Assert.That(id.Value, Is.EqualTo((ulong)i + 1));
                Assert.That(id > previous, Is.True);
                Assert.That(grid.FromId(id), Is.EqualTo(c));
                previous = id;
            }

            Assert.That(grid.TryFromId(HexId.None, out _), Is.False);
            Assert.That(grid.TryFromId(new HexId((ulong)grid.CellCount + 1), out _), Is.False);
        }

        [TestCase(12, 8)]
        [TestCase(13, 9)]
        [TestCase(4, 2)]
        public void Neighbors_AreSymmetric_AndUnique(int w, int h)
        {
            HexGrid grid = Grid(w, h);
            for (int i = 0; i < grid.CellCount; i++)
            {
                HexCoord c = grid.FromIndex(i);
                var seen = new HashSet<HexCoord>();
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    var dir = (HexDirection)d;
                    if (!grid.TryGetNeighbor(c, dir, out HexCoord n))
                    {
                        Assert.That(HexDirections.ChangesRow(dir), Is.True, "yalnız kutup satırlarında komşu eksik olabilir");
                        continue;
                    }

                    Assert.That(seen.Add(n), Is.True, $"{c} komşuları tekrarlıyor");
                    Assert.That(grid.TryGetNeighbor(n, HexDirections.Opposite(dir), out HexCoord back), Is.True);
                    Assert.That(back, Is.EqualTo(c), $"{c} {dir} → {n} geri dönmüyor");
                }
            }
        }

        [Test]
        public void Wrap_AcrossAntimeridian_AndPolesDoNotWrap()
        {
            HexGrid grid = Grid(12, 8);
            Assert.That(grid.TryGetNeighbor(new HexCoord(11, 2), HexDirection.East, out HexCoord e), Is.True);
            Assert.That(e, Is.EqualTo(new HexCoord(0, 2)));
            Assert.That(grid.TryGetNeighbor(new HexCoord(0, 2), HexDirection.West, out HexCoord w), Is.True);
            Assert.That(w, Is.EqualTo(new HexCoord(11, 2)));
            Assert.That(grid.TryGetNeighbor(new HexCoord(11, 3), HexDirection.NorthEast, out HexCoord ne), Is.True);
            Assert.That(ne, Is.EqualTo(new HexCoord(0, 2)));
            Assert.That(grid.TryGetNeighbor(new HexCoord(5, 0), HexDirection.NorthEast, out _), Is.False);
            Assert.That(grid.TryGetNeighbor(new HexCoord(5, 7), HexDirection.SouthWest, out _), Is.False);
            Assert.That(grid.StepDistance(new HexCoord(0, 4), new HexCoord(11, 4)), Is.EqualTo(1));
        }

        [TestCase(12, 8, 1UL)]
        [TestCase(13, 9, 2UL)]
        [TestCase(30, 16, 3UL)]
        public void StepDistance_EqualsBfsDistance(int w, int h, ulong seed)
        {
            HexGrid grid = Grid(w, h);
            var rng = new TestRng(seed);
            for (int trial = 0; trial < 40; trial++)
            {
                HexCoord start = grid.FromIndex(rng.Next(grid.CellCount));
                int[] bfs = Bfs(grid, start);
                for (int i = 0; i < grid.CellCount; i++)
                {
                    Assert.That(grid.StepDistance(start, grid.FromIndex(i)), Is.EqualTo(bfs[i]), $"{start} → {grid.FromIndex(i)}");
                }
            }
        }

        [Test]
        public void Centers_SpanTheGlobe()
        {
            HexGrid grid = Grid(12, 8);
            Assert.That(grid.CenterLatitude(0), Is.EqualTo(Fixed64.FromInt(70)));
            Assert.That(grid.CenterLatitude(7), Is.EqualTo(Fixed64.FromInt(-70)));
            Assert.That(grid.CenterLongitude(new HexCoord(0, 0)), Is.EqualTo(Fixed64.FromInt(-165)));
            Assert.That(grid.CenterLongitude(new HexCoord(0, 1)), Is.EqualTo(Fixed64.FromInt(-150)));
            Assert.That(grid.CenterLongitude(new HexCoord(11, 1)), Is.EqualTo(Fixed64.FromInt(-180)), "odd satır son hücresi dikişe sarmalanır");
            for (int i = 0; i < grid.CellCount; i++)
            {
                Fixed64 lon = grid.CenterLongitude(grid.FromIndex(i));
                Assert.That(lon >= Fixed64.FromInt(-180) && lon < Fixed64.FromInt(180), Is.True);
            }
        }

        [Test]
        public void Constructor_RejectsInvalidShapes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Grid(3, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => Grid(12, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HexGrid(12, 8, Fixed64.FromInt(10), Fixed64.FromInt(20)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HexGrid(12, 8, Fixed64.FromInt(91), Fixed64.FromInt(0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => Grid(12, 8).IndexOf(new HexCoord(12, 0)));
        }

        private static int[] Bfs(HexGrid grid, HexCoord start)
        {
            var dist = new int[grid.CellCount];
            Array.Fill(dist, -1);
            var queue = new Queue<HexCoord>();
            dist[grid.IndexOf(start)] = 0;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                HexCoord c = queue.Dequeue();
                for (int d = 0; d < 6; d++)
                {
                    if (grid.TryGetNeighbor(c, (HexDirection)d, out HexCoord n) && dist[grid.IndexOf(n)] < 0)
                    {
                        dist[grid.IndexOf(n)] = dist[grid.IndexOf(c)] + 1;
                        queue.Enqueue(n);
                    }
                }
            }

            return dist;
        }
    }
}
