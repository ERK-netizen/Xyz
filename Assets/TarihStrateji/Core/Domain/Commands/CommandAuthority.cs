namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>Bir emrin hangi yetkiyle verilebildiği.</summary>
    public enum CommandAuthority
    {
        /// <summary>Kurallar içinde oyuncu emri (birlik hareketi, inşaat, diplomasi…).</summary>
        Player = 0,

        /// <summary>Kural dışı düzenleme (kaynak ekle, sahiplik değiştir…). Yalnız Sandbox.</summary>
        Sandbox = 1,
    }
}
