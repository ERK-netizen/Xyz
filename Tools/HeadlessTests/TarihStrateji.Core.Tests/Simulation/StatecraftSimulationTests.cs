using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Characters;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Commands.Statecraft;
using TarihStrateji.Core.Simulation.Scenario;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Tests.Economy;
using TarihStrateji.Core.Tests.World;

namespace TarihStrateji.Core.Tests.Simulation
{
    /// <summary>Araştırma + siyaset + karakterler, ekonomiyle birlikte çekirdekte (P4, P5, P15).</summary>
    [TestFixture]
    public class StatecraftSimulationTests
    {
        private static readonly StandardContent Standard = EconomyScenario.Standard;
        private static readonly CountryId Tr = BaselineStatecraft.Turkey;
        private static readonly CountryId Gr = BaselineStatecraft.Greece;

        private static SimulationSetup Setup() => StandardRules.Install(new SimulationSetup(), Standard);

        private static SimulationKernel NewKernel(CountryId player, ulong seed = 1920)
        {
            GameState state = GameState.CreateNew(seed, GameMode.Normal, TestSimulation.Start, player, TestWorlds.Coastal(out _, out _));
            _ = new EconomyScenario(state.World, state.Population, state.Economy);
            Assert.That(TestWorlds.CountryA, Is.EqualTo(Tr));
            Assert.That(TestWorlds.CountryB, Is.EqualTo(Gr));
            state.Research.AddCountry(Tr, Standard.Core.Technology, new[] { BaselineTechnologies.ModernBureaucracy }, state.Effects, state.Today);
            state.Research.AddCountry(Gr, Standard.Core.Technology, new[] { BaselineTechnologies.MachineGun, BaselineTechnologies.AssemblyLine }, state.Effects, state.Today);
            state.Politics.AddCountry(Tr, Standard.Politics, BaselineStatecraft.Republic, BaselineStatecraft.RepublicanPeoples,
                new[] { Pair(BaselineStatecraft.RepublicanPeoples, "7"), Pair(BaselineStatecraft.Progressive, "3") }, Fixed64.Parse("0.45"), state.Today);
            state.Politics.AddCountry(Gr, Standard.Politics, BaselineStatecraft.ConstitutionalMonarchy, BaselineStatecraft.GreekLiberal,
                new[] { Pair(BaselineStatecraft.GreekLiberal, "5"), Pair(BaselineStatecraft.GreekPeoples, "5") }, Fixed64.Parse("0.5"), state.Today);
            state.Characters.AddCountry(Tr);
            state.Characters.AddCountry(Gr);
            return TestSimulation.Kernel(Setup(), state);
        }

        private static KeyValuePair<PartyId, Fixed64> Pair(PartyId p, string w) => new KeyValuePair<PartyId, Fixed64>(p, Fixed64.Parse(w));

        [Test]
        public void ScenarioStartsInConsistentEras()
        {
            SimulationKernel k = NewKernel(Tr);
            Assert.That(k.State.Research.Countries[Tr].Era, Is.EqualTo(0));
            Assert.That(k.State.Research.Countries[Gr].Era, Is.EqualTo(1), "montaj hattı + makineli tüfek, 1919 sonrası");
            TestSimulation.AdvanceOk(k, 1);
        }

