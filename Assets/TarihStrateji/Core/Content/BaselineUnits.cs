using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Content
{
    /// <summary>Temel birlik türü kimlikleri (kayıt biçiminin parçası; değiştirilmez).</summary>
    public static class BaselineUnits
    {
        public static readonly UnitTypeId InfantryDivision = new UnitTypeId(1);
        public static readonly UnitTypeId ArtilleryRegiment = new UnitTypeId(2);
        public static readonly UnitTypeId CavalryDivision = new UnitTypeId(3);

        public static readonly UnitTypeId DestroyerFlotilla = new UnitTypeId(11);
        public static readonly UnitTypeId BattleshipSquadron = new UnitTypeId(12);
        public static readonly UnitTypeId TransportFlotilla = new UnitTypeId(13);

        public static readonly UnitTypeId FighterWing = new UnitTypeId(21);
        public static readonly UnitTypeId BomberWing = new UnitTypeId(22);
    }
}
