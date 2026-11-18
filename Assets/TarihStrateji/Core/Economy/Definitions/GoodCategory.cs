namespace TarihStrateji.Core.Economy.Definitions
{
    /// <summary>Mal sınıfı. Askerî sınıflar (ekipman, gemi, uçak) yalnız girdi tüketen tesislerden çıkar.</summary>
    public enum GoodCategory : byte
    {
        Food = 0,
        Raw = 1,
        Industrial = 2,
        Consumer = 3,
        MilitaryEquipment = 4,
        Ship = 5,
        Aircraft = 6,
    }
}
