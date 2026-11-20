using NUnit.Framework;
using TarihStrateji.Unity.Bootstrap;

namespace TarihStrateji.Tests.EditMode
{
    /// <summary>Cihaz ölçümünün sayıları (Adım 17): yüzdelik, biçim, özet ve rapor.</summary>
    public class MeasurementLogTests
    {
        [Test]
        public void Percentile_NearestRank_OnUnsortedSample_WithoutChangingIt()
        {
            var values = new long[] { 50, 10, 40, 20, 30 };
            Assert.That(MeasurementLog.Percentile(values, 50), Is.EqualTo(30L));
            Assert.That(MeasurementLog.Percentile(values, 0), Is.EqualTo(10L));
            Assert.That(MeasurementLog.Percentile(values, 100), Is.EqualTo(50L));
            Assert.That(MeasurementLog.Percentile(values, 95), Is.EqualTo(50L));
            Assert.That(values[0], Is.EqualTo(50L), "örnek sıralanmaz");
            Assert.That(MeasurementLog.Percentile(new long[0], 50), Is.EqualTo(0L));
        }

        [Test]
        public void Formatting_UsesTurkishDecimalComma()
        {
            Assert.That(MeasurementLog.Millis(12_345), Is.EqualTo("12,3 ms"));
            Assert.That(MeasurementLog.Millis(16_667), Is.EqualTo("16,7 ms"));
            Assert.That(MeasurementLog.Megabytes(3_565_158), Is.EqualTo("3,4 MB"));
            Assert.That(MeasurementLog.FramesPerSecond(16_667), Is.EqualTo(60L));
            Assert.That(MeasurementLog.FramesPerSecond(0), Is.EqualTo(0L));
        }

        [Test]
        public void SummaryAndReport_ContainTheMeasuredNumbers()
        {
            var log = new MeasurementLog { SaveMicros = 120_000, AllocatedBytes = 200L * 1_048_576L, Device = "Deneme cihazı" };
            for (int i = 1; i <= 20; i++)
            {
                log.AddDay(i * 1_000L);
                log.AddFrame(16_667);
            }

            string summary = log.Summary();
            StringAssert.Contains("gün P50 11,0 ms", summary);
            StringAssert.Contains("P95 19,0 ms", summary);
            StringAssert.Contains("60 fps", summary);
            StringAssert.Contains("kayıt 120,0 ms", summary);
            StringAssert.Contains("200,0 MB", summary);
            string report = log.Report("20.11.2026 12:00:00");
            StringAssert.Contains("Deneme cihazı", report);
            StringAssert.Contains("20 örnek", report);
        }
    }
}
