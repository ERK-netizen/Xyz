using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.AI.Perception
{
    /// <summary>Kurallarca geçerli bir inşaat seçeneği: tesis türü ve kendi sektörü.</summary>
    public readonly struct AiBuildOption
    {
        public AiBuildOption(FacilityTypeId facilityType, SectorId sector)
        {
            FacilityType = facilityType;
            Sector = sector;
        }

        public FacilityTypeId FacilityType { get; }

        public SectorId Sector { get; }
    }
}
