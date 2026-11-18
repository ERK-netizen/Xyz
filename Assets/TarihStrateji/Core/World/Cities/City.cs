using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Cities
{
    /// <summary>
    /// Şehir: ebeveyn dünya hücresinin içinde 2–6 bitişik sektör (CLAUDE.md §2.3). Şehrin
    /// fiili kontrolcüsü merkez (<see cref="SectorKind.Administrative"/>) sektörünün
    /// kontrolcüsüdür; sahiplik ve kontrol sektör düzeyinde ayrı tutulur.
    /// </summary>
    public sealed class City
    {
        private readonly List<SectorId> _sectors;

        internal City(CityId id, string nameKey, HexId hex, CityTier tier, List<SectorId> sectors)
        {
            Id = id;
            NameKey = nameKey;
            Hex = hex;
            Tier = tier;
            _sectors = sectors;
        }

        public CityId Id { get; }

        public string NameKey { get; }

        public HexId Hex { get; }

        public CityTier Tier { get; }

        /// <summary>Artan kimlik sırasında; ilk eleman merkez sektördür.</summary>
        public IReadOnlyList<SectorId> Sectors => _sectors;

        public SectorId AdministrativeSector => _sectors[0];
    }
}
