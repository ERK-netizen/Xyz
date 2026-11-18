using System;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Politics.Definitions;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Oyunun tüm tanım verisi (DEF): ekonomi, demografi, hareket, teknoloji, siyaset, karakterler.
    /// Durum değildir; kayda girmez.
    /// </summary>
    public sealed class GameContent
    {
        public GameContent(
            EconomyCatalog economy,
            DemographyParameters demography,
            TerrainMovementTable movement,
            TechCatalog technology,
            PoliticsCatalog politics,
            CharacterCatalog characters)
        {
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Demography = demography ?? throw new ArgumentNullException(nameof(demography));
            Movement = movement ?? throw new ArgumentNullException(nameof(movement));
            Technology = technology ?? throw new ArgumentNullException(nameof(technology));
            Politics = politics ?? throw new ArgumentNullException(nameof(politics));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
        }

        public EconomyCatalog Economy { get; }

        public DemographyParameters Demography { get; }

        public TerrainMovementTable Movement { get; }

        public TechCatalog Technology { get; }

        public PoliticsCatalog Politics { get; }

        public CharacterCatalog Characters { get; }
    }
}
