using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.AI;
using TarihStrateji.Core.AI.Definitions;
using TarihStrateji.Core.AI.Perception;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Systems;
using TarihStrateji.Core.Tests.Scenario;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Campaign
{
    [TestFixture]
    public class AiDirectorTests
    {
        private static readonly CountryId A = AegeanWar.Turkey;
        private static readonly CountryId B = AegeanWar.Greece;
        private static readonly TransportNodeId Edirne = new TransportNodeId(9);
        private static readonly TransportNodeId Patras = new TransportNodeId(12);
        private static readonly AiParameters Prm = AegeanWar.Content.Ai;

        /// <summary>Kural sistemleriyle (YZ'siz) bir gün ilerletir: istihbarat (P14) dolar.</summary>
        private static void Observe(AegeanWar s)
        {
            SimulationKernel k = TestSimulation.Kernel(AegeanWar.Setup(), s.State);
            TestSimulation.AdvanceOk(k, 1);
        }

        private static List<AiIntent> Decide(AegeanWar s, CountryId country, AiParameters prm = null)
        {
            var borders = new List<ulong>();
            BorderIndex.Compute(s.State.World, borders);
            AiPerception p = AiPerceptionBuilder.Build(s.State, AegeanWar.Content, country, borders);
            var intents = new List<AiIntent>();
            AiDirector.Decide(p, prm ?? Prm, intents);
            return intents;
        }

        private static string Describe(List<AiIntent> intents)
        {
            var parts = new List<string>();
            foreach (AiIntent i in intents)
            {
                parts.Add(i.Kind + ":" + i.Id + ":" + i.Country.Value + ":" + i.Target + ":" + i.Secondary);
            }

            return string.Join(" ", parts);
        }

        private static AegeanWar AtPeace()
        {
            return new AegeanWar(atWar: false);
        }

        [Test]
        public void Ai_ReadsOnlyItsObservedWorld_UnseenEnemiesChangeNothing()
        {
            List<AiIntent> Run(bool hiddenArmy)
            {
                var s = new AegeanWar();
                s.Place(A, "sehir.ege.edirne", Edirne);
                s.Place(B, "sehir.ege.selanik", AegeanWar.Drama);
                if (hiddenArmy)
                {
                    // A'nın sensörlerinin çok ötesinde, B'nin kendi topraklarında büyük bir ordu.
                    for (int i = 0; i < 5; i++)
                    {
                        s.Place(B, "sehir.ege.atina", Patras);
                    }
                }

                Observe(s);
                return Decide(s, A);
            }

            Assert.That(Describe(Run(true)), Is.EqualTo(Describe(Run(false))), "görülmeyen birlikler YZ kararını etkilemez");
        }

        [Test]
        public void Ai_AttacksAWeakVisibleEnemy_ButNotAStrongOne()
        {
            var weak = new AegeanWar();
            LandUnit a1 = weak.Place(A, "sehir.ege.edirne", Edirne);
            weak.Place(A, "sehir.ege.edirne", Edirne);
            LandUnit b1 = weak.Place(B, "sehir.ege.selanik", AegeanWar.Drama);
            b1.Manpower = 3000;
            Observe(weak);
            List<AiIntent> intents = Decide(weak, A);
            Assert.That(intents.Exists(i => i.Kind == AiIntentKind.Attack && i.Id == a1.Id.Value && i.Target.Equals(TargetRef.Entity(TargetKind.LandUnit, b1.Id.Value))), Is.True, Describe(intents));

            var strong = new AegeanWar();
            strong.Place(A, "sehir.ege.edirne", Edirne);
            for (int i = 0; i < 3; i++)
            {
                strong.Place(B, "sehir.ege.selanik", AegeanWar.Drama);
            }

            Observe(strong);
            Assert.That(Decide(strong, A).Exists(i => i.Kind == AiIntentKind.Attack), Is.False, "1'e 3 saldırılmaz");
        }

        [Test]
        public void Ai_DeclaresWarOnlyWithClearSuperiority_AndNeverWithAggressionZero()
        {
            AegeanWar s = AtPeace();
            for (int i = 0; i < 6; i++)
            {
                s.Place(A, "sehir.ege.edirne", Edirne);
            }

            List<AiIntent> Day(AiParameters prm)
            {
                // Karar günü: (gün + ülke) mod aralık == 0 olana kadar ilerlet (YZ'siz kurallar).
                SimulationKernel k = TestSimulation.Kernel(AegeanWar.Setup(), s.State);
                do
                {
                    TestSimulation.AdvanceOk(k, 1);
                }
                while ((s.State.Today.DayNumber + (long)A.Value) % prm.DecisionIntervalDays != 0);

                return Decide(s, A, prm);
            }

            List<AiIntent> aggressive = Day(Prm);
            Assert.That(aggressive.Exists(i => i.Kind == AiIntentKind.DeclareWar && i.Country == B), Is.True, Describe(aggressive));
            Assert.That(aggressive.Exists(i => i.Kind == AiIntentKind.DeclareWar && i.Country == new CountryId(3)), Is.False, "sınırı olmayan ülkeye savaş yok");

            var pacifist = new AiParameters(Prm.DecisionIntervalDays, Fixed64.Zero, Prm.DeclareWarPowerRatio, Prm.MinimumWarPower, Prm.DeclareWarMinStability,
                Prm.GarrisonEstimatePerCity, Prm.AttackPowerRatio, Prm.AttackMinOrganization, Prm.DefendRadius, Prm.AdvanceSearchRadius, Prm.UnitsPerAdvanceTarget,
                Prm.PeaceAfterWarDays, Prm.StalemateDays, Prm.LosingHeldShare, Prm.LosingPowerRatio, Prm.RecruitPerCity, Prm.RecruitThreatMargin, Prm.RecruitUnitType,
                Prm.MaxMobilizationShare, Prm.ConstructionReserve, Prm.MaxConcurrentConstructions, new List<FacilityTypeId>(Prm.BuildPriority).ToArray(),
                Prm.TreasuryHigh, Prm.TreasuryLow, Prm.TaxStep, Prm.MinTaxRate, Prm.MaxTaxRate);
            Assert.That(Day(pacifist).Exists(i => i.Kind == AiIntentKind.DeclareWar), Is.False, "saldırganlık 0");
        }

        [Test]
        public void Ai_AcceptsPeaceWhenLosing_AndRejectsCessionWhenNot()
        {
            var losing = new AegeanWar();
            for (int i = 0; i < losing.State.World.Cities.Count; i++)
            {
                losing.State.World.TryGetCity(losing.State.World.Cities[i], out City city);
                if (city.Owner == B)
                {
                    city.Controller = A;
                }
            }

            DiplomacyRules.Propose(losing.State.Diplomacy, AegeanWar.Content.Diplomacy, ProposalKind.Peace, A, B, new RegionTransfer[0], losing.State.Today);
            AiPerception p = Perceive(losing, B);
            var intents = new List<AiIntent>();
            Core.AI.Layers.DiplomacyLayer.Decide(p, Prm, new Core.AI.Layers.StrategicAssessment(p, Prm), intents);
            Assert.That(intents.Exists(i => i.Kind == AiIntentKind.AcceptProposal), Is.True, "şehirlerini kaybeden B barışı kabul eder");

            var fine = new AegeanWar();
            for (int i = 0; i < 3; i++)
            {
                fine.Place(B, "sehir.ege.selanik", AegeanWar.Drama);
            }

            DiplomacyRules.Propose(fine.State.Diplomacy, AegeanWar.Content.Diplomacy, ProposalKind.Peace, A, B, new[] { new RegionTransfer(AegeanWar.SelanikRegion, A) }, fine.State.Today);
            p = Perceive(fine, B);
            intents.Clear();
            Core.AI.Layers.DiplomacyLayer.Decide(p, Prm, new Core.AI.Layers.StrategicAssessment(p, Prm), intents);
            Assert.That(intents.Exists(i => i.Kind == AiIntentKind.RejectProposal), Is.True, "kaybetmeyen B toprak isteyen barışı reddeder");
        }

        private static AiPerception Perceive(AegeanWar s, CountryId country)
        {
            var borders = new List<ulong>();
            BorderIndex.Compute(s.State.World, borders);
            return AiPerceptionBuilder.Build(s.State, AegeanWar.Content, country, borders);
        }

        [Test]
        public void Kernel_RunsAiForOthers_ButNeverIssuesOrdersForThePlayer()
        {
            var s = new AegeanWar();
            LandUnit mine = s.Place(A, "sehir.ege.edirne", Edirne);
            LandUnit theirs = s.Place(B, "sehir.ege.selanik", AegeanWar.Drama);
            theirs.Manpower = 10_000;
            mine.Manpower = 2_000;
            SimulationSetup setup = AegeanWar.Setup(ai: true);
            SimulationKernel k = TestSimulation.Kernel(setup, s.State);
            for (int d = 0; d < 10; d++)
            {
                TestSimulation.AdvanceOk(k, 1);
                for (int i = 0; i < k.State.Journal.Pending.Count; i++)
                {
                    Assert.That(k.State.Journal.Pending[i].Command.Issuer, Is.Not.EqualTo(A), "oyuncu adına YZ emri yok");
                }
            }

            Assert.That(mine.AttackTarget.IsValid || mine.Route.Count > 0, Is.False, "oyuncunun birliği emirsiz kaldı");
        }

        [Test]
        public void Mobilization_IsCappedByWorkingAgeShare()
        {
            var s = AtPeace();
            AiPerception p = Perceive(s, A);
            p.Mobilized = (Fixed64.FromInt(p.WorkingAge) * Prm.MaxMobilizationShare).RoundToLong();
            var intents = new List<AiIntent>();
            Core.AI.Layers.ManagementLayer.Decide(p, Prm, new Core.AI.Layers.StrategicAssessment(p, Prm), intents);
            Assert.That(intents.Exists(i => i.Kind == AiIntentKind.Recruit), Is.False, "seferberlik tavanında toplama yok");
            p.Mobilized = 0;
            intents.Clear();
            Core.AI.Layers.ManagementLayer.Decide(p, Prm, new Core.AI.Layers.StrategicAssessment(p, Prm), intents);
            Assert.That(intents.Exists(i => i.Kind == AiIntentKind.Recruit), Is.True, "ordusuz ülke toplar");
        }
    }
}
