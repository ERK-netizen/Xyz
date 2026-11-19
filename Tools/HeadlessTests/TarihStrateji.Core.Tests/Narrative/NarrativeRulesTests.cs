using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Tests.World;

namespace TarihStrateji.Core.Tests.Narrative
{
    [TestFixture]
    public class NarrativeRulesTests
    {
        private static readonly CountryId A = TestWorlds.CountryA;
        private static readonly GameDate Day = GameDate.FromYmd(1925, 1, 1);
        private static readonly NarrativeCatalog Baseline = BaselineContent.Create().Narrative;

        private sealed class Stub : ICountryConditions
        {
            public bool AtWar;

            public bool Holds(CountryId country, Condition condition)
            {
                switch (condition.Kind)
                {
                    case ConditionKind.AtWar: return AtWar;
                    case ConditionKind.NotAtWar: return !AtWar;
                    default: return true;
                }
            }
        }

        private static EventOption Option(string key) => new EventOption(key, new[] { new EffectGrant(EconomyStatKeys.ProductionEfficiency, EffectOperation.Percent, Fixed64.Parse("0.1")) }, 10, Fixed64.FromInt(100));

        [Test]
        public void Catalog_RejectsFocusCyclesAndEventsWithoutOptions()
        {
            var a = new FocusDefinition(new FocusId(1), "odak.a", CountryId.None, 10, new[] { new FocusId(2) }, null, null, null, Fixed64.Zero);
            var b = new FocusDefinition(new FocusId(2), "odak.b", CountryId.None, 10, new[] { new FocusId(1) }, null, null, null, Fixed64.Zero);
            Assert.That(NarrativeCatalog.Create(new EventDefinition[0], new[] { a, b }).ErrorKey, Is.EqualTo(NarrativeErrorKeys.OdakDongusu));
            var noOptions = new EventDefinition(new EventId(1), "olay.x", CountryId.None, null, 1, true, 5, new EventOption[0]);
            Assert.That(NarrativeCatalog.Create(new[] { noOptions }, new FocusDefinition[0]).ErrorKey, Is.EqualTo(NarrativeErrorKeys.TanimGecersiz));
            Assert.That(Baseline.Focuses.Count, Is.EqualTo(5));
            Assert.That(Baseline.AreExclusive(BaselineNarrative.NavalBuildup, BaselineNarrative.ArmyModernization), Is.True, "dışlama simetrik");
        }

        [Test]
        public void FocusTree_PrerequisitesExclusionsConditions_AndCompletion()
        {
            var state = new NarrativeState();
            var stub = new Stub();
            Assert.That(NarrativeRules.CheckSelectFocus(state, Baseline, stub, A, BaselineNarrative.HeavyIndustry).ErrorKey, Is.EqualTo(NarrativeErrorKeys.OnKosulEksik));
            Assert.That(NarrativeRules.CheckSelectFocus(state, Baseline, stub, A, BaselineNarrative.Industrialization).IsValid, Is.True);
            NarrativeRules.SelectFocus(state, A, BaselineNarrative.Industrialization);
            Assert.That(NarrativeRules.CheckSelectFocus(state, Baseline, stub, A, BaselineNarrative.Industrialization).ErrorKey, Is.EqualTo(NarrativeErrorKeys.OdakSurmekte));

            var outcomes = new List<NarrativeOutcome>();
            for (int d = 0; d < 59; d++)
            {
                NarrativeRules.AdvanceFocusDay(state, Baseline, Day.AddDays(d), outcomes, new List<string>());
            }

            Assert.That(outcomes, Is.Empty);
            NarrativeRules.AdvanceFocusDay(state, Baseline, Day.AddDays(59), outcomes, new List<string>());
            Assert.That(outcomes.Count, Is.EqualTo(1));
            Assert.That(outcomes[0].IsFocus && outcomes[0].Focus == BaselineNarrative.Industrialization, Is.True);
            Assert.That(state.Countries[A].CompletedFocuses.ContainsKey(BaselineNarrative.Industrialization), Is.True);

            var effects = new EffectEngine();
            NarrativeRules.ApplyGrants(effects, Baseline, outcomes[0], Day, new List<EffectId>());
            Assert.That(effects.Evaluate(EffectTarget.Of(A), EconomyStatKeys.ProductionEfficiency, Fixed64.One, Day.AddDays(5000)), Is.EqualTo(Fixed64.Parse("1.05")), "odak etkisi kalıcı");

            Assert.That(NarrativeRules.CheckSelectFocus(state, Baseline, stub, A, BaselineNarrative.Industrialization).ErrorKey, Is.EqualTo(NarrativeErrorKeys.OdakTamamlandi));
            Assert.That(NarrativeRules.CheckSelectFocus(state, Baseline, stub, A, BaselineNarrative.HeavyIndustry).IsValid, Is.True);

            state.Countries[A].CompletedFocuses.Add(BaselineNarrative.ArmyModernization, Day);
            Assert.That(NarrativeRules.CheckSelectFocus(state, Baseline, stub, A, BaselineNarrative.NavalBuildup).ErrorKey, Is.EqualTo(NarrativeErrorKeys.Dislanmis));
            stub.AtWar = true;
            Assert.That(NarrativeRules.CheckSelectFocus(state, Baseline, stub, A, BaselineNarrative.Neutrality).ErrorKey, Is.EqualTo(NarrativeErrorKeys.KosulSaglanmiyor));
        }

