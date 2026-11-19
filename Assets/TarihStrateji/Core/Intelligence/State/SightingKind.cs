namespace TarihStrateji.Core.Intelligence.State
{
    /// <summary>Gözlenen varlık türü. Değerler kayıt biçimine yazılır; asla yeniden numaralanmaz.</summary>
    public enum SightingKind : byte
    {
        LandUnit = 1,
        Fleet = 2,
        AirWing = 3,
    }
}
