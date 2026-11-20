using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Gemiye binme ve çıkarma (§6, REG-006, MAP-G05): binme yalnız kendi kontrolündeki limanda, filo ile
    /// aynı düğümde ve filo türünün nakliye kapasitesi &gt; 0 ise (dolmamışsa). Gemideki birlik filoyla
    /// taşınır, karada hareket/saldırı yapamaz. Çıkarma hedefi liman şehridir: filo limanda veya limana deniz
    /// yoluyla bitişik düğümdeyse. Kendi/kendi kontrolündeki limana doğrudan iner; düşman limanına çıkarma
    /// <see cref="JointWarfareParameters.AmphibiousAttackFactor"/> ile aynı muharebe çözücüsünde çözülür.
    /// </summary>
    public static class GeoAmphibiousRules
    {
        public static int CarriedBy(GeoWarfareContext ctx, FleetId fleet)
        {
            int count = 0;
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                if (ctx.Warfare.Units.ValueAt(i).Carrier == fleet)
                {
                    count++;
                }
            }

            return count;
        }

        public static ValidationResult CheckEmbark(GeoWarfareContext ctx, CountryId country, LandUnitId unitId, FleetId fleetId)
        {
            ValidationResult owned = GeoUnitRules.CheckOwned(ctx, country, unitId, out LandUnit unit);
            if (!owned.IsValid)
            {
                return owned;
            }

            ValidationResult fleetOwned = GeoNavalRules.CheckOwned(ctx, country, fleetId, out Fleet fleet);
            if (!fleetOwned.IsValid)
            {
                return fleetOwned;
            }

            if (unit.IsEmbarked)
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikGemide);
            }

            ShipTypeDefinition type = ctx.Catalog.ShipTypes[fleet.Type];
            if (type.TransportCapacity <= 0)
            {
                return ValidationResult.Fail(WarfareErrorKeys.NakliyeKapasitesiYok);
            }

            City port = GeoUnitQueries.CityAt(ctx, fleet.Node);
            if (port == null || port.Controller != country || unit.Node != fleet.Node || unit.RouteProgress != 0L || fleet.RouteProgress != 0L)
            {
                return ValidationResult.Fail(WarfareErrorKeys.AyniLimandaDegil);
            }

            return CarriedBy(ctx, fleetId) < type.TransportCapacity ? ValidationResult.Ok : ValidationResult.Fail(WarfareErrorKeys.NakliyeKapasitesiDolu);
        }

        public static void Embark(LandUnit unit, Fleet fleet)
        {
            GeoMovementRules.Stop(unit);
            unit.Carrier = fleet.Id;
            unit.Node = fleet.Node;
        }

        public static ValidationResult CheckLanding(GeoWarfareContext ctx, CountryId country, LandUnitId unitId, CityId target)
        {
            ValidationResult owned = GeoUnitRules.CheckOwned(ctx, country, unitId, out LandUnit unit);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (!unit.IsEmbarked || !ctx.Warfare.Fleets.TryGetValue(unit.Carrier, out Fleet fleet))
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikGemideDegil);
            }

            ShipTypeDefinition type = ctx.Catalog.ShipTypes[fleet.Type];
            if (type.TransportCapacity <= 0)
            {
                return ValidationResult.Fail(WarfareErrorKeys.NakliyeKapasitesiYok);
            }

            if (!ctx.World.TryGetCity(target, out City city) || !ctx.World.IsPort(city) || fleet.RouteProgress != 0L)
            {
                return ValidationResult.Fail(WarfareErrorKeys.CikarmaHedefiGecersiz);
            }

            if (city.Controller != country && !ctx.IsHostile(country, city.Controller))
            {
                return ValidationResult.Fail(WarfareErrorKeys.CikarmaHedefiGecersiz);
            }

            return fleet.Node == city.Node || SeaAdjacent(ctx, fleet.Node, city.Node)
                ? ValidationResult.Ok
                : ValidationResult.Fail(WarfareErrorKeys.CikarmaHedefiGecersiz);
        }

        public static void SetLanding(LandUnit unit, CityId target)
        {
            unit.AttackTarget = TargetRef.Entity(TargetKind.City, target.Value);
        }

        /// <summary>P13 (deniz muharebesinden sonra): çıkarma emirleri hedef liman düğümüne göre gruplanıp çözülür.</summary>
        public static void ResolveLandings(GeoWarfareContext ctx, ulong worldSeed, List<string> notes)
        {
            var groups = new SortedDictionary<ulong, List<LandUnit>>();
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit unit = ctx.Warfare.Units.ValueAt(i);
                if (!unit.IsEmbarked || unit.AttackTarget.Kind != TargetKind.City)
                {
                    continue;
                }

                var target = new CityId(unit.AttackTarget.EntityId);
                if (!CheckLanding(ctx, unit.Owner, unit.Id, target).IsValid)
                {
                    unit.AttackTarget = TargetRef.None;
                    continue;
                }

                if (!groups.TryGetValue(target.Value, out List<LandUnit> list))
                {
                    list = new List<LandUnit>();
                    groups.Add(target.Value, list);
                }

                list.Add(unit);
            }

            foreach (KeyValuePair<ulong, List<LandUnit>> group in groups)
            {
                ctx.World.TryGetCity(new CityId(group.Key), out City city);
                List<LandUnit> units = group.Value;
                if (ctx.IsHostile(units[0].Owner, city.Controller) || GeoUnitQueries.HasHostileUnitsAt(ctx, city.Node, units[0].Owner))
                {
                    var rng = new RngStream(worldSeed, RngSubsystem.Combat, ctx.Today.DayNumber, city.Node.Value, 1UL);
                    GeoCombatRules.ResolveAt(ctx, units, city.Node, ctx.Catalog.Joint.AmphibiousAttackFactor, ref rng, notes);
                }
                else
                {
                    for (int i = 0; i < units.Count; i++)
                    {
                        units[i].Carrier = FleetId.None;
                        units[i].Node = city.Node;
                        units[i].AttackTarget = TargetRef.None;
                    }
                }

                for (int i = 0; i < units.Count; i++)
                {
                    if (!units[i].IsEmbarked)
                    {
                        notes.Add(WarfareNotes.Landed + " @ " + units[i].Id.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }
        }

        private static bool SeaAdjacent(GeoWarfareContext ctx, TransportNodeId a, TransportNodeId b)
        {
            IReadOnlyList<TransportEdge> edges = ctx.World.Transport.EdgesOf(a);
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].Kind == TransportEdgeKind.SeaLane && edges[i].Other(a) == b)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
