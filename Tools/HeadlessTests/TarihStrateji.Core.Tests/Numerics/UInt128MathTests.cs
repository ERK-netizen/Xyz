using System;
using System.Numerics;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Tests.Numerics
{
    [TestFixture]
    public class UInt128MathTests
    {
        private static readonly BigInteger Two64 = BigInteger.One << 64;

        private static readonly ulong[] Edge =
        {
            0UL, 1UL, 2UL, 3UL, 999_999UL, 1_000_000UL, 0xFFFFFFFFUL, 0x100000000UL,
            0x7FFFFFFFFFFFFFFFUL, 0x8000000000000000UL, ulong.MaxValue - 1UL, ulong.MaxValue,
        };

        [Test]
        public void Multiply_MatchesBigInteger_OnEdges()
        {
            foreach (ulong a in Edge)
            {
                foreach (ulong b in Edge)
                {
                    UInt128Math.Multiply(a, b, out ulong hi, out ulong lo);
                    BigInteger expected = (BigInteger)a * b;
                    Assert.That(((BigInteger)hi * Two64) + lo, Is.EqualTo(expected), $"{a} * {b}");
                }
            }
        }

        [Test]
        public void MultiplyAndDivide_MatchBigInteger_OnRandomValues()
        {
            var rng = new Random(20261118);
            var buffer = new byte[8];
            for (int i = 0; i < 20_000; i++)
            {
                rng.NextBytes(buffer);
                ulong a = BitConverter.ToUInt64(buffer, 0) >> rng.Next(0, 64);
                rng.NextBytes(buffer);
                ulong b = BitConverter.ToUInt64(buffer, 0) >> rng.Next(0, 64);
                rng.NextBytes(buffer);
                ulong d = (BitConverter.ToUInt64(buffer, 0) >> rng.Next(0, 63)) | 1UL;

                UInt128Math.Multiply(a, b, out ulong hi, out ulong lo);
                BigInteger product = (BigInteger)a * b;
                Assert.That(((BigInteger)hi * Two64) + lo, Is.EqualTo(product));

                if (hi < d)
                {
                    ulong q = UInt128Math.Divide(hi, lo, d, out ulong r);
                    Assert.That((BigInteger)q, Is.EqualTo(product / d));
                    Assert.That((BigInteger)r, Is.EqualTo(product % d));
                }
                else
                {
                    Assert.Throws<OverflowException>(() => UInt128Math.Divide(hi, lo, d, out _));
                }
            }
        }

        [Test]
        public void Divide_ByZero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() => UInt128Math.Divide(0UL, 5UL, 0UL, out _));
        }

        [Test]
        public void Divide_WithLargeDivisor_HandlesShiftCarry()
        {
            // divisor > 2^63: kaydırma sırasında elde (carry) yolu çalışır.
            ulong d = 0xF000000000000001UL;
            ulong hi = 0xEFFFFFFFFFFFFFFFUL;
            ulong lo = 0x123456789ABCDEF0UL;
            ulong q = UInt128Math.Divide(hi, lo, d, out ulong r);
            BigInteger n = ((BigInteger)hi * Two64) + lo;
            Assert.That((BigInteger)q, Is.EqualTo(n / d));
            Assert.That((BigInteger)r, Is.EqualTo(n % d));
        }

        [TestCase(0UL, 0UL)]
        [TestCase(1UL, 1UL)]
        [TestCase(2UL, 1UL)]   // 1.414 → 1
        [TestCase(3UL, 2UL)]   // 1.732 → 2
        [TestCase(6UL, 2UL)]   // 2.449 → 2
        [TestCase(7UL, 3UL)]   // 2.645 → 3
        [TestCase(1_000_000_000_000UL, 1_000_000UL)]
        [TestCase(ulong.MaxValue, 4294967296UL)] // 4294967295.99999 → 2^32
        public void SqrtRounded_SmallValues(ulong n, ulong expected)
        {
            Assert.That(UInt128Math.SqrtRounded(0UL, n), Is.EqualTo(expected));
        }

        [Test]
        public void SqrtRounded_MatchesBigIntegerOracle_On128BitValues()
        {
            var rng = new Random(7);
            var buffer = new byte[8];
            for (int i = 0; i < 2_000; i++)
            {
                rng.NextBytes(buffer);
                ulong hi = BitConverter.ToUInt64(buffer, 0) >> rng.Next(0, 64);
                rng.NextBytes(buffer);
                ulong lo = BitConverter.ToUInt64(buffer, 0);
                BigInteger n = ((BigInteger)hi * Two64) + lo;

                BigInteger root = UInt128Math.SqrtRounded(hi, lo);
                // Doğru yuvarlanmış kök: (root - 0.5)^2 <= n < (root + 0.5)^2
                // ⇔ 4n >= (2root - 1)^2 ve 4n < (2root + 1)^2
                BigInteger fourN = 4 * n;
                BigInteger lower = (2 * root) - 1;
                BigInteger upper = (2 * root) + 1;
                Assert.That(root == 0 || fourN >= lower * lower, Is.True, $"alt sınır n={n}");
                Assert.That(fourN < upper * upper, Is.True, $"üst sınır n={n}");
            }
        }
    }
}
