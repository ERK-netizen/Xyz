using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Commands.Military;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Tests.Naval
{
    /// <summary>
    /// Çekirdek üzerinden ortak harekât: nakliye filosu kur, tümen topla ve limana yürüt, gemiye
    /// bindir, boğazı geç, boş kıyıya çıkar; hava ve kıyı bombardımanı desteğiyle düşman limanını al;
    /// ana limana dönen nakliye konvoyla işgal limanını ikmal eder.
    /// </summary>
    [TestFixture]
    public class JointOperationKernelTests
    {
        private static SimulationSetup Setup() => StandardRules.Install(new SimulationSetup { HashIntervalDays = 1 }, StraitScenario.Content);

        private static (SimulationKernel Kernel, StraitScenario Scenario) NewKernel()
        {
            var s = new StraitScenario();
            return (TestSimulation.Kernel(Setup(), s.State), s);
        }

        private static void Accept(SimulationKernel k, GameCommand c)
        {
            CommandReceipt r = k.Submit(c);
            Assert.That(r.Accepted, Is.True, c.Kind + ": " + r.Validation.ErrorKey);
        }

        private static readonly LandUnitId First = new LandUnitId(1);
        private static readonly LandUnitId Second = new LandUnitId(2);
        private static readonly FleetId Transports = new FleetId(1);
        private static readonly FleetId Battleships = new FleetId(2);
        private static readonly AirWingId Bombers = new AirWingId(1);

        /// <summary>Gün <paramref name="from"/>'dan <paramref name="to"/>'ya harekât takvimi; her gün durum hash'i.</summary>
        private static List<string> Run(SimulationKernel k, int from, int to)
        {
            var hashes = new List<string>();
            for (int d = from; d < to; d++)
            {
                switch (d)
                {
                    case 0:
                        Accept(k, new BuildFleetCommand(TestWorlds.CountryA, BaselineUnits.TransportFlotilla, new HexCoord(109, 4)));
                        Accept(k, new BuildFleetCommand(TestWorlds.CountryA, BaselineUnits.BattleshipSquadron, new HexCoord(109, 4)));
                        Accept(k, new RecruitUnitCommand(TestWorlds.CountryA, BaselineUnits.InfantryDivision, new HexCoord(103, 4)));
                        Accept(k, new RecruitUnitCommand(TestWorlds.CountryA, BaselineUnits.InfantryDivision, new HexCoord(103, 4)));
                        Accept(k, new RecruitAirWingCommand(TestWorlds.CountryA, BaselineUnits.BomberWing, new HexCoord(103, 4)));
                        break;
                    case 1:
                        Accept(k, new MoveUnitCommand(TestWorlds.CountryA, First, new HexCoord(109, 4)));
                        Accept(k, new MoveUnitCommand(TestWorlds.CountryA, Second, new HexCoord(109, 4)));
                        break;
                    case 14:
                        Accept(k, new EmbarkUnitCommand(TestWorlds.CountryA, First, Transports));
                        Accept(k, new EmbarkUnitCommand(TestWorlds.CountryA, Second, Transports));
                        break;
                    case 15:
                        Accept(k, new MoveFleetCommand(TestWorlds.CountryA, Transports, new HexCoord(112, 3)));
                        Accept(k, new MoveFleetCommand(TestWorlds.CountryA, Battleships, new HexCoord(112, 4)));
                        break;
                    case 18:
                        Accept(k, new NavalLandingCommand(TestWorlds.CountryA, First, new HexCoord(113, 3)));
                        Accept(k, new NavalLandingCommand(TestWorlds.CountryA, Second, new HexCoord(113, 3)));
                        break;
                    case 19:
                        // Boşalan nakliye ana limana döner: işgal edilecek limana konvoy taşıyacak.
                        Accept(k, new MoveFleetCommand(TestWorlds.CountryA, Transports, new HexCoord(109, 4)));
                        break;
                    case 20:
                        Accept(k, new AssignAirMissionCommand(TestWorlds.CountryA, Bombers, AirMission.GroundSupport, new HexCoord(113, 4)));
                        Accept(k, new OrderAttackCommand(TestWorlds.CountryA, First, new HexCoord(113, 4)));
                        Accept(k, new OrderAttackCommand(TestWorlds.CountryA, Second, new HexCoord(113, 4)));
                        break;
                }

                Result<DayReport> r = k.AdvanceDay();
                Assert.That(r.IsSuccess && !r.Value.Halted, Is.True, r.IsSuccess ? string.Join("\n", r.Value.Violations) : r.ErrorKey);
                Assert.That(r.Value.RejectedCommands, Is.Empty, "gün " + d);
                hashes.Add(Sha256.ToHex(r.Value.StateHash));
            }

            return hashes;
        }

        [Test]
        public void AmphibiousOperation_LandsAndTakesTheEnemyPort_WithJointSupport()
        {
            (SimulationKernel k, StraitScenario s) = NewKernel();
            Run(k, 0, 19);
            Assert.That(k.State.Warfare.Units[First].IsEmbarked, Is.False, "boş kıyıya muharebesiz çıkarma");
            Assert.That(k.State.Warfare.Units[Second].Hex, Is.EqualTo(s.Hex(113, 3)));
            Assert.That(k.State.Warfare.Fleets[Battleships].Hex, Is.EqualTo(s.Hex(112, 4)));

            Run(k, 19, 20);
            Assert.That(k.State.Warfare.Units[First].Supply, Is.EqualTo(Domain.Numerics.Fixed64.Zero), "limansız kıyıda ikmal yok (P11, çıkarmanın ertesi günü)");

            Run(k, 20, 40);
            Assert.That(s.Cell(113, 4).Controller, Is.EqualTo(TestWorlds.CountryA), "düşman limanı alındı");
            Assert.That(s.Cell(113, 4).Owner, Is.EqualTo(TestWorlds.CountryB), "işgal sahipliği değiştirmez");
            Assert.That(k.State.Warfare.Units[First].Hex, Is.EqualTo(s.Hex(113, 4)));
            Assert.That(k.State.Warfare.Fleets[Transports].Hex, Is.EqualTo(s.Hex(109, 4)));
            Assert.That(k.State.Warfare.Units[First].Supply, Is.EqualTo(Domain.Numerics.Fixed64.Parse("0.7")), "işgal limanı konvoyla ikmal kaynağı");
        }

        [Test]
        public void ConstitutionalRejections_ThroughTheKernel()
        {
            (SimulationKernel k, StraitScenario s) = NewKernel();
            Accept(k, new BuildFleetCommand(TestWorlds.CountryA, BaselineUnits.DestroyerFlotilla, new HexCoord(109, 4)));
            Accept(k, new RecruitUnitCommand(TestWorlds.CountryA, BaselineUnits.InfantryDivision, new HexCoord(109, 4)));
            Accept(k, new RecruitAirWingCommand(TestWorlds.CountryA, BaselineUnits.FighterWing, new HexCoord(103, 4)));
            TestSimulation.AdvanceOk(k, 1);

            Assert.That(k.Submit(new EmbarkUnitCommand(TestWorlds.CountryA, new LandUnitId(1), new FleetId(1))).Validation.ErrorKey,
                Is.EqualTo(WarfareErrorKeys.NakliyeKapasitesiYok), "transportCapacity == 0 → çıkarma imkânsız");
            Assert.That(k.Submit(new AssignAirMissionCommand(TestWorlds.CountryA, new AirWingId(1), AirMission.AirSuperiority, new HexCoord(118, 4))).Validation.ErrorKey,
                Is.EqualTo(WarfareErrorKeys.MenzilDisi), "menzil dışı hava desteği imkânsız");
            Assert.That(k.Submit(new MoveFleetCommand(TestWorlds.CountryA, new FleetId(1), new HexCoord(105, 4))).Accepted, Is.False);
            Assert.That(k.Submit(new BuildFleetCommand(TestWorlds.CountryB, BaselineUnits.DestroyerFlotilla, new HexCoord(113, 4))).Validation.ErrorKey,
                Is.EqualTo(SimulationErrorKeys.YabanciUlkeEmri));
            Assert.That(s.State.Warfare.Fleets.Count, Is.EqualTo(1));
        }

        [Test]
        public void JointOperation_SameSeed_SameHashes_AndSaveLoadContinuesIdentically()
        {
            (SimulationKernel a, _) = NewKernel();
            List<string> expected = Run(a, 0, 40);
            (SimulationKernel b, _) = NewKernel();
            Assert.That(Run(b, 0, 40), Is.EqualTo(expected));

            (SimulationKernel c, _) = NewKernel();
            Run(c, 0, 17); // filo denizde, birlikler gemide
            Assert.That(c.State.Warfare.Units[First].IsEmbarked, Is.True);
            Result<SimulationKernel> restored = SimulationKernel.Restore(Setup(), c.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            Assert.That(Run(restored.Value, 17, 40), Is.EqualTo(expected.GetRange(17, 23)));
        }

        [Test]
        public void GoldenHash_JointOperation_Day40()
        {
            (SimulationKernel k, _) = NewKernel();
            List<string> hashes = Run(k, 0, 40);
            Assert.That(hashes[39], Is.EqualTo(Support.GoldenValues.JointOperationDay40), "yeni hash: " + hashes[39]);
        }
    }
}
