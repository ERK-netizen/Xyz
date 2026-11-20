using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Commands.Diplomacy;
using TarihStrateji.Core.Tests.Scenario;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Tests.Diplomacy
{
    /// <summary>
    /// Çekirdek üzerinden: A (oyuncu) odak seçer, B'ye barış teklif eder (B yanıtlamaz, teklif düşer),
    /// C ile ittifak teklifi bekler; B'nin teklifini (A'nın işgal ettiği bölge B'ye döner) kabul eder,
    /// birlikleri kendi topraklarına çekilir; savaş yorgunluğu olayına yanıt verir.
    /// </summary>
    [TestFixture]
    public class DiplomacyKernelTests
    {
        private static readonly CountryId A = TestWorlds.CountryA;
        private static readonly CountryId B = TestWorlds.CountryB;

        private static SimulationSetup Setup() => AegeanWar.Setup();

        private static (SimulationKernel Kernel, AegeanWar Scenario, LandUnit Raider) NewKernel()
        {
            var s = new AegeanWar(seed: 4242);
            LandUnit raider = s.Place(A, "sehir.ege.edirne", AegeanWar.Drama);
            s.SetController(AegeanWar.SelanikRegion, A);
            return (TestSimulation.Kernel(Setup(), s.State), s, raider);
        }

        private static List<string> Run(SimulationKernel k, int from, int to)
        {
            var hashes = new List<string>();
            for (int d = from; d < to; d++)
            {
                if (d == 0)
                {
                    Assert.That(k.Submit(new SelectFocusCommand(A, BaselineNarrative.Industrialization)).Accepted, Is.True);
                    Assert.That(k.Submit(new ProposeTreatyCommand(A, ProposalKind.Peace, B, new RegionTransfer[0])).Accepted, Is.True);
                }

                if (d == 40)
                {
                    // B barış önerir; A (oyuncu) kabul edecek. Yapay zekâ Adım 9'da; burada B'nin teklifi kural API'siyle kurulur.
                    DiplomacyRules.Propose(k.State.Diplomacy, AegeanWar.Content.Diplomacy, ProposalKind.Peace, B, A, new RegionTransfer[0], k.State.Today);
                }

                if (d == 41)
                {
                    ProposalId fromB = new ProposalId(k.State.Diplomacy.ProposalIds.NextValue - 1);
                    CommandReceipt r = k.Submit(new RespondProposalCommand(A, fromB, true));
                    Assert.That(r.Accepted, Is.True, r.Validation.ErrorKey);
                }

                for (int p = 0; p < 1; p++)
                {
                    if (k.State.Narrative.Countries.TryGetValue(A, out CountryNarrative n) && n.Pending.Count > 0)
                    {
                        Assert.That(k.Submit(new ChooseEventOptionCommand(A, n.Pending[0].EventId, 0)).Accepted, Is.True);
                    }
                }

                Result<DayReport> report = k.AdvanceDay();
                Assert.That(report.IsSuccess && !report.Value.Halted, Is.True, report.IsSuccess ? string.Join("\n", report.Value.Violations) : report.ErrorKey);
                hashes.Add(Sha256.ToHex(report.Value.StateHash));
            }

            return hashes;
        }

        [Test]
        public void PlayerDiplomacy_FocusAndEvents_ThroughTheKernel()
        {
            (SimulationKernel k, AegeanWar s, LandUnit raider) = NewKernel();
            Assert.That(k.Submit(new DeclareWarCommand(A, B, new RegionId[0])).Validation.ErrorKey, Is.EqualTo(DiplomacyErrorKeys.ZatenSavasta));
            Assert.That(k.Submit(new RespondProposalCommand(B, new ProposalId(1), true)).Accepted, Is.False, "oyuncu başka ülke adına yanıt veremez");

            Run(k, 0, 31);
            Assert.That(k.State.Diplomacy.Proposals[new ProposalId(1)].Status, Is.EqualTo(ProposalStatus.Expired), "yanıtsız teklif düştü");
            Assert.That(k.State.Diplomacy.AtWar(A, B), Is.True, "tek taraflı barış yok");

            Run(k, 31, 60);
            Assert.That(k.State.Diplomacy.AtWar(A, B), Is.False, "iki taraflı onayla barış");
            Assert.That(s.ControllerOf(AegeanWar.SelanikRegion), Is.EqualTo(B), "işgal sahibine döndü");
            Assert.That(k.State.World.Transport.TryGetNode(raider.Node, out Core.World.Geography.TransportNode at), Is.True);
            Assert.That(at.Kind == Core.World.Geography.TransportNodeKind.City || at.Kind == Core.World.Geography.TransportNodeKind.Port, Is.True, "birlik kendi şehrine çekildi");
            k.State.World.TryGetControl(at.Region, out Core.World.Geography.RegionControl home);
            Assert.That(home.Owner, Is.EqualTo(A));
            Assert.That(k.State.Narrative.Countries[A].CompletedFocuses.ContainsKey(BaselineNarrative.Industrialization), Is.True);
            Assert.That(k.State.Effects.Evaluate(EffectTarget.Of(A), EconomyStatKeys.ProductionEfficiency, Fixed64.One, k.State.Today) >= Fixed64.Parse("1.05"), Is.True);
        }

        [Test]
        public void EventChoice_ThroughTheKernel_AndUnansweredEventsDefault()
        {
            GameContent c = AegeanWar.Content;
            var levy = new EventDefinition(new EventId(50), "olay.test.savasVergisi", CountryId.None, new[] { new Condition(ConditionKind.AtWar, 0, Fixed64.Zero) }, 1, true, 5,
                new[]
                {
                    new EventOption("secenek.ode", new[] { new EffectGrant(EconomyStatKeys.TaxEfficiency, EffectOperation.Percent, Fixed64.Parse("-0.1")) }, 30, Fixed64.FromInt(-1000)),
                    new EventOption("secenek.borc", null, 0, Fixed64.FromInt(3000)),
                });
            var content = new GameContent(c.Economy, c.Demography, c.Technology, c.Politics, c.Characters, c.Warfare,
                c.Diplomacy, c.Intelligence, NarrativeCatalog.Create(new[] { levy }, new FocusDefinition[0]).Value, c.Ai);
            var s = new AegeanWar();
            SimulationKernel k = TestSimulation.Kernel(StandardRules.Install(new SimulationSetup(), content), s.State);
            TestSimulation.AdvanceOk(k, 1);
            Assert.That(k.State.Narrative.Countries[A].Pending.Count, Is.EqualTo(1), "1 günlük ortalama: ilk gün tetiklenir");
            Fixed64 treasuryA = k.State.Economy.Countries[A].Treasury;
            Assert.That(k.Submit(new ChooseEventOptionCommand(A, levy.Id, 2)).Validation.ErrorKey, Is.EqualTo(NarrativeErrorKeys.SecenekGecersiz));
            Assert.That(k.Submit(new ChooseEventOptionCommand(A, levy.Id, 1)).Accepted, Is.True);
            Assert.That(k.AdvanceDay().IsSuccess, Is.True);
            Assert.That(k.State.Narrative.Countries[A].Pending.Count, Is.EqualTo(0));
            Assert.That(k.State.Economy.Countries[A].Ledger.OtherIncome, Is.EqualTo(Fixed64.FromInt(3000)), "seçilen seçeneğin geliri deftere yazılır");

            TestSimulation.AdvanceOk(k, 4);
            Assert.That(k.State.Narrative.Countries[B].Pending.Count, Is.EqualTo(0), "B yanıtlamadı: ilk seçenek uygulandı");
            Assert.That(k.State.Effects.Evaluate(EffectTarget.Of(B), EconomyStatKeys.TaxEfficiency, Fixed64.One, k.State.Today), Is.EqualTo(Fixed64.Parse("0.9")));
            Assert.That(k.State.Economy.Countries[A].Treasury > treasuryA, Is.True);
        }

        [Test]
        public void SameSeed_SameHashes_AndSaveLoadContinuesIdentically()
        {
            (SimulationKernel a, _, _) = NewKernel();
            List<string> expected = Run(a, 0, 60);
            (SimulationKernel b, _, _) = NewKernel();
            Assert.That(Run(b, 0, 60), Is.EqualTo(expected));

            (SimulationKernel c, _, _) = NewKernel();
            Run(c, 0, 35);
            Result<SimulationKernel> restored = SimulationKernel.Restore(Setup(), c.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            Assert.That(Run(restored.Value, 35, 60), Is.EqualTo(expected.GetRange(35, 25)));
        }

        [Test]
        public void GoldenHash_Diplomacy_Day60()
        {
            (SimulationKernel k, _, _) = NewKernel();
            List<string> hashes = Run(k, 0, 60);
            Assert.That(hashes[59], Is.EqualTo(Support.GoldenValues.DiplomacyDay60), "yeni hash: " + hashes[59]);
        }
    }
}
