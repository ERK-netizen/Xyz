using System;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Tests.Numerics
{
    [TestFixture]
    public class FixedTrigTests
    {
        private const double Ulp = 1e-6;

        private static Fixed64 F(double v) => Fixed64.FromRaw((long)Math.Round(v * 1e6, MidpointRounding.AwayFromZero));

        private static double D(Fixed64 v) => v.Raw / 1e6;

        [Test]
        public void SinCos_MatchDoubleOracle_AcrossFullCircleAndBeyond()
        {
            for (long milli = -720_000; milli <= 720_000; milli += 37)
            {
                double deg = milli / 1000.0;
                double rad = deg * Math.PI / 180.0;
                Assert.That(D(FixedTrig.SinDegrees(F(deg))), Is.EqualTo(Math.Sin(rad)).Within(Ulp), $"sin({deg})");
                Assert.That(D(FixedTrig.CosDegrees(F(deg))), Is.EqualTo(Math.Cos(rad)).Within(Ulp), $"cos({deg})");
            }
        }

        [TestCase(0, 0)]
        [TestCase(30, 0.5)]
        [TestCase(90, 1)]
        [TestCase(180, 0)]
        [TestCase(270, -1)]
        [TestCase(-90, -1)]
        public void Sin_ExactAtSpecialAngles(int degrees, double expected)
        {
            Assert.That(D(FixedTrig.SinDegrees(Fixed64.FromInt(degrees))), Is.EqualTo(expected).Within(Ulp));
        }

        [Test]
        public void Asin_IsInverseOfSin()
        {
            for (int i = -1000; i <= 1000; i++)
            {
                double y = i / 1000.0;
                double expected = Math.Asin(y) * 180.0 / Math.PI;
                // asin, 1'e yaklaştıkça kötü koşullanır: girdi 10^-6 nicemlenmiştir.
                double tolerance = Math.Abs(y) > 0.999 ? 0.1 : 2e-5;
                Assert.That(D(FixedTrig.AsinDegrees(F(y))), Is.EqualTo(expected).Within(tolerance), $"asin({y})");
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => FixedTrig.AsinDegrees(Fixed64.FromRaw(1_000_001)));
        }

        [Test]
        public void DegreeRadianConversions()
        {
            Assert.That(D(FixedTrig.DegreesToRadians(Fixed64.FromInt(180))), Is.EqualTo(Math.PI).Within(Ulp));
            Assert.That(D(FixedTrig.RadiansToDegrees(FixedTrig.Pi)), Is.EqualTo(180.0).Within(1e-4));
            Assert.That(D(FixedTrig.RadiansToDegrees(Fixed64.One)), Is.EqualTo(180.0 / Math.PI).Within(Ulp));
        }

        [Test]
        public void CentralAngle_MatchesHaversineOracle_IncludingSeamAndAntipodes()
        {
            var rng = new Random(1071);
            for (int i = 0; i < 5000; i++)
            {
                double lat1 = rng.NextDouble() * 170 - 85, lon1 = rng.NextDouble() * 360 - 180;
                double lat2, lon2;
                if (i % 3 == 0)
                {
                    // Yakın noktalar: harita komşuluklarının ölçeği.
                    lat2 = Math.Clamp(lat1 + (rng.NextDouble() - 0.5), -89, 89);
                    lon2 = lon1 + (rng.NextDouble() - 0.5);
                }
                else
                {
                    lat2 = rng.NextDouble() * 170 - 85;
                    lon2 = rng.NextDouble() * 360 - 180;
                }

                Fixed64 fl1 = F(lat1), fo1 = F(lon1), fl2 = F(lat2), fo2 = F(lon2);
                double expected = Haversine(D(fl1), D(fo1), D(fl2), D(fo2));
                Assert.That(D(FixedTrig.CentralAngleRadians(fl1, fo1, fl2, fo2)), Is.EqualTo(expected).Within(3e-6),
                    $"({lat1},{lon1})-({lat2},{lon2})");
            }

            // ±180° dikişi: 179.5 ile -179.5 arası 1° boylam.
            double seam = D(FixedTrig.CentralAngleRadians(Fixed64.Zero, F(179.5), Fixed64.Zero, F(-179.5)));
            Assert.That(seam, Is.EqualTo(Math.PI / 180).Within(2e-6));
            Assert.That(D(FixedTrig.CentralAngleRadians(Fixed64.Zero, Fixed64.Zero, Fixed64.Zero, Fixed64.FromInt(180))),
                Is.EqualTo(Math.PI).Within(2e-6));
        }

        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            double r = Math.PI / 180;
            double dlat = (lat2 - lat1) * r, dlon = (lon2 - lon1) * r;
            double h = Math.Pow(Math.Sin(dlat / 2), 2) + Math.Cos(lat1 * r) * Math.Cos(lat2 * r) * Math.Pow(Math.Sin(dlon / 2), 2);
            return 2 * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }
    }
}
