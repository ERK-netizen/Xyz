using System;
using System.Collections.Generic;
using TarihStrateji.Core.Characters;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Economy;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Politics;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research;
using TarihStrateji.Core.Research.State;
using TarihStrateji.Core.Simulation.Commands;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Persistence;

namespace TarihStrateji.Core.Simulation.Persistence
{
    /// <summary>
    /// <see cref="GameState"/>'in kanonik ikili anlık görüntüsü. Kayıt yükü ve durum hash'i bu
    /// baytlardan üretilir. Hash için "geçici" alanlar (arayüz sürümü) yazılmaz; böylece hash
    /// yalnız simülasyonun anlamlı durumunu yansıtır.
    /// </summary>
    public static class GameStateCodec
    {
        public const uint Tag = 0x454D4147; // "GAME"
        private const uint JournalTag = 0x4C4E524A; // "JRNL"

        /// <summary>Durum şemasının sürümü. Değiştiğinde Persistence'a göç eklenir.</summary>
        public const int SchemaVersion = 1;

        public static byte[] Encode(GameState state, CommandRegistry registry, bool includeVolatile)
        {
            var writer = new CanonicalWriter(64 * 1024);
            Write(state, registry, writer, includeVolatile);
            return writer.ToArray();
        }

        public static void Write(GameState state, CommandRegistry registry, CanonicalWriter w, bool includeVolatile)
        {
            if (state == null || registry == null)
            {
                throw new ArgumentNullException(state == null ? nameof(state) : nameof(registry));
            }

            w.WriteTag(Tag);
            w.WriteInt32(SchemaVersion);
            w.WriteBool(includeVolatile);
            w.WriteUInt64(state.WorldSeed);
            w.WriteByte((byte)state.Mode);
            w.WriteDate(state.Today);
            w.WriteId(state.PlayerCountry);
            if (includeVolatile)
            {
                w.WriteInt64(state.StateVersion);
            }

            w.WriteUInt64(state.CommandIds.NextValue);
            w.WriteUInt64(state.ResolutionIds.NextValue);
            WorldSnapshotCodec.Write(state.World, w);
            EffectSnapshotCodec.Write(state.Effects, w);
            PopulationSnapshotCodec.Write(state.Population, w);
            EconomySnapshotCodec.Write(state.Economy, w);
            ResearchSnapshotCodec.Write(state.Research, w);
            PoliticsSnapshotCodec.Write(state.Politics, w);
            CharacterSnapshotCodec.Write(state.Characters, w);
            WarfareSnapshotCodec.Write(state.Warfare, w);
            state.Wars.Write(w);

            w.WriteTag(JournalTag);
            IReadOnlyList<JournalEntry> pending = state.Journal.Pending;
            w.WriteInt32(pending.Count);
            for (int i = 0; i < pending.Count; i++)
            {
                JournalEntry entry = pending[i];
                if (!registry.TryGet(entry.Command.Kind, out ICommandHandler handler))
                {
                    throw new InvalidOperationException(SimulationErrorKeys.BilinmeyenEmirTuru + ": " + entry.Command.Kind);
                }

                w.WriteId(entry.Id);
                w.WriteDate(entry.SubmittedOn);
                w.WriteId(entry.Command.Issuer);
                w.WriteString(entry.Command.Kind);
                var payload = new CanonicalWriter(64);
                handler.Write(entry.Command, payload);
                w.WriteBytes(payload.ToArray());
            }

            IReadOnlyList<ResolutionId> resolved = state.Ledger.Resolved;
            w.WriteInt32(resolved.Count);
            for (int i = 0; i < resolved.Count; i++)
            {
                w.WriteId(resolved[i]);
            }
        }

