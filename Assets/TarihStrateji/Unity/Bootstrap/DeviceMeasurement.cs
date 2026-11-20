using System;
using System.Diagnostics;
using System.IO;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Persistence;
using TarihStrateji.Core.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TarihStrateji.Unity.Bootstrap
{
    /// <summary>
    /// Cihazda performans ölçümü (Adım 17). Kare kare ilerler: (1) normal oyunda 240 kare süresi, (2) asıl oyunun
    /// anlık görüntüsünden otomatik kayıtsız ayrı bir çekirdek kopyası (oyuncunun oyunu ilerlemez, kayıtlarına
    /// dokunulmaz), (3) kopyada YZ dahil 60 günün her birinin süresi (karede bir gün), (4) kopyanın ayrı
    /// <c>olcum</c> klasörüne atomik kaydının süresi, (5) bellek ve cihaz bilgisi. Rapor
    /// <c>persistentDataPath/olcum_YYYYAAGG_SSDDss.txt</c> dosyasına ve günlüğe yazılır, özet arayüzde görünür.
    /// Süre ölçümü yalnız burada (Unity katmanı) yapılır; çekirdekte saat kullanılmaz.
    /// </summary>
    public sealed class DeviceMeasurement
    {
        public const int FrameSamples = 240;
        public const int DaySamples = 60;
        public const string Slot = "olcum";

        private readonly Func<SimulationKernel> _source;
        private readonly Func<SimulationSetup> _setupWithoutAutosave;
        private readonly string _directory;
        private readonly Stopwatch _watch = new Stopwatch();
        private MeasurementLog _log;
        private SimulationKernel _copy;
        private int _stage;

        public DeviceMeasurement(Func<SimulationKernel> source, Func<SimulationSetup> setupWithoutAutosave, string directory)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _setupWithoutAutosave = setupWithoutAutosave ?? throw new ArgumentNullException(nameof(setupWithoutAutosave));
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public bool IsRunning => _stage > 0;

        /// <summary>Boş (hiç ölçülmedi), ilerleme ya da son özet.</summary>
        public string Status { get; private set; } = string.Empty;

        public string LastReportPath { get; private set; } = string.Empty;

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            _log = new MeasurementLog
            {
                Device = SystemInfo.deviceModel + " · " + SystemInfo.processorType + " × " + SystemInfo.processorCount + " · "
                    + SystemInfo.systemMemorySize + " MB RAM · " + SystemInfo.graphicsDeviceName + " · " + Screen.width + "×" + Screen.height,
            };
            _copy = null;
            _stage = 1;
            Status = "Ölçülüyor: kare süreleri…";
        }

        /// <summary>Her karede bir kez (GameBootstrap.Update) çağrılır.</summary>
        public void Tick()
        {
            switch (_stage)
            {
                case 1:
                    _log.AddFrame((long)(Time.unscaledDeltaTime * 1_000_000f));
                    if (_log.FrameMicros.Count >= FrameSamples)
                    {
                        _stage = 2;
                        Status = "Ölçülüyor: kopya kuruluyor…";
                    }

                    break;
                case 2:
                    BuildCopy();
                    break;
                case 3:
                    StepDay();
                    break;
                case 4:
                    Finish();
                    break;
            }
        }

        private void BuildCopy()
        {
            _watch.Restart();
            byte[] snapshot = _source().CaptureSnapshot();
            Result<SimulationKernel> copy = SimulationKernel.Restore(_setupWithoutAutosave(), snapshot);
            _watch.Stop();
            _log.CopyMicros = Micros();
            _log.SnapshotBytes = snapshot.Length;
            if (!copy.IsSuccess)
            {
                Fail("kopya kurulamadı (" + copy.ErrorKey + ")");
                return;
            }

            _copy = copy.Value;
            _stage = 3;
        }

        private void StepDay()
        {
            _watch.Restart();
            Result<DayReport> report = _copy.AdvanceDay();
            _watch.Stop();
            if (!report.IsSuccess || report.Value.Halted)
            {
                Fail("kopya simülasyonu durdu");
                return;
            }

            _log.AddDay(Micros());
            Status = "Ölçülüyor: gün " + _log.DayMicros.Count + "/" + DaySamples;
            if (_log.DayMicros.Count >= DaySamples)
            {
                _stage = 4;
            }
        }

        private void Finish()
        {
            string saves = Path.Combine(_directory, "olcum");
            Directory.CreateDirectory(saves);
            _watch.Restart();
            ValidationResult saved = GameSaveService.Save(_copy, new FileSaveStore(saves), Slot, "Ölçüm");
            _watch.Stop();
            _log.SaveMicros = Micros();
            _log.ManagedBytes = GC.GetTotalMemory(false);
            _log.AllocatedBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            _copy = null;
            _stage = 0;

            DateTime now = DateTime.Now;
            string report = _log.Report(now.ToString("dd.MM.yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture))
                + (saved.IsValid ? string.Empty : "Kayıt hatası: " + saved.ErrorKey + "\n");
            LastReportPath = Path.Combine(_directory, "olcum_" + MeasurementLog.FileStamp(now) + ".txt");
            File.WriteAllText(LastReportPath, report);
            Debug.Log(report);
            Status = _log.Summary() + " · " + Path.GetFileName(LastReportPath);
        }

        private void Fail(string reason)
        {
            _copy = null;
            _stage = 0;
            Status = "Ölçüm yapılamadı: " + reason;
            Debug.LogWarning(Status);
        }

        private long Micros() => _watch.ElapsedTicks * 1_000_000L / Stopwatch.Frequency;
    }
}
