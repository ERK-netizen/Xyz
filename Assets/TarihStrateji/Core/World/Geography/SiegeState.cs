namespace TarihStrateji.Core.World.Geography
{
    /// <summary>Şehrin kuşatma durumu (ADR-012). Değerler kayıtta saklanır.</summary>
    public enum SiegeState : byte
    {
        None = 0,
        /// <summary>Düşman çevreliyor; dış bağlantılar kısmen açık.</summary>
        Encircling = 1,
        /// <summary>Tüm kara ve deniz bağlantıları kesik; ikmal kaynağı olamaz.</summary>
        Besieged = 2,
    }
}
