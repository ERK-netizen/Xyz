using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Filolar (§6): kendi ana limanında (sahip + kontrol, kuşatılmamış) kurulur; gemiler envanterden,
    /// mürettebat şehrin bölge nüfusundan gelir. Hareket deniz yolu grafında, çok günlü; düşman filosunun
    /// düğümüne giren filo durur (temas). Aynı düğümdeki düşman filoları tek <see cref="CombatResolver"/> ile
    /// çarpışır (+ ortak destek); gücü eşiğin altına düşen filo batar (taşıdığı birliklerle), teşkilatı
    /// çöken en yakın kendi limanına çekilir. Abluka: limana deniz yoluyla bitişik düğümde düşman filosu
    /// varken şehrin kontrolcüsünün limanda veya bitişik düğümlerde filosu yoksa.
    /// </summary>
    public static class GeoNavalRules
    {
        public static bool IsHomePort(GeoWarfareContext ctx, CountryId country, City city) =>
            ctx.World.IsPort(city) && city.Owner == country && city.Controller == country && city.Siege != SiegeState.Besieged;

        public static ValidationResult CheckBuild(GeoWarfareContext ctx, CountryId country, UnitTypeId typeId, CityId at)
        {
            if (!ctx.Catalog.ShipTypes.TryGetValue(typeId, out ShipTypeDefinition type))
            {
                return ValidationResult.Fail(WarfareErrorKeys.FiloTuruBilinmiyor);
            }

            if (!ctx.World.TryGetCity(at, out City city) || !IsHomePort(ctx, country, city))
            {
                return ValidationResult.Fail(WarfareErrorKeys.LimanGecersiz);
            }

            if (!ctx.Population.Regions.TryGetValue(city.Region, out RegionPopulation region))
            {
                return ValidationResult.Fail(WarfareErrorKeys.NufusYok);
            }

            ValidationResult crew = PopulationRules.CheckMobilize(region, type.Crew);
            return crew.IsValid ? InventoryRules.CheckAvailable(ctx.Economy, country, type.Equipment, Fixed64.One) : crew;
        }

        public static Result<FleetId> Build(GeoWarfareContext ctx, CountryId country, UnitTypeId typeId, CityId at)
        {
            ValidationResult check = CheckBuild(ctx, country, typeId, at);
            if (!check.IsValid)
            {
                return Result<FleetId>.Failure(check.ErrorKey);
            }

            ShipTypeDefinition type = ctx.Catalog.ShipTypes[typeId];
            ctx.World.TryGetCity(at, out City city);
            PopulationRules.Mobilize(ctx.Population.Regions[city.Region], type.Crew);
            InventoryRules.Consume(ctx.Economy, country, type.Equipment, Fixed64.One);
            var id = new FleetId(ctx.Warfare.FleetIds.Allocate());
            var fleet = new Fleet(id, country, typeId, city.Node, city.Region)
            {
                Manpower = type.Crew,
                Equipment = Fixed64.One,
                Organization = type.MaxOrganization * ctx.Catalog.Parameters.InitialOrganization,
            };
            ctx.Warfare.Fleets.Add(id, fleet);
            return Result<FleetId>.Success(id);
        }

        public static ValidationResult CheckOwned(GeoWarfareContext ctx, CountryId country, FleetId fleetId, out Fleet fleet)
        {
            if (!ctx.Warfare.Fleets.TryGetValue(fleetId, out fleet))
            {
                return ValidationResult.Fail(WarfareErrorKeys.FiloBulunamadi);
            }

            return fleet.Owner == country ? ValidationResult.Ok : ValidationResult.Fail(WarfareErrorKeys.FiloBizimDegil);
        }

        /// <summary>Filo hedefi: kendi kontrolündeki liman şehri, dost filo veya noktaya en yakın deniz düğümü.</summary>
        public static ValidationResult CheckMove(GeoWarfareContext ctx, TransportPathfinder finder, CountryId country, FleetId fleetId, TargetRef target, List<TransportEdgeId> route)
        {
            route.Clear();
            ValidationResult owned = CheckOwned(ctx, country, fleetId, out Fleet fleet);
            if (!owned.IsValid)
            {
                return owned;
            }

            var access = new NavalAccessCostModel(ctx, country);
            TransportNodeId destination = Destination(ctx, target, access);
            if (!destination.IsValid || !ctx.World.Transport.TryGetNode(destination, out TransportNode node) || !access.CanEnter(node))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
            }

            TransportNodeId start = fleet.Node;
            if (fleet.RouteProgress > 0L && fleet.RouteList.Count > 0)
            {
                ctx.World.Transport.TryGetEdge(fleet.RouteList[0], out TransportEdge current);
                route.Add(current.Id);
                start = current.Other(fleet.Node);
            }

            if (start == destination)
            {
                return route.Count > 0 ? ValidationResult.Ok : ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
            }

            Result<TransportPath> path = finder.FindPath(start, destination, access);
            if (!path.IsSuccess)
            {
                route.Clear();
                return ValidationResult.Fail(WarfareErrorKeys.RotaYok);
            }

            for (int i = 0; i < path.Value.Edges.Count; i++)
            {
                route.Add(path.Value.Edges[i]);
            }

            return ValidationResult.Ok;
        }

        public static void SetRoute(Fleet fleet, IReadOnlyList<TransportEdgeId> route)
        {
            bool keep = fleet.RouteProgress > 0L && fleet.RouteList.Count > 0 && route.Count > 0 && route[0] == fleet.RouteList[0];
            fleet.RouteList.Clear();
            for (int i = 0; i < route.Count; i++)
            {
                fleet.RouteList.Add(route[i]);
            }

            if (!keep)
            {
                fleet.RouteProgress = 0L;
            }
        }

        /// <summary>P12: filolar ilerler; taşıdıkları birlikler filoyla aynı düğümdedir.</summary>
        public static void AdvanceDay(GeoWarfareContext ctx, List<string> notes)
        {
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet fleet = ctx.Warfare.Fleets.ValueAt(i);
                if (fleet.RouteList.Count == 0)
                {
                    continue;
                }

                ShipTypeDefinition type = ctx.Catalog.ShipTypes[fleet.Type];
                long budget = (type.SpeedPerDay * ctx.Geo.MarchCostPerSpeedUnit).RoundToLong();
                var access = new NavalAccessCostModel(ctx, fleet.Owner);
                while (fleet.RouteList.Count > 0 && budget > 0L)
                {
                    ctx.World.Transport.TryGetEdge(fleet.RouteList[0], out TransportEdge edge);
                    TransportNodeId nextId = edge.Other(fleet.Node);
                    ctx.World.Transport.TryGetNode(fleet.Node, out TransportNode from);
                    ctx.World.Transport.TryGetNode(nextId, out TransportNode next);
                    if (!access.TryGetCost(edge, from, next, out long step))
                    {
                        Stop(fleet);
                        break;
                    }

                    long need = step - fleet.RouteProgress;
                    if (budget < need)
                    {
                        fleet.RouteProgress += budget;
                        break;
                    }

                    budget -= need;
                    fleet.Node = nextId;
                    fleet.RouteProgress = 0L;
                    fleet.RouteList.RemoveAt(0);
                    if (HasHostileFleetAt(ctx, nextId, fleet.Owner))
                    {
                        Stop(fleet);
                        notes.Add(WarfareNotes.NavalContact + " @ " + fleet.Id.Value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }
                }

                SyncCargo(ctx, fleet);
            }
        }

        public static void Stop(Fleet fleet)
        {
            fleet.RouteList.Clear();
            fleet.RouteProgress = 0L;
        }

        public static bool HasHostileFleetAt(GeoWarfareContext ctx, TransportNodeId node, CountryId country)
        {
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.Node == node && f.RouteProgress == 0L && ctx.IsHostile(country, f.Owner))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>P13: düşman filolarının bulunduğu düğümler (kimlik sırasıyla) için deniz muharebesi.</summary>
        public static void ResolveBattles(GeoWarfareContext ctx, ulong worldSeed, List<string> notes)
        {
            var contested = new SortedSet<ulong>();
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.RouteProgress == 0L && HasHostileFleetAt(ctx, f.Node, f.Owner))
                {
                    contested.Add(f.Node.Value);
                }
            }

            foreach (ulong node in contested)
            {
                var rng = new RngStream(worldSeed, RngSubsystem.NavalCombat, ctx.Today.DayNumber, node, 0UL);
                ResolveBattle(ctx, new TransportNodeId(node), ref rng, notes);
            }
        }

        private static void ResolveBattle(GeoWarfareContext ctx, TransportNodeId node, ref RngStream rng, List<string> notes)
        {
            var sideA = new List<Fleet>();
            var sideB = new List<Fleet>();
            CountryId a = CountryId.None;
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.Node != node || f.RouteProgress != 0L)
                {
                    continue;
                }

                if (!a.IsValid)
                {
                    a = f.Owner;
                }

                if (f.Owner == a)
                {
                    sideA.Add(f);
                }
                else if (ctx.IsHostile(a, f.Owner))
                {
                    sideB.Add(f);
                }
            }

            if (sideA.Count == 0 || sideB.Count == 0)
            {
                return;
            }

            CountryId b = sideB[0].Owner;
            Fixed64 powerA = (SidePower(ctx, sideA) * GeoCombatRules.CountryModifier(ctx, a, WarfareStatKeys.NavalAttack)) + GeoJointSupportRules.Support(ctx, node, a);
            Fixed64 powerB = (SidePower(ctx, sideB) * GeoCombatRules.CountryModifier(ctx, b, WarfareStatKeys.NavalAttack)) + GeoJointSupportRules.Support(ctx, node, b);
            CombatOutcome outcome = CombatResolver.Resolve(powerA, powerB, ctx.Catalog.Parameters, ref rng);
            notes.Add(WarfareNotes.NavalBattle + " @ " + node.Value.ToString(CultureInfo.InvariantCulture));
            Damage(ctx, sideA, outcome.AttackerDamage);
            Damage(ctx, sideB, outcome.DefenderDamage);
            Settle(ctx, sideA, notes);
            Settle(ctx, sideB, notes);
        }

        private static Fixed64 SidePower(GeoWarfareContext ctx, List<Fleet> fleets)
        {
            Fixed64 sum = Fixed64.Zero;
            for (int i = 0; i < fleets.Count; i++)
            {
                ShipTypeDefinition type = ctx.Catalog.ShipTypes[fleets[i].Type];
                sum += CombatResolver.FleetPower(fleets[i], type, type.Attack);
            }

            return sum;
        }

        private static void Damage(GeoWarfareContext ctx, List<Fleet> fleets, Fixed64 damage)
        {
            for (int i = 0; i < fleets.Count; i++)
            {
                Fleet f = fleets[i];
                ShipTypeDefinition type = ctx.Catalog.ShipTypes[f.Type];
                f.Organization = Fixed64.Max(Fixed64.Zero, f.Organization - (type.MaxOrganization * damage));
                long manpower = f.Manpower;
                Fixed64 equipment = f.Equipment;
                CasualtyRules.ApplyLoss(ctx.Population, f.HomeRegion, type.Crew, ref manpower, ref equipment, damage * ctx.Catalog.Parameters.StrengthLossPerDamage);
                f.Manpower = manpower;
                f.Equipment = equipment;
            }
        }

        private static void Settle(GeoWarfareContext ctx, List<Fleet> fleets, List<string> notes)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            for (int i = 0; i < fleets.Count; i++)
            {
                Fleet f = fleets[i];
                ShipTypeDefinition type = ctx.Catalog.ShipTypes[f.Type];
                if (f.Strength(type) < prm.DestroyedBelowStrength)
                {
                    Sink(ctx, f, notes);
                }
                else if (f.Organization < type.MaxOrganization * prm.RetreatOrganization)
                {
                    Withdraw(ctx, f);
                }
            }
        }

        /// <summary>Filo batar: mürettebat ve taşınan birlikler ölü sayılır.</summary>
        public static void Sink(GeoWarfareContext ctx, Fleet fleet, List<string> notes)
        {
            var cargo = new List<LandUnit>();
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.Carrier == fleet.Id)
                {
                    cargo.Add(u);
                }
            }

            for (int i = 0; i < cargo.Count; i++)
            {
                CasualtyRules.RecordDeaths(ctx.Population, cargo[i].HomeRegion, cargo[i].Manpower);
                cargo[i].Manpower = 0L;
                ctx.Warfare.Units.Remove(cargo[i].Id);
            }

            CasualtyRules.RecordDeaths(ctx.Population, fleet.HomeRegion, fleet.Manpower);
            fleet.Manpower = 0L;
            ctx.Warfare.Fleets.Remove(fleet.Id);
            notes.Add(WarfareNotes.FleetSunk + " @ " + fleet.Id.Value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>En yakın kendi limanına rota alır (yoksa yerinde kalır).</summary>
        public static void Withdraw(GeoWarfareContext ctx, Fleet fleet)
        {
            var access = new NavalAccessCostModel(ctx, fleet.Owner);
            var finder = new TransportPathfinder(ctx.World.Transport);
            TransportPath best = null;
            for (int i = 0; i < ctx.World.Cities.Count; i++)
            {
                ctx.World.TryGetCity(ctx.World.Cities[i], out City city);
                if (!IsHomePort(ctx, fleet.Owner, city) || city.Node == fleet.Node)
                {
                    continue;
                }

                Result<TransportPath> path = finder.FindPath(fleet.Node, city.Node, access);
                if (path.IsSuccess && (best == null || path.Value.Cost < best.Cost))
                {
                    best = path.Value;
                }
            }

            Stop(fleet);
            if (best != null)
            {
                SetRoute(fleet, best.Edges);
            }
        }

        /// <summary>P14: abluka listesi yeniden hesaplanır (artan şehir kimliği).</summary>
        public static void UpdateBlockades(GeoWarfareContext ctx)
        {
            ctx.Warfare.BlockadedList.Clear();
            for (int i = 0; i < ctx.World.Cities.Count; i++)
            {
                ctx.World.TryGetCity(ctx.World.Cities[i], out City city);
                if (ctx.World.IsPort(city) && IsBlockaded(ctx, city))
                {
                    ctx.Warfare.BlockadedList.Add(city.Id);
                }
            }
        }

        public static bool IsBlockaded(GeoWarfareContext ctx, City city)
        {
            bool enemy = false;
            bool own = HasFleetOf(ctx, city.Node, city.Controller);
            IReadOnlyList<TransportEdge> edges = ctx.World.Transport.EdgesOf(city.Node);
            for (int e = 0; e < edges.Count; e++)
            {
                if (edges[e].Kind != TransportEdgeKind.SeaLane)
                {
                    continue;
                }

                TransportNodeId sea = edges[e].Other(city.Node);
                enemy |= HasHostileFleetAt(ctx, sea, city.Controller);
                own |= HasFleetOf(ctx, sea, city.Controller);
            }

            return enemy && !own;
        }

        private static bool HasFleetOf(GeoWarfareContext ctx, TransportNodeId node, CountryId country)
        {
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.Node == node && f.RouteProgress == 0L && f.Owner == country)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Taşınan birliklerin düğümü filonunkiyle eşitlenir.</summary>
        public static void SyncCargo(GeoWarfareContext ctx, Fleet fleet)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.Carrier == fleet.Id)
                {
                    u.Node = fleet.Node;
                }
            }
        }

        /// <summary>Nüfus ve envanterden takviye (yoktan asker/gemi/uçak yok).</summary>
        public static void Reinforce(GeoWarfareContext ctx, CountryId owner, RegionId home, long fullCrew, IReadOnlyList<GoodAmount> equipmentCost, Fixed64 rate, ref long manpower, ref Fixed64 equipment)
        {
            long missing = fullCrew - manpower;
            if (missing > 0L && ctx.Population.Regions.TryGetValue(home, out RegionPopulation region))
            {
                long wanted = (Fixed64.FromInt(fullCrew) * rate).RoundToLong();
                long men = wanted < missing ? wanted : missing;
                if (men > region.AvailableManpower)
                {
                    men = region.AvailableManpower;
                }

                if (men > 0L && PopulationRules.Mobilize(region, men).IsValid)
                {
                    manpower += men;
                }
            }

            Fixed64 lacking = Fixed64.One - equipment;
            if (lacking.IsPositive)
            {
                Fixed64 gain = Fixed64.Min(rate, lacking);
                if (InventoryRules.Consume(ctx.Economy, owner, equipmentCost, gain).IsValid)
                {
                    equipment += gain;
                }
            }
        }

        private static TransportNodeId Destination(GeoWarfareContext ctx, TargetRef target, NavalAccessCostModel access)
        {
            switch (target.Kind)
            {
                case TargetKind.City:
                    return ctx.World.TryGetCity(new CityId(target.EntityId), out City city) ? city.Node : TransportNodeId.None;
                case TargetKind.Fleet:
                    return ctx.Warfare.Fleets.TryGetValue(new FleetId(target.EntityId), out Fleet fleet) ? fleet.Node : TransportNodeId.None;
                case TargetKind.Point:
                    TransportNodeId best = TransportNodeId.None;
                    long bestDistance = long.MaxValue;
                    IReadOnlyList<TransportNodeId> nodes = ctx.World.Transport.Nodes;
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        ctx.World.Transport.TryGetNode(nodes[i], out TransportNode node);
                        if (!access.CanEnter(node))
                        {
                            continue;
                        }

                        long d = GeoMath.DistanceMeters(target.Point, node.Location);
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            best = node.Id;
                        }
                    }

                    return best;
                default:
                    return TransportNodeId.None;
            }
        }
    }
}
