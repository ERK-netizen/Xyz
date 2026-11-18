using System;
using System.Numerics;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Tests.Support;

namespace TarihStrateji.Core.Tests.Numerics
{
    [TestFixture]
    public class Fixed64Tests
    {
        private static Fixed64 F(string text) => Fixed64.Parse(text);

        /// <summary>Kâhin: n / d, en yakına, yarıda sıfırdan uzağa.</summary>
        private static BigInteger RoundHalfAwayDiv(BigInteger n, BigInteger d)
        {
            int sign = n.Sign * d.Sign;
            BigInteger an = BigInteger.Abs(n);
            BigInteger ad = BigInteger.Abs(d);
            BigInteger q = BigInteger.DivRem(an, ad, out BigInteger r);
            if (r * 2 >= ad)
            {
                q += 1;
            }

            return sign < 0 ? -q : q;
        }

        private static bool FitsLong(BigInteger v) => v >= long.MinValue && v <= long.MaxValue;

        [Test]
        public void Scale_IsSingleMillion()
        {
            Assert.That(Fixed64.Scale, Is.EqualTo(1_000_000L));
            Assert.That(Fixed64.One.Raw, Is.EqualTo(1_000_000L));
            Assert.That(Fixed64.Half.Raw, Is.EqualTo(500_000L));
        }

        [TestCase("0", 0L)]
        [TestCase("1", 1_000_000L)]
        [TestCase("-1", -1_000_000L)]
        [TestCase("+2.5", 2_500_000L)]
        [TestCase("0.000001", 1L)]
        [TestCase(".5", 500_000L)]
        [TestCase("7.", 7_000_000L)]
        [TestCase("0.0000005", 1L)]      // yarıda sıfırdan uzağa
        [TestCase("-0.0000005", -1L)]
        [TestCase("0.00000049", 0L)]
        [TestCase("1.23456789", 1_234_568L)]
        [TestCase("9223372036854.775807", long.MaxValue)]
        [TestCase("-9223372036854.775808", long.MinValue)]
        public void Parse_ExactAndRounded(string text, long raw)
        {
            Assert.That(F(text).Raw, Is.EqualTo(raw));
        }

        [TestCase("")]
        [TestCase("-")]
        [TestCase(".")]
        [TestCase("1.2.3")]
        [TestCase("1,5")]
        [TestCase("1e5")]
        [TestCase(" 1")]
        [TestCase("9223372036854.775808")]
        [TestCase("-9223372036854.775809")]
        [TestCase("99999999999999999999999")]
        public void TryParse_RejectsInvalid(string text)
        {
            Assert.That(Fixed64.TryParse(text, out _), Is.False);
            Assert.Throws<FormatException>(() => Fixed64.Parse(text));
        }

