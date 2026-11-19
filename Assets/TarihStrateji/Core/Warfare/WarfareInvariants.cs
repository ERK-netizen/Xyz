using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare
{
    /// <summary>
    /// Kara birliği değişmezleri: tür tanımlı, karada, değerler aralıkta, rota hücreleri geçerli;
    /// aynı hücrede birbirine düşman birlik olamaz; kuşatma listesi sıralı ve geçerli.
    /// </summary>
    public static class WarfareInvariants
    {
        public static int Check(WarfareContext ctx, List<string> violations)
        {
            int before = violations.Count;
            WorldMap map = ctx.World;
            WarfareState state = ctx.Warfare;
            for (int i = 0; i < state.Units.Count; i++)
            {
                LandUnit u = state.Units.ValueAt(i);
                string at = " @ " + u.Id.Value.ToString(CultureInfo.InvariantCulture);
                if (!ctx.Catalog.UnitTypes.TryGetValue(u.Type, out UnitTypeDefinition type) || !u.Owner.IsValid
                    || !map.Grid.TryFromId(u.Hex, out _))
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                    continue;
                }

                if (u.IsEmbarked)
                {
                    // Gemideki birlik taşıyan filonun hücresindedir ve yolu yoktur.
                    if (!state.Fleets.TryGetValue(u.Carrier, out Fleet carrier) || carrier.Owner != u.Owner || carrier.Hex != u.Hex || u.Path.Count > 0)
                    {
                        violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                    }
                }
                else if (map.Cell(u.Hex).IsWater)
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                }

                if (u.Manpower < 0L || u.Manpower > type.Manpower || OutOfUnit(u.Equipment) || OutOfUnit(u.Supply)
                    || u.Organization.IsNegative || u.Organization > type.MaxOrganization || u.ActionPoints.IsNegative
                    || u.ActionPoints > type.ActionPointsPerDay || u.MoveBudget.IsNegative)
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                }

                for (int p = 0; p < u.Path.Count; p++)
                {
                    if (!map.Grid.TryFromId(u.Path[p], out _))
                    {
                        violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                    }
                }

                for (int j = i + 1; j < state.Units.Count; j++)
                {
                    LandUnit v = state.Units.ValueAt(j);
                    if (v.Hex == u.Hex && !u.IsEmbarked && !v.IsEmbarked && UnitQueries.IsHostile(ctx, u.Owner, v.Owner))
                    {
                        violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                    }
                }
            }

            for (int i = 0; i < state.Besieged.Count; i++)
            {
                if (!map.Cities.ContainsKey(state.Besieged[i]) || (i > 0 && state.Besieged[i - 1] >= state.Besieged[i]))
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + " @ kusatma");
                }
            }

            for (int i = 0; i < state.Blockaded.Count; i++)
            {
                if (!map.Cities.ContainsKey(state.Blockaded[i]) || (i > 0 && state.Blockaded[i - 1] >= state.Blockaded[i]))
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + " @ abluka");
                }
            }

            CheckFleets(ctx, violations);
            CheckAirWings(ctx, violations);

            return violations.Count - before;
        }

        private static void CheckFleets(WarfareContext ctx, List<string> violations)
        {
            WorldMap map = ctx.World;
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                string at = " @ filo " + f.Id.Value.ToString(CultureInfo.InvariantCulture);
                if (!ctx.Catalog.ShipTypes.TryGetValue(f.Type, out ShipTypeDefinition type) || !f.Owner.IsValid || !map.Grid.TryFromId(f.Hex, out _))
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                    continue;
                }

                HexCell cell = map.Cell(f.Hex);
                if ((!cell.IsWater && !NavalQueries.HasPort(map, cell)) || f.Manpower < 0L || f.Manpower > type.Crew || OutOfUnit(f.Equipment)
                    || f.Organization.IsNegative || f.Organization > type.MaxOrganization || f.MoveBudget.IsNegative
                    || NavalQueries.CarriedCount(ctx, f.Id) > type.TransportCapacity)
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                }

                for (int p = 0; p < f.Path.Count; p++)
                {
                    if (!map.Grid.TryFromId(f.Path[p], out _))
                    {
                        violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                    }
                }
            }
        }

        private static void CheckAirWings(WarfareContext ctx, List<string> violations)
        {
            WorldMap map = ctx.World;
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                string at = " @ kanat " + w.Id.Value.ToString(CultureInfo.InvariantCulture);
                if (!ctx.Catalog.AirWingTypes.TryGetValue(w.Type, out AirWingTypeDefinition type) || !w.Owner.IsValid || !map.Cities.ContainsKey(w.Base)
                    || w.Manpower < 0L || w.Manpower > type.Crew || OutOfUnit(w.Equipment) || w.Organization.IsNegative || w.Organization > type.MaxOrganization
                    || (w.Mission == AirMission.None) != !w.MissionTarget.IsValid || (w.MissionTarget.IsValid && !map.Grid.TryFromId(w.MissionTarget, out _)))
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                }
            }
        }

        private static bool OutOfUnit(Fixed64 v) => v.IsNegative || v > Fixed64.One;
    }
}
