using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Tests.Military
{
    [TestFixture]
    public class SupplyTests
    {
        private static readonly LandMovementCostModel Land = new LandMovementCostModel(FrontScenario.Content.Movement);

        private static SupplyGrid Compute(FrontScenario s, params HexCoord[] sources) =>
            SupplyRules.Compute(s.State.World, Land, OwnTerritorySupplyAccess.Instance, TestWorlds.CountryA, sources, FrontScenario.Content.Logistics.BaseSupplyRange);

        private static Fixed64 At(FrontScenario s, SupplyGrid g, int col, int row) => g.Level(s.State.World.Grid.IndexOf(new HexCoord(col, row)));

        [Test]
        public void Level_FallsWithDistance_AndStopsAtBorder()
        {
            var s = new FrontScenario();
            SupplyGrid g = Compute(s, new HexCoord(102, 4));
            Assert.That(At(s, g, 102, 4), Is.EqualTo(Fixed64.One));
            Fixed64 near = At(s, g, 104, 4), far = At(s, g, 108, 4);
            Assert.That(near > far && far.IsPositive, Is.True, $"{near} > {far} > 0");
            Assert.That(At(s, g, 110, 4), Is.EqualTo(Fixed64.Zero), "düşman toprağına ikmal geçmez");
            Assert.That(At(s, g, 102, 0), Is.EqualTo(Fixed64.Zero), "deniz");
        }

        [Test]
        public void Railway_ExtendsReach_AndEnemyLineCutsIt()
        {
            var s = new FrontScenario();
            Fixed64 before = At(s, Compute(s, new HexCoord(102, 4)), 109, 4);
            for (int col = 102; col <= 109; col++)
            {
                s.State.World.SetInfrastructure(new HexCoord(col, 4), new HexInfrastructure(0, 2, false));
            }

            Fixed64 after = At(s, Compute(s, new HexCoord(102, 4)), 109, 4);
            Assert.That(after > before, Is.True, $"demiryolu: {before} → {after}");

            for (int row = 1; row <= 8; row++)
            {
                s.State.World.SetHexController(new HexCoord(105, row), TestWorlds.CountryB); // kuzey-güney düşman hattı
            }

            Assert.That(At(s, Compute(s, new HexCoord(102, 4)), 108, 4), Is.EqualTo(Fixed64.Zero), "hat ikmali keser");
        }

        [Test]
        public void NoSources_MeansNoSupply()
        {
            var s = new FrontScenario();
            SupplyGrid g = Compute(s);
            for (int i = 0; i < g.CellCount; i++)
            {
                Assert.That(g.Level(i), Is.EqualTo(Fixed64.Zero));
            }
        }
    }
}