        [TestCase(0L, "0")]
        [TestCase(1_500_000L, "1.5")]
        [TestCase(-1_500_000L, "-1.5")]
        [TestCase(1L, "0.000001")]
        [TestCase(-1L, "-0.000001")]
        [TestCase(long.MinValue, "-9223372036854.775808")]
        [TestCase(long.MaxValue, "9223372036854.775807")]
        public void ToString_IsInvariantAndTrimmed(long raw, string expected)
        {
            Assert.That(Fixed64.FromRaw(raw).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void ToString_Parse_RoundTrips()
        {
            var rng = new Random(42);
            for (int i = 0; i < 10_000; i++)
            {
                long raw = rng.NextInt64(long.MinValue, long.MaxValue);
                Fixed64 v = Fixed64.FromRaw(raw);
                Assert.That(Fixed64.Parse(v.ToString()), Is.EqualTo(v));
            }
        }

        [Test]
        public void Arithmetic_BasicValues()
        {
            Assert.That(F("1.5") + F("2.25"), Is.EqualTo(F("3.75")));
            Assert.That(F("1.5") - F("2.25"), Is.EqualTo(F("-0.75")));
            Assert.That(F("1.5") * F("-2"), Is.EqualTo(F("-3")));
            Assert.That(F("10") / F("4"), Is.EqualTo(F("2.5")));
            Assert.That(F("1") / F("3"), Is.EqualTo(F("0.333333")));
            Assert.That(F("2") / F("3"), Is.EqualTo(F("0.666667")));
            Assert.That(F("-2") / F("3"), Is.EqualTo(F("-0.666667")));
            Assert.That(F("7") % F("2"), Is.EqualTo(F("1")));
            Assert.That(F("-7") % F("2"), Is.EqualTo(F("-1")));
            Assert.That(F("2.5") * 4, Is.EqualTo(F("10")));
            Assert.That(3 * F("0.5"), Is.EqualTo(F("1.5")));
            Assert.That(F("1") / 3, Is.EqualTo(F("0.333333")));
            Assert.That(-F("4"), Is.EqualTo(F("-4")));
        }

        [Test]
        public void Multiply_RoundsHalfAwayFromZero()
        {
            Fixed64 tiny = Fixed64.Epsilon;
            Assert.That((tiny * Fixed64.Half).Raw, Is.EqualTo(1L));
            Assert.That((-tiny * Fixed64.Half).Raw, Is.EqualTo(-1L));
            Assert.That((tiny * F("0.499999")).Raw, Is.EqualTo(0L));
            Assert.That((F("-0.000003") * Fixed64.Half).Raw, Is.EqualTo(-2L)); // -1.5 → -2
        }

        [Test]
        public void MultiplyAndDivide_MatchBigIntegerOracle()
        {
            var rng = new Random(1918);
            for (int i = 0; i < 50_000; i++)
            {
                long a = rng.NextInt64(-4_000_000_000_000L, 4_000_000_000_000L) >> rng.Next(0, 40);
                long b = rng.NextInt64(-4_000_000_000_000L, 4_000_000_000_000L) >> rng.Next(0, 40);
                Fixed64 fa = Fixed64.FromRaw(a);
                Fixed64 fb = Fixed64.FromRaw(b);

                BigInteger mul = RoundHalfAwayDiv((BigInteger)a * b, Fixed64.Scale);
                if (FitsLong(mul))
                {
                    Assert.That((fa * fb).Raw, Is.EqualTo((long)mul), $"{a} * {b}");
                }
                else
                {
                    Assert.Throws<OverflowException>(() => _ = fa * fb);
                }

                if (b != 0)
                {
                    BigInteger div = RoundHalfAwayDiv((BigInteger)a * Fixed64.Scale, b);
                    if (FitsLong(div))
                    {
                        Assert.That((fa / fb).Raw, Is.EqualTo((long)div), $"{a} / {b}");
                    }
                    else
                    {
                        Assert.Throws<OverflowException>(() => _ = fa / fb);
                    }
                }
            }
        }

        [Test]
        public void Overflow_AlwaysThrows()
        {
            Assert.Throws<OverflowException>(() => _ = Fixed64.MaxValue + Fixed64.Epsilon);
            Assert.Throws<OverflowException>(() => _ = Fixed64.MinValue - Fixed64.Epsilon);
            Assert.Throws<OverflowException>(() => _ = -Fixed64.MinValue);
            Assert.Throws<OverflowException>(() => _ = Fixed64.MaxValue * F("2"));
            Assert.Throws<OverflowException>(() => _ = Fixed64.MaxValue / F("0.5"));
            Assert.Throws<OverflowException>(() => _ = Fixed64.MinValue * Fixed64.MinusOne);
            Assert.Throws<OverflowException>(() => Fixed64.FromInt(long.MaxValue / 100));
            Assert.Throws<OverflowException>(() => _ = Fixed64.MaxValue * 2L);
        }

        [Test]
        public void MinValue_EdgeCases_DoNotWrap()
        {
            Assert.That(Fixed64.MinValue * Fixed64.One, Is.EqualTo(Fixed64.MinValue));
            Assert.That(Fixed64.MinValue / Fixed64.One, Is.EqualTo(Fixed64.MinValue));
            Assert.That(Fixed64.MinValue % Fixed64.FromRaw(-1L), Is.EqualTo(Fixed64.Zero));
            Assert.That(Fixed64.MinValue.RoundToLong(), Is.EqualTo(-9_223_372_036_855L));
        }

        [Test]
        public void DivideByZero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() => _ = Fixed64.One / Fixed64.Zero);
            Assert.Throws<DivideByZeroException>(() => _ = Fixed64.One / 0L);
            Assert.Throws<DivideByZeroException>(() => _ = Fixed64.One % Fixed64.Zero);
            Assert.Throws<DivideByZeroException>(() => Fixed64.FromRatio(1, 0));
            Assert.Throws<DivideByZeroException>(() => Fixed64.MulDiv(Fixed64.One, Fixed64.One, Fixed64.Zero));
        }

