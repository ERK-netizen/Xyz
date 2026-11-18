namespace TarihStrateji.Core.World.Grid
{
    /// <summary>
    /// Sivri tepeli altıgenin altı komşu yönü. Sıra sabittir ve tüm döngüler bu sırayla
    /// yürür (determinizm). Kuzey = satır numarası azalır.
    /// </summary>
    public enum HexDirection
    {
        East = 0,
        NorthEast = 1,
        NorthWest = 2,
        West = 3,
        SouthWest = 4,
        SouthEast = 5,
    }
}
