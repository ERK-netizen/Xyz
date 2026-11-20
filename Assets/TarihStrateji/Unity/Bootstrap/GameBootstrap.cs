using System;
using System.IO;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Persistence;
using TarihStrateji.Core.Simulation;
using UnityEngine;

namespace TarihStrateji.Unity.Bootstrap
{
    /// <summary>
    /// Oyun oturumu: içerik, kurallar ve YZ kurulur; "1914 MVP" coğrafi paketi
    /// (<c>Resources/Senaryolar/1914_mvp</c>) okunur; son oturum kaydı varsa (aynı haritayla) yüklenir, yoksa
    /// senaryo Osmanlı İmparatorluğu ile başlar. Günler <see cref="DaysPerSecond"/> hızında ana iş
    /// parçacığında ilerler (karede en çok 4 gün). Oyun kuralı yazmaz: yalnız çekirdeği çalıştırır
    /// ve sunuma <see cref="IGameSession"/> verir. Uygulama arka plana alınınca veya kapanınca
    /// "son_oturum" yuvasına atomik kayıt yapılır; 30 günde bir otomatik kayıt halkası işler.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour, IGameSession
    {
        public const string LastSessionSlot = "son_oturum";
        public const string PackageResource = "Senaryolar/1914_mvp";
        private const int MaxDaysPerFrame = 4;

        private SimulationKernel _kernel;
        private FileSaveStore _store;
        private BootstrapCommandFactory _orders;
        private float _accumulator;
        private int _daysPerSecond = 1;

        public IGameReadModel ReadModel => _kernel;

        public IGameCommandGateway Commands => _kernel;

        public IGameCommandFactory Orders => _orders;

        public int DaysPerSecond => _daysPerSecond;

        /// <summary>Son durdurma nedeni (değişmez ihlali vb.) veya boş.</summary>
        public string HaltReason { get; private set; } = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnStartup()
        {
            if (FindAnyObjectByType<GameBootstrap>() != null)
            {
                return;
            }

            var host = new GameObject("TarihStrateji.Oturum");
            DontDestroyOnLoad(host);
            host.AddComponent<GameBootstrap>();
        }

        public void SetDaysPerSecond(int days)
        {
            _daysPerSecond = Mathf.Clamp(days, 0, 8);
            _accumulator = 0f;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            GameContent content = BaselineContent.Create();
            GeoPackage package = LoadPackage();
            string directory = Path.Combine(Application.persistentDataPath, "kayitlar");
            Directory.CreateDirectory(directory);
            _store = new FileSaveStore(directory);
            SimulationSetup setup = CreateSetup(content, package, _store);

            Result<LoadedGame> loaded = GameSaveService.Load(_store, LastSessionSlot, setup);
            if (loaded.IsSuccess)
            {
                _kernel = loaded.Value.Kernel;
            }
            else
            {
                ulong seed = (ulong)Environment.TickCount;
                Result<SimulationKernel> created = SimulationKernel.Create(setup, ScenarioFactory.Create1914(content, package, seed, GameMode.Normal, Scenario1914.Ottoman));
                if (!created.IsSuccess)
                {
                    throw new InvalidOperationException("Oyun çekirdeği kurulamadı: " + created.ErrorKey);
                }

                _kernel = created.Value;
            }

            _orders = new BootstrapCommandFactory(() => _kernel);
        }

        private static GeoPackage LoadPackage()
        {
            var asset = Resources.Load<TextAsset>(PackageResource);
            if (asset == null)
            {
                throw new InvalidOperationException("Senaryo paketi bulunamadı: Resources/" + PackageResource);
            }

            Result<GeoPackage> package = GeoPackageReader.Read(asset.bytes);
            if (!package.IsSuccess)
            {
                throw new InvalidOperationException("Senaryo paketi okunamadı: " + package.ErrorKey);
            }

            return package.Value;
        }

        private static SimulationSetup CreateSetup(GameContent content, GeoPackage package, FileSaveStore store)
        {
            // Cihazda durum hash'i ve otomatik kayıt seyrek: 30 günde bir (CLAUDE.md §2.5).
            var setup = new SimulationSetup
            {
                GeoWorldTemplate = package.BuildWorld,
                CountryNameKeys = package.CountryNameKey,
                HashIntervalDays = 30,
                AutosaveIntervalDays = 30,
                AutosaveSink = new AutosaveRing(store),
            };
            StandardRules.Install(setup, content);
            return StandardRules.InstallArtificialIntelligence(setup, content);
        }

        private void Update()
        {
            if (_kernel == null || _daysPerSecond <= 0)
            {
                return;
            }

            if (_kernel.IsHalted)
            {
                HaltReason = string.Join("\n", _kernel.HaltReasons);
                SetDaysPerSecond(0);
                Debug.LogError("Simülasyon durdu: " + HaltReason);
                return;
            }

            _accumulator += Time.unscaledDeltaTime * _daysPerSecond;
            int steps = 0;
            while (_accumulator >= 1f && steps < MaxDaysPerFrame)
            {
                _accumulator -= 1f;
                steps++;
                Result<DayReport> report = _kernel.AdvanceDay();
                if (!report.IsSuccess || report.Value.Halted)
                {
                    break;
                }
            }

            if (_accumulator > MaxDaysPerFrame)
            {
                _accumulator = 0f;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveSession();
            }
        }

        private void OnApplicationQuit()
        {
            SaveSession();
        }

        private void SaveSession()
        {
            if (_kernel == null || _kernel.IsHalted)
            {
                return;
            }

            ValidationResult saved = GameSaveService.Save(_kernel, _store, LastSessionSlot, "Son oturum");
            if (!saved.IsValid)
            {
                Debug.LogWarning("Kayıt yazılamadı: " + saved.ErrorKey);
            }
        }
    }
}
