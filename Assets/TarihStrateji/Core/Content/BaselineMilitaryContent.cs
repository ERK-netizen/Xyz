using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.World.Map;

using TarihStrateji.Core.Warfare.Geo;

using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Temel kara harbi içeriği (1920'ler). Bir piyade tümeni 10 000 asker ve 100 piyade
    /// teçhizatıdır; savunmada (16) saldırıdan (10) güçlüdür. Eşit güçteki muharebede iki taraf
    /// da günde azami teşkilatın %20'sini kaybeder; şans günlük güce ±%75 uygulanır; teşkilatı
    /// %30'un altına düşen savunan geri çekilir. Arazi: tepe ×2, dağ ×2,5, orman ×1,5.
    /// Denge bandı (game-balance, testle kilitli): eşit piyade ovada %10–30, 2:1 ovada %60–90,
    /// 3:1 tepede %55–80, 3:1 dağda %35–65 saldırgan başarısı ("3:1 kuralı").
    /// Deniz ve hava (Adım 7): çıkarma saldırısı ×0,5; konvoy limanı ikmal seviyesi 0,7; bir
    /// bombardıman kanadı yakın destekte tam güçte bir tümen saldırısının yaklaşık %40'ını ekler.
    /// </summary>
    public static class BaselineMilitaryContent
    {
        public static WarfareCatalog CreateWarfare(EconomyCatalog economy)
        {
            var types = new[]
            {
                new UnitTypeDefinition(BaselineUnits.InfantryDivision, "birlik.piyadeTumeni", 10_000,
                    new[] { new GoodAmount(BaselineGoods.InfantryEquipment, F("100")) },
                    F("10"), F("16"), F("60"), F("30"), F("2"), F("1")),
                new UnitTypeDefinition(BaselineUnits.ArtilleryRegiment, "birlik.topcuAlayi", 3_000,
                    new[] { new GoodAmount(BaselineGoods.Artillery, F("36")), new GoodAmount(BaselineGoods.InfantryEquipment, F("10")) },
                    F("16"), F("6"), F("40"), F("25"), F("1"), F("1")),
                new UnitTypeDefinition(BaselineUnits.CavalryDivision, "birlik.suvariTumeni", 6_000,
                    new[] { new GoodAmount(BaselineGoods.InfantryEquipment, F("60")) },
                    F("9"), F("8"), F("50"), F("55"), F("3"), F("1")),
            };

            var terrain = new Fixed64[TerrainRules.TerrainTypeCount];
            terrain[(int)TerrainType.Ocean] = Fixed64.One;
            terrain[(int)TerrainType.Lake] = Fixed64.One;
            terrain[(int)TerrainType.Plains] = Fixed64.One;
            terrain[(int)TerrainType.Forest] = F("1.5");
            terrain[(int)TerrainType.Hills] = F("2");
            terrain[(int)TerrainType.Mountains] = F("2.5");
            terrain[(int)TerrainType.Desert] = Fixed64.One;
            terrain[(int)TerrainType.Marsh] = F("1.6");
            terrain[(int)TerrainType.Tundra] = F("1.1");

            var parameters = new WarfareParameters(
                terrain,
                fortressBonusPerSector: F("0.25"),
                baseDamage: F("0.2"),
                strengthLossPerDamage: F("0.25"),
                retreatOrganization: F("0.3"),
                minimumAttackOrganization: F("0.3"),
                garrisonDefense: F("6"),
                besiegedGarrisonFactor: F("0.5"),
                outOfSupplyOrganizationLoss: F("0.05"),
                outOfSupplyStrengthLoss: F("0.005"),
                organizationRecovery: F("0.05"),
                reinforcementRate: F("0.02"),
                destroyedBelowStrength: F("0.1"),
                initialOrganization: F("0.5"),
                randomSpread: F("0.75"));
            // Filolar: gemi malından çekilir; hız km/gün. Nakliye filosu iki tümen taşır, savaşmaz.
            var ships = new[]
            {
                new ShipTypeDefinition(BaselineUnits.DestroyerFlotilla, "filo.muhripFilotillasi", 1_500,
                    new[] { new GoodAmount(BaselineGoods.Ship, F("3")) }, F("6"), F("50"), F("700"), 0, F("1")),
                new ShipTypeDefinition(BaselineUnits.BattleshipSquadron, "filo.zirhliFilosu", 3_000,
                    new[] { new GoodAmount(BaselineGoods.Ship, F("4")) }, F("20"), F("60"), F("450"), 0, F("6")),
                new ShipTypeDefinition(BaselineUnits.TransportFlotilla, "filo.nakliyeFilosu", 1_000,
                    new[] { new GoodAmount(BaselineGoods.Ship, F("2")) }, Fixed64.Zero, F("30"), F("400"), 2, Fixed64.Zero),
            };

            // Hava kanatları: avcı hava üstünlüğü, bombardıman yakın destek; menzil üssten km.
            var air = new[]
            {
                new AirWingTypeDefinition(BaselineUnits.FighterWing, "kanat.avci", 150,
                    new[] { new GoodAmount(BaselineGoods.Aircraft, F("50")) }, F("12"), F("1"), F("40"), F("350")),
                new AirWingTypeDefinition(BaselineUnits.BomberWing, "kanat.bombardiman", 300,
                    new[] { new GoodAmount(BaselineGoods.Aircraft, F("40")) }, F("3"), F("4"), F("40"), F("700")),
            };

            var joint = new JointWarfareParameters(
                amphibiousAttackFactor: F("0.5"),
                convoySupplyLevel: F("0.7"),
                portRecovery: F("0.1"),
                airMissionOrganizationCost: F("0.05"),
                airRecovery: F("0.1"),
                airWingsPerCity: 4);
            Result<WarfareCatalog> catalog = WarfareCatalog.Create(types, ships, air, parameters, joint, economy);
            if (!catalog.IsSuccess)
            {
                throw new InvalidOperationException("Temel harp içeriği geçersiz: " + catalog.ErrorKey);
            }

            return catalog.Value;
        }

        /// <summary>İkmal menzili: ovada yolsuz ≈ 400 km; demiryolu boyunca 1600 km'ye kadar.</summary>
        public static LogisticsParameters CreateLogistics() => new LogisticsParameters(F("400"));

        /// <summary>
        /// Coğrafi savaş (ADR-011). Maliyet birimi yürüyüş dakikası/km; günlük bütçe = hız × 20. Piyade (30)
        /// yolda 30 km, patikada 20 km, demiryolunda 300 km/gün; boğaz geçişi 10 km/gün; muhrip (700) denizde
        /// 700 km/gün. Bölge işgali 3 gün ikmalli tutma, güvenli hâle gelme 7 gün; şehir gelişim seviyesi
        /// başına 4 yapı slotu; ikmal menzili patikada ≈ 400 km.
        /// </summary>
        public static GeoWarfareParameters CreateGeoWarfare() => new GeoWarfareParameters(
            new long[] { 0L, 30L, 20L, 2L, 0L, 60L },
            new long[] { 0L, 0L, 0L, 0L, 20L, 0L },
            F("20"),
            new RegionControlParameters(3, 7),
            4,
            F("12000"));

        private static Fixed64 F(string text) => Fixed64.Parse(text);
    }
}
