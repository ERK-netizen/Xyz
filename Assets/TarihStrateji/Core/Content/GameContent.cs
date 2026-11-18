using System;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Content modülünün yükleyebildiği tanım verisi (DEF): ekonomi, demografi, hareket, teknoloji.
    /// Siyaset ve karakter katalogları, bağımlılık grafı (CLAUDE.md §2.2) gereği Simulation'daki
    /// <c>StandardContent</c> bileşiminde tutulur. Durum değildir; kayda girmez. Aynı kayıt aynı içerikle
    /// yüklenmelidir (içerik sürümü Adım 10'da kayıt üst verisine eklenir).
    /// </summary>
    public sealed class GameContent
    {
        public GameContent(
            EconomyCatalog economy,
            DemographyParameters demography,
            TerrainMovementTable movement,
            TechCatalog technology)
        {
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Demography = demography ?? throw new ArgumentNullException(nameof(demography));
            Movement = movement ?? throw new ArgumentNullException(nameof(movement));
            Technology = technology ?? throw new ArgumentNullException(nameof(technology));
        }

        public EconomyCatalog Economy { get; }

        public DemographyParameters Demography { get; }

        public TerrainMovementTable Movement { get; }

        public TechCatalog Technology { get; }
    }
}
