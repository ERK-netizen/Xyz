using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Hava kanatları ve hava üsleri. Hava üssü: hücresi ve merkez sektörü ülkenin kontrolünde,
    /// kuşatılmamış şehir; en çok <see cref="JointWarfareParameters.AirWingsPerCity"/> kanat. Kanat
    /// yalnız sahip olunan şehirde kurulur. Görev hedefi üssten büyük çember mesafesiyle türün
    /// menzili içinde olmalıdır (menzil dışı destek doğrulamada reddedilir). Üs düşerse kanat en
    /// yakın boş üsse geçer; yoksa dağılır.
    /// </summary>
    public static class AirRules
    {
        public const string RebasedNote = "bilgi.hava.usDegisti";
        public const string LostNote = "bilgi.hava.kanatKaybedildi";

        public static bool IsAirBase(WarfareContext ctx, CountryId country, City city)
        {
            return ctx.World.Cell(city.Hex).Controller == country && ctx.World.AdministrativeSectorOf(city).Controller == country
                && !ctx.Warfare.IsBesieged(city.Id);
        }

        public static int WingsAt(WarfareContext ctx, CityId city)
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

        public static ValidationResult CheckRecruit(WarfareContext ctx, CountryId country, UnitTypeId typeId, HexCoord at)
        {
            if (!ctx.Catalog.AirWingTypes.TryGetValue(typeId, out AirWingTypeDefinition type))
            {
                return ValidationResult.Fail(WarfareErrorKeys.KanatTuruBilinmiyor);
            }

            ValidationResult baseCheck = CheckBaseAt(ctx, country, at, out City city);
            if (!baseCheck.IsValid)
            {
                return baseCheck;
            }

            HexCell cell = ctx.World.Cell(at);
            if (cell.Owner != country)
            {
                return ValidationResult.Fail(WarfareErrorKeys.HavaUssuGecersiz);
            }

            if (!ctx.Population.Regions.TryGetValue(cell.Region, out RegionPopulation region))
            {
                return ValidationResult.Fail(WarfareErrorKeys.NufusYok);
            }

            ValidationResult crew = PopulationRules.CheckMobilize(region, type.Crew);
            return crew.IsValid ? InventoryRules.CheckAvailable(ctx.Economy, country, type.Equipment, Fixed64.One) : crew;
        }

        public static Result<AirWingId> Recruit(WarfareContext ctx, CountryId country, UnitTypeId typeId, HexCoord at)
        {
            ValidationResult check = CheckRecruit(ctx, country, typeId, at);
            if (!check.IsValid)
            {
                return Result<AirWingId>.Failure(check.ErrorKey);
            }

            AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[typeId];
            HexCell cell = ctx.World.Cell(at);
            PopulationRules.Mobilize(ctx.Population.Regions[cell.Region], type.Crew);
            InventoryRules.Consume(ctx.Economy, country, type.Equipment, Fixed64.One);
            var id = new AirWingId(ctx.Warfare.AirWingIds.Allocate());
            var wing = new AirWing(id, country, typeId, cell.City, cell.Region)
            {
                Manpower = type.Crew,
                Equipment = Fixed64.One,
                Organization = type.MaxOrganization * ctx.Catalog.Parameters.InitialOrganization,
            };
            ctx.Warfare.AirWings.Add(id, wing);
            return Result<AirWingId>.Success(id);
        }

        public static ValidationResult CheckOwned(WarfareContext ctx, CountryId country, AirWingId wingId, out AirWing wing)
        {
            if (!ctx.Warfare.AirWings.TryGetValue(wingId, out wing))
            {
                return ValidationResult.Fail(WarfareErrorKeys.KanatBulunamadi);
            }

            return wing.Owner != country ? ValidationResult.Fail(WarfareErrorKeys.KanatBizimDegil) : ValidationResult.Ok;
        }

        public static ValidationResult CheckRebase(WarfareContext ctx, CountryId country, AirWingId wingId, HexCoord at)
        {
            ValidationResult owned = CheckOwned(ctx, country, wingId, out AirWing wing);
            if (!owned.IsValid)
            {
                return owned;
            }

            ValidationResult baseCheck = CheckBaseAt(ctx, country, at, out City city);

            // Kanadın zaten bulunduğu üs kendi yuvasını sayar: dolu olması geçişi engellemez.
            bool sameBase = city != null && city.Id == wing.Base;
            return baseCheck.IsValid || (sameBase && baseCheck.ErrorKey == WarfareErrorKeys.UsDolu) ? ValidationResult.Ok : baseCheck;
        }

        public static void Rebase(AirWing wing, CityId city)
        {
            wing.Base = city;
            wing.Mission = AirMission.None;
            wing.MissionTarget = HexId.None;
        }

        public static ValidationResult CheckMission(WarfareContext ctx, CountryId country, AirWingId wingId, AirMission mission, HexCoord target)
        {
            ValidationResult owned = CheckOwned(ctx, country, wingId, out AirWing wing);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (mission == AirMission.None)
            {
                return ValidationResult.Ok;
            }

            if (mission != AirMission.AirSuperiority && mission != AirMission.GroundSupport && mission != AirMission.NavalStrike)
            {
                return ValidationResult.Fail(WarfareErrorKeys.GorevHedefiGecersiz);
            }

            if (!ctx.World.Grid.Contains(target))
            {
                return ValidationResult.Fail(WarfareErrorKeys.GorevHedefiGecersiz);
            }

            HexCell cell = ctx.World.Cell(target);
            if ((mission == AirMission.GroundSupport && cell.IsWater) || (mission == AirMission.NavalStrike && !cell.IsWater))
            {
                return ValidationResult.Fail(WarfareErrorKeys.GorevHedefiGecersiz);
            }

            AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[wing.Type];
            HexCoord from = ctx.World.Grid.FromId(ctx.World.Cities[wing.Base].Hex);
            return ctx.World.Distances.GreatCircleKm(from, target) > type.RangeKm
                ? ValidationResult.Fail(WarfareErrorKeys.MenzilDisi)
                : ValidationResult.Ok;
        }

        public static void SetMission(AirWing wing, AirMission mission, HexId target)
        {
            wing.Mission = mission;
            wing.MissionTarget = mission == AirMission.None ? HexId.None : target;
        }

        public static ValidationResult CheckDisband(WarfareContext ctx, CountryId country, AirWingId wingId)
        {
            return CheckOwned(ctx, country, wingId, out _);
        }

        public static void Remove(WarfareContext ctx, AirWing wing, bool returnEquipment)
        {
            AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[wing.Type];
            if (returnEquipment && wing.Equipment.IsPositive)
            {
                InventoryRules.Return(ctx.Economy, wing.Owner, type.Equipment, wing.Equipment);
            }

            CasualtyRules.SendHome(ctx, wing.HomeRegion, wing.Manpower);
            wing.Manpower = 0L;
            ctx.Warfare.AirWings.Remove(wing.Id);
        }

        /// <summary>
        /// P11: görevdeki kanat teşkilat harcar (geri çekilme eşiğinin altında görev biter); üste
        /// bekleyen kanat toparlanır ve pilot/uçak takviyesi alır.
        /// </summary>
        public static void ApplyDay(WarfareContext ctx)
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
                        SetMission(w, AirMission.None, HexId.None);
                    }

                    continue;
                }

                w.Organization = Fixed64.Min(type.MaxOrganization, w.Organization + (type.MaxOrganization * joint.AirRecovery));
                long manpower = w.Manpower;
                Fixed64 equipment = w.Equipment;
                FleetRules.Reinforce(ctx, w.Owner, w.HomeRegion, type.Crew, type.Equipment, prm.ReinforcementRate, ref manpower, ref equipment);
                w.Manpower = manpower;
                w.Equipment = equipment;
            }
        }

        /// <summary>P14: üssü düşen kanat en yakın boş üsse (km, eşitlikte küçük CityId) geçer; yoksa dağılır.</summary>
        public static void UpdateBases(WarfareContext ctx, List<string> notes)
        {
            var lost = new List<AirWing>();
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                if (!IsAirBase(ctx, w.Owner, ctx.World.Cities[w.Base]))
                {
                    lost.Add(w);
                }
            }

            for (int i = 0; i < lost.Count; i++)
            {
                AirWing w = lost[i];
                HexCoord from = ctx.World.Grid.FromId(ctx.World.Cities[w.Base].Hex);
                City best = null;
                Fixed64 bestKm = Fixed64.MaxValue;
                for (int c = 0; c < ctx.World.Cities.Count; c++)
                {
                    City city = ctx.World.Cities.ValueAt(c);
                    if (city.Id == w.Base || !IsAirBase(ctx, w.Owner, city) || WingsAt(ctx, city.Id) >= ctx.Catalog.Joint.AirWingsPerCity)
                    {
                        continue;
                    }

                    Fixed64 km = ctx.World.Distances.GreatCircleKm(from, ctx.World.Grid.FromId(city.Hex));
                    if (km < bestKm)
                    {
                        bestKm = km;
                        best = city;
                    }
                }

                string at = " @ " + w.Id.Value.ToString(CultureInfo.InvariantCulture);
                if (best != null)
                {
                    Rebase(w, best.Id);
                    notes.Add(RebasedNote + at);
                }
                else
                {
                    Remove(ctx, w, false);
                    notes.Add(LostNote + at);
                }
            }
        }

        private static ValidationResult CheckBaseAt(WarfareContext ctx, CountryId country, HexCoord at, out City city)
        {
            city = null;
            if (!ctx.World.Grid.Contains(at))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HavaUssuGecersiz);
            }

            HexCell cell = ctx.World.Cell(at);
            if (!cell.City.IsValid)
            {
                return ValidationResult.Fail(WarfareErrorKeys.HavaUssuGecersiz);
            }

            city = ctx.World.Cities[cell.City];
            if (!IsAirBase(ctx, country, city))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HavaUssuGecersiz);
            }

            return WingsAt(ctx, city.Id) >= ctx.Catalog.Joint.AirWingsPerCity ? ValidationResult.Fail(WarfareErrorKeys.UsDolu) : ValidationResult.Ok;
        }
    }
}
