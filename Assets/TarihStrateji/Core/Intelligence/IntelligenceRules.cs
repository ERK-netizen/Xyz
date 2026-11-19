using System.Collections.Generic;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Intelligence.Definitions;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Intelligence
{
    /// <summary>
    /// <see cref="ObservedWorld"/> güncellemesi (P14). Görünür hücreler, sensörlerden altıgen
    /// adımıyla yarıçap kadar yayılarak bulunur. Görünür hücredeki yabancı varlıklar temas
    /// raporu olur: en yakın sensöre <see cref="IntelligenceParameters.ExactRange"/> içindeyse güç
    /// kesin, değilse <see cref="IntelligenceParameters.EstimateStep"/>'e yuvarlanır. Son konumu
    /// bugün görünen ama orada görülmeyen temas silinir (gittiği görülür); bellek süresini aşan
    /// temas unutulur. Ülke kendi varlıklarını rapor etmez (zaten bilir).
    /// </summary>
    public static class IntelligenceRules
    {
        public static void Update(
            WorldMap world,
            IntelligenceParameters prm,
            ObservedWorld view,
            IReadOnlyList<Sensor> sensors,
            IReadOnlyList<Sighting> sightings,
            GameDate today,
            int[] scratch)
        {
            ComputeVisible(world, sensors, view.VisibleList, scratch);
            var seenToday = new List<ulong>();
            for (int i = 0; i < sightings.Count; i++)
            {
                Sighting s = sightings[i];
                if (s.Owner == view.Country || !world.Grid.TryFromId(s.Hex, out HexCoord at) || !view.IsVisible(world.Grid.IndexOf(at)))
                {
                    continue;
                }

                bool exact = NearestSensorSteps(world, sensors, at) <= prm.ExactRange;
                ulong key = ((ulong)s.Kind << 56) | s.EntityId;
                int index = view.IndexOf(key);
                ContactReport report;
                if (index >= 0)
                {
                    report = view.ContactList[index];
                }
                else
                {
                    report = new ContactReport(s.Kind, s.EntityId, s.Owner);
                    view.ContactList.Insert(~index, report);
                }

                report.LastHex = s.Hex;
                report.EstimatedStrength = exact ? s.Strength : Estimate(s.Strength, prm.EstimateStep);
                report.IsExact = exact;
                report.SeenOn = today;
                seenToday.Add(key);
            }

            seenToday.Sort();
            for (int i = view.ContactList.Count - 1; i >= 0; i--)
            {
                ContactReport c = view.ContactList[i];
                bool stale = today.DayNumber - c.SeenOn.DayNumber >= prm.ContactMemoryDays;
                bool vanished = seenToday.BinarySearch(c.SortKey) < 0 && world.Grid.TryFromId(c.LastHex, out HexCoord last) && view.IsVisible(world.Grid.IndexOf(last));
                if (stale || vanished)
                {
                    view.ContactList.RemoveAt(i);
                }
            }
        }

        /// <summary>Gücü adıma yuvarlar (en yakına), [adım, 1] aralığında; güç 0 ise 0.</summary>
        public static Fixed64 Estimate(Fixed64 strength, Fixed64 step)
        {
            if (!strength.IsPositive)
            {
                return Fixed64.Zero;
            }

            Fixed64 steps = Fixed64.Round(Fixed64.MulDiv(strength, Fixed64.One, step));
            return Fixed64.Clamp(steps * step, step, Fixed64.One);
        }

        /// <summary>Sensör yarıçapları içindeki hücre dizinleri (artan, tekrarsız).</summary>
        public static void ComputeVisible(WorldMap world, IReadOnlyList<Sensor> sensors, List<int> into, int[] scratch)
        {
            into.Clear();
            HexGrid grid = world.Grid;
            var frontier = new List<int>();
            var next = new List<int>();

            // scratch: hücre başına "bu hücreye ulaşan en büyük kalan yarıçap + 1" (0 = hiç).
            for (int s = 0; s < sensors.Count; s++)
            {
                Sensor sensor = sensors[s];
                if (!grid.Contains(sensor.Center) || sensor.Radius < 0)
                {
                    continue;
                }

                int start = grid.IndexOf(sensor.Center);
                if (scratch[start] >= sensor.Radius + 1)
                {
                    continue;
                }

                if (scratch[start] == 0)
                {
                    into.Add(start);
                }

                scratch[start] = sensor.Radius + 1;
                frontier.Clear();
                frontier.Add(start);
                for (int ring = sensor.Radius; ring > 0 && frontier.Count > 0; ring--)
                {
                    next.Clear();
                    for (int f = 0; f < frontier.Count; f++)
                    {
                        HexCoord c = grid.FromIndex(frontier[f]);
                        for (int d = 0; d < HexDirections.Count; d++)
                        {
                            if (!grid.TryGetNeighbor(c, (HexDirection)d, out HexCoord n))
                            {
                                continue;
                            }

                            int ni = grid.IndexOf(n);
                            if (scratch[ni] >= ring)
                            {
                                continue;
                            }

                            if (scratch[ni] == 0)
                            {
                                into.Add(ni);
                            }

                            scratch[ni] = ring;
                            next.Add(ni);
                        }
                    }

                    frontier.Clear();
                    frontier.AddRange(next);
                }
            }

            for (int i = 0; i < into.Count; i++)
            {
                scratch[into[i]] = 0;
            }

            into.Sort();
        }

        private static int NearestSensorSteps(WorldMap world, IReadOnlyList<Sensor> sensors, HexCoord at)
        {
            int best = int.MaxValue;
            for (int i = 0; i < sensors.Count; i++)
            {
                int d = world.Grid.StepDistance(sensors[i].Center, at);
                if (d < best)
                {
                    best = d;
                }
            }

            return best;
        }
    }
}
