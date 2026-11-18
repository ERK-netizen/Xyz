using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Control;
using TarihStrateji.Core.World.Geo;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.World
{
    /// <summary>
    /// Tek kanonik dünya (CLAUDE.md §2.3). <see cref="WorldBuilder"/> ile kurulur. Değiştirici
    /// yöntemler yalnız SimulationKernel tarafından çağrılır (§2.5); her biri önce doğrular,
    /// geçersizse durumu değiştirmeden <see cref="ValidationResult"/> döner.
    /// </summary>
    public sealed class WorldMap
    {
        private readonly HexCell[] _cells;

        internal WorldMap(
            HexGrid grid,
            HexCell[] cells,
            StableIdMap<RegionId, StrategicRegion> regions,
            StableIdMap<CityId, City> cities,
            StableIdMap<SectorId, CitySector> sectors,
            StableIdAllocator regionIds,
            StableIdAllocator cityIds,
            StableIdAllocator sectorIds)
        {
            Grid = grid;
            Distances = new HexDistanceTable(grid);
            _cells = cells;
            Regions = regions;
            Cities = cities;
            Sectors = sectors;
            RegionIds = regionIds;
            CityIds = cityIds;
            SectorIds = sectorIds;
        }

        public HexGrid Grid { get; }

        public HexDistanceTable Distances { get; }

        public int CellCount => _cells.Length;

        public StableIdMap<RegionId, StrategicRegion> Regions { get; }

        public StableIdMap<CityId, City> Cities { get; }

        public StableIdMap<SectorId, CitySector> Sectors { get; }

        public StableIdAllocator RegionIds { get; }

        public StableIdAllocator CityIds { get; }

        public StableIdAllocator SectorIds { get; }

        /// <summary>Kimlik sırasında (satır, sonra sütun) hücre.</summary>
        public HexCell CellAt(int index)
        {
            return _cells[index];
        }

        public HexCell Cell(HexCoord coord)
        {
            return _cells[Grid.IndexOf(coord)];
        }

        public HexCell Cell(HexId id)
        {
            return _cells[Grid.IndexOf(Grid.FromId(id))];
        }

        public bool TryGetNeighborCell(HexCell cell, HexDirection direction, out HexCell neighbor)
        {
            if (Grid.TryGetNeighbor(cell.Coord, direction, out HexCoord n))
            {
                neighbor = _cells[Grid.IndexOf(n)];
                return true;
            }

            neighbor = null;
            return false;
        }

        public CitySector AdministrativeSectorOf(City city)
        {
            return Sectors[city.AdministrativeSector];
        }

        // ------------------------------------------------------------------ değiştiriciler

        /// <summary>
        /// Hücre altyapısını değiştirir. Altyapı bina slotu tüketmez. Su hücresinde yol/demiryolu
        /// olmaz; liman bağlantısı liman sektörlü bir şehir gerektirir.
        /// </summary>
        public ValidationResult SetInfrastructure(HexCoord coord, HexInfrastructure infrastructure)
        {
            if (!Grid.Contains(coord))
            {
                return ValidationResult.Fail(WorldErrorKeys.HucreDisinda);
            }

            HexCell cell = Cell(coord);
            ValidationResult check = ValidateInfrastructure(cell, infrastructure);
            if (!check.IsValid)
            {
                return check;
            }

            cell.Infrastructure = infrastructure;
            return ValidationResult.Ok;
        }

        /// <summary>Altyapı değişikliğini uygulamadan doğrular (emir doğrulaması için).</summary>
        public ValidationResult CheckInfrastructure(HexCoord coord, HexInfrastructure infrastructure)
        {
            return Grid.Contains(coord) ? ValidateInfrastructure(Cell(coord), infrastructure) : ValidationResult.Fail(WorldErrorKeys.HucreDisinda);
        }

        /// <summary>
        /// Sektörün fiili kontrolcüsünü değiştirir (işgal, geri alma). Merkez sektör değişirse
        /// şehir hücresinin ve bölgenin kontrolcüsü yeniden türetilir. Sahiplik değişmez.
        /// </summary>
        public ValidationResult SetSectorController(SectorId sectorId, CountryId controller)
        {
            if (!Sectors.TryGetValue(sectorId, out CitySector sector))
            {
                return ValidationResult.Fail(WorldErrorKeys.SektorBulunamadi);
            }

            sector.Controller = controller;
            City city = Cities[sector.City];
            if (sector.Id == city.AdministrativeSector)
            {
                HexCell cell = Cell(city.Hex);
                cell.Controller = controller;
                RefreshRegionController(cell.Region);
            }

            return ValidationResult.Ok;
        }

        /// <summary>
        /// Şehirsiz hücrenin fiili kontrolcüsünü değiştirir. Şehirli hücrede kontrol sektörler
        /// üzerinden değişir (<see cref="SetSectorController"/>).
        /// </summary>
        public ValidationResult SetHexController(HexCoord coord, CountryId controller)
        {
            if (!Grid.Contains(coord))
            {
                return ValidationResult.Fail(WorldErrorKeys.HucreDisinda);
            }

            HexCell cell = Cell(coord);
            if (cell.City.IsValid)
            {
                return ValidationResult.Fail(WorldErrorKeys.KontrolTutarsiz);
            }

            cell.Controller = controller;
            RefreshRegionController(cell.Region);
            return ValidationResult.Ok;
        }

        /// <summary>
        /// Bölgenin hukuki sahipliğini devreder (yalnız barış/antlaşma sonucu). Bölgedeki tüm
        /// hücrelerin ve şehir sektörlerinin sahibi değişir; fiili kontrol değişmez.
        /// </summary>
        public ValidationResult TransferRegionOwnership(RegionId regionId, CountryId newOwner)
        {
            if (!Regions.TryGetValue(regionId, out StrategicRegion region))
            {
                return ValidationResult.Fail(WorldErrorKeys.BolgeBulunamadi);
            }

            if (region.IsSea)
            {
                return ValidationResult.Fail(WorldErrorKeys.BolgeKarisik);
            }

            region.Owner = newOwner;
            for (int i = 0; i < region.Hexes.Count; i++)
            {
                HexCell cell = Cell(region.Hexes[i]);
                cell.Owner = newOwner;
                if (cell.City.IsValid)
                {
                    City city = Cities[cell.City];
                    for (int s = 0; s < city.Sectors.Count; s++)
                    {
                        Sectors[city.Sectors[s]].Owner = newOwner;
                    }
                }
            }

            return ValidationResult.Ok;
        }

        /// <summary>Sektörde bina slotu ayırır (Economy, Adım 4).</summary>
        public ValidationResult UseBuildingSlots(SectorId sectorId, int count)
        {
            if (!Sectors.TryGetValue(sectorId, out CitySector sector))
            {
                return ValidationResult.Fail(WorldErrorKeys.SektorBulunamadi);
            }

            if (count <= 0 || count > sector.FreeBuildingSlots)
            {
                return ValidationResult.Fail(WorldErrorKeys.SektorSlotKapasitesi);
            }

            sector.BuildingSlotsUsed += count;
            return ValidationResult.Ok;
        }

        /// <summary>Sektörde ayrılmış bina slotlarını bırakır.</summary>
        public ValidationResult ReleaseBuildingSlots(SectorId sectorId, int count)
        {
            if (!Sectors.TryGetValue(sectorId, out CitySector sector))
            {
                return ValidationResult.Fail(WorldErrorKeys.SektorBulunamadi);
            }

            if (count <= 0 || count > sector.BuildingSlotsUsed)
            {
                return ValidationResult.Fail(WorldErrorKeys.SektorSlotKapasitesi);
            }

            sector.BuildingSlotsUsed -= count;
            return ValidationResult.Ok;
        }

        internal ValidationResult ValidateInfrastructure(HexCell cell, HexInfrastructure infrastructure)
        {
            if (cell.IsWater && (infrastructure.HasLandInfrastructure || infrastructure.HasPort))
            {
                return ValidationResult.Fail(WorldErrorKeys.AltyapiSudaOlamaz);
            }

            if (infrastructure.HasPort && !HasPortSector(cell))
            {
                return ValidationResult.Fail(WorldErrorKeys.LimanSehirGerektirir);
            }

            return ValidationResult.Ok;
        }

        internal bool HasPortSector(HexCell cell)
        {
            if (!cell.City.IsValid || !Cities.TryGetValue(cell.City, out City city))
            {
                return false;
            }

            for (int i = 0; i < city.Sectors.Count; i++)
            {
                if (Sectors[city.Sectors[i]].Kind == SectorKind.Port)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshRegionController(RegionId regionId)
        {
            if (regionId.IsValid && Regions.TryGetValue(regionId, out StrategicRegion region))
            {
                region.Controller = ControlRules.DeriveRegionController(this, region);
            }
        }

        internal static List<HexId> SortedUnique(List<HexId> hexes)
        {
            hexes.Sort();
            for (int i = 1; i < hexes.Count; i++)
            {
                if (hexes[i] == hexes[i - 1])
                {
                    throw new ArgumentException(WorldErrorKeys.BolgeCakismasi);
                }
            }

            return hexes;
        }
    }
}
