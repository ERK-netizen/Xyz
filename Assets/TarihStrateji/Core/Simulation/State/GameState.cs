using System;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research.State;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

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
            GeoWorld world,
            EffectEngine effects,
            PopulationState population,
            EconomyState economy,
            ResearchState research,
            PoliticsState politics,
            CharacterState characters,
            WarfareState warfare,
            DiplomacyState diplomacy,
            IntelligenceState intelligence,
            NarrativeState narrative,
            StableIdAllocator commandIds,
            StableIdAllocator resolutionIds)
        {
            WorldSeed = worldSeed;
            Mode = mode;
            Today = today;
            PlayerCountry = playerCountry;
            World = world ?? throw new ArgumentNullException(nameof(world));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Population = population ?? throw new ArgumentNullException(nameof(population));
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Research = research ?? throw new ArgumentNullException(nameof(research));
            Politics = politics ?? throw new ArgumentNullException(nameof(politics));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Warfare = warfare ?? throw new ArgumentNullException(nameof(warfare));
            Diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
            Intelligence = intelligence ?? throw new ArgumentNullException(nameof(intelligence));
            Narrative = narrative ?? throw new ArgumentNullException(nameof(narrative));
            Hostility = new DiplomaticHostility(diplomacy);
            CommandIds = commandIds;
            ResolutionIds = resolutionIds;
            Journal = new CommandJournal();
            Ledger = new ResolutionLedger();
        }

        public ulong WorldSeed { get; }

        public GameMode Mode { get; }

        public GameDate Today { get; internal set; }

        public CountryId PlayerCountry { get; }

        /// <summary>Tek kanonik dünya (ADR-011): ülke/bölge çokgenleri, şehirler, ulaşım grafı ve kontrol durumu.</summary>
        public GeoWorld World { get; }

        public EffectEngine Effects { get; }

        public PopulationState Population { get; }

        public EconomyState Economy { get; }

        public ResearchState Research { get; }

        public PoliticsState Politics { get; }

        public CharacterState Characters { get; }

        public WarfareState Warfare { get; }

        public DiplomacyState Diplomacy { get; }

        /// <summary>Savaş hâli görünümü (Warfare'in <c>IHostility</c>'si); kaynak <see cref="Diplomacy"/>.</summary>
        public DiplomaticHostility Hostility { get; }

        public IntelligenceState Intelligence { get; }

        public NarrativeState Narrative { get; }

        public CommandJournal Journal { get; }

        public ResolutionLedger Ledger { get; }

        public StableIdAllocator CommandIds { get; }

        public StableIdAllocator ResolutionIds { get; }

        /// <summary>Arayüz önbellekleri için sürüm; simülasyon durumu değildir, hash'e girmez.</summary>
        public long StateVersion { get; internal set; }

        /// <summary>Yeni oyun durumu. Gözlemci modunda oyuncu ülkesi olmaz.</summary>
        public static GameState CreateNew(ulong worldSeed, GameMode mode, GameDate startDate, CountryId playerCountry, GeoWorld world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (mode == GameMode.Observer ? playerCountry.IsValid : !playerCountry.IsValid)
            {
                throw new ArgumentException("Oyuncu ülkesi moda uygun değil.", nameof(playerCountry));
            }

            return new GameState(worldSeed, mode, startDate, playerCountry, world, new EffectEngine(), new PopulationState(), new EconomyState(), new ResearchState(), new PoliticsState(), new CharacterState(), new WarfareState(),
                new DiplomacyState(), new IntelligenceState(), new NarrativeState(), new StableIdAllocator(), new StableIdAllocator());
        }

        /// <summary>Bugünün anahtarıyla PRNG akışı.</summary>
        public RngStream Rng(RngSubsystem subsystem, ulong eventId, ulong entityId)
        {
            return new RngStream(WorldSeed, subsystem, Today.DayNumber, eventId, entityId);
        }
    }
}
