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
using TarihStrateji.Core.Tests.Military;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Tests.Campaign
{
    [TestFixture]
    public class AiDirectorTests
    {
        private static readonly CountryId A = TestWorlds.CountryA;
        private static readonly CountryId B = TestWorlds.CountryB;
        private static readonly AiParameters Prm = FrontScenario.Content.Ai;

        /// <summary>Kural sistemleriyle (YZ'siz) bir gün ilerletir: istihbarat (P14) dolar.</summary>
        private static void Observe(FrontScenario s)
        {
            SimulationKernel k = TestSimulation.Kernel(StandardRules.Install(new SimulationSetup(), FrontScenario.Content), s.State);
            TestSimulation.AdvanceOk(k, 1);
        }

        private static List<AiIntent> Decide(FrontScenario s, CountryId country, AiParameters prm = null)
        {
            var borders = new List<ulong>();
            BorderIndex.Compute(s.State.World, borders);
            AiPerception p = AiPerceptionBuilder.Build(s.State, FrontScenario.Content, country, borders, new bool[s.State.World.CellCount]);
            var intents = new List<AiIntent>();
            AiDirector.Decide(p, prm ?? Prm, intents);
            return intents;
        }

        private static string Describe(List<AiIntent> intents)
        {
            var parts = new List<string>();
            foreach (AiIntent i in intents)
            {
                parts.Add(i.Kind + ":" + i.Id + ":" + i.Country.Value + ":" + i.Target.Column + "," + i.Target.Row + ":" + i.Secondary);
            }

            return string.Join(" ", parts);
        }

        private static FrontScenario AtPeace()
        {
            var s = new FrontScenario();
            DiplomacyRules.ConcludePeace(s.State.Diplomacy, s.State.World, new Core.Diplomacy.Definitions.DiplomacyParameters(30, 1825, 3650, 0, Fixed64.Half, Fixed64.Parse("0.25")),
                A, B, new RegionTransfer[0], s.State.Today);
            return s;
        }

        [Test]
        public void Ai_ReadsOnlyItsObservedWorld_UnseenEnemiesChangeNothing()
        {
            List<AiIntent> Run(bool hiddenArmy)
            {
                var s = new FrontScenario();
                s.Place(A, BaselineUnits.InfantryDivision, 108, 4);
                s.Place(B, BaselineUnits.InfantryDivision, 110, 4);
                if (hiddenArmy)
                {
                    // A'nın sensörlerinin çok ötesinde, B'nin kendi topraklarında büyük bir ordu.
                    for (int i = 0; i < 5; i++)
                    {
                        s.Place(B, BaselineUnits.InfantryDivision, 118, 7);
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
            var weak = new FrontScenario();
            LandUnit a1 = weak.Place(A, BaselineUnits.InfantryDivision, 109, 4);
            weak.Place(A, BaselineUnits.InfantryDivision, 109, 4);
            LandUnit b1 = weak.Place(B, BaselineUnits.InfantryDivision, 110, 4);
            b1.Manpower = 3000;
            Observe(weak);
            List<AiIntent> intents = Decide(weak, A);
            Assert.That(intents.Exists(i => i.Kind == AiIntentKind.Attack && i.Id == a1.Id.Value && i.Target == new HexCoord(110, 4)), Is.True, Describe(intents));

            var strong = new FrontScenario();
            strong.Place(A, BaselineUnits.InfantryDivision, 109, 4);
            for (int i = 0; i < 3; i++)
            {
                strong.Place(B, BaselineUnits.InfantryDivision, 110, 4);
            }

            Observe(strong);
            Assert.That(Decide(strong, A).Exists(i => i.Kind == AiIntentKind.Attack), Is.False, "1'e 3 saldırılmaz");
        }

        [Test]
        public void Ai_DeclaresWarOnlyWithClearSuperiority_AndNeverWithAggressionZero()
        {
            FrontScenario s = AtPeace();
            for (int i = 0; i < 6; i++)
            {
                s.Place(A, BaselineUnits.InfantryDivision, 104, 4);
            }

            List<AiIntent> Day(AiParameters prm)
            {
                // Karar günü: (gün + ülke) mod aralık == 0 olana kadar ilerlet (YZ'siz kurallar).
                SimulationKernel k = TestSimulation.Kernel(StandardRules.Install(new SimulationSetup(), FrontScenario.Content), s.State);
                do
                {
                    TestSimulation.AdvanceOk(k, 1);
                }
                while ((s.State.Today.DayNumber + (long)A.Value) % prm.DecisionIntervalDays != 0);

                return Decide(s, A, prm);
            }

            List<AiIntent> aggressive = Day(Prm);
            Assert.That(aggressive.Exists(i => i.Kind == AiIntentKind.DeclareWar && i.Country == B), Is.True, Describe(aggressive));
            Assert.That(aggressive.Exists(i => i.Kind == AiIntentKind.DeclareWar && i.Country == TestWorlds.CountryC), Is.False, "tarafsız C komşu değil (sınır yok)");

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
            var losing = new FrontScenario();
            losing.State.World.SetSectorController(losing.CityB.Sectors[1], A);
            foreach (SectorId sector in losing.CityB.Sectors)
            {
                losing.State.World.SetSectorController(sector, A);
            }

            DiplomacyRules.Propose(losing.State.Diplomacy, FrontScenario.Content.Diplomacy, ProposalKind.Peace, A, B, new RegionTransfer[0], losing.State.Today);
            AiPerception p = Perceive(losing, B);
            var intents = new List<AiIntent>();
            Core.AI.Layers.DiplomacyLayer.Decide(p, Prm, new Core.AI.Layers.StrategicAssessment(p, Prm), intents);
            Assert.That(intents.Exists(i => i.Kind == AiIntentKind.AcceptProposal), Is.True, "tek şehrini kaybeden B barışı kabul eder");

            var fine = new FrontScenario();
            for (int i = 0; i < 3; i++)
            {
                fine.Place(B, BaselineUnits.InfantryDivision, 114, 4);
            }

            DiplomacyRules.Propose(fine.State.Diplomacy, FrontScenario.Content.Diplomacy, ProposalKind.Peace, A, B, new[] { new RegionTransfer(fine.East, A) }, fine.State.Today);
            p = Perceive(fine, B);
            intents.Clear();
            Core.AI.Layers.DiplomacyLayer.Decide(p, Prm, new Core.AI.Layers.StrategicAssessment(p, Prm), intents);
            Assert.That(intents.Exists(i => i.Kind == AiIntentKind.RejectProposal), Is.True, "kaybetmeyen B toprak isteyen barışı reddeder");
        }

        private static AiPerception Perceive(FrontScenario s, CountryId country)
        {
            var borders = new List<ulong>();
            BorderIndex.Compute(s.State.World, borders);
            return AiPerceptionBuilder.Build(s.State, FrontScenario.Content, country, borders, new bool[s.State.World.CellCount]);
        }

        [Test]
        public void Kernel_RunsAiForOthers_ButNeverIssuesOrdersForThePlayer()
        {
            var s = new FrontScenario();
            LandUnit mine = s.Place(A, BaselineUnits.InfantryDivision, 109, 4);
            LandUnit theirs = s.Place(B, BaselineUnits.InfantryDivision, 110, 4);
            theirs.Manpower = 10_000;
            mine.Manpower = 2_000;
            SimulationSetup setup = StandardRules.InstallArtificialIntelligence(StandardRules.Install(new SimulationSetup(), FrontScenario.Content), FrontScenario.Content);
            SimulationKernel k = TestSimulation.Kernel(setup, s.State);
            for (int d = 0; d < 10; d++)
            {
                TestSimulation.AdvanceOk(k, 1);
                for (int i = 0; i < k.State.Journal.Pending.Count; i++)
                {
                    Assert.That(k.State.Journal.Pending[i].Command.Issuer, Is.Not.EqualTo(A), "oyuncu adına YZ emri yok");
                }
            }

            Assert.That(mine.AttackTarget.IsValid || mine.Path.Count > 0, Is.False, "oyuncunun birliği emirsiz kaldı");
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
