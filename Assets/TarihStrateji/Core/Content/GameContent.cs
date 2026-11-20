using System;
using TarihStrateji.Core.AI.Definitions;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Diplomacy.Definitions;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Intelligence.Definitions;
using TarihStrateji.Core.Politics.Definitions;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Geo;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Oyunun tüm tanım verisi (DEF): ekonomi, demografi, teknoloji, siyaset, karakterler, savaş
    /// (coğrafi hareket/ikmal dahil), diplomasi, istihbarat, anlatı ve YZ.
    /// Durum değildir; kayda girmez.
    /// </summary>
    public sealed class GameContent
    {
        public GameContent(
            EconomyCatalog economy,
            DemographyParameters demography,
            TechCatalog technology,
            PoliticsCatalog politics,
            CharacterCatalog characters,
            WarfareCatalog warfare,
            DiplomacyParameters diplomacy,
            IntelligenceParameters intelligence,
            NarrativeCatalog narrative,
            AiParameters ai,
            GeoWarfareParameters geoWarfare = null)
        {
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Demography = demography ?? throw new ArgumentNullException(nameof(demography));
            Technology = technology ?? throw new ArgumentNullException(nameof(technology));
            Politics = politics ?? throw new ArgumentNullException(nameof(politics));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Warfare = warfare ?? throw new ArgumentNullException(nameof(warfare));
            Diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
            Intelligence = intelligence ?? throw new ArgumentNullException(nameof(intelligence));
            Narrative = narrative ?? throw new ArgumentNullException(nameof(narrative));
            Ai = ai ?? throw new ArgumentNullException(nameof(ai));
            GeoWarfare = geoWarfare ?? BaselineMilitaryContent.CreateGeoWarfare();
        }

        /// <summary>Coğrafi (altıgensiz) savaş parametreleri (ADR-011).</summary>
        public GeoWarfareParameters GeoWarfare { get; }

        public EconomyCatalog Economy { get; }

        public DemographyParameters Demography { get; }

        public TechCatalog Technology { get; }

        public PoliticsCatalog Politics { get; }

        public CharacterCatalog Characters { get; }

        public WarfareCatalog Warfare { get; }

        public DiplomacyParameters Diplomacy { get; }

        public IntelligenceParameters Intelligence { get; }

        public NarrativeCatalog Narrative { get; }

        public AiParameters Ai { get; }
    }
}
