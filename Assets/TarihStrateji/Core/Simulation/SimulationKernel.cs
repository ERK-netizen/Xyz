using System;
using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Simulation.Commands;
using TarihStrateji.Core.Simulation.Persistence;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation
{
    /// <summary>
    /// Dünya durumunun tek yazıcısı (CLAUDE.md §2.5). Sunum katmanı yalnız
    /// <see cref="IGameCommandGateway"/> ve <see cref="IGameReadModel"/> ile konuşur.
    /// <list type="bullet">
    /// <item><see cref="Submit"/>: mod kuralları + işleyici doğrulaması; geçerli emir günlüğe
    /// girer. Anında çözülen (taktik) emir hemen uygulanır, <see cref="ResolutionId"/> alır ve
    /// P1'de ikinci kez uygulanmaz.</item>
    /// <item><see cref="AdvanceDay"/>: P0–P15 sabit sırada. Bir sistem hata fırlatırsa veya bir
    /// değişmez bozulursa simülasyon durur (<see cref="IsHalted"/>); bozuk durumla devam etmez.</item>
    /// </list>
    /// Tek iş parçacığından kullanılır.
    /// </summary>
    public sealed class SimulationKernel : IGameCommandGateway, IGameReadModel
    {
        private readonly GameState _state;
        private readonly CommandRegistry _commands;
        private readonly List<ISimulationSystem>[] _phases;
        private readonly List<IInvariantCheck> _invariants;
        private readonly IAutosaveSink _autosave;
        private readonly int _autosaveIntervalDays;
        private readonly int _hashIntervalDays;
        private readonly List<string> _haltReasons = new List<string>();
        private readonly CanonicalWriter _hashWriter = new CanonicalWriter(64 * 1024);
        private byte[] _lastStateHash;

        private SimulationKernel(SimulationSetup setup, GameState state)
        {
            _state = state;
            _commands = setup.Commands;
            _autosave = setup.AutosaveSink;
            _autosaveIntervalDays = setup.AutosaveIntervalDays;
            _hashIntervalDays = setup.HashIntervalDays;
            _phases = new List<ISimulationSystem>[SimulationPhases.Count];
            for (int p = 0; p < _phases.Length; p++)
            {
                _phases[p] = new List<ISimulationSystem>();
            }

            for (int i = 0; i < setup.Systems.Count; i++)
            {
                _phases[(int)setup.Systems[i].Phase].Add(setup.Systems[i]);
            }

            _invariants = new List<IInvariantCheck> { new WorldInvariantCheck() };
            for (int i = 0; i < setup.Invariants.Count; i++)
            {
                _invariants.Add(setup.Invariants[i]);
            }
        }

        public event Action<long> StateChanged;

        public GameState State => _state;

        public GameMode Mode => _state.Mode;

        public GameDate CurrentDate => _state.Today;

        public CountryId PlayerCountry => _state.PlayerCountry;

        public long StateVersion => _state.StateVersion;

        public bool IsHalted => _haltReasons.Count > 0;

        public IReadOnlyList<string> HaltReasons => _haltReasons;

        /// <summary>Son hesaplanan durum hash'i (SHA-256) veya henüz yoksa <c>null</c>.</summary>
        public byte[] LastStateHash => _lastStateHash;

        public static Result<SimulationKernel> Create(SimulationSetup setup, GameState state)
        {
            if (setup == null || state == null)
            {
                return Result<SimulationKernel>.Failure(SimulationErrorKeys.KurulumEksik);
            }

            if (setup.AutosaveIntervalDays < 0 || setup.HashIntervalDays < 0 || (setup.AutosaveIntervalDays > 0 && setup.AutosaveSink == null))
            {
                return Result<SimulationKernel>.Failure(SimulationErrorKeys.KurulumEksik);
            }

            return Result<SimulationKernel>.Success(new SimulationKernel(setup, state));
        }

        /// <summary>Kayıt anlık görüntüsünden (geçici alanlar dahil) çekirdek kurar.</summary>
        public static Result<SimulationKernel> Restore(SimulationSetup setup, byte[] snapshot)
        {
            if (setup == null || snapshot == null)
            {
                return Result<SimulationKernel>.Failure(SimulationErrorKeys.KurulumEksik);
            }

            Result<GameState> state = GameStateCodec.Decode(snapshot, setup.Commands);
            return state.IsSuccess ? Create(setup, state.Value) : Result<SimulationKernel>.Failure(state.ErrorKey);
        }

        // ------------------------------------------------------------------ emirler

        public ValidationResult Validate(GameCommand command)
        {
            return ValidateCore(command, out _);
        }

        public CommandReceipt Submit(GameCommand command)
        {
            ValidationResult validation = ValidateCore(command, out ICommandHandler handler);
            if (!validation.IsValid)
            {
                return CommandReceipt.Reject(validation, _state.Today);
            }

            var commandId = new CommandId(_state.CommandIds.Allocate());
            if (handler.ResolvesImmediately)
            {
                var resolution = new ResolutionId(_state.ResolutionIds.Allocate());
                RngStream rng = _state.Rng(RngSubsystem.Tactical, commandId.Value, command.Issuer.Value);
                try
                {
                    handler.Apply(new CommandContext(_state, commandId, resolution, rng), command);
                }
                catch (Exception e)
                {
                    Halt(SimulationErrorKeys.SistemHatasi + ": " + command.Kind + ": " + e.Message);
                    return CommandReceipt.Reject(ValidationResult.Fail(SimulationErrorKeys.SimulasyonDurdu), _state.Today);
                }

                _state.Ledger.Record(resolution);
            }
            else
            {
                _state.Journal.AddPending(new JournalEntry(commandId, _state.Today, command, ResolutionId.None));
            }

            BumpVersion();
            return CommandReceipt.Accept(commandId, _state.Today);
        }

        private ValidationResult ValidateCore(GameCommand command, out ICommandHandler handler)
        {
            handler = null;
            if (IsHalted)
            {
                return ValidationResult.Fail(SimulationErrorKeys.SimulasyonDurdu);
            }

            ValidationResult authority = GameModeRules.CheckAuthority(_state.Mode, command);
            if (!authority.IsValid)
            {
                return authority;
            }

            if (command.Authority == CommandAuthority.Player && command.Issuer != _state.PlayerCountry)
            {
                return ValidationResult.Fail(SimulationErrorKeys.YabanciUlkeEmri);
            }

            if (!_commands.TryGet(command.Kind, out handler))
            {
                return ValidationResult.Fail(SimulationErrorKeys.BilinmeyenEmirTuru);
            }

            return handler.Validate(_state, command);
        }

        // ------------------------------------------------------------------ günlük adım

        /// <summary>Bir gün ilerletir (P0–P15). Durmuş simülasyonda başarısız döner.</summary>
        public Result<DayReport> AdvanceDay()
        {
            if (IsHalted)
            {
                return Result<DayReport>.Failure(SimulationErrorKeys.SimulasyonDurdu);
            }

            var notes = new List<string>();
            var rejected = new List<string>();
            var violations = new List<string>();
            var context = new SimulationContext(_state, notes);
            int applied = 0;
            int expired = 0;
            byte[] hash = null;
            bool autosaved = false;

            for (int p = 0; p < SimulationPhases.Count && violations.Count == 0; p++)
            {
                var phase = (SimulationPhase)p;
                context.CurrentPhase = phase;
                try
                {
                    switch (phase)
                    {
                        case SimulationPhase.P0DayOpen:
                            _state.Today = _state.Today.AddDays(1);
                            break;
                        case SimulationPhase.P1Orders:
                            applied = ApplyPendingCommands(rejected);
                            break;
                        case SimulationPhase.P2EventsAndEffects:
                            expired = _state.Effects.ExpireBefore(_state.Today, new List<EffectId>());
                            break;
                    }

                    List<ISimulationSystem> systems = _phases[p];
                    for (int s = 0; s < systems.Count; s++)
                    {
                        systems[s].Execute(context);
                    }

                    if (phase == SimulationPhase.P15AiInvariantsAndSave)
                    {
                        EnqueueAiOrders(context, rejected);
                    }
                }
                catch (Exception e)
                {
                    violations.Add(SimulationErrorKeys.SistemHatasi + ": P" + p.ToString(CultureInfo.InvariantCulture) + ": " + e.GetType().Name + ": " + e.Message);
                }
            }

            if (violations.Count == 0)
            {
                for (int i = 0; i < _invariants.Count; i++)
                {
                    _invariants[i].Check(_state, violations);
                }
            }

            if (violations.Count > 0)
            {
                for (int i = 0; i < violations.Count; i++)
                {
                    Halt(violations[i]);
                }
            }
            else
            {
                int day = _state.Today.DayNumber;
                if (_hashIntervalDays > 0 && day % _hashIntervalDays == 0)
                {
                    hash = ComputeStateHash();
                    _lastStateHash = hash;
                }

                _state.Ledger.Clear();
                BumpVersionSilently();
                if (_autosaveIntervalDays > 0 && day % _autosaveIntervalDays == 0)
                {
                    _autosave.Autosave(_state, CaptureSnapshot());
                    autosaved = true;
                }
            }

            RaiseStateChanged();
            return Result<DayReport>.Success(new DayReport(_state.Today, applied, rejected, expired, notes, violations, hash, autosaved));
        }

        /// <summary>
        /// YZ emirleri: oyuncu ülkesi adına olamaz; mod kuralı (Normal'de hile yok) ve işleyici
        /// doğrulaması geçerse yeni <see cref="CommandId"/> ile günlüğe girer; reddedilenler
        /// rapora yazılır.
        /// </summary>
        private void EnqueueAiOrders(SimulationContext context, List<string> rejected)
        {
            List<GameCommand> orders = context.AiOrderList;
            for (int i = 0; i < orders.Count; i++)
            {
                GameCommand command = orders[i];
                ValidationResult result;
                if (command == null || !command.Issuer.IsValid || command.Issuer == _state.PlayerCountry)
                {
                    result = ValidationResult.Fail(SimulationErrorKeys.YabanciUlkeEmri);
                }
                else if (_state.Mode == GameMode.Normal && command.Authority != CommandAuthority.Player)
                {
                    result = ValidationResult.Fail(DomainErrorKeys.HileNormalModda);
                }
                else if (!_commands.TryGet(command.Kind, out ICommandHandler handler))
                {
                    result = ValidationResult.Fail(SimulationErrorKeys.BilinmeyenEmirTuru);
                }
                else
                {
                    result = handler.Validate(_state, command);
                }

                if (!result.IsValid)
                {
                    rejected.Add("yz:" + (command == null ? "?" : command.Kind) + ": " + result.ErrorKey);
                    continue;
                }

                var id = new CommandId(_state.CommandIds.Allocate());
                _state.Journal.AddPending(new JournalEntry(id, _state.Today, command, ResolutionId.None));
            }

            orders.Clear();
        }

        private int ApplyPendingCommands(List<string> rejected)
        {
            IReadOnlyList<JournalEntry> pending = _state.Journal.Pending;
            int applied = 0;
            for (int i = 0; i < pending.Count; i++)
            {
                JournalEntry entry = pending[i];
                _commands.TryGet(entry.Command.Kind, out ICommandHandler handler);
                ValidationResult still = handler == null
                    ? ValidationResult.Fail(SimulationErrorKeys.BilinmeyenEmirTuru)
                    : handler.Validate(_state, entry.Command);
                if (!still.IsValid)
                {
                    rejected.Add(entry.Id.Value.ToString(CultureInfo.InvariantCulture) + ": " + still.ErrorKey);
                    continue;
                }

                RngStream rng = _state.Rng(RngSubsystem.Kernel, entry.Id.Value, entry.Command.Issuer.Value);
                handler.Apply(new CommandContext(_state, entry.Id, ResolutionId.None, rng), entry.Command);
                applied++;
            }

            _state.Journal.ClearPending();
            return applied;
        }

        // ------------------------------------------------------------------ anlık görüntü

        /// <summary>Kayıt yükü: geçici alanlar dahil kanonik baytlar.</summary>
        public byte[] CaptureSnapshot()
        {
            return GameStateCodec.Encode(_state, _commands, true);
        }

        /// <summary>Simülasyon durumunun SHA-256 özeti (geçici alanlar hariç).</summary>
        public byte[] ComputeStateHash()
        {
            _hashWriter.Reset();
            GameStateCodec.Write(_state, _commands, _hashWriter, false);
            return _hashWriter.ComputeSha256();
        }

        private void Halt(string reason)
        {
            _haltReasons.Add(reason);
        }

        private void BumpVersion()
        {
            BumpVersionSilently();
            RaiseStateChanged();
        }

        private void BumpVersionSilently()
        {
            _state.StateVersion++;
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(_state.StateVersion);
        }
    }
}