        [Test]
        public void Events_FireDeterministically_WaitForAnswer_AndDefaultToTheFirstOption()
        {
            var always = new EventDefinition(new EventId(9), "olay.test", CountryId.None, new[] { new Condition(ConditionKind.AtWar, 0, Fixed64.Zero) },
                1, true, 5, new[] { Option("secenek.a"), Option("secenek.b") });
            NarrativeCatalog catalog = NarrativeCatalog.Create(new[] { always }, new FocusDefinition[0]).Value;
            var state = new NarrativeState();
            var stub = new Stub();
            var countries = new[] { A, TestWorlds.CountryB };
            NarrativeRules.FireDay(state, catalog, stub, countries, 42, Day, new List<string>());
            Assert.That(state.Countries.Count, Is.EqualTo(0), "koşul sağlanmıyor");

            stub.AtWar = true;
            var notes = new List<string>();
            NarrativeRules.FireDay(state, catalog, stub, countries, 42, Day, notes);
            Assert.That(state.Countries[A].Pending.Count, Is.EqualTo(1));
            Assert.That(notes.Count, Is.EqualTo(2));
            NarrativeRules.FireDay(state, catalog, stub, countries, 42, Day.AddDays(1), notes);
            Assert.That(state.Countries[A].Pending.Count, Is.EqualTo(1), "bekleyen olay tekrar tetiklenmez");

            Assert.That(NarrativeRules.CheckChoose(state, catalog, A, always.Id, 2).ErrorKey, Is.EqualTo(NarrativeErrorKeys.SecenekGecersiz));
            Assert.That(NarrativeRules.CheckChoose(state, catalog, A, always.Id, 1).IsValid, Is.True);
            NarrativeOutcome chosen = NarrativeRules.Choose(state, A, always.Id, 1);
            Assert.That(chosen.Option, Is.EqualTo(1));
            Assert.That(NarrativeRules.TreasuryChange(catalog, chosen), Is.EqualTo(Fixed64.FromInt(100)));

            var outcomes = new List<NarrativeOutcome>();
            NarrativeRules.ExpireDay(state, Day.AddDays(4), outcomes);
            Assert.That(outcomes, Is.Empty);
            NarrativeRules.ExpireDay(state, Day.AddDays(5), outcomes);
            Assert.That(outcomes.Count, Is.EqualTo(1), "B yanıtlamadı: ilk seçenek");
            Assert.That(outcomes[0].Country == TestWorlds.CountryB && outcomes[0].Option == 0, Is.True);

            NarrativeRules.FireDay(state, catalog, stub, countries, 42, Day.AddDays(9), notes);
            Assert.That(state.Countries[A].Pending.Count + state.Countries[TestWorlds.CountryB].Pending.Count, Is.EqualTo(0), "bir kez olan olay");

            var w = new CanonicalWriter();
            NarrativeSnapshotCodec.Write(state, w);
            Result<NarrativeState> back = NarrativeSnapshotCodec.Read(new CanonicalReader(w.ToArray()));
            Assert.That(back.IsSuccess, Is.True);
            var w2 = new CanonicalWriter();
            NarrativeSnapshotCodec.Write(back.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));
        }
    }
}
