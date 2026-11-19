using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>Liman, filo ve deniz hücresi sorguları (salt okunur).</summary>
    public static class NavalQueries
    {
        /// <summary>Şehrin <see cref="SectorKind.Port"/> sektörü; yoksa <c>false</c>.</summary>
        public static bool TryGetPort(WorldMap map, City city, out CitySector port)
        {
            for (int i = 0; i < city.Sectors.Count; i++)
            {
                CitySector s = map.Sectors[city.Sectors[i]];
                if (s.Kind == SectorKind.Port)
                {
                    port = s;
                    return true;
                }
            }

            port = null;
            return false;
        }

        public static bool HasPort(WorldMap map, HexCell cell)
        {
            return cell.City.IsValid && TryGetPort(map, map.Cities[cell.City], out _);
        }

        /// <summary>Kullanılabilir liman: şehir hücresi, merkez ve liman sektörü ülkenin fiili kontrolünde.</summary>
        public static bool IsUsablePort(WorldMap map, CountryId country, City city)
        {
            return map.Cell(city.Hex).Controller == country && map.AdministrativeSectorOf(city).Controller == country
                && TryGetPort(map, city, out CitySector port) && port.Controller == country;
        }

        /// <summary>Ana liman: kullanılabilir ve hücre ülkenin hukuki sahipliğinde (işgal edilmiş değil).</summary>
        public static bool IsHomePort(WorldMap map, CountryId country, City city)
        {
            return map.Cell(city.Hex).Owner == country && IsUsablePort(map, country, city);
        }

        /// <summary>Filo girebilir mi: her deniz hücresi (düşman filosuyla temas dahil) veya kendi kullanılabilir limanı.</summary>
        public static bool CanFleetEnter(WarfareContext ctx, CountryId country, HexCell cell)
        {
            if (cell.IsWater)
            {
                return true;
            }

            return cell.City.IsValid && IsUsablePort(ctx.World, country, ctx.World.Cities[cell.City]);
        }

        public static bool HasHostileFleets(WarfareContext ctx, HexId hex, CountryId country)
        {
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.Hex == hex && UnitQueries.IsHostile(ctx, f.Owner, country))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasFleetOf(WarfareContext ctx, HexId hex, CountryId country)
        {
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.Hex == hex && f.Owner == country)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Filonun taşıdığı kara birlikleri (kimlik sırasıyla).</summary>
        public static void Carried(WarfareContext ctx, FleetId fleet, List<LandUnit> into)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.Carrier == fleet)
                {
                    into.Add(u);
                }
            }
        }

        public static int CarriedCount(WarfareContext ctx, FleetId fleet)
        {
            int count = 0;
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                if (ctx.Warfare.Units.ValueAt(i).Carrier == fleet)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
