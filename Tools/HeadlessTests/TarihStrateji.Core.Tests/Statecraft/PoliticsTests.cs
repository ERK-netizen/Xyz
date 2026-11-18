using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Politics;
using TarihStrateji.Core.Politics.Definitions;
using TarihStrateji.Core.Politics.State;

namespace TarihStrateji.Core.Tests.Statecraft
{
    [TestFixture]
    public class PoliticsTests
    {
        private static readonly PoliticsCatalog Catalog = BaselineStatecraftContent.CreatePolitics();
        private static readonly GameDate Start = GameDate.FromYmd(1920, 4, 23);
        private static readonly CountryId Tr = BaselineStatecraft.Turkey;

        private static Fixed64 F(string s) => Fixed64.Parse(s);

        private static CountryPolitics NewTurkey(PoliticsState state, PoliticsCatalog catalog, string stability = "0.5")
        {
            var support = new[]
            {
                new KeyValuePair<PartyId, Fixed64>(BaselineStatecraft.RepublicanPeoples, F("7")),
                new KeyValuePair<PartyId, Fixed64>(BaselineStatecraft.Progressive, F("3")),
            };
            Result<CountryPolitics> r = state.AddCountry(Tr, catalog, BaselineStatecraft.Republic, BaselineStatecraft.RepublicanPeoples, support, F(stability), Start);
            Assert.That(r.IsSuccess, Is.True, r.ErrorKey);
            return r.Value;
        }

        [Test]
        public void AddCountry_ValidatesPartiesAndRanges()
        {
            var state = new PoliticsState();
            var foreign = new[] { new KeyValuePair<PartyId, Fixed64>(BaselineStatecraft.GreekLiberal, F("1")) };
            Assert.That(state.AddCountry(Tr, Catalog, BaselineStatecraft.Republic, BaselineStatecraft.GreekLiberal, foreign, F("0.5"), Start).ErrorKey, Is.EqualTo(PoliticsErrorKeys.PartiBuUlkeninDegil));
            Assert.That(state.AddCountry(Tr, Catalog, new GovernmentFormId(9), PartyId.None, new KeyValuePair<PartyId, Fixed64>[0], F("0.5"), Start).ErrorKey, Is.EqualTo(PoliticsErrorKeys.YonetimBilinmiyor));
            Assert.That(state.AddCountry(Tr, Catalog, BaselineStatecraft.Republic, PartyId.None, new KeyValuePair<PartyId, Fixed64>[0], F("0.5"), Start).ErrorKey, Is.EqualTo(PoliticsErrorKeys.PartiBilinmiyor), "seçimli rejim iktidar ister");
            Assert.That(state.AddCountry(Tr, Catalog, BaselineStatecraft.SingleParty, PartyId.None, new KeyValuePair<PartyId, Fixed64>[0], F("1.5"), Start).ErrorKey, Is.EqualTo(PoliticsErrorKeys.IstikrarAraligi));
            CountryPolitics tr = NewTurkey(state, Catalog);
            Assert.That(tr.NextElection, Is.EqualTo(Start.AddDays(1461)));
            Assert.That(tr.SupportShare(BaselineStatecraft.RepublicanPeoples), Is.EqualTo(F("0.7")));
            Assert.That(tr.InstitutionStrength(BaselineStatecraft.Army), Is.EqualTo(Fixed64.Half));
        }

