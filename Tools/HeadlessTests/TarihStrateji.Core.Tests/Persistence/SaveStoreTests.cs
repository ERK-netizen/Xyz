using System;
using System.IO;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Persistence;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Persistence;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Tests.World;

namespace TarihStrateji.Core.Tests.Persistence
{
    [TestFixture]
    public class SaveStoreTests
    {
        private string _dir;
        private FileSaveStore _store;

        [SetUp]
        public void CreateDirectory()
        {
            _dir = Path.Combine(Path.GetTempPath(), "tsk-test-" + Guid.NewGuid().ToString("N"));
            _store = new FileSaveStore(_dir);
        }

        [TearDown]
        public void DeleteDirectory()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        private static SimulationKernel NewKernel() =>
            TestSimulation.Kernel(TestSimulation.Setup(new RandomTurmoilSystem()), TestSimulation.NewState(77));

        private static SimulationSetup Setup() => TestSimulation.Setup(new RandomTurmoilSystem());

        [Test]
        public void SaveAndLoad_RoundTrip_ContinuesIdentically()
        {
            SimulationKernel kernel = NewKernel();
            TestSimulation.AdvanceOk(kernel, 10);
            Assert.That(GameSaveService.Save(kernel, _store, "kampanya_1", "Kurtuluş").IsValid, Is.True);
            Assert.That(File.Exists(_store.PathOf("kampanya_1") + ".tmp"), Is.False, "geçici dosya kalmaz");

            Result<LoadedGame> loaded = GameSaveService.Load(_store, "kampanya_1", Setup());
            Assert.That(loaded.IsSuccess, Is.True, loaded.ErrorKey);
            Assert.That(loaded.Value.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(loaded.Value.Metadata.Label, Is.EqualTo("Kurtuluş"));
            Assert.That(loaded.Value.Metadata.GameDate, Is.EqualTo(kernel.CurrentDate));
            Assert.That(loaded.Value.Metadata.PlayerCountry, Is.EqualTo(TestWorlds.CountryA));
            Assert.That(loaded.Value.Metadata.StateSchemaVersion, Is.EqualTo(GameStateCodec.SchemaVersion));

            SimulationKernel copy = loaded.Value.Kernel;
            TestSimulation.AdvanceOk(kernel, 20);
            TestSimulation.AdvanceOk(copy, 20);
            Assert.That(copy.ComputeStateHash(), Is.EqualTo(kernel.ComputeStateHash()));
        }

        [Test]
        public void CorruptPrimary_FallsBackToBackup()
        {
            SimulationKernel kernel = NewKernel();
            GameSaveService.Save(kernel, _store, "yuva", "eski");
            TestSimulation.AdvanceOk(kernel, 5);
            GameSaveService.Save(kernel, _store, "yuva", "yeni");
            string path = _store.PathOf("yuva");
            Assert.That(File.Exists(path + ".bak"), Is.True);

            byte[] bytes = File.ReadAllBytes(path);
            bytes[bytes.Length / 2] ^= 0x40; // tek bit çevir
            File.WriteAllBytes(path, bytes);

            Result<LoadedGame> loaded = GameSaveService.Load(_store, "yuva", Setup());
            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.Value.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackup));
            Assert.That(loaded.Value.Metadata.Label, Is.EqualTo("eski"));
            Assert.That(_store.Load("yuva").WasRecovered, Is.True);
        }

