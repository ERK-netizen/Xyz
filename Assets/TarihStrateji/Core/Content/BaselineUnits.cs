using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Content
{
    /// <summary>Temel birlik türü kimlikleri (kayıt biçiminin parçası; değiştirilmez).</summary>
    public static class BaselineUnits
    {
        public static readonly UnitTypeId InfantryDivision = new UnitTypeId(1);
        public static readonly UnitTypeId ArtilleryRegiment = new UnitTypeId(2);
        public static readonly UnitTypeId CavalryDivision = new UnitTypeId(3);
    }
}
