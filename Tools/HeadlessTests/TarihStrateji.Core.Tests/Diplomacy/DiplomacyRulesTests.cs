using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Diplomacy.Definitions;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Tests.World;

using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Diplomacy
{
    [TestFixture]
    public class DiplomacyRulesTests
    {
        private static readonly CountryId A = TestWorlds.CountryA;
        private static readonly CountryId B = TestWorlds.CountryB;
        private static readonly CountryId C = TestWorlds.CountryC;
        private static readonly DiplomacyParameters Prm = BaselineDiplomacyContent.CreateDiplomacy();
        private static readonly RegionId[] NoGoals = new RegionId[0];
        private static readonly RegionTransfer[] NoTransfers = new RegionTransfer[0];

        /// <summary>A–B savaşı olmayan, temiz diplomatik başlangıç.</summary>
        private static WarWorld Peaceful()
        {
            var s = new WarWorld();
            DiplomacyRules.ConcludePeace(s.State.Diplomacy, s.State.World, new DiplomacyParameters(30, 1825, 3650, 0, Prm.DemandControlShare, Prm.CapitulationThreshold), A, B, NoTransfers, s.State.Today);
            Assert.That(s.State.Diplomacy.AtWar(A, B), Is.False);
            return s;
        }

        /// <summary>Kıyı test dünyası üzerinde A–B savaşı (A batı + liman şehri, B doğu + iç şehir).</summary>
        private sealed class WarWorld
        {
            public WarWorld()
            {
                State = GameState.CreateNew(1, GameMode.Normal, GameDate.FromYmd(1925, 1, 1), A, TestWorlds.Coastal());
                DiplomacyRules.DeclareWar(State.Diplomacy, A, B, NoGoals, State.Today);
            }

            public GameState State { get; }

            public RegionId West => TestWorlds.West;

            public RegionId East => TestWorlds.East;

            public City Inland
            {
                get
                {
                    State.World.TryGetCity(TestWorlds.InlandCity, out City city);
                    return city;
                }
            }

            public RegionControl Control(RegionId region)
            {
                State.World.TryGetControl(region, out RegionControl control);
                return control;
            }
        }

        private static void OccupyRegion(GeoWorld world, RegionId region, CountryId by)
        {
            world.TryGetControl(region, out RegionControl control);
            control.Controller = by;
            control.Stage = by == control.Owner ? RegionControlStage.Secured : RegionControlStage.Occupied;
            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City city);
                if (city.Region == region)
                {
                    city.Controller = by;
                }
            }
        }

        [Test]
        public void DeclareWar_Validations_AndDefensiveAlliesJoin()
        {
            WarWorld s = Peaceful();
            DiplomacyState d = s.State.Diplomacy;
            GameDate today = s.State.Today;
            Assert.That(DiplomacyRules.CheckDeclareWar(d, s.State.World, A, A, NoGoals, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.UlkeGecersiz));
            Assert.That(DiplomacyRules.CheckDeclareWar(d, s.State.World, A, B, new[] { s.West }, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.SavasHedefiGecersiz), "kendi bölgesi hedef olamaz");

            DiplomacyRules.Propose(d, Prm, ProposalKind.Alliance, B, C, NoTransfers, today);
            Assert.That(DiplomacyRules.Respond(d, s.State.World, Prm, new ProposalId(1), true, today), Is.False);
            Assert.That(d.HasTreaty(TreatyKind.Alliance, B, C, today), Is.True);

            Assert.That(DiplomacyRules.CheckDeclareWar(d, s.State.World, A, B, new[] { s.East }, today).IsValid, Is.True);
            List<WarId> opened = DiplomacyRules.DeclareWar(d, A, B, new[] { s.East }, today);
            Assert.That(opened.Count, Is.EqualTo(2), "B'nin müttefiki C savunmaya katılır");
            Assert.That(d.AtWar(A, B) && d.AtWar(A, C), Is.True);
            Assert.That(d.AtWar(B, C), Is.False);
            Assert.That(d.Wars[opened[1]].CalledBy, Is.EqualTo(opened[0]));
            Assert.That(d.Wars[opened[0]].Goals, Is.EqualTo(new[] { s.East }));
            Assert.That(DiplomacyRules.CheckDeclareWar(d, s.State.World, A, B, NoGoals, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.ZatenSavasta));
            Assert.That(DiplomacyRules.CheckDeclareWar(d, s.State.World, B, C, NoGoals, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.Muttefik));
        }

        [Test]
        public void DefensiveAlly_WithATruceOrPactWithTheAttacker_StaysOut()
        {
            WarWorld s = Peaceful();
            DiplomacyState d = s.State.Diplomacy;
            GameDate today = s.State.Today;
            DiplomacyRules.Propose(d, Prm, ProposalKind.Alliance, B, C, NoTransfers, today);
            DiplomacyRules.Respond(d, s.State.World, Prm, new ProposalId(1), true, today);
            DiplomacyRules.StartWar(d, A, C, today);
            DiplomacyRules.ConcludePeace(d, s.State.World, Prm, A, C, NoTransfers, today); // A–C ateşkesi
            List<WarId> opened = DiplomacyRules.DeclareWar(d, A, B, NoGoals, today);
            Assert.That(opened.Count, Is.EqualTo(1), "ateşkesli müttefik savunmaya çağrılmaz");
            Assert.That(d.AtWar(A, C), Is.False);
            var violations = new List<string>();
            Assert.That(DiplomacyInvariants.Check(d, s.State.World, today, violations), Is.EqualTo(0), string.Join("\n", violations));
        }

        [Test]
        public void NonAggressionPact_BlocksWar_UntilItExpires()
        {
            WarWorld s = Peaceful();
            DiplomacyState d = s.State.Diplomacy;
            GameDate today = s.State.Today;
            ProposalId id = DiplomacyRules.Propose(d, Prm, ProposalKind.NonAggression, A, B, NoTransfers, today);
            Assert.That(DiplomacyRules.CheckPropose(d, s.State.World, Prm, ProposalKind.NonAggression, B, A, NoTransfers, today).ErrorKey,
                Is.EqualTo(DiplomacyErrorKeys.TeklifZatenBekliyor));
            Assert.That(DiplomacyRules.CheckRespond(d, s.State.World, Prm, A, id, true, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.TeklifBizeDegil), "yalnız muhatap yanıtlar");
            Assert.That(DiplomacyRules.CheckRespond(d, s.State.World, Prm, B, id, true, today).IsValid, Is.True);
            DiplomacyRules.Respond(d, s.State.World, Prm, id, true, today);
            Assert.That(d.Proposals[id].Status, Is.EqualTo(ProposalStatus.Accepted));
            Assert.That(DiplomacyRules.CheckDeclareWar(d, s.State.World, B, A, NoGoals, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.SaldirmazlikPakti));

            GameDate afterPact = today.AddDays(Prm.NonAggressionDays);
            DiplomacyRules.ExpireDay(d, afterPact);
            Assert.That(d.Treaties.Count, Is.EqualTo(0));
            Assert.That(DiplomacyRules.CheckDeclareWar(d, s.State.World, B, A, NoGoals, afterPact).IsValid, Is.True);
        }

        [Test]
        public void Peace_IsBilateral_DemandsNeedOccupation_AndUnansweredOffersLapse()
        {
            var s = new WarWorld();
            DiplomacyState d = s.State.Diplomacy;
            GameDate today = s.State.Today;
            var demand = new[] { new RegionTransfer(s.East, A) };
            Assert.That(DiplomacyRules.CheckPropose(d, s.State.World, Prm, ProposalKind.Peace, A, B, demand, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.IsgalYetersiz));
            Assert.That(DiplomacyRules.CheckPropose(d, s.State.World, Prm, ProposalKind.Peace, A, B, new[] { new RegionTransfer(s.West, B) }, today).IsValid,
                Is.True, "kendi bölgesini vermek serbest");
            Assert.That(DiplomacyRules.CheckPropose(d, s.State.World, Prm, ProposalKind.Peace, A, C, NoTransfers, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.SavasYok));

            ProposalId lapsed = DiplomacyRules.Propose(d, Prm, ProposalKind.Peace, A, B, NoTransfers, today);
            DiplomacyRules.ExpireDay(d, today.AddDays(Prm.ProposalLifetimeDays));
            Assert.That(d.Proposals[lapsed].Status, Is.EqualTo(ProposalStatus.Expired));
            Assert.That(d.AtWar(A, B), Is.True, "yanıtsız barış teklifi savaşı bitirmez");

            OccupyRegion(s.State.World, s.East, A);
            Assert.That(DiplomacyRules.CheckPropose(d, s.State.World, Prm, ProposalKind.Peace, A, B, demand, today).IsValid, Is.True);
            ProposalId id = DiplomacyRules.Propose(d, Prm, ProposalKind.Peace, A, B, demand, today);
            Assert.That(DiplomacyRules.CheckRespond(d, s.State.World, Prm, B, id, true, today).IsValid, Is.True);
            Assert.That(DiplomacyRules.Respond(d, s.State.World, Prm, id, true, today), Is.True);

            Assert.That(d.AtWar(A, B), Is.False);
            Assert.That(s.Control(s.East).Owner, Is.EqualTo(A));
            Assert.That(s.Inland.Owner, Is.EqualTo(A));
            Assert.That(s.Inland.Controller, Is.EqualTo(A));
            Assert.That(DiplomacyRules.CheckDeclareWar(d, s.State.World, B, A, NoGoals, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.Ateskes));
        }

        [Test]
        public void Peace_RestoresMutualOccupationToOwners()
        {
            var s = new WarWorld();
            OccupyRegion(s.State.World, s.West, B);
            s.Control(s.East).Controller = A;
            s.Control(s.East).Stage = RegionControlStage.Occupied;
            s.Inland.Controller = A;
            DiplomacyRules.ConcludePeace(s.State.Diplomacy, s.State.World, Prm, A, B, NoTransfers, s.State.Today);
            Assert.That(s.Control(s.East).Controller, Is.EqualTo(B));
            Assert.That(s.Control(s.East).Stage, Is.EqualTo(RegionControlStage.Secured));
            Assert.That(s.Control(s.West).Controller, Is.EqualTo(A));
            Assert.That(s.Inland.Controller, Is.EqualTo(B));
        }

        [Test]
        public void LosingAllCities_Capitulates_AndOccupiedRegionsChangeHands()
        {
            var s = new WarWorld();
            OccupyRegion(s.State.World, s.East, A);
            Assert.That(CapitulationRules.HeldCityShare(s.State.World, B), Is.EqualTo(Domain.Numerics.Fixed64.Zero));
            var capitulated = new List<CountryId>();
            var notes = new List<string>();
            CapitulationRules.ApplyDay(s.State.Diplomacy, s.State.World, Prm, s.State.Today, capitulated, notes);
            Assert.That(capitulated, Is.EqualTo(new[] { B }));
            Assert.That(s.State.Diplomacy.AtWar(A, B), Is.False);
            Assert.That(s.Control(s.East).Owner, Is.EqualTo(A));
            Assert.That(notes, Has.Some.StartsWith(CapitulationRules.CapitulatedNote));
            Assert.That(CapitulationRules.HeldCityShare(s.State.World, A), Is.EqualTo(Domain.Numerics.Fixed64.One));
        }

        [Test]
        public void BeatenCountryWithAFightingAlly_GoesIntoExile_AndReturnsOrSurrenders()
        {
            var s = new WarWorld();
            DiplomacyState d = s.State.Diplomacy;
            GameDate today = s.State.Today;
            DiplomacyRules.Propose(d, Prm, ProposalKind.Alliance, B, C, NoTransfers, today);
            DiplomacyRules.Respond(d, s.State.World, Prm, new ProposalId(1), true, today);
            DiplomacyRules.StartWar(d, A, C, today);
            OccupyRegion(s.State.World, s.East, A);

            var capitulated = new List<CountryId>();
            CapitulationRules.ApplyDay(d, s.State.World, Prm, today, capitulated, new List<string>());
            Assert.That(capitulated, Is.Empty);
            Assert.That(d.Exiles[B], Is.EqualTo(C), "hükümet savaşan müttefike sürgüne gider");
            Assert.That(d.AtWar(A, B), Is.True, "savaş sürer");

            OccupyRegion(s.State.World, s.East, B);
            CapitulationRules.ApplyDay(d, s.State.World, Prm, today, capitulated, new List<string>());
            Assert.That(d.IsExiled(B), Is.False, "kurtarılınca sürgün biter");

            OccupyRegion(s.State.World, s.East, A);
            CapitulationRules.ApplyDay(d, s.State.World, Prm, today, capitulated, new List<string>());
            Assert.That(d.IsExiled(B), Is.True);
            DiplomacyRules.ConcludePeace(d, s.State.World, Prm, A, C, NoTransfers, today);
            CapitulationRules.ApplyDay(d, s.State.World, Prm, today, capitulated, new List<string>());
            Assert.That(capitulated, Is.EqualTo(new[] { B }), "ev sahibi savaştan çekilince sürgün hükümeti teslim olur");
            Assert.That(d.IsExiled(B), Is.False);
            Assert.That(DiplomacyRules.CheckDeclareWar(d, s.State.World, B, A, NoGoals, today).ErrorKey, Is.EqualTo(DiplomacyErrorKeys.Ateskes));
        }

        [Test]
        public void Snapshot_RoundTrips_AndInvariantsHold()
        {
            WarWorld s = Peaceful();
            DiplomacyState d = s.State.Diplomacy;
            DiplomacyRules.Propose(d, Prm, ProposalKind.Alliance, B, C, NoTransfers, s.State.Today);
            DiplomacyRules.Respond(d, s.State.World, Prm, new ProposalId(1), true, s.State.Today);
            DiplomacyRules.DeclareWar(d, A, B, new[] { s.East }, s.State.Today.AddDays(400));
            DiplomacyRules.Propose(d, Prm, ProposalKind.Peace, B, A, new[] { new RegionTransfer(s.East, A) }, s.State.Today);
            d.Exiles.Add(B, C);
            var violations = new List<string>();
            Assert.That(DiplomacyInvariants.Check(d, s.State.World, s.State.Today.AddDays(400), violations), Is.EqualTo(0), string.Join("\n", violations));

            var w = new CanonicalWriter();
            DiplomacySnapshotCodec.Write(d, w);
            Result<DiplomacyState> back = DiplomacySnapshotCodec.Read(new CanonicalReader(w.ToArray()));
            Assert.That(back.IsSuccess, Is.True, back.ErrorKey);
            var w2 = new CanonicalWriter();
            DiplomacySnapshotCodec.Write(back.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));
            Assert.That(back.Value.AtWar(A, C), Is.True, "savaş dizini yüklemede kurulur");
        }
    }
}
