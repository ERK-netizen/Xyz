using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;

namespace TarihStrateji.Core.Tests.Effects
{
    [TestFixture]
    public class EffectEngineTests
    {
        private static readonly GameDate Day1 = GameDate.FromYmd(1920, 1, 1);
        private static readonly EffectTarget Turkey = EffectTarget.Of(new CountryId(1));
        private const string Industry = "uretim.sanayi";

        private static Fixed64 F(string s) => Fixed64.Parse(s);

        [Test]
        public void Evaluate_AddsThenAppliesSummedPercent()
        {
            var e = new EffectEngine();
            e.Add(Turkey, Industry, EffectOperation.Add, F("10"), Day1, 0, "kaynak.a");
            e.Add(Turkey, Industry, EffectOperation.Percent, F("0.25"), Day1, 0, "kaynak.b");
            e.Add(Turkey, Industry, EffectOperation.Percent, F("-0.05"), Day1, 0, "kaynak.c");
            e.Add(Turkey, "baska.stat", EffectOperation.Add, F("999"), Day1, 0, null);
            Assert.That(e.Evaluate(Turkey, Industry, F("100"), Day1), Is.EqualTo(F("132"))); // (100+10) × 1.20
            Assert.That(e.Evaluate(EffectTarget.Of(new CountryId(2)), Industry, F("100"), Day1), Is.EqualTo(F("100")));
        }

        [Test]
        public void Evaluate_IsOrderIndependent()
        {
            var values = new[] { "3.3", "-1.25", "0.000001", "7" };
            var percents = new[] { "0.1", "-0.2", "0.33" };
            var a = new EffectEngine();
            var b = new EffectEngine();
            foreach (string v in values) a.Add(Turkey, Industry, EffectOperation.Add, F(v), Day1, 0, null);
            foreach (string p in percents) a.Add(Turkey, Industry, EffectOperation.Percent, F(p), Day1, 0, null);
            for (int i = percents.Length - 1; i >= 0; i--) b.Add(Turkey, Industry, EffectOperation.Percent, F(percents[i]), Day1, 0, null);
            for (int i = values.Length - 1; i >= 0; i--) b.Add(Turkey, Industry, EffectOperation.Add, F(values[i]), Day1, 0, null);
            Assert.That(a.Evaluate(Turkey, Industry, F("50"), Day1), Is.EqualTo(b.Evaluate(Turkey, Industry, F("50"), Day1)));
        }

        [Test]
        public void Percent_ClampsMultiplierAtZero_AndMultiTargetStacks()
        {
            var e = new EffectEngine();
            e.Add(Turkey, Industry, EffectOperation.Percent, F("-1.5"), Day1, 0, null);
            Assert.That(e.Evaluate(Turkey, Industry, F("100"), Day1), Is.EqualTo(Fixed64.Zero));

            var g = new EffectEngine();
            g.Add(EffectTarget.Global, Industry, EffectOperation.Percent, F("0.1"), Day1, 0, null);
            g.Add(Turkey, Industry, EffectOperation.Percent, F("0.1"), Day1, 0, null);
            g.Add(EffectTarget.Of(new RegionId(4)), Industry, EffectOperation.Add, F("5"), Day1, 0, null);
            var targets = new List<EffectTarget> { EffectTarget.Global, Turkey, EffectTarget.Of(new RegionId(4)) };
            Assert.That(g.Evaluate(targets, Industry, F("95"), Day1), Is.EqualTo(F("120")));
        }

