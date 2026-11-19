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
using TarihStrateji.Core.Tests.Military;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Tests.Intel
{
    [TestFixture]
    public class ObservedWorldTests
    {
        private static readonly IntelligenceParameters Prm = FrontScenario.Content.Intelligence;

        private static WorldMap Map() => new FrontScenario().State.World;

        [Test]
        public void Visibility_IsAHexDisc_DeduplicatedAndSorted()
        {
            WorldMap map = Map();
            var cells = new List<int>();
            var scratch = new int[map.CellCount];
            IntelligenceRules.ComputeVisible(map, new[] { new Sensor(new HexCoord(105, 4), 1) }, cells, scratch);
            Assert.That(cells.Count, Is.EqualTo(7));
            IntelligenceRules.ComputeVisible(map, new[] { new Sensor(new HexCoord(105, 4), 2) }, cells, scratch);
            Assert.That(cells.Count, Is.EqualTo(19));
            IntelligenceRules.ComputeVisible(map, new[] { new Sensor(new HexCoord(105, 4), 2), new Sensor(new HexCoord(106, 4), 1), new Sensor(new HexCoord(105, 4), 1) }, cells, scratch);
            Assert.That(cells.Count, Is.EqualTo(19), "örtüşen sensörler tekrar saymaz");
            Assert.That(cells, Is.Ordered.Ascending);
            Assert.That(scratch, Is.All.EqualTo(0), "çalışma dizisi temizlenir");
            IntelligenceRules.ComputeVisible(map, new[] { new Sensor(new HexCoord(0, 4), 1) }, cells, scratch);
            Assert.That(cells, Has.Member(map.Grid.IndexOf(new HexCoord(719, 4))), "±180° dikişinden görülür");
        }

        [Test]
        public void Contacts_ExactWhenClose_EstimatedWhenFar_AndOwnUnitsNeverReported()
        {
            WorldMap map = Map();
            var state = new IntelligenceState();
            ObservedWorld view = state.For(TestWorlds.CountryA);
            var sensors = new[] { new Sensor(new HexCoord(105, 4), 2) };
            var sightings = new[]
            {
                new Sighting(SightingKind.LandUnit, 7, TestWorlds.CountryB, map.Grid.ToId(new HexCoord(106, 4)), Fixed64.Parse("0.61")),
                new Sighting(SightingKind.LandUnit, 8, TestWorlds.CountryB, map.Grid.ToId(new HexCoord(107, 4)), Fixed64.Parse("0.61")),
                new Sighting(SightingKind.LandUnit, 9, TestWorlds.CountryB, map.Grid.ToId(new HexCoord(110, 4)), Fixed64.One),
                new Sighting(SightingKind.LandUnit, 1, TestWorlds.CountryA, map.Grid.ToId(new HexCoord(105, 4)), Fixed64.One),
                new Sighting(SightingKind.Fleet, 7, TestWorlds.CountryB, map.Grid.ToId(new HexCoord(105, 3)), Fixed64.Parse("0.1")),
            };
            GameDate day = GameDate.FromYmd(1921, 7, 2);
            IntelligenceRules.Update(map, Prm, view, sensors, sightings, day, new int[map.CellCount]);

            Assert.That(view.Contacts.Count, Is.EqualTo(3), "uzaktaki ve kendi birliği yok");
            Assert.That(view.TryGetContact(SightingKind.LandUnit, 7, out ContactReport near), Is.True);
            Assert.That(near.IsExact && near.EstimatedStrength == Fixed64.Parse("0.61"), Is.True);
            Assert.That(view.TryGetContact(SightingKind.LandUnit, 8, out ContactReport far), Is.True);
            Assert.That(far.IsExact, Is.False);
            Assert.That(far.EstimatedStrength, Is.EqualTo(Fixed64.Parse("0.5")), "%25'lik adıma yuvarlanır");
            Assert.That(view.TryGetContact(SightingKind.Fleet, 7, out ContactReport fleet), Is.True, "aynı kimlik farklı tür ayrı temas");
            Assert.That(fleet.EstimatedStrength, Is.EqualTo(Fixed64.Parse("0.1")), "yakın: kesin");
            Assert.That(IntelligenceRules.Estimate(Fixed64.Parse("0.05"), Fixed64.Parse("0.25")), Is.EqualTo(Fixed64.Parse("0.25")), "var olan varlık 0 tahmin edilmez");
        }

        [Test]
        public void Contacts_VanishWhenSeenGone_AndAreForgottenAfterMemory()
        {
            WorldMap map = Map();
            ObservedWorld view = new IntelligenceState().For(TestWorlds.CountryA);
            var scratch = new int[map.CellCount];
            HexId there = map.Grid.ToId(new HexCoord(106, 4));
            GameDate day = GameDate.FromYmd(1921, 7, 2);
            IntelligenceRules.Update(map, Prm, view, new[] { new Sensor(new HexCoord(105, 4), 2) }, new[] { new Sighting(SightingKind.LandUnit, 7, TestWorlds.CountryB, there, Fixed64.One) }, day, scratch);
            Assert.That(view.Contacts.Count, Is.EqualTo(1));

            // Sensör uzaklaştı: son konum görünmüyor → temas hatırlanır, bellek süresi sonunda unutulur.
            var farSensor = new[] { new Sensor(new HexCoord(101, 4), 1) };
            IntelligenceRules.Update(map, Prm, view, farSensor, new Sighting[0], day.AddDays(Prm.ContactMemoryDays - 1), scratch);
            Assert.That(view.Contacts.Count, Is.EqualTo(1));
            IntelligenceRules.Update(map, Prm, view, farSensor, new Sighting[0], day.AddDays(Prm.ContactMemoryDays), scratch);
            Assert.That(view.Contacts.Count, Is.EqualTo(0));

            // Son konumu görünür ama orada değil → silinir.
            IntelligenceRules.Update(map, Prm, view, new[] { new Sensor(new HexCoord(105, 4), 2) }, new[] { new Sighting(SightingKind.LandUnit, 7, TestWorlds.CountryB, there, Fixed64.One) }, day, scratch);
            IntelligenceRules.Update(map, Prm, view, new[] { new Sensor(new HexCoord(105, 4), 2) }, new Sighting[0], day.AddDays(1), scratch);
            Assert.That(view.Contacts.Count, Is.EqualTo(0));
        }

        [Test]
        public void Kernel_FogOfWar_OnlyNearbyEnemiesAreKnown_AndContactsSurviveSaveLoad()
        {
            var s = new FrontScenario();
            LandUnit scout = s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 108, 4);
            LandUnit nearEnemy = s.Place(TestWorlds.CountryB, BaselineUnits.InfantryDivision, 110, 4);
            LandUnit farEnemy = s.Place(TestWorlds.CountryB, BaselineUnits.InfantryDivision, 117, 7);
            SimulationSetup setup = StandardRules.Install(new SimulationSetup(), FrontScenario.Content);
            SimulationKernel k = TestSimulation.Kernel(setup, s.State);
            TestSimulation.AdvanceOk(k, 1);

            ObservedWorld a = k.State.Intelligence.Countries[TestWorlds.CountryA];
            Assert.That(a.TryGetContact(SightingKind.LandUnit, nearEnemy.Id.Value, out _), Is.True);
            Assert.That(a.TryGetContact(SightingKind.LandUnit, farEnemy.Id.Value, out _), Is.False, "savaş sisi");
            Assert.That(a.TryGetContact(SightingKind.LandUnit, scout.Id.Value, out _), Is.False);
            ObservedWorld b = k.State.Intelligence.Countries[TestWorlds.CountryB];
            Assert.That(b.TryGetContact(SightingKind.LandUnit, scout.Id.Value, out _), Is.True);

            Result<SimulationKernel> restored = SimulationKernel.Restore(setup, k.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            Assert.That(restored.Value.State.Intelligence.Countries[TestWorlds.CountryA].Contacts.Count, Is.EqualTo(a.Contacts.Count));

            var w = new CanonicalWriter();
            IntelligenceSnapshotCodec.Write(k.State.Intelligence, w);
            var w2 = new CanonicalWriter();
            IntelligenceSnapshotCodec.Write(IntelligenceSnapshotCodec.Read(new CanonicalReader(w.ToArray())).Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));
        }
    }
}
