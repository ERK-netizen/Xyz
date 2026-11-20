using System;
using System.Collections.Generic;
using TarihStrateji.Core.Diplomacy.Definitions;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.AI.Perception
{
    /// <summary>
    /// Bir YZ ülkesinin karar girdisi (CLAUDE.md §2.6: YZ yalnız kendi <see cref="ObservedWorld"/>'ünü
    /// okur). İçerik: kamuya açık bilgi (harita ve fiili kontrol, diplomatik durum), ülkenin kendi
    /// durumu (kendi birlikleri, hazine, istikrar, araştırma/odak/olay seçenekleri, toplama yerleri)
    /// ve savaş sisi altındaki görüş. Yabancı birlikler yalnız <see cref="Observed"/> temaslarıyla
    /// görülür; gerçek dünyanın birlik listesi buraya hiç girmez. Simulation doldurur.
    /// </summary>
    public sealed class AiPerception
    {
        public AiPerception(
            CountryId country,
            GameDate today,
            GeoWorld world,
            DiplomacyState diplomacy,
            DiplomacyParameters diplomacyParameters,
            ObservedWorld observed,
            WarfareCatalog warfare)
        {
            Country = country;
            Today = today;
            World = world ?? throw new ArgumentNullException(nameof(world));
            Diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
            DiplomacyParameters = diplomacyParameters ?? throw new ArgumentNullException(nameof(diplomacyParameters));
            Observed = observed ?? throw new ArgumentNullException(nameof(observed));
            Warfare = warfare ?? throw new ArgumentNullException(nameof(warfare));
        }

        public CountryId Country { get; }

        public GameDate Today { get; }

        /// <summary>
        /// Senaryonun YZ için en erken savaş ilanı tarihi (tanım verisi; 1914'te Temmuz Krizi: 28.07.1914). Bu
        /// tarihten önce YZ savaş ilan etmez; savunma, ittifak çağrısı ve oyuncu bundan etkilenmez.
        /// </summary>
        public GameDate EarliestWar { get; set; } = GameDate.MinValue;

        /// <summary>Coğrafya, ulaşım grafı ve siyasi harita (bölge/şehir sahipliği ve kontrolü kamuya açıktır).</summary>
        public GeoWorld World { get; }

        /// <summary>Savaşlar, antlaşmalar, teklifler (kamuya açık diplomasi).</summary>
        public DiplomacyState Diplomacy { get; }

        public DiplomacyParameters DiplomacyParameters { get; }

        public ObservedWorld Observed { get; }

        public WarfareCatalog Warfare { get; }

        /// <summary>Ülkenin kendi kara birlikleri (kimlik sırasıyla).</summary>
        public List<LandUnit> OwnUnits { get; } = new List<LandUnit>();

        public Fixed64 Treasury { get; set; }

        public Fixed64 TaxRate { get; set; }

        /// <summary>İstikrar (siyaset durumu olmayan ülkede 1).</summary>
        public Fixed64 Stability { get; set; } = Fixed64.One;

        public TechnologyId CurrentResearch { get; set; }

        /// <summary>Şu an başlanabilir teknolojiler (Simulation kurallarla süzmüştür), kimlik sırasıyla.</summary>
        public List<TechnologyId> AvailableTechnologies { get; } = new List<TechnologyId>();

        public FocusId CurrentFocus { get; set; }

        public List<FocusId> AvailableFocuses { get; } = new List<FocusId>();

        public List<EventId> PendingEvents { get; } = new List<EventId>();

        /// <summary>Kendi bölgelerinin çalışma çağı nüfusu ve seferber sayısı.</summary>
        public long WorkingAge { get; set; }

        public long Mobilized { get; set; }

        public int ConstructionsInProgress { get; set; }

        /// <summary>Geçerli inşaat seçenekleri, tercih sırasıyla (Simulation kurallarla süzmüştür).</summary>
        public List<AiBuildOption> BuildOptions { get; } = new List<AiBuildOption>();

        /// <summary>Toplama yapılabilecek kendi şehirleri (YZ'nin toplama türü için geçerli), kimlik sırasıyla.</summary>
        public List<CityId> RecruitSites { get; } = new List<CityId>();

        /// <summary>Kara sınırı paylaşılan ülkeler (siyasi haritadan), kimlik sırasıyla.</summary>
        public List<CountryId> Neighbors { get; } = new List<CountryId>();

    }
}