        [Test]
        public void Duration_StartAndExpiry()
        {
            var e = new EffectEngine();
            Result<EffectId> threeDays = e.Add(Turkey, Industry, EffectOperation.Add, F("1"), Day1, 3, null);
            e.Add(Turkey, Industry, EffectOperation.Add, F("10"), Day1.AddDays(2), 0, null);
            Assert.That(e.TryGet(threeDays.Value, out EffectInstance inst) && inst.LastActiveDay == Day1.AddDays(2), Is.True);
            Assert.That(e.Evaluate(Turkey, Industry, F("0"), Day1), Is.EqualTo(F("1")));
            Assert.That(e.Evaluate(Turkey, Industry, F("0"), Day1.AddDays(2)), Is.EqualTo(F("11")));
            Assert.That(e.Evaluate(Turkey, Industry, F("0"), Day1.AddDays(3)), Is.EqualTo(F("10")), "süresi geçen etki değerlendirmeye girmez");

            var expired = new List<EffectId>();
            Assert.That(e.ExpireBefore(Day1.AddDays(2), expired), Is.EqualTo(0));
            Assert.That(e.ExpireBefore(Day1.AddDays(3), expired), Is.EqualTo(1));
            Assert.That(expired, Is.EqualTo(new[] { threeDays.Value }));
            Assert.That(e.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_Validation_And_Remove()
        {
            var e = new EffectEngine();
            Assert.That(e.Add(Turkey, "", EffectOperation.Add, F("1"), Day1, 0, null).ErrorKey, Is.EqualTo(EffectErrorKeys.IstatistikAnahtariBos));
            Assert.That(e.Add(new EffectTarget(EffectScope.Country, 0), Industry, EffectOperation.Add, F("1"), Day1, 0, null).ErrorKey, Is.EqualTo(EffectErrorKeys.HedefGecersiz));
            Assert.That(e.Add(new EffectTarget(EffectScope.Global, 5), Industry, EffectOperation.Add, F("1"), Day1, 0, null).ErrorKey, Is.EqualTo(EffectErrorKeys.HedefGecersiz));
            Assert.That(e.Add(Turkey, Industry, (EffectOperation)9, F("1"), Day1, 0, null).ErrorKey, Is.EqualTo(EffectErrorKeys.IslemGecersiz));
            Assert.That(e.Add(Turkey, Industry, EffectOperation.Add, F("1"), Day1, -1, null).ErrorKey, Is.EqualTo(EffectErrorKeys.SureGecersiz));
            Assert.That(e.Add(Turkey, Industry, EffectOperation.Add, F("1"), GameDate.MaxValue, 2, null).ErrorKey, Is.EqualTo(EffectErrorKeys.SureGecersiz));

            EffectId id = e.Add(Turkey, Industry, EffectOperation.Add, F("4"), Day1, 0, null).Value;
            Assert.That(e.Remove(id), Is.True);
            Assert.That(e.Remove(id), Is.False);
            Assert.That(e.Evaluate(Turkey, Industry, F("1"), Day1), Is.EqualTo(F("1")));
            Assert.That(e.Add(Turkey, Industry, EffectOperation.Add, F("4"), Day1, 0, null).Value.Value, Is.EqualTo(2UL), "kimlik yeniden kullanılmaz");
        }

        [Test]
        public void Snapshot_RoundTrips_ByteForByte()
        {
            var e = new EffectEngine();
            e.Add(Turkey, Industry, EffectOperation.Add, F("1.5"), Day1, 5, "olay.x");
            e.Add(EffectTarget.Of(new CityId(77)), "savunma", EffectOperation.Percent, F("0.2"), Day1, 0, null);
            e.Remove(e.Add(Turkey, Industry, EffectOperation.Add, F("9"), Day1, 0, null).Value);
            var w = new CanonicalWriter();
            EffectSnapshotCodec.Write(e, w);
            byte[] first = w.ToArray();

            Result<EffectEngine> restored = EffectSnapshotCodec.Read(new CanonicalReader(first));
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            var w2 = new CanonicalWriter();
            EffectSnapshotCodec.Write(restored.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(first));
            Assert.That(restored.Value.Evaluate(Turkey, Industry, F("0"), Day1), Is.EqualTo(F("1.5")));
            Assert.That(restored.Value.Add(Turkey, Industry, EffectOperation.Add, F("1"), Day1, 0, null).Value.Value, Is.EqualTo(4UL), "dağıtıcı geri yüklenir");
        }
    }
}
