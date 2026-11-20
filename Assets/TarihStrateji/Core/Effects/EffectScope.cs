namespace TarihStrateji.Core.Effects
{
    /// <summary>Etkinin hedef türü. Sayısal değerler kayıt biçimidir; değiştirilmez (3 ve 5 emekli altıgen/sektör kapsamlarıdır, yeniden kullanılmaz).</summary>
    public enum EffectScope : byte
    {
        Global = 0,
        Country = 1,
        Region = 2,
        City = 4,
        LandUnit = 6,
        Fleet = 7,
        AirWing = 8,
        Character = 9,
    }
}
