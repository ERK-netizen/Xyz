using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Filo kurma, hareket, liman bakımı, terhis ve batma. Gemiler ortak envanterden çekilir,
    /// mürettebat liman bölgesinden seferber edilir. Filo düşman filosunun bulunduğu hücreye girince
    /// durur (temas); muharebe P13'te <see cref="NavalCombatRules"/> ile çözülür.
    /// </summary>
    public static class FleetRules
    {
        public const string ContactNote = "bilgi.deniz.temas";
        public const string SunkNote = "bilgi.deniz.filoBatti";

        public static ValidationResult CheckBuild(WarfareContext ctx, CountryId country, UnitTypeId typeId, HexCoord at)
        {
            if (!ctx.Catalog.ShipTypes.TryGetValue(typeId, out ShipTypeDefinition type))
            {
                return ValidationResult.Fail(WarfareErrorKeys.FiloTuruBilinmiyor);
            }

            if (!ctx.World.Grid.Contains(at))
            {
                return ValidationResult.Fail(WarfareErrorKeys.LimanGecersiz);
            }

            HexCell cell = ctx.World.Cell(at);
            if (!cell.City.IsValid || !NavalQueries.IsHomePort(ctx.World, country, ctx.World.Cities[cell.City]))
            {
                return ValidationResult.Fail(WarfareErrorKeys.LimanGecersiz);
            }

            if (!ctx.Population.Regions.TryGetValue(cell.Region, out RegionPopulation region))
            {
                return ValidationResult.Fail(WarfareErrorKeys.NufusYok);
            }

            ValidationResult crew = PopulationRules.CheckMobilize(region, type.Crew);
            return crew.IsValid ? InventoryRules.CheckAvailable(ctx.Economy, country, type.Equipment, Fixed64.One) : crew;
        }

        public static Result<FleetId> Build(WarfareContext ctx, CountryId country, UnitTypeId typeId, HexCoord at)
        {
            ValidationResult check = CheckBuild(ctx, country, typeId, at);
            if (!check.IsValid)
            {
                return Result<FleetId>.Failure(check.ErrorKey);
            }

            ShipTypeDefinition type = ctx.Catalog.ShipTypes[typeId];
            HexCell cell = ctx.World.Cell(at);
            PopulationRules.Mobilize(ctx.Population.Regions[cell.Region], type.Crew);
            InventoryRules.Consume(ctx.Economy, country, type.Equipment, Fixed64.One);
            var id = new FleetId(ctx.Warfare.FleetIds.Allocate());
            var fleet = new Fleet(id, country, typeId, cell.Id, cell.Region)
            {
                Manpower = type.Crew,
                Equipment = Fixed64.One,
                Organization = type.MaxOrganization * ctx.Catalog.Parameters.InitialOrganization,
            };
            ctx.Warfare.Fleets.Add(id, fleet);
            return Result<FleetId>.Success(id);
        }

        public static ValidationResult CheckOwned(WarfareContext ctx, CountryId country, FleetId fleetId, out Fleet fleet)
        {
            if (!ctx.Warfare.Fleets.TryGetValue(fleetId, out fleet))
            {
                return ValidationResult.Fail(WarfareErrorKeys.FiloBulunamadi);
            }

            return fleet.Owner != country ? ValidationResult.Fail(WarfareErrorKeys.FiloBizimDegil) : ValidationResult.Ok;
        }

        public static ValidationResult CheckMove(WarfareContext ctx, HexPathfinder pathfinder, CountryId country, FleetId fleetId, HexCoord target, List<HexId> path)
        {
            ValidationResult owned = CheckOwned(ctx, country, fleetId, out Fleet fleet);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (!ctx.World.Grid.Contains(target) || !NavalQueries.CanFleetEnter(ctx, country, ctx.World.Cell(target)))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
            }

            PathResult result = pathfinder.FindPath(ctx.World.Grid.FromId(fleet.Hex), target);
            if (!result.Found || result.Steps == 0)
            {
                return ValidationResult.Fail(WarfareErrorKeys.RotaYok);
            }

            path.Clear();
            for (int i = 1; i < result.Path.Count; i++)
            {
                path.Add(ctx.World.Grid.ToId(result.Path[i]));
            }

            return ValidationResult.Ok;
        }

        public static void SetPath(Fleet fleet, IReadOnlyList<HexId> path)
        {
            fleet.PathList.Clear();
            for (int i = 0; i < path.Count; i++)
            {
                fleet.PathList.Add(path[i]);
            }
        }

        public static void ClearOrders(Fleet fleet)
        {
            fleet.PathList.Clear();
            fleet.MoveBudget = Fixed64.Zero;
        }

        /// <summary>P12: bütçe kadar ilerler; düşman filosu olan hücreye girince durur, taşınanlar filoyla gider.</summary>
        public static void AdvanceDay(WarfareContext ctx, IMovementCostModel movement, List<string> notes)
        {
            var carried = new List<LandUnit>();
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet fleet = ctx.Warfare.Fleets.ValueAt(i);
                if (fleet.PathList.Count == 0)
                {
                    fleet.MoveBudget = Fixed64.Zero;
                    continue;
                }

                ShipTypeDefinition type = ctx.Catalog.ShipTypes[fleet.Type];
                fleet.MoveBudget = Fixed64.Min(fleet.MoveBudget + type.SpeedPerDay, type.SpeedPerDay * 2);
                while (fleet.PathList.Count > 0)
                {
                    HexCell from = ctx.World.Cell(fleet.Hex);
                    HexCell to = ctx.World.Cell(fleet.PathList[0]);
                    if (!TryStepKm(ctx, from, to, out Fixed64 km) || !movement.TryGetStepCost(from, to, km, out Fixed64 cost)
                        || !NavalQueries.CanFleetEnter(ctx, fleet.Owner, to))
                    {
                        ClearOrders(fleet);
                        notes.Add(MovementRules.BlockedNote + " @ filo " + fleet.Id.Value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }

                    if (cost > fleet.MoveBudget)
                    {
                        break;
                    }

                    fleet.MoveBudget -= cost;
                    fleet.Hex = to.Id;
                    fleet.PathList.RemoveAt(0);
                    if (NavalQueries.HasHostileFleets(ctx, to.Id, fleet.Owner))
                    {
                        ClearOrders(fleet);
                        notes.Add(ContactNote + " @ " + to.Id.Value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }
                }

                if (fleet.PathList.Count == 0)
                {
                    fleet.MoveBudget = Fixed64.Zero;
                }

                carried.Clear();
                NavalQueries.Carried(ctx, fleet.Id, carried);
                for (int c = 0; c < carried.Count; c++)
                {
                    carried[c].Hex = fleet.Hex;
                }
            }
        }

        /// <summary>P11: kendi kullanılabilir limanındaki filo teşkilat toparlar, mürettebat ve gemi takviyesi alır.</summary>
        public static void ApplyPortDay(WarfareContext ctx)
        {
            JointWarfareParameters joint = ctx.Catalog.Joint;
            Fixed64 rate = ctx.Catalog.Parameters.ReinforcementRate;
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet fleet = ctx.Warfare.Fleets.ValueAt(i);
                HexCell cell = ctx.World.Cell(fleet.Hex);
                if (!cell.City.IsValid || !NavalQueries.IsUsablePort(ctx.World, fleet.Owner, ctx.World.Cities[cell.City]))
                {
                    continue;
                }

                ShipTypeDefinition type = ctx.Catalog.ShipTypes[fleet.Type];
                fleet.Organization = Fixed64.Min(type.MaxOrganization, fleet.Organization + (type.MaxOrganization * joint.PortRecovery));
                long manpower = fleet.Manpower;
                Fixed64 equipment = fleet.Equipment;
                Reinforce(ctx, fleet.Owner, fleet.HomeRegion, type.Crew, type.Equipment, rate, ref manpower, ref equipment);
                fleet.Manpower = manpower;
                fleet.Equipment = equipment;
            }
        }

        /// <summary>Filo/kanat takviyesi: eksik insan bölgeden seferber edilir, eksik teçhizat envanterden çekilir.</summary>
        internal static void Reinforce(WarfareContext ctx, CountryId owner, RegionId home, long fullCrew, IReadOnlyList<GoodAmount> equipmentList, Fixed64 rate, ref long manpower, ref Fixed64 equipment)
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

            Fixed64 missingEquipment = Fixed64.One - equipment;
            if (missingEquipment.IsPositive)
            {
                Fixed64 gain = Fixed64.Min(rate, missingEquipment);
                if (InventoryRules.Consume(ctx.Economy, owner, equipmentList, gain).IsValid)
                {
                    equipment += gain;
                }
            }
        }

        public static ValidationResult CheckDisband(WarfareContext ctx, CountryId country, FleetId fleetId)
        {
            ValidationResult owned = CheckOwned(ctx, country, fleetId, out Fleet fleet);
            if (!owned.IsValid)
            {
                return owned;
            }

            HexCell cell = ctx.World.Cell(fleet.Hex);
            if (!cell.City.IsValid || !NavalQueries.IsUsablePort(ctx.World, country, ctx.World.Cities[cell.City]))
            {
                return ValidationResult.Fail(WarfareErrorKeys.LimanGecersiz);
            }

            return NavalQueries.CarriedCount(ctx, fleetId) > 0 ? ValidationResult.Fail(WarfareErrorKeys.FiloYuklu) : ValidationResult.Ok;
        }

        public static ValidationResult Disband(WarfareContext ctx, CountryId country, FleetId fleetId)
        {
            ValidationResult check = CheckDisband(ctx, country, fleetId);
            if (check.IsValid)
            {
                Fleet fleet = ctx.Warfare.Fleets[fleetId];
                ShipTypeDefinition type = ctx.Catalog.ShipTypes[fleet.Type];
                if (fleet.Equipment.IsPositive)
                {
                    InventoryRules.Return(ctx.Economy, fleet.Owner, type.Equipment, fleet.Equipment);
                }

                CasualtyRules.SendHome(ctx, fleet.HomeRegion, fleet.Manpower);
                fleet.Manpower = 0L;
                ctx.Warfare.Fleets.Remove(fleet.Id);
            }

            return check;
        }

        /// <summary>Filo batar: mürettebat ve taşınan birlikler kaybedilir (ölüm olarak kaydedilir).</summary>
        public static void Sink(WarfareContext ctx, Fleet fleet, List<string> notes)
        {
            var carried = new List<LandUnit>();
            NavalQueries.Carried(ctx, fleet.Id, carried);
            for (int i = 0; i < carried.Count; i++)
            {
                LandUnit u = carried[i];
                CasualtyRules.RecordDeaths(ctx, u, u.Manpower);
                u.Manpower = 0L;
                UnitRules.Remove(ctx, u, false);
                notes.Add(CombatRules.UnitDestroyedNote + " @ " + u.Id.Value.ToString(CultureInfo.InvariantCulture));
            }

            CasualtyRules.RecordDeaths(ctx, fleet.HomeRegion, fleet.Manpower);
            fleet.Manpower = 0L;
            ctx.Warfare.Fleets.Remove(fleet.Id);
            notes.Add(SunkNote + " @ " + fleet.Id.Value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Filo komşu, düşman filosu olmayan ilk deniz hücresine (yön sırasıyla) çekilir; yoksa batar.
        /// <paramref name="avoid"/> hücresine (ör. çatışma hücresi) dönülmez.
        /// </summary>
        public static void Withdraw(WarfareContext ctx, Fleet fleet, HexId avoid, List<string> notes)
        {
            ClearOrders(fleet);
            HexCoord from = ctx.World.Grid.FromId(fleet.Hex);
            for (int d = 0; d < HexDirections.Count; d++)
            {
                if (!ctx.World.Grid.TryGetNeighbor(from, (HexDirection)d, out HexCoord n))
                {
                    continue;
                }

                HexCell cell = ctx.World.Cell(n);
                if (cell.IsWater && cell.Id != avoid && !NavalQueries.HasHostileFleets(ctx, cell.Id, fleet.Owner))
                {
                    fleet.Hex = cell.Id;
                    var carried = new List<LandUnit>();
                    NavalQueries.Carried(ctx, fleet.Id, carried);
                    for (int i = 0; i < carried.Count; i++)
                    {
                        carried[i].Hex = fleet.Hex;
                    }

                    return;
                }
            }

            Sink(ctx, fleet, notes);
        }

        /// <summary>Şehir el değiştirince limandaki, yeni kontrolcüye ait olmayan filolar denize çıkar (yoksa batar).</summary>
        public static void EvictFromPort(WarfareContext ctx, City city, CountryId newController, List<string> notes)
        {
            var evicted = new List<Fleet>();
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.Hex == city.Hex && f.Owner != newController)
                {
                    evicted.Add(f);
                }
            }

            for (int i = 0; i < evicted.Count; i++)
            {
                Withdraw(ctx, evicted[i], HexId.None, notes);
            }
        }

        private static bool TryStepKm(WarfareContext ctx, HexCell from, HexCell to, out Fixed64 km)
        {
            for (int d = 0; d < HexDirections.Count; d++)
            {
                if (ctx.World.Grid.TryGetNeighbor(from.Coord, (HexDirection)d, out HexCoord n) && n == to.Coord)
                {
                    return ctx.World.Distances.TryGetNeighborKm(from.Coord, (HexDirection)d, out km);
                }
            }

            km = Fixed64.Zero;
            return false;
        }
    }
}
