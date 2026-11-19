namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Ulaşım kenarı türü: kara (patika/açık arazi, yol, demiryolu), boğaz geçişi (kara birliğinin
    /// kısa su geçişi, en çok <c>TransportGraph.MaxStraitCrossingMeters</c>) ve deniz yolu (yalnız
    /// filolar). Kara kenarı yalnız aynı ya da kara geçişine açık komşu bölgeleri bağlar.
    /// </summary>
    public enum TransportEdgeKind : byte
    {
        Track = 1,
        Road = 2,
        Rail = 3,
        SeaLane = 4,
        StraitCrossing = 5,
    }
}
