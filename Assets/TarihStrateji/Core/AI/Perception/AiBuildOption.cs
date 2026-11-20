using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.AI.Perception
{
    /// <summary>Kurallarca geçerli bir inşaat seçeneği: tesis türü ve kendi şehri.</summary>
    public readonly struct AiBuildOption
    {
        public AiBuildOption(FacilityTypeId facilityType, CityId city)
        {
            FacilityType = facilityType;
            City = city;
        }

        public FacilityTypeId FacilityType { get; }

        public CityId City { get; }
    }
}
