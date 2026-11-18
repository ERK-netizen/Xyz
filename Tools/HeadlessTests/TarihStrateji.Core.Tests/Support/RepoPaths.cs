using System;
using System.IO;

namespace TarihStrateji.Core.Tests.Support
{
    /// <summary>Test çalışma klasöründen yukarı çıkarak depo kökünü (CLAUDE.md) bulur.</summary>
    public static class RepoPaths
    {
        private static readonly Lazy<string> RootLazy = new Lazy<string>(FindRoot);

        public static string Root => RootLazy.Value;

        public static string CoreDir => Path.Combine(Root, "Assets", "TarihStrateji", "Core");

        public static string UnityDir => Path.Combine(Root, "Assets", "TarihStrateji", "Unity");

        private static string FindRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "CLAUDE.md"))
                    && Directory.Exists(Path.Combine(dir.FullName, "Assets")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("Depo kökü (CLAUDE.md + Assets/) bulunamadı.");
        }
    }
}
