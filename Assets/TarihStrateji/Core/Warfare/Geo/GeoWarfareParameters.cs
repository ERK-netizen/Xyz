using System;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Coğrafi savaş parametreleri (içerik verisi, ADR-011). Hareket maliyeti birimi "yürüyüş dakikası"dır:
    /// kenar türü başına km maliyeti <see cref="LandCostPerKm"/> / <see cref="SeaCostPerKm"/>; birliğin günlük
    /// bütçesi = tür hızı × <see cref="MarchCostPerSpeedUnit"/>.
    /// </summary>
    public sealed class GeoWarfareParameters
    {
        public GeoWarfareParameters(long[] landCostPerKm, long[] seaCostPerKm, Fixed64 marchCostPerSpeedUnit,
            RegionControlParameters regionControl, int buildingSlotsPerDevelopment, Fixed64 supplyRange)
        {
            LandCostPerKm = landCostPerKm ?? throw new ArgumentNullException(nameof(landCostPerKm));
            SeaCostPerKm = seaCostPerKm ?? throw new ArgumentNullException(nameof(seaCostPerKm));
            if (!marchCostPerSpeedUnit.IsPositive || buildingSlotsPerDevelopment < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(marchCostPerSpeedUnit));
            }

            MarchCostPerSpeedUnit = marchCostPerSpeedUnit;
            RegionControl = regionControl ?? throw new ArgumentNullException(nameof(regionControl));
            BuildingSlotsPerDevelopment = buildingSlotsPerDevelopment;
            if (!supplyRange.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(supplyRange));
            }

            SupplyRange = supplyRange;
        }

        /// <summary>İkmal menzili (hareket maliyeti birimi; ör. patikada km × patika maliyeti).</summary>
        public Fixed64 SupplyRange { get; }

        /// <summary>İndeks = <see cref="TransportEdgeKind"/>; 0 = kara kuvveti kullanamaz.</summary>
        public long[] LandCostPerKm { get; }

        public long[] SeaCostPerKm { get; }

        public Fixed64 MarchCostPerSpeedUnit { get; }

        public RegionControlParameters RegionControl { get; }

        public int BuildingSlotsPerDevelopment { get; }
    }
}
