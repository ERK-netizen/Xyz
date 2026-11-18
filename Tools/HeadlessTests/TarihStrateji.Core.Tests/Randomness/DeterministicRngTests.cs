using System;
using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;

namespace TarihStrateji.Core.Tests.Randomness
{
    [TestFixture]
    public class DeterministicRngTests
    {
        /// <summary>Değerler bağımsız bir Python uygulamasıyla hesaplandı (aynı SplitMix64 anahtar karıştırması).</summary>
        [TestCase(0UL, RngSubsystem.Kernel, 0, 0UL, 0UL, 0UL, 0x7522633222ed50d2UL)]
        [TestCase(1923UL, RngSubsystem.Combat, 701_000, 42UL, 7UL, 0UL, 0x8dc132f4e8766eeaUL)]
        [TestCase(1923UL, RngSubsystem.Combat, 701_000, 42UL, 7UL, 1UL, 0x312b4868943f60d8UL)]
        [TestCase(ulong.MaxValue, RngSubsystem.Tactical, 3_652_058, ulong.MaxValue, ulong.MaxValue, 99UL, 0x8e6690dea91775f3UL)]
        [TestCase(1923UL, RngSubsystem.Combat, 701_000, 7UL, 42UL, 0UL, 0xc5d708ac5f9e8cefUL)]
        public void Value_MatchesIndependentOracle(ulong seed, RngSubsystem sub, int day, ulong ev, ulong ent, ulong draw, ulong expected)
        {
            Assert.That(DeterministicRng.Value(seed, sub, day, ev, ent, draw), Is.EqualTo(expected));
        }

        [Test]
        public void EveryKeyComponent_ChangesTheValue_AndSwappedComponentsDiffer()
        {
            ulong baseValue = DeterministicRng.Value(1, RngSubsystem.Combat, 10, 20, 30, 0);
            Assert.That(DeterministicRng.Value(2, RngSubsystem.Combat, 10, 20, 30, 0), Is.Not.EqualTo(baseValue));
            Assert.That(DeterministicRng.Value(1, RngSubsystem.Economy, 10, 20, 30, 0), Is.Not.EqualTo(baseValue));
            Assert.That(DeterministicRng.Value(1, RngSubsystem.Combat, 11, 20, 30, 0), Is.Not.EqualTo(baseValue));
            Assert.That(DeterministicRng.Value(1, RngSubsystem.Combat, 10, 21, 30, 0), Is.Not.EqualTo(baseValue));
            Assert.That(DeterministicRng.Value(1, RngSubsystem.Combat, 10, 20, 31, 0), Is.Not.EqualTo(baseValue));
            Assert.That(DeterministicRng.Value(1, RngSubsystem.Combat, 10, 20, 30, 1), Is.Not.EqualTo(baseValue));
            Assert.That(DeterministicRng.Value(1, RngSubsystem.Combat, 10, 30, 20, 0), Is.Not.EqualTo(baseValue));
        }

        [Test]
        public void Streams_AreReproducible_AndIndependentOfOtherStreams()
        {
            var a = new RngStream(5, RngSubsystem.Movement, 100, 1, 2);
            var other = new RngStream(5, RngSubsystem.Combat, 100, 1, 2);
            var first = new List<ulong>();
            for (int i = 0; i < 50; i++)
            {
                first.Add(a.NextUInt64());
                other.NextUInt64(); // başka bir akıştan çekiliş a'yı etkilemez
            }

            var b = new RngStream(5, RngSubsystem.Movement, 100, 1, 2);
            for (int i = 0; i < 50; i++)
            {
                Assert.That(b.NextUInt64(), Is.EqualTo(first[i]));
            }

            Assert.That(b.DrawCount, Is.EqualTo(50UL));
        }

        [Test]
        public void NextInt_IsUniformAndInRange()
        {
            var rng = new RngStream(1918, RngSubsystem.Events, 1, 0, 0);
            var counts = new int[7];
            const int n = 70_000;
            for (int i = 0; i < n; i++)
            {
                int v = rng.NextInt(7);
                Assert.That(v, Is.InRange(0, 6));
                counts[v]++;
            }

            double chi = 0;
            foreach (int c in counts)
            {
                chi += Math.Pow(c - n / 7.0, 2) / (n / 7.0);
            }

            Assert.That(chi, Is.LessThan(22.46), "χ² (6 sd, p=0.001)");
            Assert.That(rng.NextInt(5, 6), Is.EqualTo(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(3, 3));
        }

        [Test]
        public void Fraction_And_Chance()
        {
            var rng = new RngStream(7, RngSubsystem.Combat, 3, 0, 0);
            int hits = 0;
            for (int i = 0; i < 100_000; i++)
            {
                Fixed64 f = rng.NextFraction();
                Assert.That(f >= Fixed64.Zero && f < Fixed64.One, Is.True);
                if (rng.Chance(Fixed64.Parse("0.3")))
                {
                    hits++;
                }
            }

            Assert.That(hits, Is.InRange(29_400, 30_600));
            ulong before = rng.DrawCount;
            Assert.That(rng.Chance(Fixed64.Zero), Is.False);
            Assert.That(rng.Chance(Fixed64.FromInt(2)), Is.True);
            Assert.That(rng.DrawCount, Is.EqualTo(before + 2), "uç olasılıklar da bir çekiliş tüketir");
        }
    }
}