        [Test]
        public void Commands_ResearchAppointInvest_FlowThroughTheKernel()
        {
            SimulationKernel k = NewKernel(Tr);
            Assert.That(k.Submit(new SetResearchCommand(Tr, BaselineTechnologies.MachineGun)).Accepted, Is.True);
            Assert.That(k.Submit(new EnqueueResearchCommand(Tr, BaselineTechnologies.TrenchDoctrine)).Accepted, Is.True);
            Assert.That(k.Submit(new AppointCharacterCommand(Tr, PositionKind.HeadOfGovernment, BaselineStatecraft.MustafaKemal)).Accepted, Is.True);
            Assert.That(k.Submit(new AppointCharacterCommand(Tr, PositionKind.ChiefOfGeneralStaff, BaselineStatecraft.FevziCakmak)).Accepted, Is.True);
            Assert.That(k.Submit(new InvestInInstitutionCommand(Tr, BaselineStatecraft.Parliament, Fixed64.FromInt(3000))).Accepted, Is.True);

            Assert.That(k.Validate(new AppointCharacterCommand(Tr, PositionKind.HeadOfGovernment, BaselineStatecraft.EleftheriosVenizelos)).ErrorKey, Is.EqualTo(CharacterErrorKeys.BaskaUlkenin));
            Assert.That(k.Validate(new AppointCharacterCommand(Gr, PositionKind.HeadOfGovernment, BaselineStatecraft.EleftheriosVenizelos)).ErrorKey, Is.EqualTo(SimulationErrorKeys.YabanciUlkeEmri));
            Assert.That(k.Validate(new InvestInInstitutionCommand(Tr, BaselineStatecraft.Army, Fixed64.FromInt(1_000_000))).ErrorKey, Is.EqualTo(TarihStrateji.Core.Economy.EconomyErrorKeys.HazineYetersiz));

            CountryEconomy tr = k.State.Economy.Countries[Tr];
            for (int d = 0; d < 50; d++)
            {
                Fixed64 before = tr.Treasury;
                TestSimulation.AdvanceOk(k, 1);
                Assert.That(tr.Treasury - before, Is.EqualTo(tr.Ledger.Net), $"gün {d}: kurum yatırımı deftere yazılmalı");
                if (d == 0)
                {
                    Assert.That(tr.Ledger.OtherSpending, Is.EqualTo(Fixed64.FromInt(3000)));
                    Assert.That(k.State.Politics.Countries[Tr].InstitutionStrength(BaselineStatecraft.Parliament), Is.EqualTo(Fixed64.Parse("0.7") - Fixed64.Parse("0.0005")));
                }
            }

            Assert.That(k.State.Research.Countries[Tr].Has(BaselineTechnologies.MachineGun), Is.True, "P1'de seçildi, 50. gün P5'te tamamlandı");
            Assert.That(k.State.Research.Countries[Tr].Current, Is.EqualTo(BaselineTechnologies.TrenchDoctrine));
            Assert.That(k.State.Characters.Countries[Tr].Holder(PositionKind.HeadOfGovernment), Is.EqualTo(BaselineStatecraft.MustafaKemal));
        }

        [Test]
        public void Leader_RaisesStability_OverTime()
        {
            SimulationKernel withLeader = NewKernel(Tr);
            SimulationKernel without = NewKernel(Tr);
            withLeader.Submit(new AppointCharacterCommand(Tr, PositionKind.HeadOfGovernment, BaselineStatecraft.MustafaKemal));
            TestSimulation.AdvanceOk(withLeader, 200);
            TestSimulation.AdvanceOk(without, 200);
            Fixed64 a = withLeader.State.Politics.Countries[Tr].Stability;
            Fixed64 b = without.State.Politics.Countries[Tr].Stability;
            Assert.That(a - b, Is.GreaterThan(Fixed64.Parse("0.05")), $"liderli {a}, lidersiz {b}");
        }

        private static List<string> Run(SimulationKernel k, int days)
        {
            var hashes = new List<string>();
            for (int d = 0; d < days; d++)
            {
                if (d == 0)
                {
                    k.Submit(new SetResearchCommand(Tr, BaselineTechnologies.AssemblyLine));
                    k.Submit(new AppointCharacterCommand(Tr, PositionKind.HeadOfGovernment, BaselineStatecraft.MustafaKemal));
                }

                if (d == 40)
                {
                    k.Submit(new InvestInInstitutionCommand(Tr, BaselineStatecraft.Army, Fixed64.FromInt(2000)));
                    k.Submit(new EnqueueResearchCommand(Tr, BaselineTechnologies.MachineGun));
                }

                Result<DayReport> r = k.AdvanceDay();
                Assert.That(r.IsSuccess && !r.Value.Halted, Is.True, r.IsSuccess ? string.Join("\n", r.Value.Violations) : r.ErrorKey);
                hashes.Add(Sha256.ToHex(r.Value.StateHash));
            }

            return hashes;
        }

        [Test]
        public void SaveMidRun_ContinuesIdentically()
        {
            List<string> expected = Run(NewKernel(Tr), 200);
            SimulationKernel first = NewKernel(Tr);
            Run(first, 90);
            Result<SimulationKernel> restored = SimulationKernel.Restore(Setup(), first.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            var resumed = new List<string>();
            for (int d = 90; d < 200; d++)
            {
                resumed.Add(Sha256.ToHex(restored.Value.AdvanceDay().Value.StateHash));
            }

            Assert.That(resumed, Is.EqualTo(expected.GetRange(90, 110)));
        }

        [Test]
        public void GoldenHash_Statecraft_Day200()
        {
            List<string> hashes = Run(NewKernel(Tr), 200);
            Assert.That(hashes[199], Is.EqualTo(Support.GoldenValues.StatecraftDay200), "yeni hash: " + hashes[199]);
        }
    }
}
