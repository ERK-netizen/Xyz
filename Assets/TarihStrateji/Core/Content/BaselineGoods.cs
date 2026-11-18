using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Content
{
    /// <summary>Temel içerikteki mal kimlikleri (kayıt biçiminin parçası; değiştirilmez).</summary>
    public static class BaselineGoods
    {
        public static readonly GoodId Food = new GoodId(1);
        public static readonly GoodId Coal = new GoodId(2);
        public static readonly GoodId Iron = new GoodId(3);
        public static readonly GoodId Steel = new GoodId(4);
        public static readonly GoodId ConsumerGoods = new GoodId(5);
        public static readonly GoodId InfantryEquipment = new GoodId(6);
        public static readonly GoodId Artillery = new GoodId(7);
        public static readonly GoodId Ship = new GoodId(8);
        public static readonly GoodId Aircraft = new GoodId(9);
    }
}
