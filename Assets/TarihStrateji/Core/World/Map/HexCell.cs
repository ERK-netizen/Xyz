using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.World.Map
{
    /// <summary>
    /// Dünya hücresinin durumu (düz veri). <see cref="Owner"/> hukuki sahiplik,
    /// <see cref="Controller"/> fiili kontroldür ve ayrı tutulur (CLAUDE.md §2.3). Değişiklikler
    /// yalnız <see cref="WorldMap"/> üzerinden ve SimulationKernel tarafından yapılır.
    /// </summary>
    public sealed class HexCell
    {
        internal HexCell(HexId id, HexCoord coord, TerrainType terrain)
        {
            Id = id;
            Coord = coord;
            Terrain = terrain;
        }

        public HexId Id { get; }

        public HexCoord Coord { get; }

        public TerrainType Terrain { get; internal set; }

        public bool IsWater => TerrainRules.IsWater(Terrain);

        /// <summary>Kara hücresi ve en az bir su komşusu var.</summary>
        public bool IsCoastal { get; internal set; }

        public RegionId Region { get; internal set; }

        public CountryId Owner { get; internal set; }

        public CountryId Controller { get; internal set; }

        public HexInfrastructure Infrastructure { get; internal set; }

        /// <summary>Hücredeki şehir; yoksa <see cref="CityId.None"/>. Bir hücrede en fazla bir şehir.</summary>
        public CityId City { get; internal set; }
    }
}