        [Test]
        public void Stability_ConvergesToTarget_FromInstitutionsAndEffects()
        {
            var state = new PoliticsState();
            CountryPolitics tr = NewTurkey(state, Catalog, "0.1");
            var effects = new EffectEngine();
            effects.Add(EffectTarget.Of(Tr), PoliticsStatKeys.StabilityTarget, EffectOperation.Add, F("0.08"), Start, 0, "karakter.test");
            // 0.55 + 0.5×(0.1 + 0.1 + 0.05) + 0.08 = 0.755
            Assert.That(PoliticsRules.StabilityTarget(tr, Catalog, effects, Start, Fixed64.Zero), Is.EqualTo(F("0.755")));
            Assert.That(PoliticsRules.StabilityTarget(tr, Catalog, effects, Start, F("0.3")), Is.EqualTo(F("0.455")), "kıtlık cezası");

            var notes = new List<string>();
            for (int d = 0; d < 400; d++) PoliticsRules.AdvanceDay(tr, Catalog, effects, Start.AddDays(d), 1, Fixed64.Zero, notes);
            Assert.That(tr.Stability, Is.InRange(F("0.65"), F("0.76")), "kurumlar aşınırken hedefe yakınsar");
            Assert.That(tr.InstitutionStrength(BaselineStatecraft.Army), Is.EqualTo(Fixed64.Half - F("0.0005") * 400));
            Assert.That(tr.SupportShare(BaselineStatecraft.RepublicanPeoples), Is.GreaterThan(F("0.7")), "istikrar > 0.5 iken iktidar güçlenir");
        }

        [Test]
        public void Investment_StrengthensInstitution_UpToOne()
        {
            var state = new PoliticsState();
            NewTurkey(state, Catalog);
            Assert.That(PoliticsRules.Invest(state, Catalog, Tr, BaselineStatecraft.Parliament, F("3000")).IsValid, Is.True);
            Assert.That(state.Countries[Tr].InstitutionStrength(BaselineStatecraft.Parliament), Is.EqualTo(F("0.7")));
            PoliticsRules.Invest(state, Catalog, Tr, BaselineStatecraft.Parliament, F("1000000"));
            Assert.That(state.Countries[Tr].InstitutionStrength(BaselineStatecraft.Parliament), Is.EqualTo(Fixed64.One));
            Assert.That(PoliticsRules.CheckInvest(state, Catalog, Tr, BaselineStatecraft.Parliament, F("1")).ErrorKey, Is.EqualTo(PoliticsErrorKeys.KurumDolu));
            Assert.That(PoliticsRules.CheckInvest(state, Catalog, Tr, new InstitutionId(9), F("1")).ErrorKey, Is.EqualTo(PoliticsErrorKeys.KurumBilinmiyor));
            Assert.That(PoliticsRules.CheckInvest(state, Catalog, Tr, BaselineStatecraft.Army, Fixed64.Zero).ErrorKey, Is.EqualTo(PoliticsErrorKeys.MiktarGecersiz));
        }

        [Test]
        public void Crisis_StartsBelowThreshold_AppliesEffects_AndEndsAfterDuration()
        {
            var certain = new CrisisDefinition(new CrisisId(1), "kriz.test", F("0.4"), Fixed64.One, 3, F("-0.05"),
                new[] { new EffectGrant("ekonomi.uretimVerimi", EffectOperation.Percent, F("-0.1")) });
            PoliticsCatalog catalog = PoliticsCatalog.Create(
                new[] { new GovernmentFormDefinition(BaselineStatecraft.Republic, "y", 1461, F("0.2")) },
                new[] { new PartyDefinition(BaselineStatecraft.RepublicanPeoples, "p1", Tr, Ideology.Nationalist), new PartyDefinition(BaselineStatecraft.Progressive, "p2", Tr, Ideology.Liberal) },
                new InstitutionDefinition[0],
                new[] { certain },
                new PoliticsParameters(F("0.02"), F("0.01"), F("0.0005"), F("0.3"))).Value;
            var state = new PoliticsState();
            CountryPolitics tr = NewTurkey(state, catalog, "0.3");
            var effects = new EffectEngine();
            var notes = new List<string>();

            PoliticsRules.AdvanceDay(tr, catalog, effects, Start, 1, Fixed64.Zero, notes);
            Assert.That(tr.ActiveCrises.ContainsKey(certain.Id), Is.True);
            Assert.That(effects.Evaluate(EffectTarget.Of(Tr), "ekonomi.uretimVerimi", Fixed64.One, Start), Is.EqualTo(F("0.9")));
            Assert.That(tr.Stability, Is.EqualTo(F("0.298") - F("0.05")), "sürüklenme (0.3 → 0.298) + şok");
            Assert.That(notes, Has.Some.StartsWith(PoliticsRules.CrisisStartedNote));

            for (int d = 1; d <= 3; d++) PoliticsRules.AdvanceDay(tr, catalog, effects, Start.AddDays(d), 1, Fixed64.Zero, notes);
            Assert.That(notes, Has.Some.StartsWith(PoliticsRules.CrisisEndedNote));
            Assert.That(tr.ActiveCrises.Count, Is.EqualTo(1), "eşik hâlâ altında: aynı gün yeniden başlar");
            Assert.That(tr.ActiveCrises[certain.Id].StartedOn, Is.EqualTo(Start.AddDays(3)));
        }

