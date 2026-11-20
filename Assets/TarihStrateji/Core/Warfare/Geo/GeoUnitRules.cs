using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Coğrafi birlik toplama: yalnız sahip + kontrol + kuşatılmamış kendi şehrinde; asker şehrin bölge
    /// nüfusundan seferber edilir, ekipman ortak envanterden çekilir (yoktan üretim yok, §2.6).
    /// </summary>
    public static class GeoUnitRules
    {
        public static ValidationResult CheckRecruit(GeoWarfareContext ctx, CountryId country, UnitTypeId typeId, CityId at)
        {
            if (!ctx.Catalog.UnitTypes.TryGetValue(typeId, out UnitTypeDefinition type))
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikTuruBilinmiyor);
            }

            if (!ctx.World.TryGetCity(at, out City city) || city.Owner != country || city.Controller != country || city.Siege == SiegeState.Besieged)
            {
                return ValidationResult.Fail(WarfareErrorKeys.ToplamaYeriGecersiz);
            }

            if (!ctx.Population.Regions.TryGetValue(city.Region, out RegionPopulation region))
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

        public static Result<LandUnitId> Recruit(GeoWarfareContext ctx, CountryId country, UnitTypeId typeId, CityId at)
        {
            ValidationResult check = CheckRecruit(ctx, country, typeId, at);
            if (!check.IsValid)
            {
                return Result<LandUnitId>.Failure(check.ErrorKey);
            }

            UnitTypeDefinition type = ctx.Catalog.UnitTypes[typeId];
            ctx.World.TryGetCity(at, out City city);
            PopulationRules.Mobilize(ctx.Population.Regions[city.Region], type.Manpower);
            InventoryRules.Consume(ctx.Economy, country, type.Equipment, Fixed64.One);
            var id = new LandUnitId(ctx.Warfare.UnitIds.Allocate());
            var unit = new LandUnit(id, country, typeId, city.Node, city.Region)
            {
                Manpower = type.Manpower,
                Equipment = Fixed64.One,
                Organization = type.MaxOrganization * ctx.Catalog.Parameters.InitialOrganization,
            };
            ctx.Warfare.Units.Add(id, unit);
            return Result<LandUnitId>.Success(id);
        }

        public static ValidationResult CheckOwned(GeoWarfareContext ctx, CountryId country, LandUnitId unitId, out LandUnit unit)
        {
            if (!ctx.Warfare.Units.TryGetValue(unitId, out unit))
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikBulunamadi);
            }

            return unit.Owner == country ? ValidationResult.Ok : ValidationResult.Fail(WarfareErrorKeys.BirlikBizimDegil);
        }

        /// <summary>Terhis: birlik gemide değil, bir düğümde durur ve düğümün bölgesi ülkenin fiilî kontrolündedir.</summary>
        public static ValidationResult CheckDisband(GeoWarfareContext ctx, CountryId country, LandUnitId unitId)
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

            return unit.RouteProgress == 0L && ctx.World.Transport.TryGetNode(unit.Node, out TransportNode node)
                && ctx.World.TryGetControl(node.Region, out RegionControl control) && control.Controller == country
                ? ValidationResult.Ok
                : ValidationResult.Fail(WarfareErrorKeys.TerhisYeriGecersiz);
        }

        /// <summary>Birliği terhis eder: asker eve döner, ekipman envantere iade edilir.</summary>
        public static ValidationResult Disband(GeoWarfareContext ctx, CountryId country, LandUnitId unitId)
        {
            ValidationResult check = CheckDisband(ctx, country, unitId);
            if (check.IsValid)
            {
                GeoCombatRules.Remove(ctx, ctx.Warfare.Units[unitId], true);
            }

            return check;
        }
    }
}
