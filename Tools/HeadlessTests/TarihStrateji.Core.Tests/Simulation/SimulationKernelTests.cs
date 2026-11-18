using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Commands;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Tests.Support;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.Simulation
{
    [TestFixture]
    public class SimulationKernelTests
    {
        private static readonly HexCoord WestHex = new HexCoord(3, 4); // A bölgesi, şehirsiz

        [SetUp]
        public void ResetCounters()
        {
            SetHexControllerHandler.Applied = 0;
            TacticalStrikeHandler.Applied = 0;
            TacticalStrikeHandler.Resolutions.Clear();
        }

        [Test]
        public void Phases_RunInFixedOrder_AndDateAdvancesAtP0()
        {
            var log = new List<string>();
            var setup = new SimulationSetup();
            for (int p = 15; p >= 0; p--)
            {
                setup.AddSystem(new PhaseRecorder((SimulationPhase)p, log)); // ters kayıt: sıra fazdan gelir
            }

            SimulationKernel kernel = TestSimulation.Kernel(setup, TestSimulation.NewState());
            TestSimulation.AdvanceOk(kernel, 1);
            string day = TestSimulation.Start.AddDays(1).ToString();
            Assert.That(log, Is.EqualTo(Enumerable.Range(0, 16).Select(p => p + "@" + day).ToList()));
            Assert.That(kernel.CurrentDate, Is.EqualTo(TestSimulation.Start.AddDays(1)));
        }

        [Test]
        public void PendingCommand_IsAppliedOnceAtNextP1()
        {
            SimulationKernel kernel = TestSimulation.Kernel(TestSimulation.Setup(), TestSimulation.NewState());
            CommandReceipt receipt = kernel.Submit(new SetHexControllerCommand(TestWorlds.CountryA, WestHex, TestWorlds.CountryB));
            Assert.That(receipt.Accepted, Is.True, receipt.Validation.ErrorKey);
            Assert.That(receipt.CommandId.Value, Is.EqualTo(1UL));
            Assert.That(kernel.State.World.Cell(WestHex).Controller, Is.EqualTo(TestWorlds.CountryA), "Submit durumu değiştirmez");
            Assert.That(kernel.State.Journal.Pending.Count, Is.EqualTo(1));

            Result<DayReport> day1 = kernel.AdvanceDay();
            Assert.That(day1.Value.AppliedCommands, Is.EqualTo(1));
            Assert.That(kernel.State.World.Cell(WestHex).Controller, Is.EqualTo(TestWorlds.CountryB));
            Assert.That(kernel.State.Journal.Pending.Count, Is.EqualTo(0));

            TestSimulation.AdvanceOk(kernel, 3);
            Assert.That(SetHexControllerHandler.Applied, Is.EqualTo(1));
        }

        [Test]
        public void CommandInvalidAtApplyTime_IsRejectedNotApplied()
        {
            SimulationKernel kernel = TestSimulation.Kernel(TestSimulation.Setup(), TestSimulation.NewState());
            Assert.That(kernel.Submit(new SetHexControllerCommand(TestWorlds.CountryA, WestHex, TestWorlds.CountryB)).Accepted, Is.True);
            Assert.That(kernel.Submit(new SetHexControllerCommand(TestWorlds.CountryA, WestHex, TestWorlds.CountryB)).Accepted, Is.True, "ikisi de gönderildiği an geçerli");

            DayReport report = kernel.AdvanceDay().Value;
            Assert.That(report.AppliedCommands, Is.EqualTo(1));
            Assert.That(report.RejectedCommands, Is.EqualTo(new[] { "2: hata.test.zatenOyle" }));
        }

        [Test]
        public void ImmediateCommand_ResolvesAtSubmit_AndIsNeverReapplied()
        {
            var probe = new LedgerProbe();
            SimulationKernel kernel = TestSimulation.Kernel(TestSimulation.Setup(probe), TestSimulation.NewState());
            City inland = kernel.State.World.Cities.ValueAt(1); // B'nin şehri
            SectorId target = inland.Sectors[1];

            CommandReceipt receipt = kernel.Submit(new TacticalStrikeCommand(TestWorlds.CountryA, target));
            Assert.That(receipt.Accepted, Is.True, receipt.Validation.ErrorKey);
            Assert.That(TacticalStrikeHandler.Applied, Is.EqualTo(1), "anında uygulandı");
            Assert.That(TacticalStrikeHandler.Resolutions[0].IsValid, Is.True);
            Assert.That(kernel.State.Ledger.Contains(TacticalStrikeHandler.Resolutions[0]), Is.True);
            Assert.That(kernel.State.Journal.Pending.Count, Is.EqualTo(0), "çözülmüş emir P1 kuyruğuna girmez");

            TestSimulation.AdvanceOk(kernel, 2);
            Assert.That(TacticalStrikeHandler.Applied, Is.EqualTo(1));
            Assert.That(probe.SeenCounts, Is.EqualTo(new[] { 1, 0 }), "P13 defteri görür, P15 sonunda boşalır");
        }

        [Test]
        public void ModeRules_AreEnforcedByTheGateway()
        {
            var sandboxCmd = new SandboxSetInfrastructureCommand(CountryId.None, WestHex, new HexInfrastructure(2, 1, false));

            SimulationKernel normal = TestSimulation.Kernel(TestSimulation.Setup(), TestSimulation.NewState());
            Assert.That(normal.Submit(sandboxCmd).Validation.ErrorKey, Is.EqualTo(DomainErrorKeys.HileNormalModda));
            Assert.That(normal.Validate(new SetHexControllerCommand(TestWorlds.CountryB, WestHex, TestWorlds.CountryC)).ErrorKey, Is.EqualTo(SimulationErrorKeys.YabanciUlkeEmri));

            SimulationKernel sandbox = TestSimulation.Kernel(TestSimulation.Setup(), TestSimulation.NewState(mode: GameMode.Sandbox));
            Assert.That(sandbox.Submit(sandboxCmd).Accepted, Is.True);
            TestSimulation.AdvanceOk(sandbox, 1);
            Assert.That(sandbox.State.World.Cell(WestHex).Infrastructure.RoadLevel, Is.EqualTo(2));
            Assert.That(sandbox.Validate(new SandboxSetInfrastructureCommand(CountryId.None, new HexCoord(0, 0), new HexInfrastructure(1, 0, false))).ErrorKey,
                Is.EqualTo(WorldErrorKeys.AltyapiSudaOlamaz));

            SimulationKernel observer = TestSimulation.Kernel(TestSimulation.Setup(), TestSimulation.NewState(mode: GameMode.Observer));
            Assert.That(observer.Submit(sandboxCmd).Validation.ErrorKey, Is.EqualTo(DomainErrorKeys.GozlemciEmirVeremez));
            Assert.That(observer.PlayerCountry.IsValid, Is.False);
            TestSimulation.AdvanceOk(observer, 2);
        }

        private sealed class UnknownCommand : GameCommand
        {
            public UnknownCommand() : base(TestWorlds.CountryA) { }

            public override CommandAuthority Authority => CommandAuthority.Player;

            public override string Kind => "emir.yok";
        }

        [Test]
        public void UnknownKind_IsRejected()
        {
            SimulationKernel kernel = TestSimulation.Kernel(TestSimulation.Setup(), TestSimulation.NewState());
            Assert.That(kernel.Submit(new UnknownCommand()).Validation.ErrorKey, Is.EqualTo(SimulationErrorKeys.BilinmeyenEmirTuru));
            Assert.That(kernel.Validate(null).ErrorKey, Is.EqualTo(DomainErrorKeys.EmirBos));
        }

        [Test]
        public void SystemException_HaltsSimulation()
        {
            SimulationKernel kernel = TestSimulation.Kernel(TestSimulation.Setup(new ExplodingSystem()), TestSimulation.NewState());
            DayReport report = kernel.AdvanceDay().Value;
            Assert.That(report.Halted, Is.True);
            Assert.That(report.Violations[0], Does.StartWith(SimulationErrorKeys.SistemHatasi + ": P8"));
            Assert.That(kernel.IsHalted, Is.True);
            Assert.That(kernel.AdvanceDay().ErrorKey, Is.EqualTo(SimulationErrorKeys.SimulasyonDurdu));
            Assert.That(kernel.Submit(new SetHexControllerCommand(TestWorlds.CountryA, WestHex, TestWorlds.CountryB)).Validation.ErrorKey,
                Is.EqualTo(SimulationErrorKeys.SimulasyonDurdu));
        }

        private sealed class CorruptingSystem : ISimulationSystem
        {
            public string Name => "test.bozucu";

            public SimulationPhase Phase => SimulationPhase.P14ControlAndIntelligence;

            public void Execute(SimulationContext context)
            {
                City city = context.World.Cities.ValueAt(0);
                context.World.Cell(city.Hex).Controller = TestWorlds.CountryC; // merkez sektörle tutarsız
            }
        }

        private sealed class AlwaysFailingInvariant : IInvariantCheck
        {
            public string Name => "test.hep";

            public int Check(GameState state, List<string> violations)
            {
                violations.Add("hata.test.degismez");
                return 1;
            }
        }

        [Test]
        public void InvariantViolation_HaltsBeforeHashAndAutosave()
        {
            SimulationKernel corrupt = TestSimulation.Kernel(TestSimulation.Setup(new CorruptingSystem()), TestSimulation.NewState());
            DayReport report = corrupt.AdvanceDay().Value;
            Assert.That(report.Halted, Is.True);
            Assert.That(report.Violations, Has.Some.StartsWith(WorldErrorKeys.KontrolTutarsiz));
            Assert.That(report.StateHash, Is.Null);

            SimulationSetup setup = TestSimulation.Setup();
            setup.AddInvariant(new AlwaysFailingInvariant());
            SimulationKernel custom = TestSimulation.Kernel(setup, TestSimulation.NewState());
            Assert.That(custom.AdvanceDay().Value.Violations, Is.EqualTo(new[] { "hata.test.degismez" }));
        }

        [Test]
        public void Effects_ExpireAtP2()
        {
            SimulationKernel kernel = TestSimulation.Kernel(TestSimulation.Setup(), TestSimulation.NewState());
            EffectEngine effects = kernel.State.Effects;
            effects.Add(EffectTarget.Of(TestWorlds.CountryA), "test", EffectOperation.Add, Fixed64.One, kernel.CurrentDate, 2, null);
            Assert.That(kernel.AdvanceDay().Value.ExpiredEffects, Is.EqualTo(0));
            Assert.That(kernel.AdvanceDay().Value.ExpiredEffects, Is.EqualTo(1));
            Assert.That(effects.Count, Is.EqualTo(0));
        }

        [Test]
        public void StateChanged_FiresWithIncreasingVersions()
        {
            SimulationKernel kernel = TestSimulation.Kernel(TestSimulation.Setup(), TestSimulation.NewState());
            var versions = new List<long>();
            kernel.StateChanged += versions.Add;
            kernel.Submit(new SetHexControllerCommand(TestWorlds.CountryA, WestHex, TestWorlds.CountryB));
            kernel.Submit(new SetHexControllerCommand(TestWorlds.CountryB, WestHex, TestWorlds.CountryB)); // reddedilir: olay yok
            TestSimulation.AdvanceOk(kernel, 2);
            Assert.That(versions, Is.EqualTo(new long[] { 1, 2, 3 }));
            Assert.That(kernel.StateVersion, Is.EqualTo(3));
        }

        [Test]
        public void Create_RejectsIncompleteSetup()
        {
            var setup = TestSimulation.Setup();
            setup.AutosaveIntervalDays = 5; // hedef yok
            Assert.That(SimulationKernel.Create(setup, TestSimulation.NewState()).ErrorKey, Is.EqualTo(SimulationErrorKeys.KurulumEksik));
            Assert.That(SimulationKernel.Create(null, TestSimulation.NewState()).IsSuccess, Is.False);
            Assert.Throws<System.ArgumentException>(() => setup.Commands.Register(new SetHexControllerHandler()));
            Assert.Throws<System.ArgumentException>(() => GameState.CreateNew(1, GameMode.Normal, TestSimulation.Start, CountryId.None, TestWorlds.Coastal(out _, out _)));
        }
    }
}
