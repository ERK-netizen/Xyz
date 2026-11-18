using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Characters;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.Research.State;
using TarihStrateji.Core.Simulation.Scenario;

namespace TarihStrateji.Core.Tests.Statecraft
{
    [TestFixture]
    public class CharacterTests
    {
        private static readonly TechCatalog Tech = BaselineContent.CreateTechnology();
        private static readonly CharacterCatalog Catalog = BaselineStandardContent.CreateCharacters(Tech.Eras.Count);
        private static readonly GameDate Start = GameDate.FromYmd(1920, 4, 23);
        private static readonly CountryId Tr = BaselineStatecraft.Turkey;

        private sealed class World
        {
            public readonly CharacterState Characters = new CharacterState();
            public readonly ResearchState Research = new ResearchState();
            public readonly EffectEngine Effects = new EffectEngine();

            public World(GameDate today)
            {
                Characters.AddCountry(Tr);
                Characters.AddCountry(BaselineStatecraft.Greece);
                Research.AddCountry(Tr, Tech, new TechnologyId[0], Effects, today);
                Research.AddCountry(BaselineStatecraft.Greece, Tech, new TechnologyId[0], Effects, today);
            }

            public Domain.Commands.ValidationResult Appoint(PositionKind p, CharacterId c, GameDate day) =>
                CharacterRules.Appoint(Characters, Catalog, Research, Effects, Tr, p, c, day);
        }

        [Test]
        public void Appointment_EnforcesCountryRoleWindowAndUniqueness()
        {
            var w = new World(Start);
            Assert.That(CharacterRules.CheckAppoint(w.Characters, Catalog, w.Research, Tr, PositionKind.HeadOfGovernment, BaselineStatecraft.EleftheriosVenizelos, Start).ErrorKey, Is.EqualTo(CharacterErrorKeys.BaskaUlkenin));
            Assert.That(CharacterRules.CheckAppoint(w.Characters, Catalog, w.Research, Tr, PositionKind.HeadOfGovernment, BaselineStatecraft.FevziCakmak, Start).ErrorKey, Is.EqualTo(CharacterErrorKeys.RolUygunDegil));
            Assert.That(CharacterRules.CheckAppoint(w.Characters, Catalog, w.Research, Tr, PositionKind.MinisterOfEconomy, BaselineStatecraft.CelalBayar, Start).ErrorKey, Is.EqualTo(CharacterErrorKeys.HenuzHizmetteDegil), "1921'den önce");
            Assert.That(CharacterRules.CheckAppoint(w.Characters, Catalog, w.Research, Tr, PositionKind.HeadOfGovernment, BaselineStatecraft.MustafaKemal, GameDate.FromYmd(1939, 1, 1)).ErrorKey, Is.EqualTo(CharacterErrorKeys.HizmetSona));
            Assert.That(CharacterRules.CheckAppoint(w.Characters, Catalog, w.Research, Tr, PositionKind.HeadOfGovernment, new CharacterId(99), Start).ErrorKey, Is.EqualTo(CharacterErrorKeys.KarakterBilinmiyor));
            Assert.That(CharacterRules.CheckAppoint(w.Characters, Catalog, w.Research, Tr, (PositionKind)9, BaselineStatecraft.MustafaKemal, Start).ErrorKey, Is.EqualTo(CharacterErrorKeys.PozisyonGecersiz));

            Assert.That(w.Appoint(PositionKind.HeadOfGovernment, BaselineStatecraft.MustafaKemal, Start).IsValid, Is.True);
            Assert.That(w.Appoint(PositionKind.ChiefOfGeneralStaff, BaselineStatecraft.MustafaKemal, Start).ErrorKey, Is.EqualTo(CharacterErrorKeys.ZatenGorevde));
            Assert.That(w.Effects.Evaluate(EffectTarget.Of(Tr), "siyaset.istikrarHedefi", Fixed64.Zero, Start), Is.EqualTo(Fixed64.Parse("0.08")));
        }

