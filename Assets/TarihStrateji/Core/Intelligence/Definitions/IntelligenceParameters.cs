using System;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Intelligence.Definitions
{
    /// <summary>
    /// Savaş sisi ayarları (veri). Görüş yarıçapları ve <see cref="ExactRange"/> metredir; görüş
    /// ulaşım grafında (kara + deniz kenarları) yol uzunluğuyla yayılır.
    /// <see cref="ExactRange"/> içindeki temasın gücü kesin, ötesindeki
    /// <see cref="EstimateStep"/>'e yuvarlanmış tahmindir. Temas raporu
    /// <see cref="ContactMemoryDays"/> gün görülmezse unutulur.
    /// </summary>
    public sealed class IntelligenceParameters
    {
        public IntelligenceParameters(long landUnitSight, long fleetSight, long citySight, long airReconSight, long exactRange, Fixed64 estimateStep, int contactMemoryDays)
        {
            if (landUnitSight < 0L || fleetSight < 0L || citySight < 0L || airReconSight < 0L || exactRange < 0L || !estimateStep.IsPositive
                || estimateStep > Fixed64.One || contactMemoryDays < 1)
            {
                throw new ArgumentException(IntelligenceErrorKeys.TanimGecersiz);
            }

            LandUnitSight = landUnitSight;
            FleetSight = fleetSight;
            CitySight = citySight;
            AirReconSight = airReconSight;
            ExactRange = exactRange;
            EstimateStep = estimateStep;
            ContactMemoryDays = contactMemoryDays;
        }

        public long LandUnitSight { get; }

        public long FleetSight { get; }

        public long CitySight { get; }

        public long AirReconSight { get; }

        public long ExactRange { get; }

        public Fixed64 EstimateStep { get; }

        public int ContactMemoryDays { get; }
    }
}