        [Test]
        public void FromRatio_And_FromPercent()
        {
            Assert.That(Fixed64.FromRatio(1, 3), Is.EqualTo(F("0.333333")));
            Assert.That(Fixed64.FromRatio(-2, 3), Is.EqualTo(F("-0.666667")));
            Assert.That(Fixed64.FromRatio(5, -10), Is.EqualTo(F("-0.5")));
            Assert.That(Fixed64.FromPercent(25), Is.EqualTo(F("0.25")));
        }

        [Test]
        public void MulDiv_UsesSingleRounding()
        {
            // (1/3 adım adım) * 3 kayıp verir; MulDiv tek yuvarlamayla kesindir.
            Fixed64 stepwise = (F("1") / F("3")) * F("3");
            Assert.That(stepwise, Is.EqualTo(F("0.999999")));
            Assert.That(Fixed64.MulDiv(F("1"), F("3"), F("3")), Is.EqualTo(F("1")));
            Assert.That(Fixed64.MulDiv(F("-2"), F("5"), F("3")), Is.EqualTo(F("-3.333333")));

            // Ara çarpım 64 biti aşsa da sonuç sığıyorsa doğru hesaplanır.
            Fixed64 big = F("9000000000000");
            Assert.That(Fixed64.MulDiv(big, big, big), Is.EqualTo(big));
        }

        [TestCase("1.5", "1", "2", "2", "1")]
        [TestCase("-1.5", "-2", "-1", "-2", "-1")]
        [TestCase("2.5", "2", "3", "3", "2")]
        [TestCase("-2.5", "-3", "-2", "-3", "-2")]
        [TestCase("2.4", "2", "3", "2", "2")]
        [TestCase("-2.4", "-3", "-2", "-2", "-2")]
        [TestCase("3", "3", "3", "3", "3")]
        public void FloorCeilingRoundTruncate(string value, string floor, string ceiling, string round, string truncate)
        {
            Fixed64 v = F(value);
            Assert.That(Fixed64.Floor(v), Is.EqualTo(F(floor)));
            Assert.That(Fixed64.Ceiling(v), Is.EqualTo(F(ceiling)));
            Assert.That(Fixed64.Round(v), Is.EqualTo(F(round)));
            Assert.That(v.FloorToLong(), Is.EqualTo(long.Parse(floor)));
            Assert.That(v.RoundToLong(), Is.EqualTo(long.Parse(round)));
            Assert.That(v.TruncateToLong(), Is.EqualTo(long.Parse(truncate)));
        }

        [TestCase("0", "0")]
        [TestCase("1", "1")]
        [TestCase("2", "1.414214")]
        [TestCase("0.25", "0.5")]
        [TestCase("1000000", "1000")]
        [TestCase("0.000001", "0.001")]
        [TestCase("9223372036854.775807", "3037000.499976")]
        public void Sqrt_IsCorrectlyRounded(string value, string expected)
        {
            Assert.That(Fixed64.Sqrt(F(value)), Is.EqualTo(F(expected)));
        }

