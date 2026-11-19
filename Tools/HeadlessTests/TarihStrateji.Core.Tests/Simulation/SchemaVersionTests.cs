using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Persistence;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Tests.Naval;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Tests.Simulation
{
    /// <summary>
    /// Kayıt şeması disiplini: bayt düzeni değişirse <see cref="GameStateCodec.SchemaVersion"/>
    /// artırılmadan bu test geçmez. Parmak izi, tüm bölümleri dolu sabit bir durumun (simülasyon
    /// çalıştırılmadan) kodlamasının SHA-256'sıdır; yalnız düzen veya bu kurulum değişince değişir.
    /// </summary>
    [TestFixture]
    public class SchemaVersionTests
    {
        private static byte[] FixedSnapshot()
        {
            var s = new StraitScenario(seed: 77);
            HexCoord port = StraitScenario.At(s.PortA, s.State.World);
            Fleet transports = s.Fleet(TestWorlds.CountryA, BaselineUnits.TransportFlotilla, port.Column, port.Row);
            AmphibiousRules.Embark(s.War, s.Unit(TestWorlds.CountryA, 109, 4), transports);
            s.Unit(TestWorlds.CountryB, 115, 4);
            AirRules.SetMission(s.Wing(TestWorlds.CountryA, BaselineUnits.BomberWing, s.InlandA), AirMission.GroundSupport, s.Hex(113, 3));
            SimulationSetup setup = StandardRules.Install(new SimulationSetup(), StraitScenario.Content);
            return GameStateCodec.Encode(s.State, setup.Commands, true);
        }

        [Test]
        public void LayoutChange_RequiresASchemaVersionBump()
        {
            string fingerprint = Sha256.ToHex(Sha256.Compute(FixedSnapshot()));
            Assert.That(GameStateCodec.SchemaVersion + ":" + fingerprint, Is.EqualTo(Support.GoldenValues.SchemaFingerprint),
                "Kayıt bayt düzeni değişti. GameStateCodec.SchemaVersion'ı artırın (gerekirse göç ekleyin) ve parmak izini güncelleyin. Yeni: "
                + GameStateCodec.SchemaVersion + ":" + fingerprint);
        }

        private static byte[] WithSchema(int schema)
        {
            byte[] bytes = FixedSnapshot();
            var w = new CanonicalWriter();
            w.WriteInt32(schema);
            byte[] patch = w.ToArray();
            System.Array.Copy(patch, 0, bytes, 4, patch.Length);
            return bytes;
        }

        [Test]
        public void OlderAndNewerSchemas_AreRejectedWithTheirOwnKeys()
        {
            SimulationSetup setup = StandardRules.Install(new SimulationSetup(), StraitScenario.Content);
            Assert.That(GameStateCodec.Decode(WithSchema(GameStateCodec.SchemaVersion), setup.Commands).IsSuccess, Is.True);
            Assert.That(GameStateCodec.Decode(WithSchema(1), setup.Commands).ErrorKey, Is.EqualTo(SimulationErrorKeys.DurumSemasiEski));
            Assert.That(GameStateCodec.Decode(WithSchema(GameStateCodec.SchemaVersion + 1), setup.Commands).ErrorKey, Is.EqualTo(SimulationErrorKeys.DurumSemasiYeni));
            Assert.That(GameStateCodec.Decode(WithSchema(0), setup.Commands).ErrorKey, Is.EqualTo(DomainErrorKeys.KayitBozuk));
        }

        [Test]
        public void SnapshotRoundTrip_IsByteExact()
        {
            SimulationSetup setup = StandardRules.Install(new SimulationSetup(), StraitScenario.Content);
            byte[] bytes = FixedSnapshot();
            Result<GameState> back = GameStateCodec.Decode(bytes, setup.Commands);
            Assert.That(back.IsSuccess, Is.True, back.ErrorKey);
            Assert.That(GameStateCodec.Encode(back.Value, setup.Commands, true), Is.EqualTo(bytes));
        }
    }
}
