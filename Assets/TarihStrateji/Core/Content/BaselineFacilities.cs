using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Content
{
    /// <summary>Temel içerikteki tesis türü kimlikleri (kayıt biçiminin parçası; değiştirilmez).</summary>
    public static class BaselineFacilities
    {
        public static readonly FacilityTypeId Farm = new FacilityTypeId(1);
        public static readonly FacilityTypeId CoalMine = new FacilityTypeId(2);
        public static readonly FacilityTypeId IronMine = new FacilityTypeId(3);
        public static readonly FacilityTypeId SteelWorks = new FacilityTypeId(4);
        public static readonly FacilityTypeId ConsumerFactory = new FacilityTypeId(5);
        public static readonly FacilityTypeId ArmsFactory = new FacilityTypeId(6);
        public static readonly FacilityTypeId ArtilleryFactory = new FacilityTypeId(7);
        public static readonly FacilityTypeId Shipyard = new FacilityTypeId(8);
        public static readonly FacilityTypeId AircraftFactory = new FacilityTypeId(9);
    }
}
