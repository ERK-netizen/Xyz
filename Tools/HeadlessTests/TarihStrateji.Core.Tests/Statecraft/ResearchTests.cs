using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Research;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.Research.State;

namespace TarihStrateji.Core.Tests.Statecraft
{
    [TestFixture]
    public class ResearchTests
    {
        private static readonly TechCatalog Catalog = BaselineContent.CreateTechnology();
        private static readonly GameDate Start = GameDate.FromYmd(1920, 4, 23);
        private static readonly CountryId Tr = new CountryId(1);

        private static TechnologyId T(ulong id) => new TechnologyId(id);

        private static EraDefinition[] Eras(int n)
        {
            var eras = new EraDefinition[n];
            for (int i = 0; i < n; i++) eras[i] = new EraDefinition(i, "cag." + i, GameDate.FromYmd(1900 + i, 1, 1), null);
            return eras;
        }

        private static TechnologyDefinition Def(ulong id, int era, params ulong[] pre)
        {
            var p = new TechnologyId[pre.Length];
            for (int i = 0; i < pre.Length; i++) p[i] = T(pre[i]);
            return new TechnologyDefinition(T(id), "t" + id, era, p, Fixed64.FromInt(10), GameDate.MinValue, null);
        }

        [Test]
        public void Catalog_RejectsCyclesUnknownPrereqsAndEraViolations()
        {
            Assert.That(TechCatalog.Create(Eras(1), new[] { Def(1, 0, 2), Def(2, 0, 3), Def(3, 0, 1) }, Fixed64.One).ErrorKey, Is.EqualTo(ResearchErrorKeys.AgacDongulu));
            Assert.That(TechCatalog.Create(Eras(1), new[] { Def(1, 0, 9) }, Fixed64.One).ErrorKey, Is.EqualTo(ResearchErrorKeys.OnkosulBilinmiyor));
            Assert.That(TechCatalog.Create(Eras(1), new[] { Def(1, 0, 1) }, Fixed64.One).ErrorKey, Is.EqualTo(ResearchErrorKeys.OnkosulBilinmiyor), "kendine ön koşul");
            Assert.That(TechCatalog.Create(Eras(2), new[] { Def(1, 1), Def(2, 0, 1) }, Fixed64.One).ErrorKey, Is.EqualTo(ResearchErrorKeys.OnkosulCagiIleride));
            Assert.That(TechCatalog.Create(Eras(2), new[] { Def(1, 2) }, Fixed64.One).ErrorKey, Is.EqualTo(ResearchErrorKeys.TanimGecersiz));
            var badOrder = new[] { new EraDefinition(0, "a", GameDate.FromYmd(1920, 1, 1), null), new EraDefinition(1, "b", GameDate.FromYmd(1910, 1, 1), null) };
            Assert.That(TechCatalog.Create(badOrder, new[] { Def(1, 0) }, Fixed64.One).ErrorKey, Is.EqualTo(ResearchErrorKeys.CagSirasiGecersiz));
            var badGate = new[] { new EraDefinition(0, "a", GameDate.MinValue, null), new EraDefinition(1, "b", GameDate.MinValue, new[] { T(1) }) };
            Assert.That(TechCatalog.Create(badGate, new[] { Def(1, 1) }, Fixed64.One).ErrorKey, Is.EqualTo(ResearchErrorKeys.CagSirasiGecersiz), "kapı teknolojisi önceki çağdan olmalı");
        }

        [Test]
        public void TopologicalOrder_RespectsPrerequisites_WithStableTies()
        {
            var catalog = TechCatalog.Create(Eras(1), new[] { Def(5, 0, 3), Def(3, 0), Def(4, 0), Def(1, 0, 4), Def(2, 0) }, Fixed64.One).Value;
            Assert.That(catalog.TopologicalOrder, Is.EqualTo(new[] { T(2), T(3), T(4), T(1), T(5) }));

            var position = new Dictionary<TechnologyId, int>();
            for (int i = 0; i < Catalog.TopologicalOrder.Count; i++) position[Catalog.TopologicalOrder[i]] = i;
            foreach (var pair in Catalog.Technologies)
            {
                foreach (TechnologyId pre in pair.Value.Prerequisites)
                {
                    Assert.That(position[pre], Is.LessThan(position[pair.Key]));
                }
            }
        }

