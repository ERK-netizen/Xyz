using System;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Oyunun tüm tanım verisi (DEF). Durum değildir; kayda girmez. Aynı kayıt aynı içerikle
    /// yüklenmelidir (içerik sürümü Adım 10'da kayıt üst verisine eklenir).
    /// </summary>
    public sealed class GameContent
    {
        public GameContent(EconomyCatalog economy, DemographyParameters demography, TerrainMovementTable movement)
        {
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Demography = demography ?? throw new ArgumentNullException(nameof(demography));
            Movement = movement ?? throw new ArgumentNullException(nameof(movement));
        }

        public EconomyCatalog Economy { get; }

        public DemographyParameters Demography { get; }

        public TerrainMovementTable Movement { get; }
    }
}
