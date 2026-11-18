namespace TarihStrateji.Core.Persistence
{
    /// <summary>Çözülmüş ve güncel biçime göç ettirilmiş kayıt dosyası.</summary>
    public sealed class SaveFile
    {
        internal SaveFile(int originalFormatVersion, SaveMetadata metadata, byte[] payload)
        {
            OriginalFormatVersion = originalFormatVersion;
            Metadata = metadata;
            Payload = payload;
        }

        /// <summary>Dosyanın diskteki biçim sürümü (göçten önce).</summary>
        public int OriginalFormatVersion { get; }

        public SaveMetadata Metadata { get; }

        /// <summary>Güncel biçimdeki durum yükü (GameStateCodec baytları).</summary>
        public byte[] Payload { get; }
    }
}
