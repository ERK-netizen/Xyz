using System;
using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Commands;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.Simulation
{
    /// <summary>Çekirdek testleri için emirler, sistemler ve kurulum.</summary>
    public static class TestSimulation
    {
        public static readonly GameDate Start = GameDate.FromYmd(1920, 4, 23);

        public static SimulationSetup Setup(params ISimulationSystem[] systems)
        {
            var setup = new SimulationSetup();
            setup.Commands.Register(new SetHexControllerHandler());
            setup.Commands.Register(new TacticalStrikeHandler());
            foreach (ISimulationSystem s in systems)
            {
                setup.AddSystem(s);
            }

            return setup;
        }

        public static GameState NewState(ulong seed = 1923, GameMode mode = GameMode.Normal)
        {
            WorldMap map = TestWorlds.Coastal(out _, out _);
            return GameState.CreateNew(seed, mode, Start, mode == GameMode.Observer ? CountryId.None : TestWorlds.CountryA, map);
        }

        public static SimulationKernel Kernel(SimulationSetup setup, GameState state)
        {
            Result<SimulationKernel> k = SimulationKernel.Create(setup, state);
            Assert.That(k.IsSuccess, Is.True, k.ErrorKey);
            return k.Value;
        }

        public static void AdvanceOk(SimulationKernel kernel, int days, List<byte[]> hashes = null)
        {
            for (int i = 0; i < days; i++)
            {
                Result<DayReport> r = kernel.AdvanceDay();
                Assert.That(r.IsSuccess, Is.True, r.ErrorKey);
                Assert.That(r.Value.Halted, Is.False, string.Join("\n", r.Value.Violations));
                hashes?.Add(r.Value.StateHash);
            }
        }
    }

    /// <summary>Günlük (P1) emir: şehirsiz kara hücresinin kontrolcüsünü değiştirir.</summary>
    public sealed class SetHexControllerCommand : GameCommand
    {
        public const string KindKey = "emir.test.hucreKontrol";

        public SetHexControllerCommand(CountryId issuer, HexCoord hex, CountryId controller) : base(issuer)
        {
            Hex = hex;
            Controller = controller;
        }

        public HexCoord Hex { get; }

        public CountryId Controller { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }

    public sealed class SetHexControllerHandler : CommandHandler<SetHexControllerCommand>
    {
        public static int Applied;

        public override string Kind => SetHexControllerCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, SetHexControllerCommand command)
        {
            if (!state.World.Grid.Contains(command.Hex))
            {
                return ValidationResult.Fail("hata.test.disarida");
            }

            HexCell cell = state.World.Cell(command.Hex);
            if (cell.IsWater || cell.City.IsValid)
            {
                return ValidationResult.Fail("hata.test.uygunDegil");
            }

            return cell.Controller == command.Controller ? ValidationResult.Fail("hata.test.zatenOyle") : ValidationResult.Ok;
        }

        protected override void ApplyTyped(CommandContext context, SetHexControllerCommand command)
        {
            Applied++;
            context.State.World.SetHexController(command.Hex, command.Controller);
        }

        protected override void WriteTyped(SetHexControllerCommand command, CanonicalWriter writer)
        {
            writer.WriteInt32(command.Hex.Column);
            writer.WriteInt32(command.Hex.Row);
            writer.WriteId(command.Controller);
        }

        protected override SetHexControllerCommand ReadTyped(CountryId issuer, CanonicalReader reader)
        {
            return new SetHexControllerCommand(issuer, new HexCoord(reader.ReadInt32(), reader.ReadInt32()), new CountryId(reader.ReadUInt64()));
        }
    }

    /// <summary>Anında çözülen taktik emir: %60 olasılıkla sektörü ele geçirir.</summary>
    public sealed class TacticalStrikeCommand : GameCommand
    {
        public const string KindKey = "emir.test.taktikSaldiri";

        public TacticalStrikeCommand(CountryId issuer, SectorId sector) : base(issuer)
        {
            Sector = sector;
        }

        public SectorId Sector { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }

    public sealed class TacticalStrikeHandler : CommandHandler<TacticalStrikeCommand>
    {
        public static int Applied;
        public static readonly List<ResolutionId> Resolutions = new List<ResolutionId>();

        public override string Kind => TacticalStrikeCommand.KindKey;

        public override bool ResolvesImmediately => true;

        protected override ValidationResult ValidateTyped(GameState state, TacticalStrikeCommand command)
        {
            if (!state.World.Sectors.TryGetValue(command.Sector, out var sector))
            {
                return ValidationResult.Fail("hata.test.sektorYok");
            }

            return sector.Controller == command.Issuer ? ValidationResult.Fail("hata.test.zatenBizde") : ValidationResult.Ok;
        }

        protected override void ApplyTyped(CommandContext context, TacticalStrikeCommand command)
        {
            Applied++;
            Resolutions.Add(context.Resolution);
            if (context.Rng.Chance(Fixed64.Parse("0.6")))
            {
                context.State.World.SetSectorController(command.Sector, command.Issuer);
            }
        }

        protected override void WriteTyped(TacticalStrikeCommand command, CanonicalWriter writer) => writer.WriteId(command.Sector);

        protected override TacticalStrikeCommand ReadTyped(CountryId issuer, CanonicalReader reader) =>
            new TacticalStrikeCommand(issuer, new SectorId(reader.ReadUInt64()));
    }

    /// <summary>
    /// P12: PRNG ile her gün birkaç şehirsiz kara hücresinin kontrolcüsünü değiştirir ve süreli
    /// etkiler ekler. Dünya, etki ve PRNG yollarını birlikte determinizm testine sokar.
    /// </summary>
    public sealed class RandomTurmoilSystem : ISimulationSystem
    {
        public string Name => "test.kargasa";

        public SimulationPhase Phase => SimulationPhase.P12Movement;

        public void Execute(SimulationContext context)
        {
            RngStream rng = context.Rng(RngSubsystem.Movement, 1, 0);
            WorldMap map = context.World;
            for (int i = 0; i < 3; i++)
            {
                HexCell cell = map.CellAt(rng.NextInt(map.CellCount));
                if (!cell.IsWater && !cell.City.IsValid)
                {
                    map.SetHexController(cell.Coord, new CountryId((ulong)rng.NextInt(1, 4)));
                }
            }

            if (rng.Chance(Fixed64.Parse("0.4")))
            {
                context.Effects.Add(
                    EffectTarget.Of(new CountryId((ulong)rng.NextInt(1, 4))),
                    "test.moral",
                    rng.Chance(Fixed64.Half) ? EffectOperation.Add : EffectOperation.Percent,
                    Fixed64.FromRaw(rng.NextInt(-500_000, 500_000)),
                    context.Today,
                    rng.NextInt(0, 12),
                    "olay.test");
            }
        }
    }

    /// <summary>Her fazda çalıştığı fazı ve gördüğü tarihi kaydeder.</summary>
    public sealed class PhaseRecorder : ISimulationSystem
    {
        private readonly List<string> _log;

        public PhaseRecorder(SimulationPhase phase, List<string> log)
        {
            Phase = phase;
            _log = log;
        }

        public string Name => "test.kaydedici." + (int)Phase;

        public SimulationPhase Phase { get; }

        public void Execute(SimulationContext context)
        {
            Assert.That(context.CurrentPhase, Is.EqualTo(Phase));
            _log.Add((int)Phase + "@" + context.Today);
        }
    }

    /// <summary>Verilen fazda bir kez istisna fırlatır.</summary>
    public sealed class ExplodingSystem : ISimulationSystem
    {
        public string Name => "test.patlayan";

        public SimulationPhase Phase => SimulationPhase.P8Production;

        public void Execute(SimulationContext context) => throw new InvalidOperationException("test patlaması");
    }

    /// <summary>P13: çözüm defterini okuyan sistem (anında çözülenleri ikinci kez uygulamaz).</summary>
    public sealed class LedgerProbe : ISimulationSystem
    {
        public readonly List<int> SeenCounts = new List<int>();

        public string Name => "test.defter";

        public SimulationPhase Phase => SimulationPhase.P13Combat;

        public void Execute(SimulationContext context)
        {
            SeenCounts.Add(context.Ledger.Count);
            for (int i = 0; i < context.Ledger.Count; i++)
            {
                Assert.That(TacticalStrikeHandler.Resolutions, Does.Contain(context.Ledger.Resolved[i]), "defterdeki her çözüm anında uygulanmış olmalı");
            }
        }
    }
}
