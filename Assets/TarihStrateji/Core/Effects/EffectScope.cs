namespace TarihStrateji.Core.Effects
{
    /// <summary>Etkinin hedef türü. Sayısal değerler kayıt biçimidir; değiştirilmez.</summary>
    public enum EffectScope : byte
    {
        Global = 0,
        Country = 1,
        Region = 2,
        Hex = 3,
        City = 4,
        Sector = 5,
        LandUnit = 6,
        Fleet = 7,
        AirWing = 8,
        Character = 9,
    }
}
