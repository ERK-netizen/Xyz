using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.Definitions
{
    /// <summary>Mal türü tanımı (içerik verisi, değişmez).</summary>
    public sealed class GoodDefinition
    {
        public GoodDefinition(GoodId id, string key, GoodCategory category, Fixed64 basePrice)
        {
            Id = id;
            Key = key;
            Category = category;
            BasePrice = basePrice;
        }

        public GoodId Id { get; }

        /// <summary>Yerelleştirme anahtarı, ör. <c>mal.celik</c>.</summary>
        public string Key { get; }

        public GoodCategory Category { get; }

        /// <summary>Birim başına temel fiyat (vergi tabanı ve pazarın başlangıç fiyatı).</summary>
        public Fixed64 BasePrice { get; }

        public bool IsMilitary => Category == GoodCategory.MilitaryEquipment || Category == GoodCategory.Ship || Category == GoodCategory.Aircraft;
    }
}
