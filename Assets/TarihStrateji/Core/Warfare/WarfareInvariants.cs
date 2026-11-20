using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare
{
    /// <summary>
    /// Savaş değişmezleri (ADR-011): tür tanımlı, konum düğümü grafta, rota kenarları geçerli ve
    /// ilerleme ilk kenarın içinde; kara birliği kara düğümünde (gemide değilse), gemideki birlik
    /// taşıyan filonun konumunda; filo deniz/liman düğümünde ve taşıma kapasitesi aşılmamış; kanat
    /// üssü var ve görev ile hedef birlikte. Değerler aralıkta; abluka listesi sıralı.
    /// </summary>
    public static class WarfareInvariants
    {
        public static int Check(GeoWarfareContext ctx, List<string> violations)
        {
            int before = violations.Count;
            GeoWorld world = ctx.World;
            WarfareState state = ctx.Warfare;
            for (int i = 0; i < state.Units.Count; i++)
            {
                LandUnit u = state.Units.ValueAt(i);
                string at = " @ " + u.Id.Value.ToString(CultureInfo.InvariantCulture);
                if (!ctx.Catalog.UnitTypes.TryGetValue(u.Type, out UnitTypeDefinition type) || !u.Owner.IsValid
                    || !world.Transport.TryGetNode(u.Node, out TransportNode node) || !RouteValid(world, u.Node, u.Route, u.RouteProgress))
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                    continue;
                }

                if (u.IsEmbarked)
                {
                    if (!state.Fleets.TryGetValue(u.Carrier, out Fleet carrier) || carrier.Owner != u.Owner || carrier.Node != u.Node
                        || u.Route.Count > 0 || u.RouteProgress != 0L)
                    {
                        violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                    }
                }
                else if (!node.IsLand)
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                }

                if (u.Manpower < 0L || u.Manpower > type.Manpower || OutOfUnit(u.Equipment) || OutOfUnit(u.Supply)
                    || u.Organization.IsNegative || u.Organization > type.MaxOrganization || u.ActionPoints.IsNegative
                    || u.ActionPoints > type.ActionPointsPerDay)
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                }
            }

            for (int i = 0; i < state.Blockaded.Count; i++)
            {
                if (!world.TryGetCity(state.Blockaded[i], out _) || (i > 0 && state.Blockaded[i - 1] >= state.Blockaded[i]))
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + " @ abluka");
                }
            }

            CheckFleets(ctx, violations);
            CheckAirWings(ctx, violations);
            return violations.Count - before;
        }

        private static void CheckFleets(GeoWarfareContext ctx, List<string> violations)
        {
            GeoWorld world = ctx.World;
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                string at = " @ filo " + f.Id.Value.ToString(CultureInfo.InvariantCulture);
                if (!ctx.Catalog.ShipTypes.TryGetValue(f.Type, out ShipTypeDefinition type) || !f.Owner.IsValid
                    || !world.Transport.TryGetNode(f.Node, out TransportNode node) || !node.IsNaval
                    || !RouteValid(world, f.Node, f.Route, f.RouteProgress)
                    || f.Manpower < 0L || f.Manpower > type.Crew || OutOfUnit(f.Equipment)
                    || f.Organization.IsNegative || f.Organization > type.MaxOrganization
                    || GeoAmphibiousRules.CarriedBy(ctx, f.Id) > type.TransportCapacity)
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                }
            }
        }

        private static void CheckAirWings(GeoWarfareContext ctx, List<string> violations)
        {
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                string at = " @ kanat " + w.Id.Value.ToString(CultureInfo.InvariantCulture);
                if (!ctx.Catalog.AirWingTypes.TryGetValue(w.Type, out AirWingTypeDefinition type) || !w.Owner.IsValid || !ctx.World.TryGetCity(w.Base, out _)
                    || w.Manpower < 0L || w.Manpower > type.Crew || OutOfUnit(w.Equipment) || w.Organization.IsNegative || w.Organization > type.MaxOrganization
                    || (w.Mission == AirMission.None) != !w.MissionTarget.IsValid)
                {
                    violations.Add(WarfareErrorKeys.DurumTutarsiz + at);
                }
            }
        }

        /// <summary>Rota düğümden başlayan bağlı kenar zinciridir; ilerleme yalnız ilk kenarda ve negatif değildir.</summary>
        private static bool RouteValid(GeoWorld world, TransportNodeId start, IReadOnlyList<TransportEdgeId> route, long progress)
        {
            if (progress < 0L || (route.Count == 0 && progress != 0L))
            {
                return false;
            }

            TransportNodeId at = start;
            for (int i = 0; i < route.Count; i++)
            {
                if (!world.Transport.TryGetEdge(route[i], out TransportEdge edge) || (edge.A != at && edge.B != at))
                {
                    return false;
                }

                at = edge.Other(at);
            }

            return true;
        }

        private static bool OutOfUnit(Fixed64 v) => v.IsNegative || v > Fixed64.One;
    }
}
