using System;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Geo;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Tests.World
{
    [TestFixture]
    public class HexDistanceTableTests
    {
        private const double R = 6371.0088;

        private static double D(Fixed64 v) => v.Raw / 1e6;

        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            double r = Math.PI / 180;
            double dlat = (lat2 - lat1) * r, dlon = (lon2 - lon1) * r;
            double h = Math.Pow(Math.Sin(dlat / 2), 2) + Math.Cos(lat1 * r) * Math.Cos(lat2 * r) * Math.Pow(Math.Sin(dlon / 2), 2);
            return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }

        [TestCase(12, 8)]
        [TestCase(360, 160)]
        public void NeighborKm_MatchesHaversineOracle_ForEveryCellAndDirection(int w, int h)
        {
            var grid = new HexGrid(w, h, Fixed64.FromInt(80), Fixed64.FromInt(-80));
            var table = new HexDistanceTable(grid);
            int step = Math.Max(1, grid.CellCount / 5000);
            for (int i = 0; i < grid.CellCount; i += step)
            {
                HexCoord c = grid.FromIndex(i);
                for (int d = 0; d < 6; d++)
                {
                    bool has = grid.TryGetNeighbor(c, (HexDirection)d, out HexCoord n);
                    Assert.That(table.TryGetNeighborKm(c, (HexDirection)d, out Fixed64 km), Is.EqualTo(has));
                    if (!has)
                    {
                        continue;
                    }

                    double expected = Haversine(D(grid.CenterLatitude(c.Row)), D(grid.CenterLongitude(c)), D(grid.CenterLatitude(n.Row)), D(grid.CenterLongitude(n)));
                    Assert.That(D(km), Is.EqualTo(expected).Within(0.05), $"{c} {(HexDirection)d}");
                    Assert.That(table.TryGetNeighborKm(n, HexDirections.Opposite((HexDirection)d), out Fixed64 back), Is.True);
                    Assert.That(Math.Abs(km.Raw - back.Raw), Is.LessThanOrEqualTo(50), "a→b ve b→a mesafesi eşit olmalı");
                }
            }

            Assert.That(table.MinimumNeighborKm.IsPositive, Is.True);
            Assert.That(table.MinimumRowChangeKm.IsPositive, Is.True);
        }

        [Test]
        public void EastWestKm_ShrinksTowardThePoles()
        {
            var grid = new HexGrid(360, 160, Fixed64.FromInt(80), Fixed64.FromInt(-80));
            var table = new HexDistanceTable(grid);
            table.TryGetNeighborKm(new HexCoord(0, 79), HexDirection.East, out Fixed64 nearEquator);
            table.TryGetNeighborKm(new HexCoord(0, 0), HexDirection.East, out Fixed64 nearPole);
            Assert.That(D(nearEquator), Is.EqualTo(2 * Math.PI * R / 360 * Math.Cos(0.5 * Math.PI / 180)).Within(0.05));
            Assert.That(nearPole < nearEquator, Is.True);
        }

        [Test]
        public void GreatCircle_IstanbulAnkara_AndAcrossTheSeam()
        {
            Fixed64 km = GeoConstants.GreatCircleKm(Fixed64.Parse("41.0082"), Fixed64.Parse("28.9784"), Fixed64.Parse("39.9334"), Fixed64.Parse("32.8597"));
            Assert.That(D(km), Is.EqualTo(Haversine(41.0082, 28.9784, 39.9334, 32.8597)).Within(0.05));
            Assert.That(D(km), Is.InRange(345.0, 355.0));

            Fixed64 seam = GeoConstants.GreatCircleKm(Fixed64.Zero, Fixed64.Parse("179.5"), Fixed64.Zero, Fixed64.Parse("-179.5"));
            Assert.That(D(seam), Is.EqualTo(Haversine(0, 179.5, 0, -179.5)).Within(0.05));
            Assert.That(D(seam), Is.LessThan(112.0), "dikişte kısa yol kullanılmalı");
        }
    }
}
