using System;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Politics.Definitions;

namespace TarihStrateji.Core.Simulation.Scenario
{
    /// <summary>
    /// Standart kuralların ihtiyaç duyduğu tüm tanım verisi: Content modülünün yükledikleri
    /// (<see cref="GameContent"/>) + bağımlılık grafı gereği Content'in göremediği siyaset ve
    /// karakter katalogları (CLAUDE.md §2.2).
    /// </summary>
    public sealed class StandardContent
    {
        public StandardContent(GameContent core, PoliticsCatalog politics, CharacterCatalog characters)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            Politics = politics ?? throw new ArgumentNullException(nameof(politics));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
        }

        public GameContent Core { get; }

        public PoliticsCatalog Politics { get; }

        public CharacterCatalog Characters { get; }
    }
}