        [Test]
        public void CrashBetweenRenames_RecoversFromCompleteTempFile()
        {
            SimulationKernel kernel = NewKernel();
            GameSaveService.Save(kernel, _store, "yuva", "tam");
            string path = _store.PathOf("yuva");
            File.Move(path, path + ".tmp"); // ana dosya taşınmadan çöktü
            SaveLoadResult result = _store.Load("yuva");
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromTemp));
            Assert.That(result.File.Metadata.Label, Is.EqualTo("tam"));
        }

        [Test]
        public void TruncatedPrimaryWithoutBackup_IsCorrupt_NotHalfLoaded()
        {
            SimulationKernel kernel = NewKernel();
            GameSaveService.Save(kernel, _store, "yuva", "x");
            string path = _store.PathOf("yuva");
            byte[] bytes = File.ReadAllBytes(path);
            File.WriteAllBytes(path, bytes.AsSpan(0, bytes.Length - 10).ToArray());
            SaveLoadResult result = _store.Load("yuva");
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Corrupt));
            Assert.That(result.ErrorKey, Is.EqualTo(PersistenceErrorKeys.UzunlukUyusmuyor));
            Assert.That(GameSaveService.Load(_store, "yuva", Setup()).IsSuccess, Is.False);
            Assert.That(_store.Load("yok").Status, Is.EqualTo(SaveLoadStatus.NotFound));
        }

        [Test]
        public void NewerVersion_IsRefused_EvenWhenBackupExists()
        {
            SimulationKernel kernel = NewKernel();
            GameSaveService.Save(kernel, _store, "yuva", "eski");
            GameSaveService.Save(kernel, _store, "yuva", "yeni");
            byte[] future = SaveFileFormat.EncodeRaw(SaveFileFormat.CurrentVersion + 1, new byte[] { 1, 2 }, new byte[] { 3 });
            File.WriteAllBytes(_store.PathOf("yuva"), future);
            SaveLoadResult result = _store.Load("yuva");
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.TooNew));
            Assert.That(result.ErrorKey, Is.EqualTo(PersistenceErrorKeys.SurumDahaYeni));
        }

        [Test]
        public void ValidFileWithInvalidContent_IsSkippedForBackup()
        {
            SimulationKernel kernel = NewKernel();
            GameSaveService.Save(kernel, _store, "yuva", "saglam");
            GameSaveService.Save(kernel, _store, "yuva", "ikinci");
            var meta = new SaveMetadata("bozuk", kernel.CurrentDate, GameMode.Normal, TestWorlds.CountryA, 1);
            Assert.That(_store.Save("yuva", meta, new byte[] { 0x47, 0x41, 0x4D, 0x45, 9 }).IsValid, Is.True); // özet sağlam, içerik çöp
            Result<LoadedGame> loaded = GameSaveService.Load(_store, "yuva", Setup());
            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.Value.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackup));
            Assert.That(loaded.Value.Metadata.Label, Is.EqualTo("ikinci"));
        }

        [Test]
        public void FileFormat_DetectsTampering()
        {
            var meta = new SaveMetadata("x", TestSimulation.Start, GameMode.Normal, TestWorlds.CountryA, 1);
            byte[] file = SaveFileFormat.Encode(meta, new byte[] { 1, 2, 3, 4 });
            Assert.That(SaveFileFormat.Decode(file, null).IsSuccess, Is.True);

            byte[] badMagic = (byte[])file.Clone();
            badMagic[0] = (byte)'X';
            Assert.That(SaveFileFormat.Decode(badMagic, null).ErrorKey, Is.EqualTo(PersistenceErrorKeys.SihirliDiziYok));

            byte[] badPayload = (byte[])file.Clone();
            badPayload[badPayload.Length - 1] ^= 1;
            Assert.That(SaveFileFormat.Decode(badPayload, null).ErrorKey, Is.EqualTo(PersistenceErrorKeys.OzetUyusmuyor));

            byte[] zero = SaveFileFormat.EncodeRaw(0, new byte[1], new byte[1]);
            Assert.That(SaveFileFormat.Decode(zero, null).ErrorKey, Is.EqualTo(PersistenceErrorKeys.SurumGecersiz));
            Assert.That(SaveFileFormat.Decode(new byte[10], null).ErrorKey, Is.EqualTo(PersistenceErrorKeys.UzunlukUyusmuyor));
        }

        private sealed class AppendByteMigration : ISaveMigration
        {
            public AppendByteMigration(int from) => FromVersion = from;

            public int FromVersion { get; }

            public bool TryMigrate(byte[] metadata, byte[] payload, out byte[] newMetadata, out byte[] newPayload)
            {
                newMetadata = metadata;
                newPayload = new byte[payload.Length + 1];
                payload.CopyTo(newPayload, 0);
                newPayload[payload.Length] = (byte)FromVersion;
                return true;
            }
        }

        [Test]
        public void MigrationChain_AppliesStepsInOrder_AndReportsGaps()
        {
            var chain = new SaveMigrationChain();
            chain.Register(new AppendByteMigration(1));
            chain.Register(new AppendByteMigration(2));
            Assert.Throws<ArgumentException>(() => chain.Register(new AppendByteMigration(2)));

            byte[] meta = { 0 };
            byte[] payload = { 9 };
            Assert.That(chain.TryUpgrade(1, 3, ref meta, ref payload, out string error), Is.True, error);
            Assert.That(payload, Is.EqualTo(new byte[] { 9, 1, 2 }));

            byte[] p2 = { 9 };
            Assert.That(chain.TryUpgrade(1, 4, ref meta, ref p2, out string gap), Is.False);
            Assert.That(gap, Is.EqualTo(PersistenceErrorKeys.GocEksik));
        }

        [Test]
        public void SlotNames_Listing_AndDelete()
        {
            Assert.That(FileSaveStore.IsValidSlotName("kampanya-1_a"), Is.True);
            foreach (string bad in new[] { "", "Buyuk", "a/b", "..", "çok", new string('a', 41), null })
            {
                Assert.That(FileSaveStore.IsValidSlotName(bad), Is.False, bad);
            }

            var meta = new SaveMetadata("x", TestSimulation.Start, GameMode.Normal, TestWorlds.CountryA, 1);
            Assert.That(_store.Save("../kacis", meta, new byte[1]).ErrorKey, Is.EqualTo(PersistenceErrorKeys.YuvaAdiGecersiz));
            _store.Save("b", meta, new byte[1]);
            _store.Save("a", meta, new byte[1]);
            _store.Save("a", meta, new byte[2]);
            Assert.That(_store.ListSlots(), Is.EqualTo(new[] { "a", "b" }), ".bak listelenmez");
            Assert.That(_store.Delete("a"), Is.True);
            Assert.That(File.Exists(_store.PathOf("a") + ".bak"), Is.False);
            Assert.That(_store.Delete("a"), Is.False);
        }

        [Test]
        public void AutosaveRing_RotatesThroughSlots_AndResumesAtOldest()
        {
            var ring = new AutosaveRing(_store, 3);
            SimulationSetup setup = Setup();
            setup.AutosaveSink = ring;
            setup.AutosaveIntervalDays = 1;
            SimulationKernel kernel = TestSimulation.Kernel(setup, TestSimulation.NewState(5));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(kernel.AdvanceDay().Value.Autosaved, Is.True);
            }

            Assert.That(ring.LastError, Is.Null);
            Assert.That(_store.ListSlots(), Is.EqualTo(new[] { "otomatik_0", "otomatik_1", "otomatik_2" }));
            Assert.That(_store.Load("otomatik_0").File.Metadata.GameDate, Is.EqualTo(kernel.CurrentDate), "4. kayıt halkada başa döner");
            Assert.That(_store.Load("otomatik_1").File.Metadata.GameDate, Is.EqualTo(kernel.CurrentDate.AddDays(-2)));

            var resumed = new AutosaveRing(_store, 3);
            resumed.Autosave(kernel.State, kernel.CaptureSnapshot());
            Assert.That(resumed.LastSlot, Is.EqualTo("otomatik_1"), "yeni oturum en eski yuvadan devam eder");
            Assert.That(GameSaveService.Load(_store, "otomatik_2", Setup()).IsSuccess, Is.True);
        }
    }
}