        [Test]
        public void Sqrt_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed64.Sqrt(Fixed64.MinusOne));
        }

        [Test]
        public void Pow_IntegerExponents()
        {
            Assert.That(Fixed64.Pow(F("2"), 0), Is.EqualTo(Fixed64.One));
            Assert.That(Fixed64.Pow(F("2"), 10), Is.EqualTo(F("1024")));
            Assert.That(Fixed64.Pow(F("1.1"), 2), Is.EqualTo(F("1.21")));
            Assert.That(Fixed64.Pow(F("-3"), 3), Is.EqualTo(F("-27")));
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed64.Pow(F("2"), -1));
        }

        [Test]
        public void MinMaxClampLerpAbs()
        {
            Assert.That(Fixed64.Min(F("1"), F("2")), Is.EqualTo(F("1")));
            Assert.That(Fixed64.Max(F("1"), F("2")), Is.EqualTo(F("2")));
            Assert.That(Fixed64.Clamp(F("5"), F("0"), F("3")), Is.EqualTo(F("3")));
            Assert.That(Fixed64.Clamp(F("-5"), F("0"), F("3")), Is.EqualTo(F("0")));
            Assert.That(Fixed64.Clamp01(F("0.4")), Is.EqualTo(F("0.4")));
            Assert.Throws<ArgumentException>(() => Fixed64.Clamp(F("1"), F("2"), F("1")));
            Assert.That(Fixed64.Lerp(F("10"), F("20"), F("0.25")), Is.EqualTo(F("12.5")));
            Assert.That(Fixed64.Abs(F("-3.5")), Is.EqualTo(F("3.5")));
            Assert.That(F("-3").Sign, Is.EqualTo(-1));
            Assert.That(Fixed64.Zero.Sign, Is.EqualTo(0));
        }

        [Test]
        public void Comparison_And_Equality()
        {
            Assert.That(F("1") < F("1.000001"), Is.True);
            Assert.That(F("-1") > F("-1.000001"), Is.True);
            Assert.That(F("2") >= F("2"), Is.True);
            Assert.That(F("2").Equals((object)F("2")), Is.True);
            Assert.That(F("2").CompareTo((object)null), Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => F("2").CompareTo("2"));
            Assert.That(F("2").GetHashCode(), Is.EqualTo(F("2").GetHashCode()));
        }

        /// <summary>
        /// Altın determinizm testi: sabit bir işlem zinciri her platformda aynı ham değerleri
        /// üretmelidir. Bu sabit değişirse Fixed64 semantiği değişmiştir; bu bir kayıt/yeniden
        /// oynatma uyumluluk kırılmasıdır ve bilinçli onay ister.
        /// </summary>
        [Test]
        public void GoldenHash_OperationChain_IsStable()
        {
            var hash = new Fnv1a64();
            Fixed64 acc = F("1918.1030");
            Fixed64 growth = F("1.0137");
            for (int i = 1; i <= 2_000; i++)
            {
                Fixed64 k = Fixed64.FromRatio(i, 7);
                acc = (acc * growth) - (acc / F("97.3"));
                acc = Fixed64.Clamp(acc, F("-1000000"), F("1000000"));
                Fixed64 mixed = Fixed64.MulDiv(acc, k, F("13.37")) + Fixed64.Sqrt(Fixed64.Abs(acc));
                hash.Add(acc.Raw);
                hash.Add(mixed.Raw);
                hash.Add(Fixed64.Floor(mixed / F("3.3")).Raw);
            }

            Assert.That(hash.Value, Is.EqualTo(GoldenValues.Fixed64Chain), $"yeni hash: 0x{hash.Value:X16}UL");
        }
    }
}
