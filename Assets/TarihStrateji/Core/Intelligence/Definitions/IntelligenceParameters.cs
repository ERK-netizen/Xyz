using System;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Intelligence.Definitions
{
    /// <summary>
    /// Savaş sisi ayarları (veri). Görüş yarıçapları hücre (altıgen adımı) cinsindendir.
    /// <see cref="ExactRange"/> içindeki temasın gücü kesin, ötesindeki
    /// <see cref="EstimateStep"/>'e yuvarlanmış tahmindir. Temas raporu
    /// <see cref="ContactMemoryDays"/> gün görülmezse unutulur.
    /// </summary>
    public sealed class IntelligenceParameters
    {
        public IntelligenceParameters(int landUnitSight, int fleetSight, int citySight, int airReconSight, int exactRange, Fixed64 estimateStep, int contactMemoryDays)
        {
            if (landUnitSight < 0 || fleetSight < 0 || citySight < 0 || airReconSight < 0 || exactRange < 0 || !estimateStep.IsPositive
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

        public int LandUnitSight { get; }

        public int FleetSight { get; }

        public int CitySight { get; }

        public int AirReconSight { get; }

        public int ExactRange { get; }

        public Fixed64 EstimateStep { get; }

        public int ContactMemoryDays { get; }
    }
}