        [Test]
        public void Availability_ExplainsEveryBlocker()
        {
            var state = new ResearchState();
            var effects = new EffectEngine();
            CountryResearch r = state.AddCountry(Tr, Catalog, new[] { BaselineTechnologies.MachineGun }, effects, Start).Value;
            Assert.That(r.Era, Is.EqualTo(0), "montaj hattı yok: savaş arası çağı kapısı kapalı");
            Assert.That(EraRules.CheckAvailable(Catalog, r, T(99), Start).ErrorKey, Is.EqualTo(ResearchErrorKeys.TeknolojiBilinmiyor));
            Assert.That(EraRules.CheckAvailable(Catalog, r, BaselineTechnologies.MachineGun, Start).ErrorKey, Is.EqualTo(ResearchErrorKeys.ZatenArastirildi));
            Assert.That(EraRules.CheckAvailable(Catalog, r, BaselineTechnologies.Electrification, Start).ErrorKey, Is.EqualTo(ResearchErrorKeys.CagaUlasilmadi));
            Assert.That(EraRules.CheckAvailable(Catalog, r, BaselineTechnologies.TrenchDoctrine, Start).IsValid, Is.True);
            Assert.That(EraRules.CheckAvailable(Catalog, r, BaselineTechnologies.AssemblyLine, Start).IsValid, Is.True);

            CountryResearch g = state.AddCountry(new CountryId(2), Catalog, new[] { BaselineTechnologies.MachineGun, BaselineTechnologies.AssemblyLine, BaselineTechnologies.MotorTransport }, effects, Start).Value;
            Assert.That(g.Era, Is.EqualTo(1));
            Assert.That(EraRules.CheckAvailable(Catalog, g, BaselineTechnologies.ArmoredForces, Start).ErrorKey, Is.EqualTo(ResearchErrorKeys.TarihPenceresiKapali), "1925'ten önce yok");
            Assert.That(EraRules.CheckAvailable(Catalog, g, BaselineTechnologies.Monoplane, Start).ErrorKey, Is.EqualTo(ResearchErrorKeys.OnkosulEksik));
            Assert.That(state.AddCountry(new CountryId(3), Catalog, new[] { BaselineTechnologies.TrenchDoctrine }, effects, Start).ErrorKey, Is.EqualTo(ResearchErrorKeys.BaslangicTutarsiz), "ön koşulsuz başlangıç");
            Assert.That(state.AddCountry(new CountryId(4), Catalog, new[] { BaselineTechnologies.Electrification, BaselineTechnologies.AssemblyLine }, effects, GameDate.FromYmd(1916, 1, 1)).ErrorKey,
                Is.EqualTo(ResearchErrorKeys.BaslangicTutarsiz), "çağı gelmemiş başlangıç teknolojisi");
        }

