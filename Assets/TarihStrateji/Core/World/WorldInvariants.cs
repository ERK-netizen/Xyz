using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.World
{
    /// <summary>
    /// Dünyanın değişmezleri. Harita kurulurken ve her gün P15'te çalışır; ihlal bir hatadır
    /// (yazılım hatası veya bozuk kayıt) ve oyun durdurulur. İhlaller sabit sırada,
    /// "anahtar @ bağlam" biçiminde eklenir.
    /// </summary>
    public static class WorldInvariants
    {
        public static int Check(WorldMap map, List<string> violations)
        {
            int before = violations.Count;
            HexGrid grid = map.Grid;
            int regionCellTotal = 0;

            for (int i = 0; i < map.CellCount; i++)
            {
                HexCell cell = map.CellAt(i);
                if (cell.Id != grid.ToId(cell.Coord))
                {
                    Add(violations, WorldErrorKeys.HucreBulunamadi, cell.Id);
                }

                if (cell.Region.IsValid)
                {
                    regionCellTotal++;
                    if (!map.Regions.ContainsKey(cell.Region))
                    {
                        Add(violations, WorldErrorKeys.BolgeBulunamadi, cell.Id);
                    }
                }

                if (map.ValidateInfrastructure(cell, cell.Infrastructure).IsValid == false)
                {
                    Add(violations, WorldErrorKeys.AltyapiSudaOlamaz, cell.Id);
                }

                if (cell.City.IsValid && !map.Cities.ContainsKey(cell.City))
                {
                    Add(violations, WorldErrorKeys.SehirBulunamadi, cell.Id);
                }
            }

            int listedTotal = 0;
            foreach (KeyValuePair<RegionId, StrategicRegion> pair in map.Regions)
            {
                StrategicRegion region = pair.Value;
                if (region.Hexes.Count == 0)
                {
                    Add(violations, WorldErrorKeys.BolgeBos, region.Id);
                }

                for (int h = 0; h < region.Hexes.Count; h++)
                {
                    listedTotal++;
                    HexId hexId = region.Hexes[h];
                    if (h > 0 && region.Hexes[h - 1] >= hexId)
                    {
                        Add(violations, WorldErrorKeys.BolgeCakismasi, region.Id);
                    }

                    if (!grid.TryFromId(hexId, out _))
                    {
                        Add(violations, WorldErrorKeys.HucreDisinda, region.Id);
                        continue;
                    }

                    HexCell cell = map.Cell(hexId);
                    if (cell.Region != region.Id)
                    {
                        Add(violations, WorldErrorKeys.BolgeCakismasi, hexId);
                    }

                    if (cell.IsWater != region.IsSea)
                    {
                        Add(violations, WorldErrorKeys.BolgeKarisik, hexId);
                    }
                }
            }

            if (listedTotal != regionCellTotal)
            {
                Add(violations, WorldErrorKeys.BolgeCakismasi, RegionId.None);
            }

            foreach (KeyValuePair<CityId, City> pair in map.Cities)
            {
                CheckCity(map, pair.Value, violations);
            }

            foreach (KeyValuePair<SectorId, CitySector> pair in map.Sectors)
            {
                CitySector sector = pair.Value;
                if (sector.BuildingSlotsUsed < 0 || sector.BuildingSlotsUsed > sector.BuildingSlotCapacity)
                {
                    Add(violations, WorldErrorKeys.SektorSlotKapasitesi, sector.Id);
                }

                if (!map.Cities.ContainsKey(sector.City))
                {
                    Add(violations, WorldErrorKeys.SehirBulunamadi, sector.Id);
                }
            }

            return violations.Count - before;
        }

        private static void CheckCity(WorldMap map, City city, List<string> violations)
        {
            if (!map.Grid.TryFromId(city.Hex, out HexCoord coord))
            {
                Add(violations, WorldErrorKeys.HucreDisinda, city.Id);
                return;
            }

            HexCell cell = map.Cell(coord);
            if (cell.City != city.Id)
            {
                Add(violations, WorldErrorKeys.SehirHucresiDolu, city.Id);
            }

            if (cell.IsWater)
            {
                Add(violations, WorldErrorKeys.SehirSudaOlamaz, city.Id);
            }

            int count = city.Sectors.Count;
            if (count != (int)city.Tier || count < CityLayout.MinSectors || count > CityLayout.MaxSectors)
            {
                Add(violations, WorldErrorKeys.SehirSektorSayisi, city.Id);
            }

            var slots = new List<SectorSlot>(count);
            int administrative = 0;
            for (int s = 0; s < count; s++)
            {
                if (!map.Sectors.TryGetValue(city.Sectors[s], out CitySector sector))
                {
                    Add(violations, WorldErrorKeys.SektorBulunamadi, city.Id);
                    return;
                }

                if (s > 0 && city.Sectors[s - 1] >= city.Sectors[s])
                {
                    Add(violations, WorldErrorKeys.SehirSektorTurleri, city.Id);
                }

                slots.Add(sector.Slot);
                if (sector.City != city.Id)
                {
                    Add(violations, WorldErrorKeys.SektorBulunamadi, sector.Id);
                }

                if (sector.Owner != cell.Owner)
                {
                    Add(violations, WorldErrorKeys.KontrolTutarsiz, sector.Id);
                }

                if (sector.Kind == SectorKind.Administrative)
                {
                    administrative++;
                    if (sector.Slot != SectorSlot.Center || s != 0)
                    {
                        Add(violations, WorldErrorKeys.SehirMerkezYok, sector.Id);
                    }

                    if (sector.Controller != cell.Controller)
                    {
                        Add(violations, WorldErrorKeys.KontrolTutarsiz, sector.Id);
                    }
                }

                if (sector.Kind == SectorKind.Port)
                {
                    bool facesWater = CityLayout.TryGetDirection(sector.Slot, out HexDirection side)
                        && map.TryGetNeighborCell(cell, side, out HexCell neighbor)
                        && neighbor.IsWater;
                    if (!facesWater)
                    {
                        Add(violations, WorldErrorKeys.LimanKiyidaDegil, sector.Id);
                    }
                }
            }

            if (administrative != 1)
            {
                Add(violations, WorldErrorKeys.SehirMerkezYok, city.Id);
            }

            if (!CityLayout.IsConnected(slots))
            {
                Add(violations, WorldErrorKeys.SehirBitisikDegil, city.Id);
            }
        }

        private static void Add(List<string> violations, string key, IStableId context)
        {
            violations.Add(key + " @ " + context.Value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
