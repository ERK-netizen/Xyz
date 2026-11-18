using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Temel içerik (1920'ler). Sayılar denge geçişlerinde (game-balance) burada değişir; kural
    /// kodu değişmez. Kaynak varsayımları:
    /// <list type="bullet">
    /// <item>Demografi: kaba doğum hızı ≈ ‰41, kaba ölüm hızı ≈ ‰23, büyüme ≈ %1,8/yıl
    /// (1927–35 Türkiye ≈ %2,1); yaş piramidi genç (0–14 ≈ %38).</item>
    /// <item>Ekonomi: 1 gıda birimi = 1000 kişinin günlük ihtiyacı; askerî mallar çelik girdisiz
    /// üretilmez.</item>
    /// </list>
    /// </summary>
    public static class BaselineContent
    {
        /// <summary>Binde paylar, 17 bant, toplam 1000.</summary>
        private static readonly int[] PyramidPerMille = { 145, 125, 110, 100, 90, 80, 70, 62, 55, 46, 38, 30, 22, 13, 8, 4, 2 };

        public static GameContent Create()
        {
            return new GameContent(CreateEconomy(), CreateDemography(), TerrainMovementTable.CreateBaseline());
        }

        public static DemographyParameters CreateDemography()
        {
            string[] fertility = { "0", "0", "0", "0.04", "0.13", "0.13", "0.11", "0.08", "0.04", "0.01", "0", "0", "0", "0", "0", "0", "0" };
            string[] mortality = { "0.09", "0.007", "0.004", "0.005", "0.007", "0.008", "0.009", "0.010", "0.012", "0.015", "0.020", "0.027", "0.038", "0.055", "0.085", "0.13", "0.22" };
            return new DemographyParameters(Parse(fertility), Parse(mortality), Fixed64.Parse("0.6"), Fixed64.FromInt(2));
        }

        /// <summary>Toplamı tam <paramref name="total"/> olan genç yaş piramidi (en büyük kalan yöntemi).</summary>
        public static long[] AgePyramid(long total)
        {
            if (total < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(total));
            }

            var cohorts = new long[AgeBands.Count];
            long assigned = 0L;
            for (int b = 0; b < AgeBands.Count; b++)
            {
                cohorts[b] = total / 1000L * PyramidPerMille[b] + (total % 1000L) * PyramidPerMille[b] / 1000L;
                assigned += cohorts[b];
            }

            for (int b = 0; assigned < total; b = (b + 1) % AgeBands.Count)
            {
                cohorts[b]++;
                assigned++;
            }

            return cohorts;
        }

        public static EconomyCatalog CreateEconomy()
        {
            var goods = new[]
            {
                new GoodDefinition(BaselineGoods.Food, "mal.gida", GoodCategory.Food, F("2")),
                new GoodDefinition(BaselineGoods.Coal, "mal.komur", GoodCategory.Raw, F("1")),
                new GoodDefinition(BaselineGoods.Iron, "mal.demir", GoodCategory.Raw, F("1.5")),
                new GoodDefinition(BaselineGoods.Steel, "mal.celik", GoodCategory.Industrial, F("6")),
                new GoodDefinition(BaselineGoods.ConsumerGoods, "mal.tuketim", GoodCategory.Consumer, F("4")),
                new GoodDefinition(BaselineGoods.InfantryEquipment, "mal.piyadeTechizati", GoodCategory.MilitaryEquipment, F("25")),
                new GoodDefinition(BaselineGoods.Artillery, "mal.top", GoodCategory.MilitaryEquipment, F("60")),
                new GoodDefinition(BaselineGoods.Ship, "mal.gemi", GoodCategory.Ship, F("8000")),
                new GoodDefinition(BaselineGoods.Aircraft, "mal.ucak", GoodCategory.Aircraft, F("900")),
            };

            SectorKind[] rural = { SectorKind.Residential, SectorKind.Industrial };
            SectorKind[] industrial = { SectorKind.Industrial };
            SectorKind[] port = { SectorKind.Port };
            var facilities = new[]
            {
                Facility(BaselineFacilities.Farm, "tesis.ciftlik", None(), A(BaselineGoods.Food, "25"), 5000, 1, "400", None(), 45, "3", rural),
                Facility(BaselineFacilities.CoalMine, "tesis.komurMadeni", None(), A(BaselineGoods.Coal, "20"), 4000, 1, "600", None(), 60, "4", rural),
                Facility(BaselineFacilities.IronMine, "tesis.demirMadeni", None(), A(BaselineGoods.Iron, "15"), 4000, 1, "700", None(), 60, "4", rural),
                Facility(BaselineFacilities.SteelWorks, "tesis.celikhane", A(BaselineGoods.Coal, "10", BaselineGoods.Iron, "10"), A(BaselineGoods.Steel, "6"), 3000, 2, "2500", None(), 120, "12", industrial),
                Facility(BaselineFacilities.ConsumerFactory, "tesis.tuketimFabrikasi", A(BaselineGoods.Steel, "1", BaselineGoods.Coal, "2"), A(BaselineGoods.ConsumerGoods, "8"), 2500, 1, "900", A(BaselineGoods.Steel, "5"), 75, "5", rural),
                Facility(BaselineFacilities.ArmsFactory, "tesis.silahFabrikasi", A(BaselineGoods.Steel, "3"), A(BaselineGoods.InfantryEquipment, "1"), 3000, 2, "3000", A(BaselineGoods.Steel, "20"), 150, "15", industrial),
                Facility(BaselineFacilities.ArtilleryFactory, "tesis.topFabrikasi", A(BaselineGoods.Steel, "4", BaselineGoods.Coal, "1"), A(BaselineGoods.Artillery, "0.3"), 3000, 2, "3500", A(BaselineGoods.Steel, "30"), 180, "18", industrial),
                Facility(BaselineFacilities.Shipyard, "tesis.tersane", A(BaselineGoods.Steel, "25", BaselineGoods.Coal, "5"), A(BaselineGoods.Ship, "0.004"), 6000, 3, "12000", A(BaselineGoods.Steel, "100"), 365, "40", port),
                Facility(BaselineFacilities.AircraftFactory, "tesis.ucakFabrikasi", A(BaselineGoods.Steel, "3", BaselineGoods.Coal, "1"), A(BaselineGoods.Aircraft, "0.02"), 3500, 2, "6000", A(BaselineGoods.Steel, "40"), 240, "25", industrial),
            };

            var parameters = new EconomyParameters(BaselineGoods.Food, F("1"), F("0.5"), F("0.5"), F("0.1"), F("0.25"), F("4"));
            Result<EconomyCatalog> catalog = EconomyCatalog.Create(goods, facilities, parameters);
            if (!catalog.IsSuccess)
            {
                throw new InvalidOperationException("Temel ekonomi içeriği geçersiz: " + catalog.ErrorKey);
            }

            return catalog.Value;
        }

        private static FacilityTypeDefinition Facility(
            FacilityTypeId id, string key, GoodAmount[] inputs, GoodAmount[] outputs, long workers, int slots,
            string money, GoodAmount[] buildGoods, int days, string upkeep, SectorKind[] sectors)
        {
            return new FacilityTypeDefinition(id, key, inputs, outputs, workers, slots, F(money), buildGoods, days, F(upkeep), sectors);
        }

        private static GoodAmount[] None() => new GoodAmount[0];

        private static GoodAmount[] A(GoodId good, string amount) => new[] { new GoodAmount(good, F(amount)) };

        private static GoodAmount[] A(GoodId g1, string a1, GoodId g2, string a2) => new[] { new GoodAmount(g1, F(a1)), new GoodAmount(g2, F(a2)) };

        private static Fixed64 F(string text) => Fixed64.Parse(text);

        private static Fixed64[] Parse(string[] values)
        {
            var result = new Fixed64[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = Fixed64.Parse(values[i]);
            }

            return result;
        }
    }
}
