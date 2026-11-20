using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Tests.Support;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Scenario
{
    /// <summary>
    /// ContentCompiler çıktısı "1914 MVP" paketi (§22, MAP-G09, REG-021/022/028): bütünlük özeti, şema, tarihî
    /// sahiplik düzeltmeleri, geometri/graf doğrulaması, bağlantılılık ve taze dünya kurulumu.
    /// </summary>
    [TestFixture]
    public class GeoPackageTests
    {
        public static string PackagePath =>
            Path.Combine(RepoPaths.UnityDir, "Bootstrap", "Resources", "Senaryolar", "1914_mvp.bytes");

        private static readonly CountryId Ottoman = new CountryId(1);
        private static readonly CountryId Greece = new CountryId(2);
        private static readonly CountryId Bulgaria = new CountryId(3);
        private static readonly CountryId Russia = new CountryId(10);
        private static readonly CountryId Britain = new CountryId(11);

        public static GeoPackage Load()
        {
            Result<GeoPackage> package = GeoPackageReader.Read(File.ReadAllBytes(PackagePath));
            Assert.That(package.IsSuccess, Is.True, package.ErrorKey);
            return package.Value;
        }

        private static PackageCity City(GeoPackage p, string key) =>
            ((List<PackageCity>)new List<PackageCity>(p.Cities)).Find(c => c.NameKey == "sehir.1914." + key);

        [Test]
        public void Package_LoadsAndValidates_WithSourceInventory()
        {
            GeoPackage p = Load();
            Assert.That(p.Scenario, Is.EqualTo("1914_mvp"));
            Assert.That(p.Date, Is.EqualTo(GameDate.FromYmd(1914, 6, 28)));
            Assert.That(p.Countries.Count, Is.EqualTo(12));
            Assert.That(p.Regions.Count, Is.EqualTo(p.Cities.Count), "her şehir bir idari bölgenin merkezi");
            Assert.That(p.Cities.Count, Is.GreaterThanOrEqualTo(120));
            Assert.That(p.Sources.Count, Is.EqualTo(1));
            Assert.That(p.Sources[0].License, Is.EqualTo("CC BY-NC-SA 4.0"));
            Assert.That(p.Sources[0].Sha256, Has.Length.EqualTo(64));
        }

        [Test]
        public void HistoricalCorrections_AreApplied()
        {
            GeoPackage p = Load();
            Assert.That(City(p, "istanbul").Owner, Is.EqualTo(Ottoman), "1913 İstanbul Antlaşması: Doğu Trakya Osmanlı");
            Assert.That(City(p, "edirne").Owner, Is.EqualTo(Ottoman));
            Assert.That(City(p, "dedeagac").Owner, Is.EqualTo(Bulgaria), "Batı Trakya 1913–1919 Bulgar");
            Assert.That(City(p, "selanik").Owner, Is.EqualTo(Greece));
            Assert.That(City(p, "kandiye").Owner, Is.EqualTo(Greece), "Girit 1913'ten beri Yunan");
            Assert.That(City(p, "kars").Owner, Is.EqualTo(Russia), "Kars 1878–1918 Rus");
            Assert.That(City(p, "kisinev").Owner, Is.EqualTo(Russia), "Besarabya 1812–1918 Rus");
            Assert.That(City(p, "lefkosa").Owner, Is.EqualTo(Britain));
            Assert.That(City(p, "istanbul").IsPort && City(p, "selanik").IsPort && City(p, "izmir").IsPort, Is.True);
        }

        [Test]
        public void World_IsConnected_AndCitiesSitInTheirRegions()
        {
            GeoPackage p = Load();
            GeoWorld world = p.BuildWorld();
            for (int i = 0; i < p.Cities.Count; i++)
            {
                PackageCity c = p.Cities[i];
                Assert.That(world.Atlas.Contains(c.Region, c.Location), Is.True, c.NameKey);
            }

            // Ulaşım grafında İstanbul'dan her şehre (karadan ya da deniz yoluyla) ulaşılır.
            var seen = new HashSet<TransportNodeId>();
            var queue = new Queue<TransportNodeId>();
            queue.Enqueue(City(p, "istanbul").Node);
            seen.Add(City(p, "istanbul").Node);
            while (queue.Count > 0)
            {
                TransportNodeId at = queue.Dequeue();
                IReadOnlyList<TransportEdge> edges = world.Transport.EdgesOf(at);
                for (int e = 0; e < edges.Count; e++)
                {
                    TransportNodeId next = edges[e].Other(at);
                    if (seen.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            var unreachable = new List<string>();
            for (int i = 0; i < p.Cities.Count; i++)
            {
                if (!seen.Contains(p.Cities[i].Node))
                {
                    unreachable.Add(p.Cities[i].NameKey);
                }
            }

            Assert.That(unreachable, Is.Empty);
            Assert.That(world.Transport.EdgeCount, Is.GreaterThan(1000));
        }

        [Test]
        public void BuildWorld_GivesIndependentFreshWorlds_WithTheSameGeometryFingerprint()
        {
            GeoPackage p = Load();
            GeoWorld a = p.BuildWorld();
            GeoWorld b = p.BuildWorld();
            a.TryGetCity(City(p, "istanbul").Id, out City istanbul);
            istanbul.Controller = Greece;
            b.TryGetCity(City(p, "istanbul").Id, out City other);
            Assert.That(other.Controller, Is.EqualTo(Ottoman), "değişken durum paylaşılmaz");

            var wa = new CanonicalWriter();
            GeoWorldSnapshotCodec.Write(p.BuildWorld(), wa);
            var wb = new CanonicalWriter();
            GeoWorldSnapshotCodec.Write(p.BuildWorld(), wb);
            Assert.That(wa.ToArray(), Is.EqualTo(wb.ToArray()));
        }

        [Test]
        public void CorruptedOrTruncatedPackages_AreRejected()
        {
            byte[] data = File.ReadAllBytes(PackagePath);
            byte[] flipped = (byte[])data.Clone();
            flipped[100] ^= 0x40;
            Assert.That(GeoPackageReader.Read(flipped).ErrorKey, Is.EqualTo(ContentErrorKeys.PaketOzetiUyusmuyor));
            Assert.That(GeoPackageReader.Read(new byte[10]).ErrorKey, Is.EqualTo(ContentErrorKeys.PaketBozuk));
            Assert.That(GeoPackageReader.Read(null).ErrorKey, Is.EqualTo(ContentErrorKeys.PaketBozuk));

            // Doğru özetli ama kesik yük: biçim hatası.
            byte[] payload = new byte[data.Length - 32 - 40];
            System.Array.Copy(data, payload, payload.Length);
            byte[] resealed = new byte[payload.Length + 32];
            System.Array.Copy(payload, resealed, payload.Length);
            System.Array.Copy(Domain.Hashing.Sha256.Compute(payload), 0, resealed, payload.Length, 32);
            Assert.That(GeoPackageReader.Read(resealed).ErrorKey, Is.EqualTo(ContentErrorKeys.PaketBozuk));
        }
    }
}
