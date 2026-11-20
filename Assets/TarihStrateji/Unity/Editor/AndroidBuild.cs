using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TarihStrateji.Unity.Editor
{
    /// <summary>
    /// Android yayın derlemesi (CLAUDE.md §4.2): IL2CPP, yalnız ARM64, Managed Stripping Medium +
    /// Assets/link.xml, dikey ekran, Development bayrağı yok. Oyun tamamen kodla kurulduğu için tek
    /// sahne boştur; yoksa oluşturulur. Komut satırı:
    /// <c>Unity -batchmode -nographics -quit -buildTarget Android -executeMethod
    /// TarihStrateji.Unity.Editor.AndroidBuild.BuildFromCommandLine [-ciktiYolu yol] [-aab]</c>.
    /// Sonuç <see cref="BuildResult.Succeeded"/> değilse çıkış kodu 1'dir.
    /// Yayın imzası (Adım 20): <c>TS_KEYSTORE_PATH</c>, <c>TS_KEYSTORE_PASS</c>, <c>TS_KEY_ALIAS</c>, <c>TS_KEY_PASS</c> ortam
    /// değişkenlerinin hepsi varsa APK/AAB bu anahtarla imzalanır (aynı anahtarla imzalı sürümler birbirinin üzerine
    /// kurulur, kayıtlar korunur); yoksa Unity'nin hata ayıklama anahtarı kullanılır. Anahtar ve parolalar depoya
    /// girmez; CI'da depo sırlarından geçici dosyaya yazılır. Sürüm kodu <c>TS_VERSION_CODE</c> (CI çalıştırma numarası)
    /// ya da <see cref="VersionCode"/>.
    /// </summary>
    public static class AndroidBuild
    {
        public const string ScenePath = "Assets/TarihStrateji/Scenes/Ana.unity";
        public const string ApplicationId = "com.tarihstrateji.oyun";
        public const string ProductName = "Tarih Strateji";
        public const string Version = "1.0.0";
        public const int VersionCode = 100;
        public const string DefaultApkPath = "Build/Android/TarihStrateji.apk";
        public const string DefaultAabPath = "Build/Android/TarihStrateji.aab";

        [MenuItem("Tarih Strateji/Android APK derle")]
        public static void BuildApkFromMenu()
        {
            BuildReport report = Build(DefaultApkPath, false);
            EditorUtility.DisplayDialog(ProductName, Summary(report), "Tamam");
        }

        public static void BuildFromCommandLine()
        {
            int exitCode = 1;
            try
            {
                bool appBundle = HasFlag("-aab");
                string output = Argument("-ciktiYolu") ?? (appBundle ? DefaultAabPath : DefaultApkPath);
                BuildReport report = Build(output, appBundle);
                string summary = Summary(report);
                Debug.Log(summary);
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)) ?? ".", "derleme-ozeti.txt"), summary, Encoding.UTF8);
                exitCode = report.summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            EditorApplication.Exit(exitCode);
        }

        public static BuildReport Build(string outputPath, bool appBundle)
        {
            ConfigurePlayer();
            EnsureScene();
            EditorUserBuildSettings.buildAppBundle = appBundle;
            EditorUserBuildSettings.development = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            if ((options.options & BuildOptions.Development) != 0)
            {
                throw new InvalidOperationException("Yayın derlemesinde BuildOptions.Development kullanılamaz.");
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return BuildPipeline.BuildPlayer(options);
        }

        public static void ConfigurePlayer()
        {
            NamedBuildTarget android = NamedBuildTarget.Android;
            PlayerSettings.companyName = "TarihStrateji";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            string code = Environment.GetEnvironmentVariable("TS_VERSION_CODE");
            PlayerSettings.Android.bundleVersionCode = int.TryParse(code, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int parsed) && parsed > 0
                ? VersionCode + parsed
                : VersionCode;
            ConfigureSigning();
            PlayerSettings.SetApplicationIdentifier(android, ApplicationId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Medium);
            PlayerSettings.SetApiCompatibilityLevel(android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.runInBackground = false;
        }

        /// <summary>Yayın anahtarı ortam değişkenleriyle verilmişse onu, yoksa hata ayıklama anahtarını seçer.</summary>
        public static bool ConfigureSigning()
        {
            string path = Environment.GetEnvironmentVariable("TS_KEYSTORE_PATH");
            string storePass = Environment.GetEnvironmentVariable("TS_KEYSTORE_PASS");
            string alias = Environment.GetEnvironmentVariable("TS_KEY_ALIAS");
            string keyPass = Environment.GetEnvironmentVariable("TS_KEY_PASS");
            bool release = !string.IsNullOrEmpty(path) && File.Exists(path) && !string.IsNullOrEmpty(storePass) && !string.IsNullOrEmpty(alias)
                && !string.IsNullOrEmpty(keyPass);
            PlayerSettings.Android.useCustomKeystore = release;
            if (release)
            {
                PlayerSettings.Android.keystoreName = Path.GetFullPath(path);
                PlayerSettings.Android.keystorePass = storePass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = keyPass;
            }

            Debug.Log(release ? "İmza: yayın anahtarı (" + alias + ")" : "İmza: hata ayıklama anahtarı (yayın anahtarı sırları tanımlı değil)");
            return release;
        }

        /// <summary>Boş ana sahneyi oluşturur (yoksa) ve derleme listesine tek sahne olarak yazar.</summary>
        public static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets");
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new IOException("Ana sahne kaydedilemedi: " + ScenePath);
                }

                AssetDatabase.Refresh();
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        public static string Summary(BuildReport report)
        {
            BuildSummary s = report.summary;
            var text = new StringBuilder();
            text.Append("Sonuç: ").Append(s.result).AppendLine();
            text.Append("Sürüm: ").Append(PlayerSettings.bundleVersion).Append(" (").Append(PlayerSettings.Android.bundleVersionCode.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append("), imza: ").Append(PlayerSettings.Android.useCustomKeystore ? "yayın anahtarı" : "hata ayıklama anahtarı").AppendLine();
            text.Append("Çıktı: ").Append(s.outputPath).AppendLine();
            text.Append("Boyut: ").Append((s.totalSize / (1024UL * 1024UL)).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(" MB").AppendLine();
            text.Append("Süre: ").Append(((int)s.totalTime.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(" s").AppendLine();
            text.Append("Hata: ").Append(s.totalErrors.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append(", uyarı: ").Append(s.totalWarnings.ToString(System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
            return text.ToString();
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static bool HasFlag(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
    }
}
