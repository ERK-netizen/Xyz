namespace TarihStrateji.Core.Persistence
{
    /// <summary>Yükleme sonucu. "Recovered" durumlarında oyuncuya bildirilir.</summary>
    public enum SaveLoadStatus
    {
        Loaded = 0,
        RecoveredFromTemp = 1,
        RecoveredFromBackup = 2,
        NotFound = 3,
        Corrupt = 4,
        TooNew = 5,
    }
}
