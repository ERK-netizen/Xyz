using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// P14 kontrol: (1) birliğin durduğu şehirsiz düşman/sahipsiz hücre, içinde düşman birliği
    /// yoksa birliğin ülkesinin kontrolüne geçer (sahiplik değişmez); (2) bir şehrin tüm kara
    /// komşuları kontrolcüsüne düşman ülkenin elinde veya düşman birliği barındırıyorsa şehir
    /// kuşatma altındadır (ikmal kaynağı olamaz, garnizonu zayıflar).
    /// </summary>
    public static class WarfareControlRules
    {
        public static void Occupy(WarfareContext ctx)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit unit = ctx.Warfare.Units.ValueAt(i);
                HexCell cell = ctx.World.Cell(unit.Hex);
                if (cell.City.IsValid || cell.Controller == unit.Owner || UnitQueries.HasHostileUnits(ctx, cell.Id, unit.Owner))
                {
                    continue;
                }

                if (!cell.Controller.IsValid || UnitQueries.IsHostile(ctx, unit.Owner, cell.Controller))
                {
                    ctx.World.SetHexController(cell.Coord, unit.Owner);
                }
            }
        }

        public static void UpdateSieges(WarfareContext ctx)
        {
            List<CityId> besieged = ctx.Warfare.BesiegedList;
            besieged.Clear();
            for (int i = 0; i < ctx.World.Cities.Count; i++)
            {
                City city = ctx.World.Cities.ValueAt(i);
                HexCell cell = ctx.World.Cell(city.Hex);
                CountryId holder = cell.Controller;
                if (!holder.IsValid)
                {
                    continue;
                }

                int land = 0;
                int hostile = 0;
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    if (!ctx.World.TryGetNeighborCell(cell, (HexDirection)d, out HexCell n) || n.IsWater)
                    {
                        continue;
                    }

                    land++;
                    if (UnitQueries.IsHostile(ctx, holder, n.Controller) || UnitQueries.HasHostileUnits(ctx, n.Id, holder))
                    {
                        hostile++;
                    }
                }

                if (land > 0 && hostile == land)
                {
                    besieged.Add(city.Id);
                }
            }
        }
    }
}