        [Test]
        public void Replacing_And_Dismissing_SwapTraits()
        {
            var w = new World(Start);
            w.Appoint(PositionKind.HeadOfGovernment, BaselineStatecraft.MustafaKemal, Start);
            Assert.That(w.Appoint(PositionKind.HeadOfGovernment, BaselineStatecraft.IsmetInonu, Start).IsValid, Is.True);
            Assert.That(w.Effects.Evaluate(EffectTarget.Of(Tr), "siyaset.istikrarHedefi", Fixed64.Zero, Start), Is.EqualTo(Fixed64.Parse("0.03")), "önceki özellikler kaldırıldı");
            Assert.That(w.Effects.Evaluate(EffectTarget.Of(Tr), "kara.saldiri", Fixed64.One, Start), Is.EqualTo(Fixed64.One));
            Assert.That(CharacterRules.Dismiss(w.Characters, w.Effects, Tr, PositionKind.HeadOfGovernment).IsValid, Is.True);
            Assert.That(w.Effects.Count, Is.EqualTo(0));
            Assert.That(CharacterRules.Dismiss(w.Characters, w.Effects, Tr, PositionKind.HeadOfGovernment).ErrorKey, Is.EqualTo(CharacterErrorKeys.PozisyonBos));
        }

        [Test]
        public void ServiceWindowEnd_DismissesAutomatically()
        {
            var w = new World(GameDate.FromYmd(1938, 11, 1));
            Assert.That(w.Appoint(PositionKind.HeadOfGovernment, BaselineStatecraft.MustafaKemal, GameDate.FromYmd(1938, 11, 1)).IsValid, Is.True);
            var notes = new List<string>();
            CharacterRules.ExpireDay(w.Characters, Catalog, w.Effects, GameDate.FromYmd(1938, 11, 10), notes);
            Assert.That(w.Characters.Countries[Tr].Holder(PositionKind.HeadOfGovernment), Is.EqualTo(BaselineStatecraft.MustafaKemal), "son gün hâlâ görevde");
            CharacterRules.ExpireDay(w.Characters, Catalog, w.Effects, GameDate.FromYmd(1938, 11, 11), notes);
            Assert.That(w.Characters.Countries[Tr].Holder(PositionKind.HeadOfGovernment).IsValid, Is.False);
            Assert.That(notes, Has.Some.EndsWith("karakter.mustafaKemal"));
            Assert.That(w.Effects.Count, Is.EqualTo(0));
        }

        [Test]
        public void EraGate_And_CatalogValidation()
        {
            var late = new CharacterDefinition(new CharacterId(50), "karakter.gelecek", Tr, CharacterRole.General, GameDate.MinValue, GameDate.MaxValue, 2, null);
            CharacterCatalog catalog = CharacterCatalog.Create(new[] { late }, Tech.Eras.Count).Value;
            var w = new World(Start);
            Assert.That(CharacterRules.CheckAppoint(w.Characters, catalog, w.Research, Tr, PositionKind.ChiefOfGeneralStaff, late.Id, Start).ErrorKey, Is.EqualTo(CharacterErrorKeys.CagUygunDegil));

            var backwards = new CharacterDefinition(new CharacterId(51), "x", Tr, CharacterRole.General, GameDate.FromYmd(1930, 1, 1), GameDate.FromYmd(1920, 1, 1), 0, null);
            Assert.That(CharacterCatalog.Create(new[] { backwards }, 3).ErrorKey, Is.EqualTo(CharacterErrorKeys.TanimGecersiz));
            var noRole = new CharacterDefinition(new CharacterId(52), "x", Tr, CharacterRole.None, GameDate.MinValue, GameDate.MaxValue, 0, null);
            Assert.That(CharacterCatalog.Create(new[] { noRole }, 3).ErrorKey, Is.EqualTo(CharacterErrorKeys.TanimGecersiz));
        }

        [Test]
        public void Invariants_And_Snapshot()
        {
            var w = new World(Start);
            w.Appoint(PositionKind.HeadOfGovernment, BaselineStatecraft.MustafaKemal, Start);
            w.Appoint(PositionKind.ChiefOfGeneralStaff, BaselineStatecraft.FevziCakmak, Start);
            var writer = new CanonicalWriter();
            CharacterSnapshotCodec.Write(w.Characters, writer);
            Result<CharacterState> back = CharacterSnapshotCodec.Read(new CanonicalReader(writer.ToArray()));
            Assert.That(back.IsSuccess, Is.True);
            var w2 = new CanonicalWriter();
            CharacterSnapshotCodec.Write(back.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(writer.ToArray()));

            var violations = new List<string>();
            Assert.That(CharacterInvariants.Check(w.Characters, Catalog, Start, violations), Is.EqualTo(0));
            Assert.That(CharacterInvariants.Check(w.Characters, Catalog, GameDate.FromYmd(1951, 1, 1), violations), Is.EqualTo(2), "pencere dışında görevde kalamaz");
        }
    }
}
