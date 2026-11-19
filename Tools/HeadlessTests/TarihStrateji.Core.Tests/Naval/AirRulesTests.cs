using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Tests.Naval
{
    [TestFixture]
    public class AirRulesTests
    {
        [Test]
        public void AirWings_NeedAnOwnedBase_RealAircraft_AndBaseCapacity()
        {
            var s = new StraitScenario();
            HexCoord inland = StraitScenario.At(s.InlandA, s.State.World);
            Assert.That(AirRules.CheckRecruit(s.War, TestWorlds.CountryA, BaselineUnits.FighterWing, StraitScenario.At(s.PortB, s.State.World)).ErrorKey, Is.EqualTo(WarfareErrorKeys.HavaUssuGecersiz));
            Assert.That(AirRules.CheckRecruit(s.War, TestWorlds.CountryA, BaselineUnits.FighterWing, new HexCoord(105, 4)).ErrorKey, Is.EqualTo(WarfareErrorKeys.HavaUssuGecersiz), "şehir değil");
            Assert.That(AirRules.CheckRecruit(s.War, TestWorlds.CountryA, BaselineUnits.InfantryDivision, inland).ErrorKey, Is.EqualTo(WarfareErrorKeys.KanatTuruBilinmiyor));
            for (int i = 0; i < 4; i++)
            {
                s.Wing(TestWorlds.CountryA, BaselineUnits.FighterWing, s.InlandA);
            }

            Assert.That(s.State.Economy.Countries[TestWorlds.CountryA].Stock(BaselineGoods.Aircraft), Is.EqualTo(Fixed64.FromInt(300)));
            Assert.That(s.State.Population.Regions[s.West].Mobilized, Is.EqualTo(600));
            Assert.That(AirRules.CheckRecruit(s.War, TestWorlds.CountryA, BaselineUnits.FighterWing, inland).ErrorKey, Is.EqualTo(WarfareErrorKeys.UsDolu));
            AirWing first = s.State.Warfare.AirWings.ValueAt(0);
            Assert.That(AirRules.CheckRebase(s.War, TestWorlds.CountryA, first.Id, StraitScenario.At(s.PortA, s.State.World)).IsValid, Is.True);
            Assert.That(AirRules.CheckRebase(s.War, TestWorlds.CountryA, first.Id, inland).IsValid, Is.True, "aynı üsse kalmak");
        }

        [Test]
        public void Missions_BeyondRange_AreRejected()
        {
            var s = new StraitScenario();
            AirWing fighter = s.Wing(TestWorlds.CountryA, BaselineUnits.FighterWing, s.InlandA);
            AirWing bomber = s.Wing(TestWorlds.CountryA, BaselineUnits.BomberWing, s.InlandA);
            WarfareContext war = s.War;
            Assert.That(AirRules.CheckMission(war, TestWorlds.CountryA, fighter.Id, AirMission.AirSuperiority, new HexCoord(110, 4)).IsValid, Is.True, "≈300 km");
            Assert.That(AirRules.CheckMission(war, TestWorlds.CountryA, fighter.Id, AirMission.AirSuperiority, new HexCoord(113, 4)).ErrorKey, Is.EqualTo(WarfareErrorKeys.MenzilDisi), "≈430 km > 350");
            Assert.That(AirRules.CheckMission(war, TestWorlds.CountryA, bomber.Id, AirMission.GroundSupport, new HexCoord(118, 4)).IsValid, Is.True, "≈640 km < 700");
            Assert.That(AirRules.CheckMission(war, TestWorlds.CountryA, bomber.Id, AirMission.GroundSupport, new HexCoord(122, 4)).ErrorKey, Is.EqualTo(WarfareErrorKeys.MenzilDisi));
            Assert.That(AirRules.CheckMission(war, TestWorlds.CountryA, bomber.Id, AirMission.GroundSupport, new HexCoord(111, 4)).ErrorKey, Is.EqualTo(WarfareErrorKeys.GorevHedefiGecersiz), "deniz");
            Assert.That(AirRules.CheckMission(war, TestWorlds.CountryA, bomber.Id, AirMission.NavalStrike, new HexCoord(108, 4)).ErrorKey, Is.EqualTo(WarfareErrorKeys.GorevHedefiGecersiz), "kara");
            Assert.That(AirRules.CheckMission(war, TestWorlds.CountryB, bomber.Id, AirMission.None, new HexCoord(108, 4)).ErrorKey, Is.EqualTo(WarfareErrorKeys.KanatBizimDegil));
        }

        [Test]
        public void GroundSupport_IsScaledByAirSuperiority()
        {
            var s = new StraitScenario();
            AirWing bomber = s.Wing(TestWorlds.CountryA, BaselineUnits.BomberWing, s.InlandA);
            HexId target = s.Hex(113, 3);
            Assert.That(JointSupportRules.Support(s.War, s.Cell(113, 3), TestWorlds.CountryA), Is.EqualTo(Fixed64.Zero));
            AirRules.SetMission(bomber, AirMission.GroundSupport, target);
            Fixed64 alone = JointSupportRules.Support(s.War, s.Cell(113, 3), TestWorlds.CountryA);
            Assert.That(alone, Is.EqualTo(Fixed64.FromInt(4)), "tam güç × tam teşkilat × hava payı 1");

            AirWing fighter = s.Wing(TestWorlds.CountryB, BaselineUnits.FighterWing, s.InlandB);
            AirRules.SetMission(fighter, AirMission.AirSuperiority, target);
            Fixed64 contested = JointSupportRules.Support(s.War, s.Cell(113, 3), TestWorlds.CountryA);
            Assert.That(contested, Is.EqualTo(Fixed64.FromInt(4) * Fixed64.MulDiv(Fixed64.FromInt(3), Fixed64.One, Fixed64.FromInt(15))), "pay = 3 / (3 + 12)");
        }

        [Test]
        public void AirBattle_FightersBeatBombers_AndBrokenWingsReturnToBase()
        {
            var s = new StraitScenario();
            AirWing bomber = s.Wing(TestWorlds.CountryA, BaselineUnits.BomberWing, s.InlandA);
            AirWing fighter = s.Wing(TestWorlds.CountryB, BaselineUnits.FighterWing, s.InlandB);
            HexId target = s.Hex(113, 3);
            AirRules.SetMission(bomber, AirMission.GroundSupport, target);
            AirRules.SetMission(fighter, AirMission.AirSuperiority, target);
            var hexes = new List<HexId>();
            AirCombatRules.ContestedHexes(s.War, hexes);
            Assert.That(hexes, Is.EqualTo(new[] { target }));
            for (int day = 0; day < 6 && bomber.Mission != AirMission.None; day++)
            {
                var rng = new RngStream(3, RngSubsystem.AirCombat, day, target.Value, 0);
                AirCombatRules.ResolveBattle(s.War, target, ref rng, new List<string>());
            }

            Assert.That(bomber.Mission, Is.EqualTo(AirMission.None), "kırılan bombardıman kanadı görevi bırakır");
            Assert.That(fighter.Organization > bomber.Organization, Is.True);
            Assert.That(bomber.Equipment < Fixed64.One, Is.True, "uçak kaybı");
        }

        [Test]
        public void ShoreBombardment_SupportsAdjacentLandBattles()
        {
            var s = new StraitScenario();
            Assert.That(JointSupportRules.Support(s.War, s.Cell(113, 3), TestWorlds.CountryB), Is.EqualTo(Fixed64.Zero));
            s.Fleet(TestWorlds.CountryB, BaselineUnits.BattleshipSquadron, 112, 3);
            Assert.That(JointSupportRules.Support(s.War, s.Cell(113, 3), TestWorlds.CountryB), Is.EqualTo(Fixed64.FromInt(6)));
            Assert.That(JointSupportRules.Support(s.War, s.Cell(113, 3), TestWorlds.CountryA), Is.EqualTo(Fixed64.Zero));
            Assert.That(JointSupportRules.Support(s.War, s.Cell(116, 3), TestWorlds.CountryB), Is.EqualTo(Fixed64.Zero), "kıyıdan uzak");
        }

        [Test]
        public void LostBase_RebasesToTheNearestFreeBase_OrTheWingIsLost()
        {
            var s = new StraitScenario();
            AirWing wing = s.Wing(TestWorlds.CountryA, BaselineUnits.FighterWing, s.PortA);
            AirRules.SetMission(wing, AirMission.AirSuperiority, s.Hex(110, 4));
            var notes = new List<string>();
            s.Occupy(s.PortA, TestWorlds.CountryB);
            AirRules.UpdateBases(s.War, notes);
            Assert.That(wing.Base, Is.EqualTo(s.InlandA.Id));
            Assert.That(wing.Mission, Is.EqualTo(AirMission.None));

            long mobilized = s.State.Population.Regions[s.West].Mobilized;
            s.Occupy(s.InlandA, TestWorlds.CountryB);
            AirRules.UpdateBases(s.War, notes);
            Assert.That(s.State.Warfare.AirWings.ContainsKey(wing.Id), Is.False);
            Assert.That(notes, Has.Some.StartsWith(AirRules.LostNote));
            Assert.That(s.State.Population.Regions[s.West].Mobilized, Is.EqualTo(mobilized - 150), "pilotlar eve döner, uçaklar kaybedilir");
        }

        [Test]
        public void MissionsCostOrganization_IdleWingsRecover()
        {
            var s = new StraitScenario();
            AirWing busy = s.Wing(TestWorlds.CountryA, BaselineUnits.FighterWing, s.InlandA);
            AirWing idle = s.Wing(TestWorlds.CountryA, BaselineUnits.FighterWing, s.InlandA);
            idle.Organization = Fixed64.FromInt(20);
            AirRules.SetMission(busy, AirMission.AirSuperiority, s.Hex(108, 4));
            AirRules.ApplyDay(s.War);
            Assert.That(busy.Organization, Is.EqualTo(Fixed64.FromInt(38)));
            Assert.That(idle.Organization, Is.EqualTo(Fixed64.FromInt(24)));
            for (int d = 0; d < 30; d++)
            {
                AirRules.ApplyDay(s.War);
            }

            Assert.That(busy.Mission, Is.EqualTo(AirMission.None), "teşkilat eşiğin altına inince görev biter");
        }
    }
}
