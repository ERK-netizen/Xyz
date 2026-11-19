using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Tests.Support;

namespace TarihStrateji.Core.Tests.Localization
{
    /// <summary>
    /// Türkçe metin tablosu (Unity/Presentation/Resources/Localization/tr.txt) kapsamı: kural
    /// kodundaki her hata/bilgi anahtarı, içerikteki her ad anahtarı, senaryo bölgeleri ve
    /// Unity katmanında kullanılan her arayüz anahtarı tabloda bulunmalıdır. Biçim: "anahtar = metin".
    /// </summary>
    [TestFixture]
    public class LocalizationCoverageTests
    {
        private static readonly Regex MessageKey = new Regex("\"((?:hata|bilgi)\\.[^\" ]+)\"", RegexOptions.Compiled);

        private static readonly Regex ContentKey = new Regex(
            "\"((?:birlik|filo|kanat|teknoloji|cag|olay|secenek|odak|mal|tesis|parti|kurum|kriz|karakter|sehir)\\.[A-Za-z0-9.]*[A-Za-z0-9])\"",
            RegexOptions.Compiled);

        private static readonly Regex UiKey = new Regex("\"((?:arayuz|ulke|arazi)\\.[A-Za-z0-9.]+)\"", RegexOptions.Compiled);

        private static string TablePath => Path.Combine(RepoPaths.UnityDir, "Presentation", "Resources", "Localization", "tr.txt");

        private static Dictionary<string, string> Table()
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string raw in File.ReadAllLines(TablePath))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int eq = line.IndexOf(" = ", StringComparison.Ordinal);
                Assert.That(eq, Is.GreaterThan(0), "biçim 'anahtar = metin' olmalı: " + line);
                string key = line.Substring(0, eq).Trim();
                string text = line.Substring(eq + 3).Trim();
                Assert.That(table.ContainsKey(key), Is.False, "tekrarlanan anahtar: " + key);
                Assert.That(text.Length, Is.GreaterThan(0), "boş metin: " + key);
                Assert.That(Regex.IsMatch(key, "^[a-z][A-Za-z0-9.]*$"), Is.True, "anahtar ASCII ve noktalı olmalı: " + key);
                table[key] = text;
            }

            return table;
        }

        private static IEnumerable<string> Literals(string root, Regex pattern)
        {
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                foreach (Match m in pattern.Matches(File.ReadAllText(file)))
                {
                    yield return m.Groups[1].Value;
                }
            }
        }

        private static void AssertAllPresent(IEnumerable<string> keys, string what)
        {
            Dictionary<string, string> table = Table();
            string[] missing = keys.Distinct(StringComparer.Ordinal).Where(k => !table.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal).ToArray();
            Assert.That(missing, Is.Empty, what + " tr.txt'de eksik:\n" + string.Join("\n", missing));
        }

        [Test]
        public void EveryRuleMessageKey_HasTurkishText() => AssertAllPresent(Literals(RepoPaths.CoreDir, MessageKey), "Hata/bilgi anahtarları");

        [Test]
        public void EveryContentNameKey_HasTurkishText() =>
            AssertAllPresent(Literals(Path.Combine(RepoPaths.CoreDir, "Content"), ContentKey), "İçerik ad anahtarları");

        [Test]
        public void EveryScenarioRegion_HasTurkishText() =>
            AssertAllPresent(AegeanScenario.CitySeeds.Select(c => "bolge.ege." + c.Key.Substring("sehir.ege.".Length)), "Senaryo bölgeleri");

        [Test]
        public void EveryUnityUiKey_HasTurkishText() => AssertAllPresent(Literals(RepoPaths.UnityDir, UiKey), "Arayüz anahtarları");
    }
}
