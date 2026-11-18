namespace TarihStrateji.Core.Persistence
{
    /// <summary><see cref="FileSaveStore.Load"/> sonucu.</summary>
    public sealed class SaveLoadResult
    {
        internal SaveLoadResult(SaveLoadStatus status, SaveFile file, string errorKey)
        {
            Status = status;
            File = file;
            ErrorKey = errorKey;
        }

        public SaveLoadStatus Status { get; }

        /// <summary>Başarılı yüklemede dosya; aksi halde <c>null</c>.</summary>
        public SaveFile File { get; }

        public string ErrorKey { get; }

        public bool Succeeded => File != null;

        public bool WasRecovered => Status == SaveLoadStatus.RecoveredFromTemp || Status == SaveLoadStatus.RecoveredFromBackup;
    }
}
