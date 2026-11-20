using System.Collections.Generic;
using TarihStrateji.Core.AI.Definitions;
using TarihStrateji.Core.AI.Perception;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.AI.Layers
{
    /// <summary>
    /// Ordu katmanı (her gün; birlikler kimlik sırasıyla), ulaşım grafında (ADR-011). Öncelik sırası:
    /// <list type="number">
    /// <item>Saldırı: bugün görülen düşman birliğinin düğümüne kara kenarıyla bitişik (ya da aynı düğümdeki),
    /// teşkilatı yeterli birliklerin toplam saldırı gücü tahmini savunmanın
    /// <see cref="AiParameters.AttackPowerRatio"/> katıysa birlikte saldırır.</item>
    /// <item>Şehir taarruzu: bitişik, içinde düşman görülmeyen düşman kontrolündeki şehre garnizon ×
    /// tahkimat eşiğiyle.</item>
    /// <item>Savunma: <see cref="AiParameters.DefendRadius"/> içinde düşman görülen, birliğimiz olmayan
    /// kendi şehrine kuş uçuşu en yakın serbest birlik.</item>
    /// <item>İlerleme: savaştaysa emri olmayan birlik en yakın uygun düğüme yürür: düşman kontrolündeki
    /// bölgenin şehirsiz kara düğümü ya da düşman şehrine bitişik düğüm (hedef başına en çok
    /// <see cref="AiParameters.UnitsPerAdvanceTarget"/> birlik).</item>
    /// <item>Takviye: kalan serbest birlikler düşmana bitişik kendi cephe düğümünde yığınak yapar.</item>
    /// </list>
    /// Yalnız düğümde duran (yol ortasında olmayan) birlikler saldırır. Zaten aynı hedefe yürüyen/saldıran
    /// birliğe yeniden emir verilmez (titreşim önlenir). Gemideki birlikler bu katmanın dışındadır.
    /// Eşitlikler kimlik sırasıyla bozulur; rastgelelik yoktur.
    /// </summary>
    public static class ArmyLayer
    {
        public static void Decide(AiPerception p, AiParameters prm, StrategicAssessment a, List<AiIntent> intents)
        {
            if (p.OwnUnits.Count == 0)
            {
                return;
            }

            var committed = new List<LandUnitId>();
            var hostileSeen = new List<ContactReport>();
            for (int i = 0; i < p.Observed.Contacts.Count; i++)
            {
                ContactReport c = p.Observed.Contacts[i];
                if (c.Kind == SightingKind.LandUnit && c.SeenOn == p.Today && p.Diplomacy.AtWar(p.Country, c.Owner))
                {
                    hostileSeen.Add(c);
                }
            }

            Attacks(p, prm, hostileSeen, committed, intents);
            if (a.AtWar)
            {
                CityAssaults(p, prm, hostileSeen, committed, intents);
            }

            Defend(p, prm, hostileSeen, committed, intents);
            if (a.AtWar)
            {
                Advance(p, prm, hostileSeen, committed, intents);
                Reinforce(p, prm, hostileSeen, committed, intents);
            }
        }

        /// <summary>
        /// Takviye: emir almamış birlikler, bugün düşman görülen bir düğüme kara kenarıyla bitişik kendi
        /// kontrolündeki en yakın cephe düğümüne yürür (yığınak; sayı sınırı yok). Cephede duran birlik kalır.
        /// </summary>
        private static void Reinforce(AiPerception p, AiParameters prm, List<ContactReport> hostile, List<LandUnitId> committed, List<AiIntent> intents)
        {
            var front = new List<TransportNodeId>();
            IReadOnlyList<TransportNodeId> nodes = p.World.Transport.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                p.World.Transport.TryGetNode(nodes[i], out TransportNode node);
                if (!node.IsLand || HasContactAt(hostile, node.Id) || !p.World.TryGetControl(node.Region, out RegionControl control)
                    || control.Controller != p.Country)
                {
                    continue;
                }

                IReadOnlyList<TransportEdge> edges = p.World.Transport.EdgesOf(node.Id);
                for (int e = 0; e < edges.Count; e++)
                {
                    if (!edges[e].IsNaval && HasContactAt(hostile, edges[e].Other(node.Id)))
                    {
                        front.Add(node.Id);
                        break;
                    }
                }
            }

            if (front.Count == 0)
            {
                return;
            }

            for (int u = 0; u < p.OwnUnits.Count; u++)
            {
                LandUnit unit = p.OwnUnits[u];
                if (committed.Contains(unit.Id) || unit.IsEmbarked || unit.Route.Count > 0 || unit.AttackTarget.IsValid
                    || front.Contains(unit.Node) || !p.World.Transport.TryGetNode(unit.Node, out TransportNode at))
                {
                    continue;
                }

                TransportNodeId best = TransportNodeId.None;
                long bestDistance = long.MaxValue;
                for (int i = 0; i < front.Count; i++)
                {
                    p.World.Transport.TryGetNode(front[i], out TransportNode node);
                    long distance = GeoMath.DistanceMeters(at.Location, node.Location);
                    if (distance <= prm.AdvanceSearchRadius && distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = node.Id;
                    }
                }

                if (best.IsValid)
                {
                    committed.Add(unit.Id);
                    p.World.Transport.TryGetNode(best, out TransportNode target);
                    City city = CityAt(p.World, best);
                    intents.Add(AiIntent.Move(unit.Id, city != null ? TargetRef.Entity(TargetKind.City, city.Id.Value) : TargetRef.AtPoint(target.Location)));
                }
            }
        }

        private static bool Fit(AiPerception p, AiParameters prm, LandUnit u)
        {
            UnitTypeDefinition type = p.Warfare.UnitTypes[u.Type];
            return !u.IsEmbarked && u.RouteProgress == 0L && u.Organization >= type.MaxOrganization * prm.AttackMinOrganization;
        }

        private static Fixed64 AttackPower(AiPerception p, LandUnit u)
        {
            UnitTypeDefinition type = p.Warfare.UnitTypes[u.Type];
            Fixed64 org = Fixed64.Half + (Fixed64.Half * Fixed64.MulDiv(u.Organization, Fixed64.One, type.MaxOrganization));
            return type.Attack * u.Strength(type) * org;
        }

        private static void Attacks(AiPerception p, AiParameters prm, List<ContactReport> hostile, List<LandUnitId> committed, List<AiIntent> intents)
        {
            Fixed64 baseDefense = p.Warfare.UnitTypes[prm.RecruitUnitType].Defense;
            var targets = new List<TransportNodeId>();
            for (int i = 0; i < hostile.Count; i++)
            {
                if (!targets.Contains(hostile[i].LastNode))
                {
                    targets.Add(hostile[i].LastNode);
                }
            }

            targets.Sort();
            var group = new List<LandUnit>();
            for (int t = 0; t < targets.Count; t++)
            {
                Fixed64 defense = Fixed64.Zero;
                ulong victim = 0UL;
                for (int i = 0; i < hostile.Count; i++)
                {
                    if (hostile[i].LastNode == targets[t])
                    {
                        defense += hostile[i].EstimatedStrength;
                        if (victim == 0UL || hostile[i].EntityId < victim)
                        {
                            victim = hostile[i].EntityId;
                        }
                    }
                }

                City city = CityAt(p.World, targets[t]);
                defense = defense * baseDefense * Fortification(city);
                group.Clear();
                Fixed64 power = Fixed64.Zero;
                for (int u = 0; u < p.OwnUnits.Count; u++)
                {
                    LandUnit unit = p.OwnUnits[u];
                    if (!committed.Contains(unit.Id) && Fit(p, prm, unit) && Adjacent(p.World, unit.Node, targets[t]))
                    {
                        group.Add(unit);
                        power += AttackPower(p, unit);
                    }
                }

                if (group.Count == 0 || power < defense * prm.AttackPowerRatio)
                {
                    continue;
                }

                TargetRef target = TargetRef.Entity(TargetKind.LandUnit, victim);
                for (int g = 0; g < group.Count; g++)
                {
                    committed.Add(group[g].Id);
                    if (!group[g].AttackTarget.Equals(target))
                    {
                        intents.Add(AiIntent.Attack(group[g].Id, target));
                    }
                }
            }
        }

        private static void CityAssaults(AiPerception p, AiParameters prm, List<ContactReport> hostile, List<LandUnitId> committed, List<AiIntent> intents)
        {
            var group = new List<LandUnit>();
            for (int c = 0; c < p.World.Cities.Count; c++)
            {
                p.World.TryGetCity(p.World.Cities[c], out City city);
                if (!p.Diplomacy.AtWar(p.Country, city.Controller) || HasContactAt(hostile, city.Node))
                {
                    continue;
                }

                group.Clear();
                Fixed64 power = Fixed64.Zero;
                for (int u = 0; u < p.OwnUnits.Count; u++)
                {
                    LandUnit unit = p.OwnUnits[u];
                    if (!committed.Contains(unit.Id) && Fit(p, prm, unit) && Adjacent(p.World, unit.Node, city.Node))
                    {
                        group.Add(unit);
                        power += AttackPower(p, unit);
                    }
                }

                Fixed64 garrison = p.Warfare.Parameters.GarrisonDefense * Fortification(city);
                if (group.Count == 0 || power < garrison * prm.AttackPowerRatio)
                {
                    continue;
                }

                TargetRef target = TargetRef.Entity(TargetKind.City, city.Id.Value);
                for (int g = 0; g < group.Count; g++)
                {
                    committed.Add(group[g].Id);
                    if (!group[g].AttackTarget.Equals(target))
                    {
                        intents.Add(AiIntent.Attack(group[g].Id, target));
                    }
                }
            }
        }

        private static void Defend(AiPerception p, AiParameters prm, List<ContactReport> hostile, List<LandUnitId> committed, List<AiIntent> intents)
        {
            for (int c = 0; c < p.World.Cities.Count; c++)
            {
                p.World.TryGetCity(p.World.Cities[c], out City city);
                if (city.Controller != p.Country || !ThreatNear(p.World, hostile, city.Location, prm.DefendRadius))
                {
                    continue;
                }

                LandUnit best = null;
                long bestDistance = long.MaxValue;
                bool garrisoned = false;
                for (int u = 0; u < p.OwnUnits.Count; u++)
                {
                    LandUnit unit = p.OwnUnits[u];
                    if (unit.IsEmbarked)
                    {
                        continue;
                    }

                    if ((unit.Node == city.Node && unit.Route.Count == 0) || RouteEnd(p.World, unit) == city.Node)
                    {
                        garrisoned = true;
                        committed.Add(unit.Id);
                        break;
                    }

                    if (committed.Contains(unit.Id) || !p.World.Transport.TryGetNode(unit.Node, out TransportNode at))
                    {
                        continue;
                    }

                    long distance = GeoMath.DistanceMeters(at.Location, city.Location);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = unit;
                    }
                }

                if (!garrisoned && best != null)
                {
                    committed.Add(best.Id);
                    intents.Add(AiIntent.Move(best.Id, TargetRef.Entity(TargetKind.City, city.Id.Value)));
                }
            }
        }

        private static void Advance(AiPerception p, AiParameters prm, List<ContactReport> hostile, List<LandUnitId> committed, List<AiIntent> intents)
        {
            var candidates = new List<TransportNodeId>();
            Candidates(p, hostile, candidates);
            if (candidates.Count == 0)
            {
                return;
            }

            var assigned = new List<TransportNodeId>();
            for (int u = 0; u < p.OwnUnits.Count; u++)
            {
                LandUnit unit = p.OwnUnits[u];
                if (committed.Contains(unit.Id) || !Fit(p, prm, unit) || unit.Route.Count > 0 || unit.AttackTarget.IsValid
                    || !p.World.Transport.TryGetNode(unit.Node, out TransportNode at))
                {
                    continue;
                }

                TransportNodeId best = TransportNodeId.None;
                GeoPoint bestPoint = default;
                long bestDistance = long.MaxValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i] == unit.Node || Count(assigned, candidates[i]) >= prm.UnitsPerAdvanceTarget)
                    {
                        continue;
                    }

                    p.World.Transport.TryGetNode(candidates[i], out TransportNode node);
                    long distance = GeoMath.DistanceMeters(at.Location, node.Location);
                    if (distance <= prm.AdvanceSearchRadius && distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = node.Id;
                        bestPoint = node.Location;
                    }
                }

                if (best.IsValid)
                {
                    assigned.Add(best);
                    committed.Add(unit.Id);
                    intents.Add(AiIntent.Move(unit.Id, TargetRef.AtPoint(bestPoint)));
                }
            }
        }

        /// <summary>
        /// İlerleme hedefleri (düğüm kimliği sırasıyla): düşman kontrolündeki bölgenin şehirsiz kara düğümleri ve
        /// düşman kontrolündeki şehirlere kara kenarıyla bitişik, düşman şehri olmayan düğümler; bugün düşman
        /// görülen düğümler hariç.
        /// </summary>
        private static void Candidates(AiPerception p, List<ContactReport> hostile, List<TransportNodeId> into)
        {
            IReadOnlyList<TransportNodeId> nodes = p.World.Transport.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                p.World.Transport.TryGetNode(nodes[i], out TransportNode node);
                if (!node.IsLand || HasContactAt(hostile, node.Id))
                {
                    continue;
                }

                City city = CityAt(p.World, node.Id);
                if (city != null)
                {
                    continue;
                }

                if (p.World.TryGetControl(node.Region, out RegionControl control) && p.Diplomacy.AtWar(p.Country, control.Controller))
                {
                    into.Add(node.Id);
                    continue;
                }

                IReadOnlyList<TransportEdge> edges = p.World.Transport.EdgesOf(node.Id);
                for (int e = 0; e < edges.Count; e++)
                {
                    City next = edges[e].IsNaval ? null : CityAt(p.World, edges[e].Other(node.Id));
                    if (next != null && p.Diplomacy.AtWar(p.Country, next.Controller) && LandAccessible(p, node))
                    {
                        into.Add(node.Id);
                        break;
                    }
                }
            }
        }

        /// <summary>Yürünebilir: kendi kontrolündeki ya da savaşta olunan ülkenin bölgesi (tarafsız toprak kapalı).</summary>
        private static bool LandAccessible(AiPerception p, TransportNode node)
        {
            return p.World.TryGetControl(node.Region, out RegionControl control)
                && (control.Controller == p.Country || p.Diplomacy.AtWar(p.Country, control.Controller));
        }

        private static City CityAt(GeoWorld world, TransportNodeId node)
        {
            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City city);
                if (city.Node == node)
                {
                    return city;
                }
            }

            return null;
        }

        private static Fixed64 Fortification(City city) =>
            city != null && city.Fortification > Fixed64.One ? city.Fortification : Fixed64.One;

        private static TransportNodeId RouteEnd(GeoWorld world, LandUnit unit)
        {
            TransportNodeId at = unit.Node;
            for (int i = 0; i < unit.Route.Count; i++)
            {
                if (!world.Transport.TryGetEdge(unit.Route[i], out TransportEdge edge))
                {
                    return TransportNodeId.None;
                }

                at = edge.Other(at);
            }

            return unit.Route.Count == 0 ? TransportNodeId.None : at;
        }

        private static bool ThreatNear(GeoWorld world, List<ContactReport> hostile, GeoPoint at, long radius)
        {
            for (int i = 0; i < hostile.Count; i++)
            {
                if (world.Transport.TryGetNode(hostile[i].LastNode, out TransportNode node) && GeoMath.DistanceMeters(node.Location, at) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasContactAt(List<ContactReport> hostile, TransportNodeId node)
        {
            for (int i = 0; i < hostile.Count; i++)
            {
                if (hostile[i].LastNode == node)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Count(List<TransportNodeId> list, TransportNodeId id)
        {
            int n = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>Aynı düğüm ya da tek kara kenarıyla (deniz yolu hariç) bitişik.</summary>
        private static bool Adjacent(GeoWorld world, TransportNodeId a, TransportNodeId b)
        {
            if (a == b)
            {
                return true;
            }

            IReadOnlyList<TransportEdge> edges = world.Transport.EdgesOf(a);
            for (int i = 0; i < edges.Count; i++)
            {
                if (!edges[i].IsNaval && edges[i].Other(a) == b)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
