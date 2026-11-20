using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Hava kanatları (§7, REG-011): kanat kendi kontrolündeki, kuşatılmamış şehirde üslenir (şehir başına
    /// <see cref="JointWarfareParameters.AirWingsPerCity"/>). Görev hedefi birlik/şehir/filo/nokta; üs ile
    /// hedef arası büyük çember mesafesi tür menzilini aşamaz (<c>hata.hava.menzilDisi</c>). Görev günlük
    /// teşkilat harcar; üste bekleyen toparlanır ve takviye alır. Üs düşerse en yakın (km, eşitlikte küçük
    /// CityId) boş üsse geçer, yoksa dağılır.
    /// </summary>
    public static class GeoAirRules
    {
        public static bool IsAirBase(GeoWarfareContext ctx, CountryId country, City city) =>
            city.Controller == country && city.Siege != SiegeState.Besieged;

        public static int WingsAt(GeoWarfareContext ctx, CityId city)
        {
            int count = 0;
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                if (ctx.Warfare.AirWings.ValueAt(i).Base == city)
                {
                    count++;
                }
            }

            return count;
        }

        public static ValidationResult CheckRecruit(GeoWarfareContext ctx, CountryId country, UnitTypeId typeId, CityId at)
        {
            if (!ctx.Catalog.AirWingTypes.TryGetValue(typeId, out AirWingTypeDefinition type))
            {
                return ValidationResult.Fail(WarfareErrorKeys.KanatTuruBilinmiyor);
            }

            if (!ctx.World.TryGetCity(at, out City city) || city.Owner != country || !IsAirBase(ctx, country, city))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HavaUssuGecersiz);
            }

            if (WingsAt(ctx, at) >= ctx.Catalog.Joint.AirWingsPerCity)
            {
                return ValidationResult.Fail(WarfareErrorKeys.UsDolu);
            }

            if (!ctx.Population.Regions.TryGetValue(city.Region, out RegionPopulation region))
            {
                return ValidationResult.Fail(WarfareErrorKeys.NufusYok);
            }

            ValidationResult crew = PopulationRules.CheckMobilize(region, type.Crew);
            return crew.IsValid ? InventoryRules.CheckAvailable(ctx.Economy, country, type.Equipment, Fixed64.One) : crew;
        }

        public static Result<AirWingId> Recruit(GeoWarfareContext ctx, CountryId country, UnitTypeId typeId, CityId at)
        {
            ValidationResult check = CheckRecruit(ctx, country, typeId, at);
            if (!check.IsValid)
            {
                return Result<AirWingId>.Failure(check.ErrorKey);
            }

            AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[typeId];
            ctx.World.TryGetCity(at, out City city);
            PopulationRules.Mobilize(ctx.Population.Regions[city.Region], type.Crew);
            InventoryRules.Consume(ctx.Economy, country, type.Equipment, Fixed64.One);
            var id = new AirWingId(ctx.Warfare.AirWingIds.Allocate());
            var wing = new AirWing(id, country, typeId, at, city.Region)
            {
                Manpower = type.Crew,
                Equipment = Fixed64.One,
                Organization = type.MaxOrganization * ctx.Catalog.Parameters.InitialOrganization,
            };
            ctx.Warfare.AirWings.Add(id, wing);
            return Result<AirWingId>.Success(id);
        }

        /// <summary>Görev hedefinin konumu; çözülemezse yanlış.</summary>
        public static bool TryTargetPoint(GeoWarfareContext ctx, TargetRef target, out GeoPoint point)
        {
            point = default;
            TransportNodeId node;
            switch (target.Kind)
            {
                case TargetKind.Point:
                    point = target.Point;
                    return true;
                case TargetKind.Fleet:
                    if (!ctx.Warfare.Fleets.TryGetValue(new FleetId(target.EntityId), out Fleet fleet))
                    {
                        return false;
                    }

                    node = fleet.Node;
                    break;
                default:
                    node = GeoUnitQueries.ResolveNode(ctx, target);
                    break;
            }

            if (!ctx.World.Transport.TryGetNode(node, out TransportNode n))
            {
                return false;
            }

            point = n.Location;
            return true;
        }

        /// <summary>Görev hedefinin ulaşım düğümü (nokta için en yakın düğüm, eşitlikte küçük kimlik).</summary>
        public static TransportNodeId TargetNode(GeoWarfareContext ctx, TargetRef target)
        {
            if (target.Kind == TargetKind.Fleet)
            {
                return ctx.Warfare.Fleets.TryGetValue(new FleetId(target.EntityId), out Fleet fleet) ? fleet.Node : TransportNodeId.None;
            }

            if (target.Kind != TargetKind.Point)
            {
                return GeoUnitQueries.ResolveNode(ctx, target);
            }

            TransportNodeId best = TransportNodeId.None;
            long bestDistance = long.MaxValue;
            IReadOnlyList<TransportNodeId> nodes = ctx.World.Transport.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                ctx.World.Transport.TryGetNode(nodes[i], out TransportNode node);
                long d = GeoMath.DistanceMeters(target.Point, node.Location);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = node.Id;
                }
            }

            return best;
        }

        public static ValidationResult CheckMission(GeoWarfareContext ctx, CountryId country, AirWingId wingId, AirMission mission, TargetRef target)
        {
            if (!ctx.Warfare.AirWings.TryGetValue(wingId, out AirWing wing))
            {
                return ValidationResult.Fail(WarfareErrorKeys.KanatBulunamadi);
            }

            if (wing.Owner != country)
            {
                return ValidationResult.Fail(WarfareErrorKeys.KanatBizimDegil);
            }

            if (mission == AirMission.None)
            {
                return ValidationResult.Ok;
            }

            if (!TryTargetPoint(ctx, target, out GeoPoint point) || !ctx.World.TryGetCity(wing.Base, out City baseCity))
            {
                return ValidationResult.Fail(WarfareErrorKeys.GorevHedefiGecersiz);
            }

            AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[wing.Type];
            long rangeMeters = (type.RangeKm * 1000L).RoundToLong();
            return GeoMath.DistanceMeters(baseCity.Location, point) <= rangeMeters
                ? ValidationResult.Ok
                : ValidationResult.Fail(WarfareErrorKeys.MenzilDisi);
        }

        public static void SetMission(AirWing wing, AirMission mission, TargetRef target)
        {
            wing.Mission = mission;
            wing.MissionTarget = mission == AirMission.None ? TargetRef.None : target;
        }

        /// <summary>P11: görevdeki kanat teşkilat harcar; üstekiler toparlanır ve takviye alır.</summary>
        public static void ApplyDay(GeoWarfareContext ctx)
        {
            JointWarfareParameters joint = ctx.Catalog.Joint;
            WarfareParameters prm = ctx.Catalog.Parameters;
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[w.Type];
                if (w.Mission != AirMission.None)
                {
                    w.Organization = Fixed64.Max(Fixed64.Zero, w.Organization - (type.MaxOrganization * joint.AirMissionOrganizationCost));
                    if (w.Organization < type.MaxOrganization * prm.RetreatOrganization)
                    {
                        SetMission(w, AirMission.None, TargetRef.None);
                    }

                    continue;
                }

                w.Organization = Fixed64.Min(type.MaxOrganization, w.Organization + (type.MaxOrganization * joint.AirRecovery));
                long manpower = w.Manpower;
                Fixed64 equipment = w.Equipment;
                GeoNavalRules.Reinforce(ctx, w.Owner, w.HomeRegion, type.Crew, type.Equipment, prm.ReinforcementRate, ref manpower, ref equipment);
                w.Manpower = manpower;
                w.Equipment = equipment;
            }
        }

        /// <summary>P14: üssü kullanılamayan kanat en yakın boş üsse geçer; yoksa dağılır.</summary>
        public static void UpdateBases(GeoWarfareContext ctx, List<string> notes)
        {
            var lost = new List<AirWing>();
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                if (ctx.World.TryGetCity(w.Base, out City current) && IsAirBase(ctx, w.Owner, current))
                {
                    continue;
                }

                CityId best = CityId.None;
                long bestDistance = long.MaxValue;
                for (int c = 0; c < ctx.World.Cities.Count; c++)
                {
                    ctx.World.TryGetCity(ctx.World.Cities[c], out City candidate);
                    if (!IsAirBase(ctx, w.Owner, candidate) || WingsAt(ctx, candidate.Id) >= ctx.Catalog.Joint.AirWingsPerCity)
                    {
                        continue;
                    }

                    long d = current != null ? GeoMath.DistanceMeters(current.Location, candidate.Location) : 0L;
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = candidate.Id;
                    }
                }

                if (best.IsValid)
                {
                    w.Base = best;
                    SetMission(w, AirMission.None, TargetRef.None);
                    notes.Add(WarfareNotes.AirRebased + " @ " + w.Id.Value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    lost.Add(w);
                }
            }

            for (int i = 0; i < lost.Count; i++)
            {
                AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[lost[i].Type];
                CasualtyRules.SendHome(ctx.Population, lost[i].HomeRegion, lost[i].Manpower);
                ctx.Warfare.AirWings.Remove(lost[i].Id);
                notes.Add(WarfareNotes.AirWingLost + " @ " + lost[i].Id.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
