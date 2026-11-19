using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// İkmal kaynakları ve konvoy (P11).
    /// <list type="bullet">
    /// <item>Ana kaynak: hücresi ve merkez sektörü ülkenin hem sahipliğinde hem kontrolünde olan,
    /// kuşatılmamış şehir (seviye 1). İşgal edilmiş şehir kendiliğinden kaynak olmaz.</item>
    /// <item>Konvoy: işgal edilmiş, kullanılabilir ve ablukasız liman, bir ana limandan düşman filosu
    /// bulunmayan deniz yoluyla ulaşılabiliyorsa ve boşta nakliye kapasitesi varsa
    /// <see cref="JointWarfareParameters.ConvoySupplyLevel"/> seviyeli kaynak olur. Konvoy kapasitesi,
    /// ana limanda demirli, yük taşımayan filoların nakliye kapasitesi toplamıdır; her konvoy limanı
    /// bir birim kullanır (limanlar <see cref="CityId"/> sırasıyla).</item>
    /// </list>
    /// </summary>
    public static class ConvoyRules
    {
        public static void CollectSources(WarfareContext ctx, CountryId country, List<SupplySource> into)
        {
            WorldMap map = ctx.World;
            var occupiedPorts = new List<City>();
            for (int c = 0; c < map.Cities.Count; c++)
            {
                City city = map.Cities.ValueAt(c);
                HexCell cell = map.Cell(city.Hex);
                if (cell.Controller != country || map.AdministrativeSectorOf(city).Controller != country || ctx.Warfare.IsBesieged(city.Id))
                {
                    continue;
                }

                if (cell.Owner == country)
                {
                    into.Add(new SupplySource(cell.Coord, Fixed64.One));
                }
                else if (NavalQueries.IsUsablePort(map, country, city) && !ctx.Warfare.IsBlockaded(city.Id))
                {
                    occupiedPorts.Add(city);
                }
            }

            if (occupiedPorts.Count == 0)
            {
                return;
            }

            int capacity = ConvoyCapacity(ctx, country);
            if (capacity <= 0)
            {
                return;
            }

            bool[] reachable = ReachableSea(ctx, country);
            for (int i = 0; i < occupiedPorts.Count && capacity > 0; i++)
            {
                if (TouchesReachableSea(ctx, occupiedPorts[i], reachable))
                {
                    into.Add(new SupplySource(map.Grid.FromId(occupiedPorts[i].Hex), ctx.Catalog.Joint.ConvoySupplyLevel));
                    capacity--;
                }
            }
        }

        /// <summary>Ana limanda demirli, yük taşımayan filoların nakliye kapasitesi toplamı.</summary>
        public static int ConvoyCapacity(WarfareContext ctx, CountryId country)
        {
            int capacity = 0;
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.Owner != country)
                {
                    continue;
                }

                ShipTypeDefinition type = ctx.Catalog.ShipTypes[f.Type];
                HexCell cell = ctx.World.Cell(f.Hex);
                if (type.TransportCapacity > 0 && cell.City.IsValid && NavalQueries.IsHomePort(ctx.World, country, ctx.World.Cities[cell.City])
                    && !ctx.Warfare.IsBlockaded(cell.City) && NavalQueries.CarriedCount(ctx, f.Id) == 0)
                {
                    capacity += type.TransportCapacity;
                }
            }

            return capacity;
        }

        /// <summary>Ana limanlardan (ablukasız) düşman filosu olmayan deniz hücreleri üzerinden erişilebilen denizler.</summary>
        public static bool[] ReachableSea(WarfareContext ctx, CountryId country)
        {
            WorldMap map = ctx.World;
            var reached = new bool[map.CellCount];
            var queue = new Queue<int>();

            // Düşman filolu hücreler bir kez işaretlenir (hücre başına filo taraması yerine O(filo)).
            var blocked = new bool[map.CellCount];
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (UnitQueries.IsHostile(ctx, f.Owner, country))
                {
                    blocked[map.Grid.IndexOf(map.Grid.FromId(f.Hex))] = true;
                }
            }

            for (int c = 0; c < map.Cities.Count; c++)
            {
                City city = map.Cities.ValueAt(c);
                if (!NavalQueries.IsHomePort(map, country, city) || ctx.Warfare.IsBlockaded(city.Id))
                {
                    continue;
                }

                HexCell cell = map.Cell(city.Hex);
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    if (map.TryGetNeighborCell(cell, (HexDirection)d, out HexCell n))
                    {
                        Visit(ctx, n, blocked, reached, queue);
                    }
                }
            }

            while (queue.Count > 0)
            {
                HexCell cell = map.CellAt(queue.Dequeue());
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    if (map.TryGetNeighborCell(cell, (HexDirection)d, out HexCell n))
                    {
                        Visit(ctx, n, blocked, reached, queue);
                    }
                }
            }

            return reached;
        }

        private static void Visit(WarfareContext ctx, HexCell cell, bool[] blocked, bool[] reached, Queue<int> queue)
        {
            int index = ctx.World.Grid.IndexOf(cell.Coord);
            if (reached[index] || !cell.IsWater || blocked[index])
            {
                return;
            }

            reached[index] = true;
            queue.Enqueue(index);
        }

        private static bool TouchesReachableSea(WarfareContext ctx, City city, bool[] reachable)
        {
            HexCell cell = ctx.World.Cell(city.Hex);
            for (int d = 0; d < HexDirections.Count; d++)
            {
                if (ctx.World.TryGetNeighborCell(cell, (HexDirection)d, out HexCell n) && n.IsWater && reachable[ctx.World.Grid.IndexOf(n.Coord)])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
