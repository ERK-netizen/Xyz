using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;
using TarihStrateji.Core.Tests.Support;

namespace TarihStrateji.Core.Tests.Architecture
{
    /// <summary>
    /// CLAUDE.md §2'deki değiştirilemez kuralların otomatik denetimi. Bir test burada
    /// kırılıyorsa kod anayasayı ihlal ediyordur; testi gevşetmek anayasa değişikliğidir.
    /// </summary>
    [TestFixture]
    public class ConstitutionTests
    {
        private const string Prefix = "TarihStrateji.Core.";

        /// <summary>CLAUDE.md §2.2 doğrudan bağımlılıkları (geçişli kapanış serbesttir).</summary>
        private static readonly Dictionary<string, string[]> Constitution = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Domain"] = Array.Empty<string>(),
            ["World"] = new[] { "Domain" },
            ["Effects"] = new[] { "Domain" },
            ["Population"] = new[] { "World" },
            ["Diplomacy"] = new[] { "World" },
            ["Intelligence"] = new[] { "World" },
            ["Research"] = new[] { "Effects" },
            ["Politics"] = new[] { "Effects" },
            ["Characters"] = new[] { "Research" },
            ["Economy"] = new[] { "Population", "Effects", "World" },
            ["Logistics"] = new[] { "Economy", "World" },
            ["Warfare"] = new[] { "Logistics", "World", "Effects" },
            ["AI"] = new[]
            {
                "Intelligence", "World", "Effects", "Population", "Economy", "Research",
                "Politics", "Characters", "Diplomacy", "Logistics", "Warfare",
            },
            ["Content"] = new[] { "Domain", "World", "Research", "Economy", "Politics", "Characters", "Logistics", "Warfare", "Diplomacy", "Intelligence", "AI" },
            ["Simulation"] = new[]
            {
                "Domain", "World", "Effects", "Population", "Economy", "Research", "Politics",
                "Characters", "Diplomacy", "Intelligence", "Logistics", "Warfare", "AI", "Content",
            },
            ["Persistence"] = new[] { "Domain", "Simulation" },
        };

        private static readonly Regex FloatKeyword = new Regex(@"\b(float|double|decimal)\b", RegexOptions.Compiled);

        private static readonly Regex FloatLiteral = new Regex(
            @"(?<![\w.])\d[\d_]*\.\d[\d_]*(?:[eE][+-]?\d+)?[fFdDmM]?(?!\w)" +
            @"|(?<![\w.])\d[\d_]*(?:[eE][+-]?\d+)?[fFdDmM](?!\w)" +
            @"|(?<![\w.])\d[\d_]*[eE][+-]?\d+(?!\w)",
            RegexOptions.Compiled);

        private static readonly string[] BannedTokens =
        {
            "UnityEngine", "UnityEditor", "System.Random", "DateTime.Now", "DateTime.UtcNow",
            "DateTime.Today", "Environment.TickCount", "Stopwatch", "Guid.NewGuid",
            "Task.Run", "Parallel.", "new Thread(", "ThreadPool", "NotImplementedException",
        };

        private static string[] CoreSources() =>
            Directory.GetFiles(RepoPaths.CoreDir, "*.cs", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();

        private static HashSet<string> Closure(string module)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>(Constitution[module]);
            while (stack.Count > 0)
            {
                string m = stack.Pop();
                if (result.Add(m))
                {
                    foreach (string d in Constitution[m])
                    {
                        stack.Push(d);
                    }
                }
            }

            return result;
        }