        public static Result<GameState> Decode(byte[] data, CommandRegistry registry)
        {
            if (data == null || registry == null)
            {
                throw new ArgumentNullException(data == null ? nameof(data) : nameof(registry));
            }

            try
            {
                var reader = new CanonicalReader(data);
                Result<GameState> result = Read(reader, registry);
                if (result.IsSuccess)
                {
                    reader.ExpectEnd();
                }

                return result;
            }
            catch (FormatException e)
            {
                return Result<GameState>.Failure(e.Message);
            }
            catch (ArgumentException)
            {
                return Result<GameState>.Failure(DomainErrorKeys.KayitBozuk);
            }
            catch (InvalidOperationException)
            {
                return Result<GameState>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }

        private static Result<GameState> Read(CanonicalReader r, CommandRegistry registry)
        {
            r.ExpectTag(Tag);
            int schema = r.ReadInt32();
            if (schema > SchemaVersion)
            {
                return Result<GameState>.Failure(SimulationErrorKeys.DurumSemasiYeni);
            }

            if (schema != SchemaVersion)
            {
                return Result<GameState>.Failure(DomainErrorKeys.KayitBozuk);
            }

            bool hasVolatile = r.ReadBool();
            ulong seed = r.ReadUInt64();
            var mode = (GameMode)r.ReadByte();
            if (mode != GameMode.Normal && mode != GameMode.Sandbox && mode != GameMode.Observer)
            {
                return Result<GameState>.Failure(DomainErrorKeys.KayitBozuk);
            }

            var today = r.ReadDate();
            var player = new CountryId(r.ReadUInt64());
            long stateVersion = hasVolatile ? r.ReadInt64() : 0L;
            var commandIds = new StableIdAllocator(r.ReadUInt64());
            var resolutionIds = new StableIdAllocator(r.ReadUInt64());

            Result<WorldMap> world = WorldSnapshotCodec.Read(r);
            if (!world.IsSuccess)
            {
                return Result<GameState>.Failure(world.ErrorKey);
            }

            Result<EffectEngine> effects = EffectSnapshotCodec.Read(r);
            if (!effects.IsSuccess)
            {
                return Result<GameState>.Failure(effects.ErrorKey);
            }

            Result<PopulationState> population = PopulationSnapshotCodec.Read(r);
            if (!population.IsSuccess)
            {
                return Result<GameState>.Failure(population.ErrorKey);
            }

            Result<EconomyState> economy = EconomySnapshotCodec.Read(r);
            if (!economy.IsSuccess)
            {
                return Result<GameState>.Failure(economy.ErrorKey);
            }

            Result<ResearchState> research = ResearchSnapshotCodec.Read(r);
            if (!research.IsSuccess)
            {
                return Result<GameState>.Failure(research.ErrorKey);
            }

            Result<PoliticsState> politics = PoliticsSnapshotCodec.Read(r);
            if (!politics.IsSuccess)
            {
                return Result<GameState>.Failure(politics.ErrorKey);
            }

            Result<CharacterState> characters = CharacterSnapshotCodec.Read(r);
            if (!characters.IsSuccess)
            {
                return Result<GameState>.Failure(characters.ErrorKey);
            }

            Result<WarfareState> warfare = WarfareSnapshotCodec.Read(r);
            if (!warfare.IsSuccess)
            {
                return Result<GameState>.Failure(warfare.ErrorKey);
            }

            var state = new GameState(seed, mode, today, player, world.Value, effects.Value, population.Value, economy.Value,
                research.Value, politics.Value, characters.Value, warfare.Value, commandIds, resolutionIds)
            {
                StateVersion = stateVersion,
            };

            state.Wars.Read(r);
            r.ExpectTag(JournalTag);
            int pendingCount = r.ReadCount(r.Remaining);
            for (int i = 0; i < pendingCount; i++)
            {
                var id = new CommandId(r.ReadUInt64());
                var submittedOn = r.ReadDate();
                var issuer = new CountryId(r.ReadUInt64());
                string kind = r.ReadString();
                byte[] payload = r.ReadBytes();
                if (!id.IsValid || id.Value >= commandIds.NextValue || !registry.TryGet(kind, out ICommandHandler handler))
                {
                    return Result<GameState>.Failure(DomainErrorKeys.KayitBozuk);
                }

                var payloadReader = new CanonicalReader(payload);
                GameCommand command = handler.Read(issuer, payloadReader);
                payloadReader.ExpectEnd();
                if (command == null || !string.Equals(command.Kind, kind, StringComparison.Ordinal))
                {
                    return Result<GameState>.Failure(DomainErrorKeys.KayitBozuk);
                }

                state.Journal.AddPending(new JournalEntry(id, submittedOn, command, ResolutionId.None));
            }

            int resolvedCount = r.ReadCount(r.Remaining);
            for (int i = 0; i < resolvedCount; i++)
            {
                var id = new ResolutionId(r.ReadUInt64());
                if (!id.IsValid || id.Value >= resolutionIds.NextValue)
                {
                    return Result<GameState>.Failure(DomainErrorKeys.KayitBozuk);
                }

                state.Ledger.Record(id);
            }

            return Result<GameState>.Success(state);
        }
    }
}
