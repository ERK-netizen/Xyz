using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Intelligence.Definitions;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Intelligence
{
    /// <summary>
    /// <see cref="ObservedWorld"/> güncellemesi (P14). Görünür düğümler, sensörlerden ulaşım grafının
    /// kenarları boyunca (kara ve deniz) yol uzunluğu yarıçapı aşmayacak kadar yayılarak bulunur
    /// (kenar açılış tarihi görüşü etkilemez). Görünür düğümdeki yabancı varlıklar temas raporu olur:
    /// en yakın sensöre yol uzunluğu <see cref="IntelligenceParameters.ExactRange"/> içindeyse güç
    /// kesin, değilse <see cref="IntelligenceParameters.EstimateStep"/>'e yuvarlanır. Son konumu bugün
    /// görünen ama orada görülmeyen temas silinir; bellek süresini aşan temas unutulur. Ülke kendi
    /// varlıklarını rapor etmez.
    /// </summary>
    public static class IntelligenceRules
    {
        public static void Update(
            GeoWorld world,
            IntelligenceParameters prm,
            ObservedWorld view,
            IReadOnlyList<Sensor> sensors,
            IReadOnlyList<Sighting> sightings,
            GameDate today)
        {
            var nearest = new Dictionary<TransportNodeId, long>();
            ComputeVisible(world, sensors, view.VisibleList, nearest);
            var seenToday = new List<ulong>();
            for (int i = 0; i < sightings.Count; i++)
            {
                Sighting s = sightings[i];
                if (s.Owner == view.Country || !view.IsVisible(s.Node))
                {
                    continue;
                }

                bool exact = nearest.TryGetValue(s.Node, out long distance) && distance <= prm.ExactRange;
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

                report.LastNode = s.Node;
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
                bool vanished = seenToday.BinarySearch(c.SortKey) < 0 && view.IsVisible(c.LastNode);
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

        /// <summary>
        /// Sensör yarıçapları içindeki düğümler (artan, tekrarsız). Her düğüm için kalan en büyük menzil ve
        /// en yakın sensöre yol uzunluğu sabit noktaya kadar gevşetilir; sonuç işlem sırasından bağımsızdır.
        /// <paramref name="nearest"/> görünür düğümlerin en yakın sensör uzaklığını alır.
        /// </summary>
        public static void ComputeVisible(GeoWorld world, IReadOnlyList<Sensor> sensors, List<TransportNodeId> into, Dictionary<TransportNodeId, long> nearest)
        {
            into.Clear();
            nearest.Clear();
            var remaining = new Dictionary<TransportNodeId, long>();
            var queue = new Queue<TransportNodeId>();
            for (int s = 0; s < sensors.Count; s++)
            {
                Sensor sensor = sensors[s];
                if (sensor.RadiusMeters < 0L || !world.Transport.TryGetNode(sensor.Center, out _))
                {
                    continue;
                }

                bool known = remaining.TryGetValue(sensor.Center, out long r);
                if (!known)
                {
                    into.Add(sensor.Center);
                }

                if (!known || r < sensor.RadiusMeters)
                {
                    remaining[sensor.Center] = sensor.RadiusMeters;
                    queue.Enqueue(sensor.Center);
                }

                nearest[sensor.Center] = 0L;
            }

            while (queue.Count > 0)
            {
                TransportNodeId at = queue.Dequeue();
                long left = remaining[at];
                long dist = nearest.TryGetValue(at, out long d) ? d : long.MaxValue;
                IReadOnlyList<TransportEdge> edges = world.Transport.EdgesOf(at);
                for (int e = 0; e < edges.Count; e++)
                {
                    TransportEdge edge = edges[e];
                    long nextLeft = left - edge.LengthMeters;
                    if (nextLeft < 0L)
                    {
                        continue;
                    }

                    TransportNodeId other = edge.Other(at);
                    bool improved = false;
                    bool seen = remaining.TryGetValue(other, out long known);
                    if (!seen)
                    {
                        into.Add(other);
                    }

                    if (!seen || known < nextLeft)
                    {
                        remaining[other] = nextLeft;
                        improved = true;
                    }

                    long nextDist = dist == long.MaxValue ? long.MaxValue : dist + edge.LengthMeters;
                    if (!nearest.TryGetValue(other, out long knownDist) || knownDist > nextDist)
                    {
                        nearest[other] = nextDist;
                        improved = true;
                    }

                    if (improved)
                    {
                        queue.Enqueue(other);
                    }
                }
            }

            into.Sort();
        }
    }
}
