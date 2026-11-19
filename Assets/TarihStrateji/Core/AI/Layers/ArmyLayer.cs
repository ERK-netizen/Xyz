using System.Collections.Generic;
using TarihStrateji.Core.AI.Definitions;
using TarihStrateji.Core.AI.Perception;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.AI.Layers
{
    /// <summary>
    /// Ordu katmanı (her gün; birlikler kimlik sırasıyla). Öncelik sırası:
    /// <list type="number">
    /// <item>Saldırı: bugün görülen düşman birliğine komşu, teşkilatı yeterli birliklerin toplam
    /// saldırı gücü, tahmini savunmanın <see cref="AiParameters.AttackPowerRatio"/> katıysa birlikte saldırır.</item>
    /// <item>Şehir taarruzu: komşu, içinde düşman görülmeyen düşman şehrine garnizon × kale eşiğiyle.</item>
    /// <item>Savunma: yakınında düşman görülen, içinde birliğimiz olmayan kendi şehrine en yakın serbest birlik.</item>
    /// <item>İlerleme: savaştaysa emri olmayan birlik en yakın düşman kontrolündeki şehirsiz hücreye
    /// (hedef başına en çok <see cref="AiParameters.UnitsPerAdvanceTarget"/> birlik).</item>
    /// </list>
    /// Zaten aynı hedefe yürüyen/saldıran birliğe yeniden emir verilmez (titreşim önlenir).
    /// Gemideki birlikler bu katmanın dışındadır.
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
            }
        }

        private static bool Fit(AiPerception p, AiParameters prm, LandUnit u)
        {
            UnitTypeDefinition type = p.Warfare.UnitTypes[u.Type];
            return !u.IsEmbarked && u.Organization >= type.MaxOrganization * prm.AttackMinOrganization;
        }

        private static Fixed64 AttackPower(AiPerception p, LandUnit u)
        {
            UnitTypeDefinition type = p.Warfare.UnitTypes[u.Type];
            Fixed64 org = Fixed64.Half + (Fixed64.Half * Fixed64.MulDiv(u.Organization, Fixed64.One, type.MaxOrganization));
            return type.Attack * u.Strength(type) * org;
        }

        private static void Attacks(AiPerception p, AiParameters prm, List<ContactReport> hostile, List<LandUnitId> committed, List<AiIntent> intents)
        {
            WorldMap map = p.Map;
            Fixed64 baseDefense = p.Warfare.UnitTypes[prm.RecruitUnitType].Defense;
            var targets = new List<HexId>();
            for (int i = 0; i < hostile.Count; i++)
            {
                if (!targets.Contains(hostile[i].LastHex))
                {
                    targets.Add(hostile[i].LastHex);
                }
            }

            targets.Sort();
            var group = new List<LandUnit>();
            for (int t = 0; t < targets.Count; t++)
            {
                HexCell cell = map.Cell(targets[t]);
                Fixed64 defense = Fixed64.Zero;
                for (int i = 0; i < hostile.Count; i++)
                {
                    if (hostile[i].LastHex == targets[t])
                    {
                        defense += hostile[i].EstimatedStrength;
                    }
                }

                defense = defense * baseDefense * p.Warfare.Parameters.TerrainDefense(cell.Terrain) * FortressFactor(p, cell);
                group.Clear();
                Fixed64 power = Fixed64.Zero;
                for (int u = 0; u < p.OwnUnits.Count; u++)
                {
                    LandUnit unit = p.OwnUnits[u];
                    if (!committed.Contains(unit.Id) && Fit(p, prm, unit) && Adjacent(map, unit.Hex, targets[t]))
                    {
                        group.Add(unit);
                        power += AttackPower(p, unit);
                    }
                }

                if (group.Count == 0 || power < defense * prm.AttackPowerRatio)
                {
                    continue;
                }

                for (int g = 0; g < group.Count; g++)
                {
                    committed.Add(group[g].Id);
                    if (group[g].AttackTarget != targets[t])
                    {
                        intents.Add(AiIntent.Attack(group[g].Id, map.Grid.FromId(targets[t])));
                    }
                }
            }
        }

        private static void CityAssaults(AiPerception p, AiParameters prm, List<ContactReport> hostile, List<LandUnitId> committed, List<AiIntent> intents)
        {
            WorldMap map = p.Map;
            var group = new List<LandUnit>();
            for (int c = 0; c < map.Cities.Count; c++)
            {
                City city = map.Cities.ValueAt(c);
                HexCell cell = map.Cell(city.Hex);
                if (!p.Diplomacy.AtWar(p.Country, cell.Controller) || HasContactAt(hostile, cell.Id))
                {
                    continue;
                }

                group.Clear();
                Fixed64 power = Fixed64.Zero;
                for (int u = 0; u < p.OwnUnits.Count; u++)
                {
                    LandUnit unit = p.OwnUnits[u];
                    if (!committed.Contains(unit.Id) && Fit(p, prm, unit) && Adjacent(map, unit.Hex, cell.Id))
                    {
                        group.Add(unit);
                        power += AttackPower(p, unit);
                    }
                }

                Fixed64 garrison = p.Warfare.Parameters.GarrisonDefense * FortressFactor(p, cell);
                if (group.Count == 0 || power < garrison * prm.AttackPowerRatio)
                {
                    continue;
                }

                for (int g = 0; g < group.Count; g++)
                {
                    committed.Add(group[g].Id);
                    if (group[g].AttackTarget != cell.Id)
                    {
                        intents.Add(AiIntent.Attack(group[g].Id, cell.Coord));
                    }
                }
            }
        }

        private static void Defend(AiPerception p, AiParameters prm, List<ContactReport> hostile, List<LandUnitId> committed, List<AiIntent> intents)
        {
            WorldMap map = p.Map;
            for (int c = 0; c < map.Cities.Count; c++)
            {
                City city = map.Cities.ValueAt(c);
                HexCell cell = map.Cell(city.Hex);
                if (cell.Controller != p.Country || !ThreatNear(map, hostile, cell.Coord, prm.DefendRadius))
                {
                    continue;
                }

                LandUnit best = null;
                int bestSteps = int.MaxValue;
                bool garrisoned = false;
                for (int u = 0; u < p.OwnUnits.Count; u++)
                {
                    LandUnit unit = p.OwnUnits[u];
                    if (unit.IsEmbarked)
                    {
                        continue;
                    }

                    if (unit.Hex == cell.Id || (unit.Path.Count > 0 && unit.Path[unit.Path.Count - 1] == cell.Id))
                    {
                        garrisoned = true;
                        committed.Add(unit.Id);
                        break;
                    }

                    if (committed.Contains(unit.Id))
                    {
                        continue;
                    }

                    int steps = map.Grid.StepDistance(map.Grid.FromId(unit.Hex), cell.Coord);
                    if (steps < bestSteps)
                    {
                        bestSteps = steps;
                        best = unit;
                    }
                }

                if (!garrisoned && best != null)
                {
                    committed.Add(best.Id);
                    intents.Add(AiIntent.Move(best.Id, cell.Coord));
                }
            }
        }

        private static void Advance(AiPerception p, AiParameters prm, List<ContactReport> hostile, List<LandUnitId> committed, List<AiIntent> intents)
        {
            WorldMap map = p.Map;
            var assigned = new List<HexId>();
            bool[] visited = p.Scratch != null && p.Scratch.Length == map.CellCount ? p.Scratch : new bool[map.CellCount];
            var frontier = new List<int>();
            var next = new List<int>();
            var touched = new List<int>();
            for (int u = 0; u < p.OwnUnits.Count; u++)
            {
                LandUnit unit = p.OwnUnits[u];
                if (committed.Contains(unit.Id) || !Fit(p, prm, unit) || unit.Path.Count > 0 || unit.AttackTarget.IsValid)
                {
                    continue;
                }

                HexId target = NearestHostileCell(p, prm, unit, hostile, assigned, visited, frontier, next, touched);
                if (target.IsValid)
                {
                    assigned.Add(target);
                    committed.Add(unit.Id);
                    intents.Add(AiIntent.Move(unit.Id, map.Grid.FromId(target)));
                }
            }
        }

        private static HexId NearestHostileCell(AiPerception p, AiParameters prm, LandUnit unit, List<ContactReport> hostile, List<HexId> assigned,
            bool[] visited, List<int> frontier, List<int> next, List<int> touched)
        {
            HexGrid grid = p.Map.Grid;
            HexId found = HexId.None;
            frontier.Clear();
            touched.Clear();
            int start = grid.IndexOf(grid.FromId(unit.Hex));
            visited[start] = true;
            touched.Add(start);
            frontier.Add(start);
            for (int ring = 0; ring < prm.AdvanceSearchRadius && frontier.Count > 0 && !found.IsValid; ring++)
            {
                next.Clear();
                for (int f = 0; f < frontier.Count && !found.IsValid; f++)
                {
                    HexCoord at = grid.FromIndex(frontier[f]);
                    for (int d = 0; d < HexDirections.Count; d++)
                    {
                        if (!grid.TryGetNeighbor(at, (HexDirection)d, out HexCoord n))
                        {
                            continue;
                        }

                        int ni = grid.IndexOf(n);
                        HexCell cell = p.Map.CellAt(ni);
                        if (visited[ni] || cell.IsWater)
                        {
                            continue;
                        }

                        visited[ni] = true;
                        touched.Add(ni);
                        next.Add(ni);
                        if (!cell.City.IsValid && p.Diplomacy.AtWar(p.Country, cell.Controller) && !HasContactAt(hostile, cell.Id)
                            && Count(assigned, cell.Id) < prm.UnitsPerAdvanceTarget)
                        {
                            found = cell.Id;
                            break;
                        }
                    }
                }

                frontier.Clear();
                frontier.AddRange(next);
            }

            for (int i = 0; i < touched.Count; i++)
            {
                visited[touched[i]] = false;
            }

            return found;
        }

        private static Fixed64 FortressFactor(AiPerception p, HexCell cell)
        {
            Fixed64 factor = Fixed64.One;
            if (!cell.City.IsValid)
            {
                return factor;
            }

            City city = p.Map.Cities[cell.City];
            for (int i = 0; i < city.Sectors.Count; i++)
            {
                CitySector s = p.Map.Sectors[city.Sectors[i]];
                if (s.Kind == SectorKind.Fortress && s.Controller == cell.Controller)
                {
                    factor += p.Warfare.Parameters.FortressBonusPerSector;
                }
            }

            return factor;
        }

        private static bool ThreatNear(WorldMap map, List<ContactReport> hostile, HexCoord at, int radius)
        {
            for (int i = 0; i < hostile.Count; i++)
            {
                if (map.Grid.StepDistance(map.Grid.FromId(hostile[i].LastHex), at) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasContactAt(List<ContactReport> hostile, HexId hex)
        {
            for (int i = 0; i < hostile.Count; i++)
            {
                if (hostile[i].LastHex == hex)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Count(List<HexId> list, HexId id)
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

        private static bool Adjacent(WorldMap map, HexId a, HexId b)
        {
            return map.Grid.StepDistance(map.Grid.FromId(a), map.Grid.FromId(b)) == 1;
        }
    }
}
