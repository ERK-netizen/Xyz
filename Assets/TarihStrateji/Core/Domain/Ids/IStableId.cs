namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>
    /// Tüm tipli kimliklerin ortak sözleşmesi. Değer 0 "yok" anlamına gelir; geçerli
    /// kimlikler 1'den başlar, yalnız artar ve asla yeniden kullanılmaz (CLAUDE.md §2.4).
    /// Simülasyon döngüleri bu değerin artan sırasıyla yürür.
    /// </summary>
    public interface IStableId
    {
        ulong Value { get; }

        bool IsValid { get; }
    }
}