        private static JsonElement ReadAsmdef(string path)
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.Clone();
        }

        private static string[] References(JsonElement asmdef) =>
            asmdef.GetProperty("references").EnumerateArray().Select(e => e.GetString()).ToArray();

        [Test]
        public void ConstitutionGraph_IsAcyclic()
        {
            foreach (string module in Constitution.Keys)
            {
                Assert.That(Closure(module), Does.Not.Contain(module), $"{module} kendine bağımlı (döngü)");
            }
        }

        [Test]
        public void EveryCoreModule_HasExactlyOneCompliantAsmdef()
        {
            string[] folders = Directory.GetDirectories(RepoPaths.CoreDir)
                .Select(Path.GetFileName)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
            Assert.That(folders, Is.EquivalentTo(Constitution.Keys), "Core modül kümesi CLAUDE.md §2.2 ile aynı olmalı");

            foreach (string module in folders)
            {
                string[] asmdefs = Directory.GetFiles(Path.Combine(RepoPaths.CoreDir, module), "*.asmdef", SearchOption.AllDirectories);
                Assert.That(asmdefs.Length, Is.EqualTo(1), $"{module}: tam bir .asmdef olmalı");
                Assert.That(Path.GetFileName(asmdefs[0]), Is.EqualTo(Prefix + module + ".asmdef"));
                Assert.That(Path.GetDirectoryName(asmdefs[0]), Is.EqualTo(Path.Combine(RepoPaths.CoreDir, module)));

                JsonElement json = ReadAsmdef(asmdefs[0]);
                Assert.That(json.GetProperty("name").GetString(), Is.EqualTo(Prefix + module));
                Assert.That(json.GetProperty("noEngineReferences").GetBoolean(), Is.True, $"{module}: noEngineReferences");
                Assert.That(json.GetProperty("autoReferenced").GetBoolean(), Is.False, $"{module}: autoReferenced");
                Assert.That(json.GetProperty("allowUnsafeCode").GetBoolean(), Is.False, $"{module}: allowUnsafeCode");
                Assert.That(json.GetProperty("precompiledReferences").GetArrayLength(), Is.EqualTo(0), $"{module}: precompiledReferences");
            }
        }

        [Test]
        public void AsmdefReferences_StayInsideConstitutionClosure()
        {
            foreach (string module in Constitution.Keys)
            {
                string path = Path.Combine(RepoPaths.CoreDir, module, Prefix + module + ".asmdef");
                HashSet<string> allowed = Closure(module);
                foreach (string reference in References(ReadAsmdef(path)))
                {
                    Assert.That(reference, Does.StartWith(Prefix), $"{module} Core dışına başvuruyor");
                    Assert.That(allowed, Does.Contain(reference.Substring(Prefix.Length)),
                        $"{module} → {reference} CLAUDE.md §2.2'de izinli değil");
                }
            }
        }

        [Test]
        public void HeadlessProjects_MirrorAsmdefReferences()
        {
            foreach (string module in Constitution.Keys)
            {
                string asmdef = Path.Combine(RepoPaths.CoreDir, module, Prefix + module + ".asmdef");
                string csproj = Path.Combine(RepoPaths.Root, "Tools", "HeadlessTests", "CoreProjects", Prefix + module, Prefix + module + ".csproj");
                Assert.That(File.Exists(csproj), Is.True, $"{csproj} yok: sync_core_projects.py çalıştırın");

                string[] projectRefs = XDocument.Load(csproj).Descendants("ProjectReference")
                    .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include").Value))
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToArray();
                string[] asmdefRefs = References(ReadAsmdef(asmdef)).OrderBy(n => n, StringComparer.Ordinal).ToArray();
                Assert.That(projectRefs, Is.EqualTo(asmdefRefs), $"{module}: sync_core_projects.py çalıştırın");
            }
        }

        [Test]
        public void PresentationAssembly_ReferencesOnlyDomain()
        {
            string path = Path.Combine(RepoPaths.UnityDir, "Presentation", "TarihStrateji.Unity.Presentation.asmdef");
            Assert.That(References(ReadAsmdef(path)), Is.EqualTo(new[] { Prefix + "Domain" }));
        }

        [Test]
        public void BootstrapAssembly_ReferencesOnlyItsAllowedClosure()
        {
            // §2.2: Unity.Bootstrap → Simulation, Persistence, Content (ve onların geçişli kapanışı).
            string path = Path.Combine(RepoPaths.UnityDir, "Bootstrap", "TarihStrateji.Unity.Bootstrap.asmdef");
            var allowed = new HashSet<string>(StringComparer.Ordinal) { "Simulation", "Persistence", "Content" };
            foreach (string root in new[] { "Simulation", "Persistence", "Content" })
            {
                allowed.UnionWith(Closure(root));
            }

            foreach (string reference in References(ReadAsmdef(path)))
            {
                Assert.That(reference.StartsWith(Prefix, StringComparison.Ordinal) && allowed.Contains(reference.Substring(Prefix.Length)), Is.True,
                    "Bootstrap izinsiz derlemeye başvuruyor: " + reference);
            }
        }

        [Test]
        public void EditorAssembly_IsEditorOnly_AndTouchesNoGameRules()
        {
            // Derleme betiği (Unity.Editor) oyuna girmez ve kural derlemelerine başvurmaz.
            JsonElement json = ReadAsmdef(Path.Combine(RepoPaths.UnityDir, "Editor", "TarihStrateji.Unity.Editor.asmdef"));
            Assert.That(json.GetProperty("includePlatforms").EnumerateArray().Select(e => e.GetString()).ToArray(), Is.EqualTo(new[] { "Editor" }));
            Assert.That(References(json), Is.Empty);
        }

        [Test]
        public void UnityAssemblies_AreExactlyTheKnownSet()
        {
            string[] names = Directory.GetFiles(RepoPaths.UnityDir, "*.asmdef", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            Assert.That(names, Is.EqualTo(new[] { "TarihStrateji.Unity.Bootstrap", "TarihStrateji.Unity.Editor", "TarihStrateji.Unity.Presentation" }));
        }

        [Test]
        public void CoreSources_DeclareTheirModuleNamespace()
        {
            var nsRegex = new Regex(@"^\s*namespace\s+([\w.]+)", RegexOptions.Multiline);
            foreach (string file in CoreSources())
            {
                string module = Path.GetRelativePath(RepoPaths.CoreDir, file).Split(Path.DirectorySeparatorChar)[0];
                string code = CSharpSourceScanner.StripCommentsAndStrings(File.ReadAllText(file));
                MatchCollection matches = nsRegex.Matches(code);
                Assert.That(matches.Count, Is.GreaterThan(0), $"{file}: namespace yok");
                foreach (Match m in matches)
                {
                    string ns = m.Groups[1].Value;
                    Assert.That(ns == Prefix + module || ns.StartsWith(Prefix + module + ".", StringComparison.Ordinal), Is.True,
                        $"{file}: '{ns}' isim alanı {Prefix}{module} altında olmalı");
                }
            }
        }

        [Test]
        public void CoreSources_ContainNoFloatingPointOrNondeterministicApis()
        {
            var violations = new List<string>();
            foreach (string file in CoreSources())
            {
                string raw = File.ReadAllText(file);
                string code = CSharpSourceScanner.StripCommentsAndStrings(raw);
                string[] lines = code.Split('\n');
                for (int n = 0; n < lines.Length; n++)
                {
                    string line = lines[n];
                    string where = $"{Path.GetRelativePath(RepoPaths.Root, file)}:{n + 1}";
                    if (FloatKeyword.IsMatch(line))
                    {
                        violations.Add($"{where}: kayan noktalı tip");
                    }

                    if (FloatLiteral.IsMatch(line))
                    {
                        violations.Add($"{where}: kayan noktalı literal '{FloatLiteral.Match(line).Value}'");
                    }

                    foreach (string token in BannedTokens)
                    {
                        if (line.Contains(token, StringComparison.Ordinal))
                        {
                            violations.Add($"{where}: yasak '{token}'");
                        }
                    }
                }

                if (raw.Contains("TODO", StringComparison.Ordinal) || raw.Contains("FIXME", StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(RepoPaths.Root, file)}: TODO/FIXME (sıfır eksik kod kuralı)");
                }
            }

            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
        }

        [Test]
        public void Scanner_DetectsViolations_AndIgnoresCommentsAndStrings()
        {
            string bad = "float x = 1.5f; var y = 2e3; long z = 0x1F; long w = 1_000_000L;";
            string clean = CSharpSourceScanner.StripCommentsAndStrings(bad);
            Assert.That(FloatKeyword.IsMatch(clean), Is.True);
            Assert.That(FloatLiteral.Matches(clean).Select(m => m.Value), Is.EqualTo(new[] { "1.5f", "2e3" }));

            string benign = "// float double 1.5\n string s = \"double 2.5\"; char c = '.'; /* decimal */ long v = 0xFFd;";
            string stripped = CSharpSourceScanner.StripCommentsAndStrings(benign);
            Assert.That(FloatKeyword.IsMatch(stripped), Is.False, stripped);
            Assert.That(FloatLiteral.IsMatch(stripped), Is.False, stripped);
            Assert.That(stripped.Split('\n').Length, Is.EqualTo(2), "satır numaraları korunmalı");
        }

        [Test]
        public void UnityVersion_IsPinnedTo63Lts()
        {
            string text = File.ReadAllText(Path.Combine(RepoPaths.Root, "ProjectSettings", "ProjectVersion.txt"));
            Assert.That(Regex.IsMatch(text, @"^m_EditorVersion: 6000\.3\.\d+f\d+\s*$", RegexOptions.Multiline), Is.True, text);
        }
    }
}