        [Test]
        public void Progress_CompletesOnExactDay_AppliesEffects_AndFollowsQueue()
        {
            var state = new ResearchState();
            var effects = new EffectEngine();
            state.AddCountry(Tr, Catalog, new TechnologyId[0], effects, Start);
            Assert.That(ResearchRules.SetCurrent(state, Catalog, Tr, BaselineTechnologies.MachineGun, Start).IsValid, Is.True);
            Assert.That(ResearchRules.Enqueue(state, Catalog, Tr, BaselineTechnologies.AssemblyLine).IsValid, Is.True);
            Assert.That(ResearchRules.Enqueue(state, Catalog, Tr, BaselineTechnologies.AssemblyLine).ErrorKey, Is.EqualTo(ResearchErrorKeys.ZatenKuyrukta));
            Assert.That(ResearchRules.Enqueue(state, Catalog, Tr, BaselineTechnologies.MachineGun).ErrorKey, Is.EqualTo(ResearchErrorKeys.ZatenKuyrukta));

            var notes = new List<string>();
            GameDate day = Start;
            for (int d = 1; d < 50; d++)
            {
                day = day.AddDays(1);
                ResearchRules.AdvanceDay(state, Catalog, effects, day, notes);
            }

            CountryResearch r = state.Countries[Tr];
            Assert.That(r.Has(BaselineTechnologies.MachineGun), Is.False);
            day = day.AddDays(1);
            ResearchRules.AdvanceDay(state, Catalog, effects, day, notes);
            Assert.That(r.Has(BaselineTechnologies.MachineGun), Is.True, "100 puan / 2 = tam 50. gün");
            Assert.That(r.Researched[BaselineTechnologies.MachineGun], Is.EqualTo(day));
            Assert.That(r.Current, Is.EqualTo(BaselineTechnologies.AssemblyLine), "kuyruktan sıradaki");
            Assert.That(effects.Evaluate(EffectTarget.Of(Tr), "kara.saldiri", Fixed64.One, day), Is.EqualTo(Fixed64.Parse("1.05")));
            Assert.That(notes, Has.Some.EndsWith("teknoloji.makineliTufek"));

            for (int d = 0; d < 75; d++)
            {
                day = day.AddDays(1);
                ResearchRules.AdvanceDay(state, Catalog, effects, day, notes);
            }

            Assert.That(r.Has(BaselineTechnologies.AssemblyLine), Is.True);
            Assert.That(r.Era, Is.EqualTo(1), "kapılar tamam ve 1919 sonrası → savaş arası çağı");
            Assert.That(notes, Has.Some.EndsWith("cag.savasArasi"));
            var violations = new List<string>();
            Assert.That(ResearchInvariants.Check(state, Catalog, violations), Is.EqualTo(0), string.Join("\n", violations));
        }

        [Test]
        public void SwitchingProject_KeepsProgress_AndSpeedEffectsApply()
        {
            var state = new ResearchState();
            var effects = new EffectEngine();
            state.AddCountry(Tr, Catalog, new TechnologyId[0], effects, Start);
            ResearchRules.SetCurrent(state, Catalog, Tr, BaselineTechnologies.MachineGun, Start);
            var notes = new List<string>();
            for (int d = 0; d < 10; d++) ResearchRules.AdvanceDay(state, Catalog, effects, Start, notes);
            ResearchRules.SetCurrent(state, Catalog, Tr, BaselineTechnologies.OilFiredShips, Start);
            effects.Add(EffectTarget.Of(Tr), ResearchStatKeys.ResearchSpeed, EffectOperation.Percent, Fixed64.One, Start, 0, "test");
            for (int d = 0; d < 10; d++) ResearchRules.AdvanceDay(state, Catalog, effects, Start, notes);
            CountryResearch r = state.Countries[Tr];
            Assert.That(r.ProgressOf(BaselineTechnologies.MachineGun), Is.EqualTo(Fixed64.FromInt(20)), "yarım ilerleme korunur");
            Assert.That(r.ProgressOf(BaselineTechnologies.OilFiredShips), Is.EqualTo(Fixed64.FromInt(40)), "%100 hız: günde 4");
        }

        [Test]
        public void Invariants_DetectCorruption_AndSnapshotRoundTrips()
        {
            var state = new ResearchState();
            var effects = new EffectEngine();
            state.AddCountry(Tr, Catalog, new[] { BaselineTechnologies.MachineGun }, effects, Start);
            ResearchRules.SetCurrent(state, Catalog, Tr, BaselineTechnologies.TrenchDoctrine, Start);
            ResearchRules.Enqueue(state, Catalog, Tr, BaselineTechnologies.AssemblyLine);
            ResearchRules.AdvanceDay(state, Catalog, effects, Start.AddDays(1), new List<string>());
            var w = new CanonicalWriter();
            ResearchSnapshotCodec.Write(state, w);
            Result<ResearchState> back = ResearchSnapshotCodec.Read(new CanonicalReader(w.ToArray()));
            Assert.That(back.IsSuccess, Is.True);
            var w2 = new CanonicalWriter();
            ResearchSnapshotCodec.Write(back.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));

            state.Countries[Tr].Researched.Add(BaselineTechnologies.Electrification, Start); // ön koşulsuz + çağı gelmemiş
            var violations = new List<string>();
            Assert.That(ResearchInvariants.Check(state, Catalog, violations), Is.GreaterThanOrEqualTo(2));
        }
    }
}
