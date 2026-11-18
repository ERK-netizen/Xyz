using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.World;

namespace TarihStrateji.Core.Simulation.State
{
    /// <summary>
    /// Simülasyonun tüm kalıcı durumu (düz veri). Kayda bütünüyle yazılır; kural mantığı
    /// sistemlerde ve emir işleyicilerindedir. Yalnız SimulationKernel değiştirir.
    /// </summary>
    public sealed class GameState
    {
        internal GameState(
            ulong worldSeed,
            GameMode mode,
            GameDate today,
            CountryId playerCountry,
            WorldMap world,
            EffectEngine effects,
            StableIdAllocator commandIds,
            StableIdAllocator resolutionIds)
        {
            WorldSeed = worldSeed;
            Mode = mode;
            Today = today;
            PlayerCountry = playerCountry;
            World = world ?? throw new ArgumentNullException(nameof(world));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            CommandIds = commandIds;
            ResolutionIds = resolutionIds;
            Journal = new CommandJournal();
            Ledger = new ResolutionLedger();
        }

        public ulong WorldSeed { get; }

        public GameMode Mode { get; }

        public GameDate Today { get; internal set; }

        public CountryId PlayerCountry { get; }

        public WorldMap World { get; }

        public EffectEngine Effects { get; }

        public CommandJournal Journal { get; }

        public ResolutionLedger Ledger { get; }

        public StableIdAllocator CommandIds { get; }

        public StableIdAllocator ResolutionIds { get; }

        /// <summary>Arayüz önbellekleri için sürüm; simülasyon durumu değildir, hash'e girmez.</summary>
        public long StateVersion { get; internal set; }

        /// <summary>Yeni oyun durumu. Gözlemci modunda oyuncu ülkesi olmaz.</summary>
        public static GameState CreateNew(ulong worldSeed, GameMode mode, GameDate startDate, CountryId playerCountry, WorldMap world)
        {
            if (mode == GameMode.Observer && playerCountry.IsValid)
            {
                throw new ArgumentException("Gözlemci modunda oyuncu ülkesi olamaz.", nameof(playerCountry));
            }

            if (mode != GameMode.Observer && !playerCountry.IsValid)
            {
                throw new ArgumentException("Oyuncu ülkesi gerekli.", nameof(playerCountry));
            }

            return new GameState(worldSeed, mode, startDate, playerCountry, world, new EffectEngine(), new StableIdAllocator(), new StableIdAllocator());
        }

        /// <summary>Bugünün anahtarıyla PRNG akışı.</summary>
        public RngStream Rng(RngSubsystem subsystem, ulong eventId, ulong entityId)
        {
            return new RngStream(WorldSeed, subsystem, Today.DayNumber, eventId, entityId);
        }
    }
}
