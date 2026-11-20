using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Persistence;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Simulation.Systems.Geo;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.State;

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
        private static readonly GameContent Content = BaselineContent.Create();

        private static SimulationSetup Setup() =>
            StandardRules.Install(new SimulationSetup { GeoWorldTemplate = AegeanScenario.BuildWorld }, Content);

        private static byte[] FixedSnapshot()
        {
            GameState state = ScenarioFactory.CreateAegean(Content, 77, GameMode.Normal, BaselineStatecraft.Turkey);
            GeoWarfareContext war = GeoWarfareContexts.From(state, Content);
            CityId istanbul = ScenarioFactory.CityOf(state, "sehir.ege.istanbul");
            state.Economy.SeedStock(BaselineStatecraft.Turkey, BaselineGoods.Aircraft, Fixed64.FromInt(100));
            Result<FleetId> transports = GeoNavalRules.Build(war, BaselineStatecraft.Turkey, BaselineUnits.TransportFlotilla, istanbul);
            Assert.That(transports.IsSuccess, Is.True, transports.ErrorKey);
            LandUnit first = state.Warfare.Units.ValueAt(0);
            GeoAmphibiousRules.Embark(first, state.Warfare.Fleets[transports.Value]);
            Result<AirWingId> wing = GeoAirRules.Recruit(war, BaselineStatecraft.Turkey, BaselineUnits.BomberWing, ScenarioFactory.CityOf(state, "sehir.ege.ankara"));
            Assert.That(wing.IsSuccess, Is.True, wing.ErrorKey);
            GeoAirRules.SetMission(state.Warfare.AirWings[wing.Value], AirMission.GroundSupport,
                TargetRef.Entity(TargetKind.City, ScenarioFactory.CityOf(state, "sehir.ege.izmir").Value));
            return GameStateCodec.Encode(state, Setup().Commands, true);
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
            SimulationSetup setup = Setup();
            Assert.That(GameStateCodec.Decode(WithSchema(GameStateCodec.SchemaVersion), setup.Commands, AegeanScenario.BuildWorld).IsSuccess, Is.True);
            Assert.That(GameStateCodec.Decode(WithSchema(1), setup.Commands).ErrorKey, Is.EqualTo(SimulationErrorKeys.DurumSemasiEski));
            Assert.That(GameStateCodec.Decode(WithSchema(GameStateCodec.SchemaVersion + 1), setup.Commands).ErrorKey, Is.EqualTo(SimulationErrorKeys.DurumSemasiYeni));
            Assert.That(GameStateCodec.Decode(WithSchema(0), setup.Commands).ErrorKey, Is.EqualTo(DomainErrorKeys.KayitBozuk));
        }

        [Test]
        public void SnapshotRoundTrip_IsByteExact()
        {
            SimulationSetup setup = Setup();
            byte[] bytes = FixedSnapshot();
            Result<GameState> back = GameStateCodec.Decode(bytes, setup.Commands, AegeanScenario.BuildWorld);
            Assert.That(back.IsSuccess, Is.True, back.ErrorKey);
            Assert.That(GameStateCodec.Encode(back.Value, setup.Commands, true), Is.EqualTo(bytes));
        }
    }
}
