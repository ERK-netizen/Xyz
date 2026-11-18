using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>Sektör → şehir → hücre → bölge eşlemesi.</summary>
    public static class EconomyGeography
    {
        public static RegionId RegionOfSector(WorldMap map, SectorId sectorId)
        {
            if (!map.Sectors.TryGetValue(sectorId, out CitySector sector))
            {
                return RegionId.None;
            }

            City city = map.Cities[sector.City];
            return map.Cell(city.Hex).Region;
        }

        /// <summary>Tesis çalışabilir mi: sektör var ve fiili kontrol tesisin sahibinde.</summary>
        public static bool IsOperational(WorldMap map, SectorId sectorId, CountryId owner)
        {
            return map.Sectors.TryGetValue(sectorId, out CitySector sector) && sector.Controller == owner;
        }
    }
}
