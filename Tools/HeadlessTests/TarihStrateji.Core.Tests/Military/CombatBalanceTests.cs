using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.Military
{
    /// <summary>
    /// game-balance "muharebe tutarlılık eğrileri": sabit eşleşmeler 200 tohumda çözülür ve
    /// saldırgan başarı oranı tasarım bandında olmalıdır (tarihî "3:1 kuralı"). Bant dışına çıkmak
    /// bir içerik/formül değişikliğinin dengeyi bozduğunu gösterir.
    /// </summary>
    [TestFixture]
    public class CombatBalanceTests
    {
        private static int AttackerWins(int attackers, TerrainType terrain, int seeds = 200)
        {
            int wins = 0;
            var target = new HexCoord(110, 4);
            for (ulong seed = 1; seed <= (ulong)seeds; seed++)
            {
                var s = new FrontScenario(seed: seed);
                s.Cell(110, 4).Terrain = terrain;
                LandUnit defender = s.Place(TestWorlds.CountryB, BaselineUnits.InfantryDivision, 110, 4);
                var force = new List<LandUnit>();
                for (int i = 0; i < attackers; i++)
                {
                    force.Add(s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 109, 4));
                }

                WarfareContext war = s.War;
                for (int day = 0; day < 60; day++)
                {
                    var ready = new List<LandUnit>();
                    foreach (LandUnit a in force)
                    {
                        if (s.State.Warfare.Units.ContainsKey(a.Id) && CombatRules.CheckAttack(war, a.Owner, a.Id, target).IsValid)
                        {
                            ready.Add(a);
                        }
                    }

                    if (ready.Count == 0)
                    {
                        break;
                    }

                    var rng = new RngStream(seed, RngSubsystem.Combat, day, ready[0].Id.Value, 0);
                    CombatRules.Resolve(war, ready, target, ref rng, new List<string>());
                    if (!s.State.Warfare.Units.ContainsKey(defender.Id) || defender.Hex != s.State.World.Grid.ToId(target))
                    {
                        wins++;
                        break;
                    }
                }
            }

            return wins;
        }

        [Test]
        public void EqualInfantry_OnPlains_DefenderUsuallyHolds() => Assert.That(AttackerWins(1, TerrainType.Plains), Is.InRange(20, 60), "%10–30");

        [Test]
        public void TwoToOne_OnPlains_UsuallySucceeds() => Assert.That(AttackerWins(2, TerrainType.Plains), Is.InRange(120, 180), "%60–90");

        [Test]
        public void ThreeToOne_InHills_SucceedsMoreOftenThanNot() => Assert.That(AttackerWins(3, TerrainType.Hills), Is.InRange(110, 160), "%55–80");

        [Test]
        public void ThreeToOne_InMountains_IsAGamble() => Assert.That(AttackerWins(3, TerrainType.Mountains), Is.InRange(70, 130), "%35–65");
    }
}
