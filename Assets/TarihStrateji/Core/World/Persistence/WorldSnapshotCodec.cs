using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.World.Persistence
{
    /// <summary>
    /// <see cref="WorldMap"/>'in kanonik ikili anlık görüntüsü. Yalnız türetilemeyen durum
    /// yazılır (save-systems): km tablosu ve kıyı bilgisi yüklemede yeniden hesaplanır.
    /// Okuma sonunda <see cref="WorldInvariants"/> çalışır; tutarsız kayıt reddedilir.
    /// </summary>
    public static class WorldSnapshotCodec
    {
        public const uint Tag = 0x444C5257; // "WRLD"
        private const uint CellsTag = 0x4C4C4543; // "CELL"
        private const uint RegionsTag = 0x4E474552; // "REGN"
        private const uint CitiesTag = 0x59544943; // "CITY"

        public static void Write(WorldMap map, CanonicalWriter w)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            w.WriteTag(Tag);
            HexGrid grid = map.Grid;
            w.WriteInt32(grid.Width);
            w.WriteInt32(grid.Height);
            w.WriteFixed64(grid.NorthLatitude);
            w.WriteFixed64(grid.SouthLatitude);
            w.WriteUInt64(map.RegionIds.NextValue);
            w.WriteUInt64(map.CityIds.NextValue);
            w.WriteUInt64(map.SectorIds.NextValue);

            w.WriteTag(CellsTag);
            for (int i = 0; i < map.CellCount; i++)
            {
                HexCell c = map.CellAt(i);
                w.WriteByte((byte)c.Terrain);
                w.WriteId(c.Region);
                w.WriteId(c.Owner);
                w.WriteId(c.Controller);
                w.WriteByte((byte)c.Infrastructure.RoadLevel);
                w.WriteByte((byte)c.Infrastructure.RailLevel);
                w.WriteBool(c.Infrastructure.HasPort);
                w.WriteId(c.City);
            }

            w.WriteTag(RegionsTag);
            w.WriteInt32(map.Regions.Count);
            foreach (KeyValuePair<RegionId, StrategicRegion> pair in map.Regions)
            {
                StrategicRegion r = pair.Value;
                w.WriteId(r.Id);
                w.WriteString(r.NameKey);
                w.WriteBool(r.IsSea);
                w.WriteId(r.Owner);
                w.WriteId(r.Controller);
                w.WriteInt32(r.Hexes.Count);
                for (int h = 0; h < r.Hexes.Count; h++)
                {
                    w.WriteId(r.Hexes[h]);
                }
            }

            w.WriteTag(CitiesTag);
            w.WriteInt32(map.Cities.Count);
            foreach (KeyValuePair<CityId, City> pair in map.Cities)
            {
                City city = pair.Value;
                w.WriteId(city.Id);
                w.WriteString(city.NameKey);
                w.WriteId(city.Hex);
                w.WriteByte((byte)city.Tier);
                w.WriteInt32(city.Sectors.Count);
                for (int s = 0; s < city.Sectors.Count; s++)
                {
                    CitySector sector = map.Sectors[city.Sectors[s]];
                    w.WriteId(sector.Id);
                    w.WriteByte((byte)sector.Slot);
                    w.WriteByte((byte)sector.Kind);
                    w.WriteId(sector.Owner);
                    w.WriteId(sector.Controller);
                    w.WriteInt32(sector.BuildingSlotCapacity);
                    w.WriteInt32(sector.BuildingSlotsUsed);
                }
            }
        }

        public static Result<WorldMap> Read(CanonicalReader r)
        {
            try
            {
                return ReadCore(r);
            }
            catch (ArgumentException)
            {
                return Result<WorldMap>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }

        private static Result<WorldMap> ReadCore(CanonicalReader r)
        {
            r.ExpectTag(Tag);
            int width = r.ReadInt32();
            int height = r.ReadInt32();
            var grid = new HexGrid(width, height, r.ReadFixed64(), r.ReadFixed64());
            var regionIds = new StableIdAllocator(r.ReadUInt64());
            var cityIds = new StableIdAllocator(r.ReadUInt64());
            var sectorIds = new StableIdAllocator(r.ReadUInt64());

            r.ExpectTag(CellsTag);
            var cells = new HexCell[grid.CellCount];
            for (int i = 0; i < cells.Length; i++)
            {
                HexCoord coord = grid.FromIndex(i);
                var terrain = (TerrainType)r.ReadByte();
                if (!TerrainRules.IsDefined(terrain))
                {
                    return Result<WorldMap>.Failure(DomainErrorKeys.KayitBozuk);
                }

                var cell = new HexCell(grid.ToId(coord), coord, terrain)
                {
                    Region = new RegionId(r.ReadUInt64()),
                    Owner = new CountryId(r.ReadUInt64()),
                    Controller = new CountryId(r.ReadUInt64()),
                };
                int road = r.ReadByte();
                int rail = r.ReadByte();
                bool port = r.ReadBool();
                cell.Infrastructure = new HexInfrastructure(road, rail, port);
                cell.City = new CityId(r.ReadUInt64());
                cells[i] = cell;
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
                    if (grid.TryGetNeighbor(cell.Coord, (HexDirection)d, out HexCoord n) && cells[grid.IndexOf(n)].IsWater)
                    {
                        cell.IsCoastal = true;
                        break;
                    }
                }
            }

            r.ExpectTag(RegionsTag);
            int regionCount = r.ReadCount(grid.CellCount);
            var regions = new StableIdMap<RegionId, StrategicRegion>(regionCount);
            for (int i = 0; i < regionCount; i++)
            {
                var id = new RegionId(r.ReadUInt64());
                string nameKey = r.ReadString();
                bool isSea = r.ReadBool();
                var owner = new CountryId(r.ReadUInt64());
                var controller = new CountryId(r.ReadUInt64());
                int hexCount = r.ReadCount(grid.CellCount);
                var hexes = new List<HexId>(hexCount);
                for (int h = 0; h < hexCount; h++)
                {
                    hexes.Add(new HexId(r.ReadUInt64()));
                }

                regions.Add(id, new StrategicRegion(id, nameKey, isSea, hexes) { Owner = owner, Controller = controller });
            }

            r.ExpectTag(CitiesTag);
            int cityCount = r.ReadCount(grid.CellCount);
            var cities = new StableIdMap<CityId, City>(cityCount);
            var sectors = new StableIdMap<SectorId, CitySector>(cityCount * 4);
            for (int i = 0; i < cityCount; i++)
            {
                var id = new CityId(r.ReadUInt64());
                string nameKey = r.ReadString();
                var hex = new HexId(r.ReadUInt64());
                var tier = (CityTier)r.ReadByte();
                int sectorCount = r.ReadCount(CityLayout.MaxSectors);
                var sectorList = new List<SectorId>(sectorCount);
                for (int s = 0; s < sectorCount; s++)
                {
                    var sectorId = new SectorId(r.ReadUInt64());
                    var slot = (SectorSlot)r.ReadByte();
                    var kind = (SectorKind)r.ReadByte();
                    var owner = new CountryId(r.ReadUInt64());
                    var controller = new CountryId(r.ReadUInt64());
                    int capacity = r.ReadInt32();
                    int used = r.ReadInt32();
                    if ((int)slot > (int)SectorSlot.SouthEast || (int)kind > (int)SectorKind.Fortress || capacity < 0)
                    {
                        return Result<WorldMap>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    sectors.Add(sectorId, new CitySector(sectorId, id, slot, kind, capacity)
                    {
                        Owner = owner,
                        Controller = controller,
                        BuildingSlotsUsed = used,
                    });
                    sectorList.Add(sectorId);
                }

                cities.Add(id, new City(id, nameKey, hex, tier, sectorList));
            }

            var map = new WorldMap(grid, cells, regions, cities, sectors, regionIds, cityIds, sectorIds);
            var violations = new List<string>();
            if (WorldInvariants.Check(map, violations) > 0)
            {
                return Result<WorldMap>.Failure(DomainErrorKeys.KayitBozuk + " / " + violations[0]);
            }

            return Result<WorldMap>.Success(map);
        }
    }
}
