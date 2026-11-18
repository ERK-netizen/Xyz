using System;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Population;

namespace TarihStrateji.Core.Tests.Population
{
    [TestFixture]
    public class DemographyTests
    {
        private static readonly DemographyParameters Baseline = BaselineContent.CreateDemography();

        private static RegionPopulation Region(long[] cohorts)
        {
            var state = new PopulationState();
            return state.AddRegion(new RegionId(1), cohorts).Value;
        }

        private static long[] OnlyBand(int band, long people)
        {
            var c = new long[AgeBands.Count];
            c[band] = people;
            return c;
        }

        /// <summary>game-balance: türetilmiş değer sabitlenir. −ln(1 − 0.09) = 0.0943107 (double).</summary>
        [Test]
        public void HazardDerivation_IsPinned_AndMatchesNaturalLog()
        {
            Fixed64 h = DemographyParameters.HazardFromAnnualProbability(Fixed64.Parse("0.09"));
            Assert.That(h.Raw / 1e6, Is.EqualTo(-Math.Log(0.91)).Within(3e-6));
            Assert.That(h, Is.EqualTo(Baseline.MortalityHazard(0)));
            Assert.That(h.Raw, Is.EqualTo(94_311L), "türetilmiş hazard değişirse demografi dengesi değişmiştir");
            for (int i = 0; i <= 50; i++)
            {
                double a = i / 100.0;
                Assert.That(DemographyParameters.HazardFromAnnualProbability(Fixed64.FromRatio(i, 100)).Raw / 1e6,
                    Is.EqualTo(-Math.Log(1 - a)).Within(2e-5), $"a={a}");
            }
        }

        [Test]
        public void AnnualSurvival_OfClosedBand_MatchesAnnualProbability()
        {
            RegionPopulation p = Region(OnlyBand(AgeBands.Last, 1_000_000));
            for (int d = 0; d < 365; d++)
            {
                DemographyRules.AdvanceDay(p, Baseline, Fixed64.One);
            }

            // 80+ bandı yaşlanmayla boşalmaz; yıllık ölüm olasılığı 0.22 → 780 000 hayatta.
            Assert.That(p.Cohort(AgeBands.Last), Is.EqualTo(780_000).Within(500));
            Assert.That(p.Total, Is.EqualTo(p.SumCohorts()));
        }

        [Test]
        public void Aging_MovesOneFifthOfABandPerYear()
        {
            var noDeath = new DemographyParameters(new Fixed64[17], new Fixed64[17], Fixed64.Half, Fixed64.Zero);
            RegionPopulation p = Region(OnlyBand(5, 1_000_000));
            for (int d = 0; d < 365; d++)
            {
                DemographyRules.AdvanceDay(p, noDeath, Fixed64.One);
            }

            Assert.That(p.Total, Is.EqualTo(1_000_000));
            // x5' = −0.2·x5 → x5(1) = e^(−0.2); x6' = 0.2·x5 − 0.2·x6 → x6(1) = 0.2·e^(−0.2).
            Assert.That(p.Cohort(5), Is.EqualTo(818_731).Within(300));
            Assert.That(p.Cohort(6), Is.EqualTo(163_746).Within(300));
        }

        [Test]
        public void EveryDay_PeopleAreConserved_AsBirthsMinusDeaths()
        {
            RegionPopulation p = Region(BaselineContent.AgePyramid(250_000));
            for (int d = 0; d < 400; d++)
            {
                long before = p.Total;
                DemographyRules.DayResult r = DemographyRules.AdvanceDay(p, Baseline, Fixed64.One);
                Assert.That(p.Total, Is.EqualTo(before + r.Births - r.Deaths));
                Assert.That(p.Total, Is.EqualTo(p.SumCohorts()));
                for (int b = 0; b < AgeBands.Count; b++)
                {
                    Assert.That(p.Cohort(b), Is.GreaterThanOrEqualTo(0));
                }
            }
        }

        /// <summary>game-balance bandı: 1920'ler genç nüfusu yılda %1–2,6 büyür (1927–35 ≈ %2,1).</summary>
        [Test]
        public void Baseline_AnnualGrowth_IsInsideHistoricalBand()
        {
            RegionPopulation p = Region(BaselineContent.AgePyramid(1_000_000));
            long births = 0, deaths = 0;
            for (int d = 0; d < 365; d++)
            {
                DemographyRules.DayResult r = DemographyRules.AdvanceDay(p, Baseline, Fixed64.One);
                births += r.Births;
                deaths += r.Deaths;
            }

            double growth = (p.Total - 1_000_000) / 1_000_000.0;
            Assert.That(growth, Is.InRange(0.010, 0.026), $"büyüme {growth:P2}");
            Assert.That(births / 1000.0, Is.InRange(35.0, 46.0), "kaba doğum hızı ‰");
            Assert.That(deaths / 1000.0, Is.InRange(18.0, 30.0), "kaba ölüm hızı ‰");
        }

        [Test]
        public void Famine_RaisesMortality()
        {
            RegionPopulation fed = Region(BaselineContent.AgePyramid(500_000));
            RegionPopulation starving = Region(BaselineContent.AgePyramid(500_000));
            long fedDeaths = 0, starvingDeaths = 0;
            for (int d = 0; d < 90; d++)
            {
                fedDeaths += DemographyRules.AdvanceDay(fed, Baseline, Fixed64.One).Deaths;
                starvingDeaths += DemographyRules.AdvanceDay(starving, Baseline, Fixed64.FromInt(3)).Deaths;
            }

            Assert.That(starvingDeaths, Is.InRange(fedDeaths * 28 / 10, fedDeaths * 32 / 10));
        }

