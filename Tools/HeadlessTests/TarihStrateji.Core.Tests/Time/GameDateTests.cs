using System;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Tests.Time
{
    [TestFixture]
    public class GameDateTests
    {
        [Test]
        public void DayNumber_MatchesDotNetCalendar_ForEveryDayInRange()
        {
            // Kâhin: System.DateTime (yalnız testte serbest). 1700–2100 arası her gün.
            var start = new DateTime(1700, 1, 1);
            int startDay = (int)(start.Ticks / TimeSpan.TicksPerDay);
            for (int offset = 0; offset < 146_463; offset++)
            {
                DateTime expected = start.AddDays(offset);
                GameDate date = GameDate.FromDayNumber(startDay + offset);
                Assert.That(date.Year, Is.EqualTo(expected.Year));
                Assert.That(date.Month, Is.EqualTo(expected.Month));
                Assert.That(date.Day, Is.EqualTo(expected.Day));
                Assert.That(GameDate.FromYmd(expected.Year, expected.Month, expected.Day).DayNumber, Is.EqualTo(startDay + offset));
                int isoIndex = ((int)expected.DayOfWeek + 6) % 7;
                Assert.That(date.IsoDayOfWeekIndex, Is.EqualTo(isoIndex));
            }
        }

        [Test]
        public void Bounds_AndValidation()
        {
            Assert.That(GameDate.MinValue.ToString(), Is.EqualTo("0001-01-01"));
            Assert.That(GameDate.MaxValue.ToString(), Is.EqualTo("9999-12-31"));
            Assert.That(GameDate.MaxValue.DayNumber, Is.EqualTo((int)(DateTime.MaxValue.Ticks / TimeSpan.TicksPerDay)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameDate.FromYmd(1900, 2, 29));
            Assert.That(GameDate.FromYmd(2000, 2, 29).ToString(), Is.EqualTo("2000-02-29"));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameDate.FromYmd(0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameDate.FromYmd(2000, 13, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameDate.FromDayNumber(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameDate.MaxValue.AddDays(1));
            Assert.That(GameDate.IsLeapYear(1900), Is.False);
            Assert.That(GameDate.IsLeapYear(2000), Is.True);
            Assert.That(GameDate.IsLeapYear(1916), Is.True);
        }

        [Test]
        public void Arithmetic_Windows_AndComparison()
        {
            GameDate armistice = GameDate.FromYmd(1918, 10, 30);
            GameDate lausanne = GameDate.FromYmd(1923, 7, 24);
            Assert.That(armistice.AddDays(2) .ToString(), Is.EqualTo("1918-11-01"));
            Assert.That(armistice.DaysUntil(lausanne), Is.EqualTo(1728));
            Assert.That(lausanne.DaysUntil(armistice), Is.EqualTo(-1728));
            Assert.That(GameDate.FromYmd(1920, 4, 23).IsWithin(armistice, lausanne), Is.True);
            Assert.That(lausanne.IsWithin(armistice, lausanne), Is.True);
            Assert.That(lausanne.AddDays(1).IsWithin(armistice, lausanne), Is.False);
            Assert.That(armistice < lausanne && lausanne >= armistice, Is.True);
        }

        [TestCase("1923-10-29", true)]
        [TestCase("1923-02-29", false)]
        [TestCase("1923-1-29", false)]
        [TestCase("19231029", false)]
        [TestCase("abcd-ef-gh", false)]
        [TestCase(null, false)]
        public void TryParse_IsStrict(string text, bool ok)
        {
            Assert.That(GameDate.TryParse(text, out GameDate date), Is.EqualTo(ok));
            if (ok)
            {
                Assert.That(date.ToString(), Is.EqualTo(text));
                Assert.That(GameDate.Parse(text), Is.EqualTo(date));
            }
            else
            {
                Assert.Throws<FormatException>(() => GameDate.Parse(text));
            }
        }
    }
}
