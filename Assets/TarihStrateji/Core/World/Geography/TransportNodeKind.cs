namespace TarihStrateji.Core.World.Geography
{
    /// <summary>Ulaşım düğümü türü. Değerler kayıtta/içerikte saklanır, değiştirilmez.</summary>
    public enum TransportNodeKind : byte
    {
        Junction = 1,
        City = 2,
        Port = 3,
        Pass = 4,
        Bridge = 5,
        Sea = 6,
    }
}
