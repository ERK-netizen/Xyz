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
    /// komşuları kontrolcüsüne düşman ülkenin elinde veya düşman birliği barındırıyorsa ve deniz
    /// yolu kapalıysa (limanı yok, liman sektörü elden çıkmış veya liman ablukada) şehir kuşatma
    /// altındadır (ikmal kaynağı ve hava üssü olamaz, garnizonu zayıflar). Kara komşusu olmayan
    /// ada şehri yalnız ablukayla kuşatılır.
    /// </summary>
    public static class WarfareControlRules
    {
        public static void Occupy(WarfareContext ctx)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit unit = ctx.Warfare.Units.ValueAt(i);
                if (unit.IsEmbarked)
                {
                    continue;
                }

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

                bool landClosed = land == 0 || hostile == land;
                bool seaOpen = NavalQueries.IsUsablePort(ctx.World, holder, city) && !ctx.Warfare.IsBlockaded(city.Id);
                if (landClosed && !seaOpen && (land > 0 || ctx.Warfare.IsBlockaded(city.Id)))
                {
                    besieged.Add(city.Id);
                }
            }
        }

        /// <summary>
        /// Liman şehri, komşu deniz hücrelerinden birinde kontrolcüsüne düşman filo varken
        /// kontrolcünün komşu denizde veya limanda filosu yoksa ablukadadır. Abluka konvoyu ve deniz
        /// yoluyla kuşatma kırmayı engeller. <see cref="UpdateSieges"/>'ten önce çağrılır.
        /// </summary>
        public static void UpdateBlockades(WarfareContext ctx)
        {
            List<CityId> blockaded = ctx.Warfare.BlockadedList;
            blockaded.Clear();
            for (int i = 0; i < ctx.World.Cities.Count; i++)
            {
                City city = ctx.World.Cities.ValueAt(i);
                HexCell cell = ctx.World.Cell(city.Hex);
                CountryId holder = cell.Controller;
                if (!holder.IsValid || !NavalQueries.TryGetPort(ctx.World, city, out _))
                {
                    continue;
                }

                bool enemy = false;
                bool own = NavalQueries.HasFleetOf(ctx, cell.Id, holder);
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    if (!ctx.World.TryGetNeighborCell(cell, (HexDirection)d, out HexCell n) || !n.IsWater)
                    {
                        continue;
                    }

                    enemy |= NavalQueries.HasHostileFleets(ctx, n.Id, holder);
                    own |= NavalQueries.HasFleetOf(ctx, n.Id, holder);
                }

                if (enemy && !own)
                {
                    blockaded.Add(city.Id);
                }
            }
        }
    }
}