        [Test]
        public void Election_IsWeightedDeterministicDraw()
        {
            var weights = new StableIdMap<PartyId, Fixed64>();
            weights.Add(new PartyId(1), F("7"));
            weights.Add(new PartyId(2), F("3"));
            Assert.That(PoliticsRules.WeightedPick(weights, F("0")), Is.EqualTo(new PartyId(1)));
            Assert.That(PoliticsRules.WeightedPick(weights, F("0.699999")), Is.EqualTo(new PartyId(1)));
            Assert.That(PoliticsRules.WeightedPick(weights, F("0.7")), Is.EqualTo(new PartyId(2)));

            int firstWins = 0;
            for (ulong seed = 1; seed <= 400; seed++)
            {
                var state = new PoliticsState();
                CountryPolitics tr = NewTurkey(state, Catalog);
                var notes = new List<string>();
                PoliticsRules.AdvanceDay(tr, Catalog, new EffectEngine(), tr.NextElection, seed, Fixed64.Zero, notes);
                Assert.That(notes, Has.Some.StartsWith(PoliticsRules.ElectionNote));
                Assert.That(tr.NextElection, Is.EqualTo(Start.AddDays(1461 * 2)));
                if (tr.RulingParty == BaselineStatecraft.RepublicanPeoples) firstWins++;

                var again = new PoliticsState();
                CountryPolitics replay = NewTurkey(again, Catalog);
                PoliticsRules.AdvanceDay(replay, Catalog, new EffectEngine(), replay.NextElection, seed, Fixed64.Zero, new List<string>());
                Assert.That(replay.RulingParty, Is.EqualTo(tr.RulingParty), "aynı tohum → aynı seçim");
            }

            Assert.That(firstWins, Is.InRange(245, 315), "≈ %70");
        }

        [Test]
        public void Snapshot_RoundTripsWithActiveCrisis()
        {
            var state = new PoliticsState();
            CountryPolitics tr = NewTurkey(state, Catalog, "0.05");
            var effects = new EffectEngine();
            for (int d = 0; d < 200 && tr.ActiveCrises.Count == 0; d++) PoliticsRules.AdvanceDay(tr, Catalog, effects, Start.AddDays(d), 5, Fixed64.Zero, new List<string>());
            Assert.That(tr.ActiveCrises.Count, Is.GreaterThan(0), "düşük istikrarda kriz çıkmalı");
            var w = new CanonicalWriter();
            PoliticsSnapshotCodec.Write(state, w);
            Result<PoliticsState> back = PoliticsSnapshotCodec.Read(new CanonicalReader(w.ToArray()));
            Assert.That(back.IsSuccess, Is.True);
            var w2 = new CanonicalWriter();
            PoliticsSnapshotCodec.Write(back.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));
            var violations = new List<string>();
            Assert.That(PoliticsInvariants.Check(back.Value, Catalog, violations), Is.EqualTo(0));
        }
    }
}