        [Test]
        public void Mobilization_ReducesLaborForce_AndIsBounded()
        {
            RegionPopulation p = Region(BaselineContent.AgePyramid(100_000));
            long working = p.WorkingAge;
            long laborBefore = p.LaborForce(Baseline.LaborParticipation);
            Assert.That(PopulationRules.Mobilize(p, 10_000).IsValid, Is.True);
            Assert.That(p.LaborForce(Baseline.LaborParticipation), Is.EqualTo(laborBefore - 6_000));
            Assert.That(p.Total, Is.EqualTo(100_000), "seferberlik nüfusu değiştirmez");
            Assert.That(PopulationRules.Mobilize(p, working).ErrorKey, Is.EqualTo(PopulationErrorKeys.InsanGucuYetersiz));
            Assert.That(PopulationRules.Mobilize(p, 0).IsValid, Is.False);
            Assert.That(PopulationRules.Demobilize(p, 4_000).IsValid, Is.True);
            Assert.That(p.Mobilized, Is.EqualTo(6_000));
            Assert.That(PopulationRules.Demobilize(p, 6_001).IsValid, Is.False);
            Assert.That(PopulationRules.SetEmployed(p, p.LaborForce(Baseline.LaborParticipation) + 1, Baseline).IsValid, Is.False);
        }

        [Test]
        public void MilitaryCasualties_RemoveExactCount_FromWorkingBands()
        {
            RegionPopulation p = Region(BaselineContent.AgePyramid(100_000));
            PopulationRules.Mobilize(p, 20_000);
            long[] before = new long[AgeBands.Count];
            for (int b = 0; b < AgeBands.Count; b++) before[b] = p.Cohort(b);

            Assert.That(PopulationRules.ApplyMilitaryCasualties(p, 1_234).IsValid, Is.True);
            Assert.That(p.Total, Is.EqualTo(100_000 - 1_234));
            Assert.That(p.Mobilized, Is.EqualTo(20_000 - 1_234));
            Assert.That(p.Total, Is.EqualTo(p.SumCohorts()));
            long removed = 0;
            for (int b = 0; b < AgeBands.Count; b++)
            {
                long delta = before[b] - p.Cohort(b);
                Assert.That(delta == 0 || AgeBands.IsWorkingAge(b), Is.True, $"bant {b}");
                removed += delta;
            }

            Assert.That(removed, Is.EqualTo(1_234));
            Assert.That(PopulationRules.ApplyMilitaryCasualties(p, 1_000_000).IsValid, Is.False);
        }

        [Test]
        public void Pyramid_SumsExactly_AndStateRejectsBadInput()
        {
            foreach (long total in new long[] { 0, 1, 999, 1_000, 123_457, 50_000_001 })
            {
                long sum = 0;
                foreach (long c in BaselineContent.AgePyramid(total)) sum += c;
                Assert.That(sum, Is.EqualTo(total));
            }

            var state = new PopulationState();
            Assert.That(state.AddRegion(new RegionId(1), new long[3]).ErrorKey, Is.EqualTo(PopulationErrorKeys.KohortSayisi));
            var negative = new long[AgeBands.Count];
            negative[4] = -1;
            Assert.That(state.AddRegion(new RegionId(1), negative).ErrorKey, Is.EqualTo(PopulationErrorKeys.NegatifNufus));
            Assert.That(state.AddRegion(new RegionId(1), new long[AgeBands.Count]).IsSuccess, Is.True);
            Assert.That(state.AddRegion(new RegionId(1), new long[AgeBands.Count]).ErrorKey, Is.EqualTo(PopulationErrorKeys.BolgeZatenVar));
            Assert.Throws<ArgumentException>(() => new DemographyParameters(new Fixed64[17], new Fixed64[16], Fixed64.Half, Fixed64.Zero));
            var fertileChild = new Fixed64[17];
            fertileChild[1] = Fixed64.Parse("0.1");
            Assert.Throws<ArgumentException>(() => new DemographyParameters(fertileChild, new Fixed64[17], Fixed64.Half, Fixed64.Zero));
        }

        [Test]
        public void Snapshot_RoundTripsCarries()
        {
            var state = new PopulationState();
            RegionPopulation p = state.AddRegion(new RegionId(4), BaselineContent.AgePyramid(77_777)).Value;
            PopulationRules.Mobilize(p, 500);
            for (int d = 0; d < 13; d++) DemographyRules.AdvanceDay(p, Baseline, Fixed64.One);
            var w = new CanonicalWriter();
            PopulationSnapshotCodec.Write(state, w);
            Result<PopulationState> back = PopulationSnapshotCodec.Read(new CanonicalReader(w.ToArray()));
            Assert.That(back.IsSuccess, Is.True);
            var w2 = new CanonicalWriter();
            PopulationSnapshotCodec.Write(back.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));

            RegionPopulation q = back.Value.Regions[new RegionId(4)];
            DemographyRules.AdvanceDay(p, Baseline, Fixed64.One);
            DemographyRules.AdvanceDay(q, Baseline, Fixed64.One);
            Assert.That(q.Total, Is.EqualTo(p.Total), "carry geri yüklenmezse ertesi gün farklılaşır");
        }
    }
}
