namespace TarihStrateji.Core.Content.Packages
{
    /// <summary>Kaynak envanteri kaydı (§22): veri kaynağı, lisans, adres, kaynak dosyanın SHA-256'sı, doğruluk notu.</summary>
    public sealed class PackageSource
    {
        public PackageSource(string id, string license, string url, string sha256, string accuracy)
        {
            Id = id;
            License = license;
            Url = url;
            Sha256 = sha256;
            Accuracy = accuracy;
        }

        public string Id { get; }

        public string License { get; }

        public string Url { get; }

        public string Sha256 { get; }

        public string Accuracy { get; }
    }
}
