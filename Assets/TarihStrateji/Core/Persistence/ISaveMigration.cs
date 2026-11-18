namespace TarihStrateji.Core.Persistence
{
    /// <summary>
    /// Bir biçim sürümünden bir sonrakine saf dönüşüm (save-systems). Yayımlanan her göç
    /// sonsuza dek korunur; silinmesi o sürümdeki tüm kayıtları kullanılmaz kılar.
    /// </summary>
    public interface ISaveMigration
    {
        /// <summary>Girdi sürümü; çıktı <c>FromVersion + 1</c>'dir.</summary>
        int FromVersion { get; }

        /// <summary>Üst veri ve yükü yeni biçime dönüştürür. Başarısızsa <c>false</c>.</summary>
        bool TryMigrate(byte[] metadata, byte[] payload, out byte[] newMetadata, out byte[] newPayload);
    }
}
