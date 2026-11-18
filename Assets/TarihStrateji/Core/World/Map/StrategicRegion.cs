using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Map
{
    /// <summary>
    /// Stratejik bölge: hücre kümesi, hukuki sahip ve fiili kontrolcü (ayrı alanlar). Hücre
    /// listesi daima artan <see cref="HexId"/> sırasındadır. Deniz bölgeleri sahipsizdir.
    /// </summary>
    public sealed class StrategicRegion
    {
        private readonly List<HexId> _hexes;

        internal StrategicRegion(RegionId id, string nameKey, bool isSea, List<HexId> sortedHexes)
        {
            Id = id;
            NameKey = nameKey;
            IsSea = isSea;
            _hexes = sortedHexes;
        }

        public RegionId Id { get; }

        /// <summary>Yerelleştirme anahtarı, ör. <c>bolge.anadolu.ankara</c>.</summary>
        public string NameKey { get; }

        public bool IsSea { get; }

        public IReadOnlyList<HexId> Hexes => _hexes;

        public CountryId Owner { get; internal set; }

        public CountryId Controller { get; internal set; }
    }
}
