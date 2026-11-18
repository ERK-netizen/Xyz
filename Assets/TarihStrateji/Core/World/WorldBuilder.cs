using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Control;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.World
{
    /// <summary>
    /// Tanım verisinden (Content, DEF) <see cref="WorldMap"/> kurar. Arazi, bölge, şehir ve
    /// altyapı önce kaydedilir; tüm doğrulama <see cref="Build"/> içinde tam bağlamla, sabit
    /// sırada yapılır ve sonunda <see cref="WorldInvariants"/> çalıştırılır. Kimlikler ekleme
    /// sırasıyla verilir.
    /// </summary>
    public sealed class WorldBuilder
    {
        private readonly HexGrid _grid;
        private readonly TerrainType[] _terrain;
        private readonly List<RegionSpec> _regions = new List<RegionSpec>();
        private readonly List<CitySpec> _cities = new List<CitySpec>();
        private readonly List<KeyValuePair<HexCoord, HexInfrastructure>> _infrastructure = new List<KeyValuePair<HexCoord, HexInfrastructure>>();
        private readonly StableIdAllocator _regionIds = new StableIdAllocator();
        private readonly StableIdAllocator _cityIds = new StableIdAllocator();
        private bool _built;

        public WorldBuilder(HexGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _terrain = new TerrainType[grid.CellCount]; // varsayılan: Ocean
        }

        public HexGrid Grid => _grid;

        public void SetTerrain(HexCoord coord, TerrainType terrain)
        {
            RequireNotBuilt();
            if (!TerrainRules.IsDefined(terrain))
            {
                throw new ArgumentOutOfRangeException(nameof(terrain));
            }

            _terrain[_grid.IndexOf(coord)] = terrain;
        }

        public TerrainType GetTerrain(HexCoord coord)
        {
            return _terrain[_grid.IndexOf(coord)];
        }

        /// <summary>Kara bölgesi ekler; sahip başlangıçta kontrolcüdür.</summary>
        public RegionId AddLandRegion(string nameKey, IReadOnlyList<HexCoord> hexes, CountryId owner)
        {
            return AddRegion(nameKey, hexes, owner, false);
        }

        /// <summary>Deniz bölgesi ekler (sahipsiz).</summary>
        public RegionId AddSeaRegion(string nameKey, IReadOnlyList<HexCoord> hexes)
        {
            return AddRegion(nameKey, hexes, CountryId.None, true);
        }

        /// <summary>
        /// Şehir ekler. <paramref name="sectors"/> sayısı kademenin sektör sayısına eşit olmalı;
        /// ilk eleman merkez yuvadaki <see cref="SectorKind.Administrative"/> sektördür, sonrakiler
        /// <paramref name="facing"/> yönünden saat yönünün tersine halka yuvalarına yerleşir.
        /// </summary>
        public CityId AddCity(string nameKey, HexCoord hex, CityTier tier, HexDirection facing, IReadOnlyList<SectorSpec> sectors)
        {
            RequireNotBuilt();
            if (sectors == null)
            {
                throw new ArgumentNullException(nameof(sectors));
            }

            var id = new CityId(_cityIds.Allocate());
            _cities.Add(new CitySpec(id, nameKey, hex, tier, facing, new List<SectorSpec>(sectors)));
            return id;
        }

        public void SetInfrastructure(HexCoord coord, HexInfrastructure infrastructure)
        {
            RequireNotBuilt();
            if (!_grid.Contains(coord))
            {
                throw new ArgumentOutOfRangeException(nameof(coord), WorldErrorKeys.HucreDisinda);
            }

            _infrastructure.Add(new KeyValuePair<HexCoord, HexInfrastructure>(coord, infrastructure));
        }

        public Result<WorldMap> Build()
        {
            RequireNotBuilt();
            _built = true;

            var cells = new HexCell[_grid.CellCount];
            for (int i = 0; i < cells.Length; i++)
            {
                HexCoord coord = _grid.FromIndex(i);
                cells[i] = new HexCell(_grid.ToId(coord), coord, _terrain[i]);
            }

            for (int i = 0; i < cells.Length; i++)
            {
                HexCell cell = cells[i];
                if (cell.IsWater)
                {
                    continue;
                }

                for (int d = 0; d < HexDirections.Count; d++)
                {
                    if (_grid.TryGetNeighbor(cell.Coord, (HexDirection)d, out HexCoord n) && TerrainRules.IsWater(_terrain[_grid.IndexOf(n)]))
                    {
                        cell.IsCoastal = true;
                        break;
                    }
                }
            }

            var regions = new StableIdMap<RegionId, StrategicRegion>(_regions.Count);
            for (int r = 0; r < _regions.Count; r++)
            {
                RegionSpec spec = _regions[r];
                if (spec.Hexes.Count == 0)
                {
                    return Result<WorldMap>.Failure(WorldErrorKeys.BolgeBos);
                }

                var ids = new List<HexId>(spec.Hexes.Count);
                for (int h = 0; h < spec.Hexes.Count; h++)
                {
                    HexCoord coord = spec.Hexes[h];
                    if (!_grid.Contains(coord))
                    {
                        return Result<WorldMap>.Failure(WorldErrorKeys.HucreDisinda);
                    }

                    HexCell cell = cells[_grid.IndexOf(coord)];
                    if (cell.Region.IsValid)
                    {
                        return Result<WorldMap>.Failure(WorldErrorKeys.BolgeCakismasi);
                    }

                    if (cell.IsWater != spec.IsSea)
                    {
                        return Result<WorldMap>.Failure(WorldErrorKeys.BolgeKarisik);
                    }

                    cell.Region = spec.Id;
                    cell.Owner = spec.Owner;
                    cell.Controller = spec.Owner;
                    ids.Add(cell.Id);
                }

                var region = new StrategicRegion(spec.Id, spec.NameKey, spec.IsSea, WorldMap.SortedUnique(ids))
                {
                    Owner = spec.Owner,
                    Controller = spec.Owner,
                };
                regions.Add(spec.Id, region);
            }

            var cities = new StableIdMap<CityId, City>(_cities.Count);
            var sectors = new StableIdMap<SectorId, CitySector>(_cities.Count * 4);
            var sectorIds = new StableIdAllocator();
            for (int c = 0; c < _cities.Count; c++)
            {
                CitySpec spec = _cities[c];
                string error = ValidateCitySpec(spec, cells);
                if (error != null)
                {
                    return Result<WorldMap>.Failure(error);
                }

                HexCell cell = cells[_grid.IndexOf(spec.Hex)];
                SectorSlot[] slots = CityLayout.SlotsFor(spec.Tier, spec.Facing);
                var sectorList = new List<SectorId>(slots.Length);
                for (int s = 0; s < slots.Length; s++)
                {
                    var sectorId = new SectorId(sectorIds.Allocate());
                    var sector = new CitySector(sectorId, spec.Id, slots[s], spec.Sectors[s].Kind, spec.Sectors[s].BuildingSlots)
                    {
                        Owner = cell.Owner,
                        Controller = cell.Controller,
                    };
                    sectors.Add(sectorId, sector);
                    sectorList.Add(sectorId);
                }

                cell.City = spec.Id;
                cities.Add(spec.Id, new City(spec.Id, spec.NameKey, cell.Id, spec.Tier, sectorList));
            }

            var map = new WorldMap(_grid, cells, regions, cities, sectors, _regionIds, _cityIds, sectorIds);

            for (int i = 0; i < _infrastructure.Count; i++)
            {
                ValidationResult applied = map.SetInfrastructure(_infrastructure[i].Key, _infrastructure[i].Value);
                if (!applied.IsValid)
                {
                    return Result<WorldMap>.Failure(applied.ErrorKey);
                }
            }

            for (int r = 0; r < regions.Count; r++)
            {
                StrategicRegion region = regions.ValueAt(r);
                region.Controller = ControlRules.DeriveRegionController(map, region);
            }

            var violations = new List<string>();
            if (WorldInvariants.Check(map, violations) > 0)
            {
                return Result<WorldMap>.Failure(violations[0]);
            }

            return Result<WorldMap>.Success(map);
        }

        private string ValidateCitySpec(CitySpec spec, HexCell[] cells)
        {
            if (!_grid.Contains(spec.Hex))
            {
                return WorldErrorKeys.HucreDisinda;
            }

            int count = (int)spec.Tier;
            if (count < CityLayout.MinSectors || count > CityLayout.MaxSectors || spec.Sectors.Count != count)
            {
                return WorldErrorKeys.SehirSektorSayisi;
            }

            HexCell cell = cells[_grid.IndexOf(spec.Hex)];
            if (cell.IsWater)
            {
                return WorldErrorKeys.SehirSudaOlamaz;
            }

            if (cell.City.IsValid)
            {
                return WorldErrorKeys.SehirHucresiDolu;
            }

            if (spec.Sectors[0].Kind != SectorKind.Administrative)
            {
                return WorldErrorKeys.SehirMerkezYok;
            }

            SectorSlot[] slots = CityLayout.SlotsFor(spec.Tier, spec.Facing);
            for (int s = 1; s < spec.Sectors.Count; s++)
            {
                SectorKind kind = spec.Sectors[s].Kind;
                if (kind == SectorKind.Administrative)
                {
                    return WorldErrorKeys.SehirSektorTurleri;
                }

                if (kind == SectorKind.Port)
                {
                    CityLayout.TryGetDirection(slots[s], out HexDirection side);
                    if (!_grid.TryGetNeighbor(spec.Hex, side, out HexCoord n) || !cells[_grid.IndexOf(n)].IsWater)
                    {
                        return WorldErrorKeys.LimanKiyidaDegil;
                    }
                }
            }

            return null;
        }

        private RegionId AddRegion(string nameKey, IReadOnlyList<HexCoord> hexes, CountryId owner, bool isSea)
        {
            RequireNotBuilt();
            if (hexes == null)
            {
                throw new ArgumentNullException(nameof(hexes));
            }

            var id = new RegionId(_regionIds.Allocate());
            _regions.Add(new RegionSpec(id, nameKey, new List<HexCoord>(hexes), owner, isSea));
            return id;
        }

        private void RequireNotBuilt()
        {
            if (_built)
            {
                throw new InvalidOperationException("WorldBuilder yalnız bir kez kullanılabilir.");
            }
        }

        private sealed class RegionSpec
        {
            public RegionSpec(RegionId id, string nameKey, List<HexCoord> hexes, CountryId owner, bool isSea)
            {
                Id = id;
                NameKey = nameKey;
                Hexes = hexes;
                Owner = owner;
                IsSea = isSea;
            }

            public RegionId Id { get; }

            public string NameKey { get; }

            public List<HexCoord> Hexes { get; }

            public CountryId Owner { get; }

            public bool IsSea { get; }
        }

        private sealed class CitySpec
        {
            public CitySpec(CityId id, string nameKey, HexCoord hex, CityTier tier, HexDirection facing, List<SectorSpec> sectors)
            {
                Id = id;
                NameKey = nameKey;
                Hex = hex;
                Tier = tier;
                Facing = facing;
                Sectors = sectors;
            }

            public CityId Id { get; }

            public string NameKey { get; }

            public HexCoord Hex { get; }

            public CityTier Tier { get; }

            public HexDirection Facing { get; }

            public List<SectorSpec> Sectors { get; }
        }
    }
}
