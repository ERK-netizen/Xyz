namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>Oyun modları birbirinden ayrıdır ve oturum boyunca değişmez (CLAUDE.md §2.6).</summary>
    public enum GameMode
    {
        /// <summary>Kurallı oyun: hile/serbest düzenleme emirleri kesin reddedilir.</summary>
        Normal = 0,

        /// <summary>Serbest düzenleme: hile emirleri dahil her şey kabul edilir.</summary>
        Sandbox = 1,

        /// <summary>Gözlemci: oyuncu emir veremez, yalnız YZ oynar.</summary>
        Observer = 2,
    }
}
