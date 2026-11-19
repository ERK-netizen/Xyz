using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Birlik toplama ve terhis. Toplama: sahip ve kontrol ettiğimiz bir şehir hücresinde; asker o
    /// bölgeden seferber edilir, ekipman ortak envanterden çekilir. Terhis: kendi kontrolümüzdeki
    /// hücrede; askerler eve döner, ekipman (kalan düzeyde) envantere iade edilir.
    /// </summary>
    public static class UnitRules
    {
        public static ValidationResult CheckRecruit(WarfareContext ctx, CountryId country, UnitTypeId typeId, HexCoord at)
        {
            if (!ctx.Catalog.UnitTypes.TryGetValue(typeId, out UnitTypeDefinition type))
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikTuruBilinmiyor);
            }

            if (!ctx.World.Grid.Contains(at))
            {
                return ValidationResult.Fail(WarfareErrorKeys.ToplamaYeriGecersiz);
            }

            HexCell cell = ctx.World.Cell(at);
            if (!cell.City.IsValid || cell.Owner != country || cell.Controller != country)
            {
                return ValidationResult.Fail(WarfareErrorKeys.ToplamaYeriGecersiz);
            }

            if (!ctx.Population.Regions.TryGetValue(cell.Region, out RegionPopulation region))
            {
                return ValidationResult.Fail(WarfareErrorKeys.NufusYok);
            }

            ValidationResult manpower = PopulationRules.CheckMobilize(region, type.Manpower);
            if (!manpower.IsValid)
            {
                return manpower;
            }

            return InventoryRules.CheckAvailable(ctx.Economy, country, type.Equipment, Fixed64.One);
        }

        public static Result<LandUnitId> Recruit(WarfareContext ctx, CountryId country, UnitTypeId typeId, HexCoord at)
        {
            ValidationResult check = CheckRecruit(ctx, country, typeId, at);
            if (!check.IsValid)
            {
                return Result<LandUnitId>.Failure(check.ErrorKey);
            }

            UnitTypeDefinition type = ctx.Catalog.UnitTypes[typeId];
            HexCell cell = ctx.World.Cell(at);
            PopulationRules.Mobilize(ctx.Population.Regions[cell.Region], type.Manpower);
            InventoryRules.Consume(ctx.Economy, country, type.Equipment, Fixed64.One);
            var id = new LandUnitId(ctx.Warfare.UnitIds.Allocate());
            var unit = new LandUnit(id, country, typeId, cell.Id, cell.Region)
            {
                Manpower = type.Manpower,
                Equipment = Fixed64.One,
                Organization = type.MaxOrganization * ctx.Catalog.Parameters.InitialOrganization,
            };
            ctx.Warfare.Units.Add(id, unit);
            return Result<LandUnitId>.Success(id);
        }

        public static ValidationResult CheckOwned(WarfareContext ctx, CountryId country, LandUnitId unitId, out LandUnit unit)
        {
            if (!ctx.Warfare.Units.TryGetValue(unitId, out unit))
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikBulunamadi);
            }

            return unit.Owner != country ? ValidationResult.Fail(WarfareErrorKeys.BirlikBizimDegil) : ValidationResult.Ok;
        }

        public static ValidationResult CheckDisband(WarfareContext ctx, CountryId country, LandUnitId unitId)
        {
            ValidationResult owned = CheckOwned(ctx, country, unitId, out LandUnit unit);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (unit.IsEmbarked)
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikGemide);
            }

            return ctx.World.Cell(unit.Hex).Controller == country ? ValidationResult.Ok : ValidationResult.Fail(WarfareErrorKeys.TerhisYeriGecersiz);
        }

        public static ValidationResult Disband(WarfareContext ctx, CountryId country, LandUnitId unitId)
        {
            ValidationResult check = CheckDisband(ctx, country, unitId);
            if (check.IsValid)
            {
                Remove(ctx, ctx.Warfare.Units[unitId], true);
            }

            return check;
        }

        /// <summary>Birliği kaldırır: kalan asker eve döner; istenirse ekipman envantere iade edilir.</summary>
        public static void Remove(WarfareContext ctx, LandUnit unit, bool returnEquipment)
        {
            UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
            if (returnEquipment && unit.Equipment.IsPositive)
            {
                InventoryRules.Return(ctx.Economy, unit.Owner, type.Equipment, unit.Equipment);
            }

            CasualtyRules.SendHome(ctx, unit);
            ctx.Warfare.Units.Remove(unit.Id);
        }

        /// <summary>P0: aksiyon puanları günlük değere yenilenir (birikmez).</summary>
        public static void RefreshActionPoints(WarfareContext ctx)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                u.ActionPoints = ctx.Catalog.UnitTypes[u.Type].ActionPointsPerDay;
            }
        }
    }
}
