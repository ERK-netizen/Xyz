namespace TarihStrateji.Core.Domain.Geography
{
    /// <summary>Emir hedefinin türü (TS-GDD-TDD-3.1 §3.2, EK A.2). Değerler kayıtta saklanır, değiştirilmez.</summary>
    public enum TargetKind : byte
    {
        None = 0,
        LandUnit = 1,
        City = 2,
        Region = 3,
        Point = 4,
        Fleet = 5,
        AirBase = 6,
    }
}
