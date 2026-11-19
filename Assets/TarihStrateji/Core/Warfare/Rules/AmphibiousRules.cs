using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Gemiye binme ve çıkarma (CLAUDE.md §2.6). Nakliye kapasitesi 0 olan filoya binilemez ve
    /// ondan çıkarma yapılamaz: ikisi de doğrulamada reddedilir. Gemideki birlik kara hareketi veya
    /// kara saldırısı yapamaz; çıkarma hedefi filonun bulunduğu limandır ya da filo hücresine komşu
    /// bir kara hücresidir. Hedefte düşman varsa çıkarma muharebesi (<see cref="JointWarfareParameters.AmphibiousAttackFactor"/>)
    /// P13'te çözülür; boş ve girilebilir hedefe birlik doğrudan çıkar.
    /// </summary>
    public static class AmphibiousRules
    {
        public const string LandedNote = "bilgi.deniz.cikarmaYapildi";

        public static ValidationResult CheckEmbark(WarfareContext ctx, CountryId country, LandUnitId unitId, FleetId fleetId)
        {
            ValidationResult owned = UnitRules.CheckOwned(ctx, country, unitId, out LandUnit unit);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (unit.IsEmbarked)
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikGemide);
            }

            ValidationResult fleetOwned = FleetRules.CheckOwned(ctx, country, fleetId, out Fleet fleet);
            if (!fleetOwned.IsValid)
            {
                return fleetOwned;
            }

            ShipTypeDefinition type = ctx.Catalog.ShipTypes[fleet.Type];
            if (type.TransportCapacity == 0)
            {
                return ValidationResult.Fail(WarfareErrorKeys.NakliyeKapasitesiYok);
            }

            if (fleet.Hex != unit.Hex || !ctx.World.Cell(fleet.Hex).City.IsValid)
            {
                return ValidationResult.Fail(WarfareErrorKeys.AyniLimandaDegil);
            }

            return NavalQueries.CarriedCount(ctx, fleetId) >= type.TransportCapacity
                ? ValidationResult.Fail(WarfareErrorKeys.NakliyeKapasitesiDolu)
                : ValidationResult.Ok;
        }

        public static void Embark(WarfareContext ctx, LandUnit unit, Fleet fleet)
        {
            MovementRules.ClearOrders(unit);
            unit.Carrier = fleet.Id;
            unit.Hex = fleet.Hex;
        }

        public static ValidationResult CheckLanding(WarfareContext ctx, CountryId country, LandUnitId unitId, HexCoord target)
        {
            ValidationResult owned = UnitRules.CheckOwned(ctx, country, unitId, out LandUnit unit);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (!unit.IsEmbarked)
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikGemideDegil);
            }

            if (!ctx.Warfare.Fleets.TryGetValue(unit.Carrier, out Fleet fleet) || ctx.Catalog.ShipTypes[fleet.Type].TransportCapacity == 0)
            {
                return ValidationResult.Fail(WarfareErrorKeys.NakliyeKapasitesiYok);
            }

            if (!ctx.World.Grid.Contains(target))
            {
                return ValidationResult.Fail(WarfareErrorKeys.CikarmaHedefiGecersiz);
            }

            HexCell cell = ctx.World.Cell(target);
            if (cell.IsWater)
            {
                return ValidationResult.Fail(WarfareErrorKeys.CikarmaHedefiGecersiz);
            }

            if (cell.Id == fleet.Hex)
            {
                // Limanda demirli filodan karaya inmek.
                return ValidationResult.Ok;
            }

            if (!CombatRules.IsAdjacent(ctx, fleet.Hex, cell.Id, out _))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefKomsuDegil);
            }

            if (IsAssault(ctx, country, cell))
            {
                UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
                return unit.Organization < type.MaxOrganization * ctx.Catalog.Parameters.MinimumAttackOrganization
                    ? ValidationResult.Fail(WarfareErrorKeys.MoralYetersiz)
                    : ValidationResult.Ok;
            }

            return UnitQueries.CanEnter(ctx, country, cell) ? ValidationResult.Ok : ValidationResult.Fail(WarfareErrorKeys.CikarmaHedefiGecersiz);
        }

        /// <summary>Hedefte düşman birliği veya düşman elinde sektör/şehir var mı (muharebeli çıkarma).</summary>
        public static bool IsAssault(WarfareContext ctx, CountryId country, HexCell cell)
        {
            if (UnitQueries.HasHostileUnits(ctx, cell.Id, country))
            {
                return true;
            }

            if (!cell.City.IsValid)
            {
                return false;
            }

            City city = ctx.World.Cities[cell.City];
            for (int i = 0; i < city.Sectors.Count; i++)
            {
                if (UnitQueries.IsHostile(ctx, country, ctx.World.Sectors[city.Sectors[i]].Controller))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Birlik gemiden iner (muharebesiz çıkarma veya kazanılmış çıkarma muharebesi).</summary>
        public static void Disembark(LandUnit unit, HexId hex)
        {
            MovementRules.ClearOrders(unit);
            unit.Carrier = FleetId.None;
            unit.Hex = hex;
        }
    }
}
