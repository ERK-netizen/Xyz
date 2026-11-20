using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Intelligence;
using TarihStrateji.Core.Intelligence.Definitions;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Tests.Scenario;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Intel
{
    /// <summary>
    /// Savaş sisi ulaşım grafında: görüş sensörden kenarlar boyunca yol uzunluğuyla yayılır. Kıyı test dünyası
    /// mesafeleri: liman–batı kavşak 45 km, batı kavşak–iç şehir 75 km, batı–doğu kavşak 50 km, doğu kavşak–iç
    /// şehir 35 km, liman–deniz 70 km.
    /// </summary>
    [TestFixture]
    public class ObservedWorldTests
    {
        private static readonly IntelligenceParameters Prm = BaselineDiplomacyContent.CreateIntelligence();

        [Test]
        public void Visibility_FollowsGraphDistance_DeduplicatedAndSorted()
        {
            GeoWorld world = TestWorlds.Coastal();
            var nodes = new List<TransportNodeId>();
            var nearest = new Dictionary<TransportNodeId, long>();
            IntelligenceRules.ComputeVisible(world, new[] { new Sensor(TestWorlds.PortNode, 50_000L) }, nodes, nearest);
            Assert.That(nodes, Is.EqualTo(new[] { TestWorlds.PortNode, TestWorlds.WestJunction }));
            IntelligenceRules.ComputeVisible(world, new[] { new Sensor(TestWorlds.PortNode, 100_000L) }, nodes, nearest);
            Assert.That(nodes, Is.EqualTo(new[] { TestWorlds.PortNode, TestWorlds.WestJunction, TestWorlds.EastJunction, TestWorlds.SeaNode }));
            Assert.That(nearest[TestWorlds.EastJunction], Is.EqualTo(95_000L));

            IntelligenceRules.ComputeVisible(world, new[]
            {
                new Sensor(TestWorlds.PortNode, 100_000L), new Sensor(TestWorlds.InlandNode, 40_000L), new Sensor(TestWorlds.PortNode, 50_000L),
            }, nodes, nearest);
            Assert.That(nodes.Count, Is.EqualTo(5), "örtüşen sensörler tekrar saymaz");
            Assert.That(nodes, Is.Ordered.Ascending);
            Assert.That(nearest[TestWorlds.EastJunction], Is.EqualTo(35_000L), "en yakın sensöre uzaklık");
        }

        [Test]
        public void Contacts_ExactWhenClose_EstimatedWhenFar_AndOwnUnitsNeverReported()
        {
            GeoWorld world = TestWorlds.Coastal();
            ObservedWorld view = new IntelligenceState().For(TestWorlds.CountryA);
            var sensors = new[] { new Sensor(TestWorlds.PortNode, 100_000L) };
            var sightings = new[]
            {
                new Sighting(SightingKind.LandUnit, 7, TestWorlds.CountryB, TestWorlds.PortNode, Fixed64.Parse("0.61")),
                new Sighting(SightingKind.LandUnit, 8, TestWorlds.CountryB, TestWorlds.EastJunction, Fixed64.Parse("0.61")),
                new Sighting(SightingKind.LandUnit, 9, TestWorlds.CountryB, TestWorlds.InlandNode, Fixed64.One),
                new Sighting(SightingKind.LandUnit, 1, TestWorlds.CountryA, TestWorlds.PortNode, Fixed64.One),
                new Sighting(SightingKind.Fleet, 7, TestWorlds.CountryB, TestWorlds.SeaNode, Fixed64.Parse("0.1")),
            };
            GameDate day = GameDate.FromYmd(1921, 7, 2);
            IntelligenceRules.Update(world, Prm, view, sensors, sightings, day);

            Assert.That(view.Contacts.Count, Is.EqualTo(3), "uzaktaki ve kendi birliği yok");
            Assert.That(view.TryGetContact(SightingKind.LandUnit, 7, out ContactReport near), Is.True);
            Assert.That(near.IsExact && near.EstimatedStrength == Fixed64.Parse("0.61"), Is.True);
            Assert.That(view.TryGetContact(SightingKind.LandUnit, 8, out ContactReport far), Is.True);
            Assert.That(far.IsExact, Is.False);
            Assert.That(far.EstimatedStrength, Is.EqualTo(Fixed64.Parse("0.5")), "%25'lik adıma yuvarlanır");
            Assert.That(view.TryGetContact(SightingKind.Fleet, 7, out ContactReport fleet), Is.True, "aynı kimlik farklı tür ayrı temas");
            Assert.That(fleet.IsExact, Is.False);
            Assert.That(IntelligenceRules.Estimate(Fixed64.Parse("0.05"), Fixed64.Parse("0.25")), Is.EqualTo(Fixed64.Parse("0.25")), "var olan varlık 0 tahmin edilmez");
        }

        [Test]
        public void Contacts_VanishWhenSeenGone_AndAreForgottenAfterMemory()
        {
            GeoWorld world = TestWorlds.Coastal();
            ObservedWorld view = new IntelligenceState().For(TestWorlds.CountryA);
            var nearSensor = new[] { new Sensor(TestWorlds.PortNode, 100_000L) };
            GameDate day = GameDate.FromYmd(1921, 7, 2);
            IntelligenceRules.Update(world, Prm, view, nearSensor, new[] { new Sighting(SightingKind.LandUnit, 7, TestWorlds.CountryB, TestWorlds.EastJunction, Fixed64.One) }, day);
            Assert.That(view.Contacts.Count, Is.EqualTo(1));

            // Sensör uzaklaştı: son konum görünmüyor → temas hatırlanır, bellek süresi sonunda unutulur.
            var farSensor = new[] { new Sensor(TestWorlds.PortNode, 10_000L) };
            IntelligenceRules.Update(world, Prm, view, farSensor, new Sighting[0], day.AddDays(Prm.ContactMemoryDays - 1));
            Assert.That(view.Contacts.Count, Is.EqualTo(1));
            IntelligenceRules.Update(world, Prm, view, farSensor, new Sighting[0], day.AddDays(Prm.ContactMemoryDays));
            Assert.That(view.Contacts.Count, Is.EqualTo(0));

            // Son konumu görünür ama orada değil → silinir.
            IntelligenceRules.Update(world, Prm, view, nearSensor, new[] { new Sighting(SightingKind.LandUnit, 7, TestWorlds.CountryB, TestWorlds.EastJunction, Fixed64.One) }, day);
            IntelligenceRules.Update(world, Prm, view, nearSensor, new Sighting[0], day.AddDays(1));
            Assert.That(view.Contacts.Count, Is.EqualTo(0));
        }

        [Test]
        public void Kernel_FogOfWar_OnlyNearbyEnemiesAreKnown_AndContactsSurviveSaveLoad()
        {
            var s = new AegeanWar(seed: 31);
            LandUnit scout = s.Place(AegeanWar.Turkey, "sehir.ege.edirne", AegeanWar.Drama);
            LandUnit nearEnemy = s.Place(AegeanWar.Greece, "sehir.ege.selanik", AegeanWar.Drama);
            LandUnit farEnemy = s.Place(AegeanWar.Greece, "sehir.ege.atina", new TransportNodeId(115));
            SimulationSetup setup = AegeanWar.Setup();
            SimulationKernel k = TestSimulation.Kernel(setup, s.State);
            TestSimulation.AdvanceOk(k, 1);

            ObservedWorld a = k.State.Intelligence.Countries[AegeanWar.Turkey];
            Assert.That(a.TryGetContact(SightingKind.LandUnit, nearEnemy.Id.Value, out _), Is.True);
            Assert.That(a.TryGetContact(SightingKind.LandUnit, farEnemy.Id.Value, out _), Is.False, "savaş sisi");
            Assert.That(a.TryGetContact(SightingKind.LandUnit, scout.Id.Value, out _), Is.False);
            ObservedWorld b = k.State.Intelligence.Countries[AegeanWar.Greece];
            Assert.That(b.TryGetContact(SightingKind.LandUnit, scout.Id.Value, out _), Is.True);

            Result<SimulationKernel> restored = SimulationKernel.Restore(setup, k.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            Assert.That(restored.Value.State.Intelligence.Countries[AegeanWar.Turkey].Contacts.Count, Is.EqualTo(a.Contacts.Count));

            var w = new CanonicalWriter();
            IntelligenceSnapshotCodec.Write(k.State.Intelligence, w);
            var w2 = new CanonicalWriter();
            IntelligenceSnapshotCodec.Write(IntelligenceSnapshotCodec.Read(new CanonicalReader(w.ToArray())).Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));
        }
    }
}
