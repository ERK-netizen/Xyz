using System;
using System.Collections.Generic;
using System.IO;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Persistence;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.State;
using UnityEngine;

namespace TarihStrateji.Unity.Bootstrap
{
    /// <summary>
    /// Oyun oturumu: içerik, kurallar ve YZ kurulur; senaryolar <see cref="ScenarioCatalog"/>'tadır (1914, 1936, 1939). Son
    /// oturum kaydı varsa kendi senaryosunun haritasıyla (yan dosya <c>son_oturum.senaryo</c>; yoksa 1914) yüklenir,
    /// yoksa 1914 Osmanlı ile başlar; yeni oyun <see cref="StartNewGame"/> ile seçilir. Günler <see cref="DaysPerSecond"/> hızında ana iş
    /// parçacığında ilerler (karede en çok 4 gün). Oyun kuralı yazmaz: yalnız çekirdeği çalıştırır
    /// ve sunuma <see cref="IGameSession"/> verir. Uygulama arka plana alınınca veya kapanınca
    /// "son_oturum" yuvasına atomik kayıt yapılır; 30 günde bir otomatik kayıt halkası işler.
    /// Kayıt yuvaları (Adım 20): elle <c>yuva_1..3</c> ve otomatik halka senaryo başına ayrı klasördedir
    /// (<c>kayitlar/&lt;senaryo&gt;/</c>; harita içeriği senaryoya bağlı olduğundan); <c>son_oturum</c> kökte, yan dosyasıyla.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour, IGameSession
    {
        public const string LastSessionSlot = "son_oturum";
        /// <summary>Son oturumun senaryo kimliğini tutan yan dosya (kayıt klasöründe); yoksa 1914 sayılır.</summary>
        public const string ScenarioSidecar = LastSessionSlot + ".senaryo";
        private const int MaxDaysPerFrame = 4;

        /// <summary>Elle kayıt yuvaları (Adım 20).</summary>
        public static readonly string[] ManualSlots = { "yuva_1", "yuva_2", "yuva_3" };

        private SimulationKernel _kernel;
        private FileSaveStore _store;
        private BootstrapCommandFactory _orders;
        private float _accumulator;
        private int _pendingDays;
        private DeviceMeasurement _measurement;
        private readonly ScenarioCatalog _catalog = new ScenarioCatalog();
        private GameContent _content;
        private ScenarioCatalog.Entry _scenario;
        private string _directory;

        public string CurrentScenario => _scenario == null ? string.Empty : _scenario.Id;

        public event Action SessionReplaced;

        /// <summary>Tek seferde istenebilecek en çok gün (+30 düğmesi ve biraz pay).</summary>
        public const int MaxSkipDays = 90;
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

        public int PendingDays => _pendingDays;

        public string MeasurementStatus => _measurement == null ? string.Empty : _measurement.Status;

        public bool IsMeasuring => _measurement != null && _measurement.IsRunning;

        public void StartMeasurement()
        {
            if (_kernel != null && !_kernel.IsHalted)
            {
                _measurement.Start();
            }
        }

        public void SkipDays(int days)
        {
            if (days > 0 && _kernel != null && !_kernel.IsHalted)
            {
                _pendingDays = Mathf.Min(_pendingDays + days, MaxSkipDays);
            }
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
            _content = BaselineContent.Create();
            _directory = Path.Combine(Application.persistentDataPath, "kayitlar");
            Directory.CreateDirectory(_directory);
            _store = new FileSaveStore(_directory);
            _measurement = new DeviceMeasurement(() => _kernel, () => CreateSetup(_scenario, false), Application.persistentDataPath);
            _orders = new BootstrapCommandFactory(() => _kernel);

            // Son oturum kendi senaryosunun haritasıyla yüklenir; yoksa ya da yüklenemezse varsayılan senaryo başlar.
            _scenario = _catalog.Find(ReadSidecar());
            Result<LoadedGame> loaded = GameSaveService.Load(_store, LastSessionSlot, CreateSetup(_scenario, true));
            if (loaded.IsSuccess)
            {
                _kernel = loaded.Value.Kernel;
                return;
            }

            ScenarioCatalog.Entry fallback = _catalog.Find(ScenarioCatalog.DefaultId);
            ValidationResult created = StartGame(fallback, fallback.DefaultPlayer);
            if (!created.IsValid)
            {
                throw new InvalidOperationException("Oyun çekirdeği kurulamadı: " + created.ErrorKey);
            }
        }

        public void GetScenarios(List<ScenarioView> into) => _catalog.Views(into);

        public ValidationResult StartNewGame(string scenarioId, CountryId player)
        {
            ScenarioCatalog.Entry entry = _catalog.Find(scenarioId);
            if (!string.Equals(entry.Id, scenarioId, StringComparison.Ordinal))
            {
                return ValidationResult.Fail("hata.senaryo.bilinmiyor");
            }

            ValidationResult started = StartGame(entry, player);
            if (started.IsValid)
            {
                SessionReplaced?.Invoke();
            }

            return started;
        }

        private ValidationResult StartGame(ScenarioCatalog.Entry entry, CountryId player)
        {
            GeoPackage package = _catalog.Package(entry);
            bool known = false;
            for (int i = 0; i < package.Countries.Count; i++)
            {
                known |= package.Countries[i] == player;
            }

            if (!known)
            {
                return ValidationResult.Fail("hata.senaryo.ulkeYok");
            }

            ulong seed = (ulong)Environment.TickCount;
            GameState state = entry.Create(_content, package, seed, GameMode.Normal, player);
            Result<SimulationKernel> created = SimulationKernel.Create(CreateSetup(entry, true), state);
            if (!created.IsSuccess)
            {
                return ValidationResult.Fail(created.ErrorKey);
            }

            _scenario = entry;
            _kernel = created.Value;
            _pendingDays = 0;
            _accumulator = 0f;
            HaltReason = string.Empty;
            WriteSidecar();
            return ValidationResult.Ok;
        }

        private string ReadSidecar()
        {
            try
            {
                string path = Path.Combine(_directory, ScenarioSidecar);
                return File.Exists(path) ? File.ReadAllText(path).Trim() : ScenarioCatalog.DefaultId;
            }
            catch (IOException)
            {
                return ScenarioCatalog.DefaultId;
            }
        }

        private void WriteSidecar()
        {
            try
            {
                File.WriteAllText(Path.Combine(_directory, ScenarioSidecar), _scenario.Id);
            }
            catch (IOException e)
            {
                Debug.LogWarning("Senaryo kimliği yazılamadı: " + e.Message);
            }
        }

        /// <summary>Senaryonun kayıt klasörü (elle yuvalar + otomatik halka).</summary>
        private FileSaveStore ScenarioStore(ScenarioCatalog.Entry entry)
        {
            string directory = Path.Combine(_directory, entry.Id);
            Directory.CreateDirectory(directory);
            return new FileSaveStore(directory);
        }

        private static bool IsManualSlot(string slot) => Array.IndexOf(ManualSlots, slot) >= 0;

        public void GetSaveSlots(List<SaveSlotView> into)
        {
            string lastScenario = ReadSidecar();
            for (int e = 0; e < _catalog.Entries.Count; e++)
            {
                ScenarioCatalog.Entry entry = _catalog.Entries[e];
                FileSaveStore store = ScenarioStore(entry);
                List<string> slots = store.ListSlots();
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int i = 0; i < slots.Count; i++)
                    {
                        bool manual = IsManualSlot(slots[i]);
                        bool auto = slots[i].StartsWith(AutosaveRing.Prefix, StringComparison.Ordinal);
                        if ((pass == 0 && manual) || (pass == 1 && auto))
                        {
                            AddSlot(into, entry, store, slots[i], manual ? SaveSlotKind.Manual : SaveSlotKind.Autosave);
                        }
                    }
                }

                if (string.Equals(lastScenario, entry.Id, StringComparison.Ordinal))
                {
                    AddSlot(into, entry, _store, LastSessionSlot, SaveSlotKind.LastSession);
                }
            }
        }

        private void AddSlot(List<SaveSlotView> into, ScenarioCatalog.Entry entry, FileSaveStore store, string slot, SaveSlotKind kind)
        {
            SaveLoadResult read = store.Load(slot);
            if (!read.Succeeded)
            {
                return;
            }

            SaveMetadata meta = read.File.Metadata;
            string name = meta.PlayerCountry.IsValid ? _catalog.Package(entry).CountryNameKey(meta.PlayerCountry) : string.Empty;
            into.Add(new SaveSlotView(entry.Id, slot, meta.GameDate, meta.PlayerCountry, name, kind));
        }

        public ValidationResult SaveGame(string slot)
        {
            if (!IsManualSlot(slot))
            {
                return ValidationResult.Fail("hata.kayit.yuvaAdiGecersiz");
            }

            if (_kernel == null || _kernel.IsHalted)
            {
                return ValidationResult.Fail("hata.kayit.simulasyonDurdu");
            }

            return GameSaveService.Save(_kernel, ScenarioStore(_scenario), slot, "Elle kayıt");
        }

        public ValidationResult LoadGame(string scenarioId, string slot)
        {
            ScenarioCatalog.Entry entry = _catalog.Find(scenarioId);
            if (!string.Equals(entry.Id, scenarioId, StringComparison.Ordinal))
            {
                return ValidationResult.Fail("hata.senaryo.bilinmiyor");
            }

            bool last = string.Equals(slot, LastSessionSlot, StringComparison.Ordinal);
            if (last && !string.Equals(ReadSidecar(), entry.Id, StringComparison.Ordinal))
            {
                return ValidationResult.Fail("hata.kayit.bulunamadi");
            }

            Result<LoadedGame> loaded = GameSaveService.Load(last ? _store : ScenarioStore(entry), slot, CreateSetup(entry, true));
            if (!loaded.IsSuccess)
            {
                return ValidationResult.Fail(loaded.ErrorKey);
            }

            _scenario = entry;
            _kernel = loaded.Value.Kernel;
            _pendingDays = 0;
            _accumulator = 0f;
            HaltReason = string.Empty;
            WriteSidecar();
            SessionReplaced?.Invoke();
            return ValidationResult.Ok;
        }

        public ValidationResult DeleteSave(string scenarioId, string slot)
        {
            ScenarioCatalog.Entry entry = _catalog.Find(scenarioId);
            if (!string.Equals(entry.Id, scenarioId, StringComparison.Ordinal) || !IsManualSlot(slot))
            {
                return ValidationResult.Fail("hata.kayit.yuvaAdiGecersiz");
            }

            return ScenarioStore(entry).Delete(slot) ? ValidationResult.Ok : ValidationResult.Fail("hata.kayit.bulunamadi");
        }

        private SimulationSetup CreateSetup(ScenarioCatalog.Entry entry, bool autosave)
        {
            FileSaveStore store = autosave ? ScenarioStore(entry) : null;
            GeoPackage package = _catalog.Package(entry);
            GameContent content = _content;
            // Cihazda durum hash'i ve otomatik kayıt seyrek: 30 günde bir (CLAUDE.md §2.5).
            var setup = new SimulationSetup
            {
                GeoWorldTemplate = package.BuildWorld,
                CountryNameKeys = package.CountryNameKey,
                Capitals = CapitalTable.FromCityKeys(package.BuildWorld(), entry.CapitalCityKeys).Of,
                AiEarliestWarDate = entry.AiEarliestWar,
                HashIntervalDays = 30,
                // Ölçüm kopyası (store yok) otomatik kayıt yapmaz: oyuncunun kayıt halkasına dokunulmaz.
                AutosaveIntervalDays = store == null ? 0 : 30,
                AutosaveSink = store == null ? null : new AutosaveRing(store),
            };
            StandardRules.Install(setup, content);
            return StandardRules.InstallArtificialIntelligence(setup, content);
        }

        private void Update()
        {
            if (_kernel == null)
            {
                return;
            }

            // Cihaz ölçümü sürerken asıl oyun ilerlemez (süreler karışmasın).
            if (_measurement.IsRunning)
            {
                _measurement.Tick();
                return;
            }

            // +N gün: hızdan bağımsız, karede en çok MaxDaysPerFrame gün (arayüz akıcı kalır).
            if (_pendingDays > 0)
            {
                int skipped = 0;
                while (_pendingDays > 0 && skipped < MaxDaysPerFrame && !_kernel.IsHalted)
                {
                    _pendingDays--;
                    skipped++;
                    Result<DayReport> report = _kernel.AdvanceDay();
                    if (!report.IsSuccess || report.Value.Halted)
                    {
                        _pendingDays = 0;
                        break;
                    }
                }

                if (_kernel.IsHalted)
                {
                    HaltReason = string.Join("\n", _kernel.HaltReasons);
                    SetDaysPerSecond(0);
                    Debug.LogError("Simülasyon durdu: " + HaltReason);
                }

                return;
            }

            if (_daysPerSecond <= 0)
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
