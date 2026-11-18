namespace TarihStrateji.Core.World.Cities
{
    /// <summary>
    /// Ebeveyn hücre içindeki 7 yuvalı "çiçek": merkez + 6 halka yuvası. Halka yuvası
    /// <c>n</c>, <c>HexDirection (n - 1)</c> yönündeki dünya komşusuna bakar.
    /// </summary>
    public enum SectorSlot
    {
        Center = 0,
        East = 1,
        NorthEast = 2,
        NorthWest = 3,
        West = 4,
        SouthWest = 5,
        SouthEast = 6,
    }
}
