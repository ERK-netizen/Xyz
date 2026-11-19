using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Commands.Military;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Tests.Military
{
    /// <summary>Kara harbinin <see cref="SimulationKernel"/> üzerinden uçtan uca davranışı.</summary>
    [TestFixture]
    public class WarKernelTests
    {
        private static SimulationSetup Setup() => StandardRules.Install(new SimulationSetup { HashIntervalDays = 1 }, FrontScenario.Content);

        private static (SimulationKernel Kernel, FrontScenario Scenario) NewKernel(ulong seed = 1921)
        {
            var s = new FrontScenario(seed: seed);
            return (TestSimulation.Kernel(Setup(), s.State), s);
        }

        [Test]
        public void ImmediateAttack_SpendsActionPoints_AndIsNotResolvedAgainInP13()
        {
            (SimulationKernel k, FrontScenario s) = NewKernel();
            LandUnit attacker = s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 109, 4);
            LandUnit defender = s.Place(TestWorlds.CountryB, BaselineUnits.InfantryDivision, 110, 4);
            TestSimulation.AdvanceOk(k, 1); // P0 aksiyon puanlarını doldurur
            Assert.That(attacker.ActionPoints, Is.EqualTo(Fixed64.FromInt(2)));

            CommandReceipt r = k.Submit(new ImmediateAttackCommand(TestWorlds.CountryA, attacker.Id, new HexCoord(110, 4)));
            Assert.That(r.Accepted, Is.True, r.Validation.ErrorKey);
            Assert.That(attacker.ActionPoints, Is.EqualTo(Fixed64.One));
            Assert.That(attacker.LastResolution, Is.Not.EqualTo(ResolutionId.None));
            Assert.That(k.State.Ledger.Contains(attacker.LastResolution), Is.True);
            Assert.That(k.State.Journal.Pending.Count, Is.EqualTo(0), "anında çözülen emir P1'e girmez");

            k.Submit(new OrderAttackCommand(TestWorlds.CountryA, attacker.Id, new HexCoord(110, 4)));
            var orgAfterImmediate = defender.Organization;
            TestSimulation.AdvanceOk(k, 1);
            Assert.That(defender.Organization >= orgAfterImmediate, Is.True, "aynı gün P13'te ikinci kez çarpışmaz (yalnız toparlanır)");
            Assert.That(k.State.Ledger.Count, Is.EqualTo(0), "defter P15 sonunda boşalır");

            k.Submit(new ImmediateAttackCommand(TestWorlds.CountryA, attacker.Id, new HexCoord(110, 4)));
            k.Submit(new ImmediateAttackCommand(TestWorlds.CountryA, attacker.Id, new HexCoord(110, 4)));
            CommandReceipt third = k.Submit(new ImmediateAttackCommand(TestWorlds.CountryA, attacker.Id, new HexCoord(110, 4)));
            Assert.That(third.Accepted, Is.False, "AP bitti");
        }

        [Test]
        public void ForeignAndInvalidOrders_AreRejected()
        {
            (SimulationKernel k, FrontScenario s) = NewKernel();
            LandUnit enemy = s.Place(TestWorlds.CountryB, BaselineUnits.InfantryDivision, 112, 4);
            Assert.That(k.Submit(new MoveUnitCommand(TestWorlds.CountryB, enemy.Id, new HexCoord(113, 4))).Accepted, Is.False, "oyuncu ülkesi değil");
            Assert.That(k.Submit(new RecruitUnitCommand(TestWorlds.CountryA, BaselineUnits.InfantryDivision, new HexCoord(104, 4))).Validation.ErrorKey, Is.EqualTo(WarfareErrorKeys.ToplamaYeriGecersiz));
            Assert.That(k.Submit(new ImmediateAttackCommand(TestWorlds.CountryA, enemy.Id, new HexCoord(111, 4))).Accepted, Is.False, "başkasının birliği");
        }

        private static List<string> Run(SimulationKernel k, FrontScenario s, int days)
        {
            var hashes = new List<string>();
            var ids = new List<LandUnitId>();
            for (int d = 0; d < days; d++)
            {
                if (d == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        k.Submit(new RecruitUnitCommand(TestWorlds.CountryA, BaselineUnits.InfantryDivision, new HexCoord(102, 4)));
                    }
                }

                if (d == 2)
                {
                    for (int i = 0; i < k.State.Warfare.Units.Count; i++)
                    {
                        LandUnit u = k.State.Warfare.Units.ValueAt(i);
                        if (u.Owner == TestWorlds.CountryA) ids.Add(u.Id);
                    }

                    s.Place(TestWorlds.CountryB, BaselineUnits.InfantryDivision, 111, 4);
                    foreach (LandUnitId id in ids) k.Submit(new MoveUnitCommand(TestWorlds.CountryA, id, new HexCoord(110, 4)));
                }

                if (d == 20)
                {
                    foreach (LandUnitId id in ids)
                    {
                        if (k.State.Warfare.Units.ContainsKey(id)) k.Submit(new OrderAttackCommand(TestWorlds.CountryA, id, new HexCoord(111, 4)));
                    }
                }

                Result<DayReport> r = k.AdvanceDay();
                Assert.That(r.IsSuccess && !r.Value.Halted, Is.True, r.IsSuccess ? string.Join("\n", r.Value.Violations) : r.ErrorKey);
                hashes.Add(Sha256.ToHex(r.Value.StateHash));
            }

            return hashes;
        }

        [Test]
        public void WarCampaign_SameSeed_SameHashes_AndSaveLoadContinuesIdentically()
        {
            (SimulationKernel a, FrontScenario sa) = NewKernel();
            List<string> expected = Run(a, sa, 60);
            (SimulationKernel b, FrontScenario sb) = NewKernel();
            Assert.That(Run(b, sb, 60), Is.EqualTo(expected));
            Assert.That(sb.Cell(111, 4).Controller, Is.EqualTo(TestWorlds.CountryA), "üç tümen tek savunanı kırıp hücreyi işgal eder");
            Assert.That(sb.Cell(111, 4).Owner, Is.EqualTo(TestWorlds.CountryB));
            Assert.That(sb.Cell(110, 4).Controller, Is.EqualTo(TestWorlds.CountryA), "yürüyüşte boş düşman hücresi işgal edildi");

            (SimulationKernel c, FrontScenario sc) = NewKernel();
            Run(c, sc, 30);
            Result<SimulationKernel> restored = SimulationKernel.Restore(Setup(), c.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            var resumed = new List<string>();
            for (int d = 30; d < 60; d++) resumed.Add(Sha256.ToHex(restored.Value.AdvanceDay().Value.StateHash));
            Assert.That(resumed, Is.EqualTo(expected.GetRange(30, 30)));
        }

        [Test]
        public void GoldenHash_WarCampaign_Day60()
        {
            (SimulationKernel k, FrontScenario s) = NewKernel();
            List<string> hashes = Run(k, s, 60);
            Assert.That(hashes[59], Is.EqualTo(Support.GoldenValues.WarDay60), "yeni hash: " + hashes[59]);
        }
    }
}
