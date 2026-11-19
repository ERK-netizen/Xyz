using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;

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
                    || !map.Grid.TryFromId(u.Hex, out _) || map.Cell(u.Hex).IsWater)
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                    continue;
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
                    if (v.Hex == u.Hex && UnitQueries.IsHostile(ctx, u.Owner, v.Owner))
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

            return violations.Count - before;
        }

        private static bool OutOfUnit(Fixed64 v) => v.IsNegative || v > Fixed64.One;
    }
}
