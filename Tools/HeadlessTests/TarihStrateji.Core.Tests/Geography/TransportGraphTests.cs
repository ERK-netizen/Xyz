using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.World.Geography;
using static TarihStrateji.Core.Tests.Geography.GeoFixtures;

namespace TarihStrateji.Core.Tests.Geography
{
    /// <summary>REG-002/015, MAP-G05, WAR-L11/L12: sahte komşuluk, ışınlanma ve sudan yürüyüş yok.</summary>
    [TestFixture]
    public class TransportGraphTests
    {
        [Test]
        public void LandEdge_BetweenNonNeighbourRegions_IsRejected()
        {
            GeoWorld world = Build();
            Assert.That(world.Transport.AddEdge(Edge(10, A1, CP, TransportEdgeKind.Road, 300_000L, GameDate.MinValue)).ErrorKey,
                Is.EqualTo(GeographyErrorKeys.KaraKenariKomsuDegil), "adaya karadan yol yok");
        }

        [Test]
        public void Edge_ShorterThanGreatCircle_IsRejected()
        {
            GeoWorld world = Build();
            Assert.That(world.Transport.AddEdge(Edge(10, A1, B1, TransportEdgeKind.Road, 10_000L, GameDate.MinValue)).ErrorKey,
                Is.EqualTo(GeographyErrorKeys.KenarUzunluguGecersiz), "ışınlanma kenarı");
        }

        [Test]
        public void SeaLane_RequiresNavalEndpoints_StraitHasMaximumLength()
        {
            GeoWorld world = Build();
            Assert.That(world.Transport.AddEdge(Edge(10, A1, S, TransportEdgeKind.SeaLane, 150_000L, GameDate.MinValue)).ErrorKey,
                Is.EqualTo(GeographyErrorKeys.DenizKenariLimanDegil));
            Assert.That(world.Transport.AddEdge(Edge(11, AP, CP, TransportEdgeKind.StraitCrossing, 140_000L, GameDate.MinValue)).ErrorKey,
                Is.EqualTo(GeographyErrorKeys.KaraKenariKomsuDegil), "130 km boğaz değildir");
        }

        [Test]
        public void Node_OutsideItsRegion_IsRejected()
        {
            GeoWorld world = Build();
            var node = new TransportNode(new TransportNodeId(99UL), P("40.5", "29.5"), A, TransportNodeKind.City);
            Assert.That(world.Transport.AddNode(node).ErrorKey, Is.EqualTo(GeographyErrorKeys.DugumBolgeDisinda));
        }

        [Test]
        public void LandForce_CannotReachIsland_NavalForceCan()
        {
            GeoWorld world = Build();
            var finder = new TransportPathfinder(world.Transport);
            Result<TransportPath> land = finder.FindPath(A1, CP, new EdgeKindCostModel(LandCosts(), false, Day1925));
            Assert.That(land.ErrorKey, Is.EqualTo(GeographyErrorKeys.RotaYok));

            Result<TransportPath> sea = finder.FindPath(AP, CP, new EdgeKindCostModel(SeaCosts(), true, Day1925));
            Assert.That(sea.IsSuccess, Is.True);
            Assert.That(sea.Value.Nodes, Is.EqualTo(new[] { AP, S, CP }));
            Assert.That(sea.Value.LengthMeters, Is.EqualTo(140_000L));
        }

        [Test]
        public void Railway_IsUsedOnlyAfterItOpens()
        {
            GeoWorld world = Build();
            var finder = new TransportPathfinder(world.Transport);
            TransportPath before = finder.FindPath(A1, B1, new EdgeKindCostModel(LandCosts(), false, Day1925)).Value;
            TransportPath after = finder.FindPath(A1, B1, new EdgeKindCostModel(LandCosts(), false, Day1935)).Value;
            Assert.That(before.Edges, Is.EqualTo(new[] { new TransportEdgeId(1UL), new TransportEdgeId(2UL) }), "yol: 40+60 km × 20");
            Assert.That(before.Cost, Is.EqualTo(2_000L));
            Assert.That(after.Edges, Is.EqualTo(new[] { new TransportEdgeId(3UL) }), "demiryolu: 90 km × 5");
            Assert.That(after.Cost, Is.EqualTo(450L));
        }

        [Test]
        public void Pathfinding_IsDeterministic_AndRespectsExpansionLimit()
        {
            GeoWorld world = Build();
            var finder = new TransportPathfinder(world.Transport);
            var model = new EdgeKindCostModel(LandCosts(), false, Day1935);
            TransportPath first = finder.FindPath(AP, B1, model).Value;
            for (int i = 0; i < 20; i++)
            {
                TransportPath again = finder.FindPath(AP, B1, model).Value;
                Assert.That(again.Nodes, Is.EqualTo(first.Nodes));
                Assert.That(again.Cost, Is.EqualTo(first.Cost));
            }

            Assert.That(finder.FindPath(AP, B1, model, 1).ErrorKey, Is.EqualTo(GeographyErrorKeys.RotaAramaSiniri));
            Assert.That(finder.FindPath(AP, new TransportNodeId(77UL), model).ErrorKey, Is.EqualTo(GeographyErrorKeys.DugumBulunamadi));
        }

        [Test]
        public void Path_IsOptimal_AgainstExhaustiveSearch()
        {
            // Kabul edilebilir sezgisel: A* maliyeti, tüm basit yolların en küçüğüne eşit.
            GeoWorld world = Build();
            var finder = new TransportPathfinder(world.Transport);
            var model = new EdgeKindCostModel(LandCosts(), false, Day1935);
            TransportNodeId[] land = { A1, A2, AP, B1 };
            foreach (TransportNodeId from in land)
            {
                foreach (TransportNodeId to in land)
                {
                    long best = Exhaustive(world.Transport, model, from, to, new System.Collections.Generic.HashSet<TransportNodeId> { from });
                    Assert.That(finder.FindPath(from, to, model).Value.Cost, Is.EqualTo(best), from + "→" + to);
                }
            }
        }

        private static long Exhaustive(TransportGraph graph, ITransportCostModel model, TransportNodeId at, TransportNodeId goal, System.Collections.Generic.HashSet<TransportNodeId> seen)
        {
            if (at == goal)
            {
                return 0L;
            }

            long best = long.MaxValue;
            graph.TryGetNode(at, out TransportNode from);
            foreach (TransportEdge edge in graph.EdgesOf(at))
            {
                TransportNodeId next = edge.Other(at);
                graph.TryGetNode(next, out TransportNode to);
                if (seen.Contains(next) || !model.TryGetCost(edge, from, to, out long cost))
                {
                    continue;
                }

                seen.Add(next);
                long rest = Exhaustive(graph, model, next, goal, seen);
                seen.Remove(next);
                if (rest != long.MaxValue && cost + rest < best)
                {
                    best = cost + rest;
                }
            }

            return best;
        }
    }
}
