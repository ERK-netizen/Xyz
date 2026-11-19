using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Geography
{
    /// <summary>
    /// Küçük sınama dünyası: A (40–41K, 28–29D) ve B (40–41K, 29–30D) kara komşusu; C adası (38–39K,
    /// 28–29D) yalnız deniz yoluyla bağlı. Düğümler: a1, a2 (şehir), aP (liman), b1 (şehir), cP (liman),
    /// S (deniz). Kenarlar: a1–a2 yol, a2–b1 yol, a1–b1 demiryolu (1930'da açılır), aP–a1 yol,
    /// aP–S ve S–cP deniz yolu.
    /// </summary>
    internal static class GeoFixtures
    {
        public static readonly CountryId Red = new CountryId(1UL);
        public static readonly CountryId Blue = new CountryId(2UL);
        public static readonly RegionId A = new RegionId(1UL);
        public static readonly RegionId B = new RegionId(2UL);
        public static readonly RegionId C = new RegionId(3UL);
        public static readonly TransportNodeId A1 = new TransportNodeId(1UL);
        public static readonly TransportNodeId A2 = new TransportNodeId(2UL);
        public static readonly TransportNodeId AP = new TransportNodeId(3UL);
        public static readonly TransportNodeId B1 = new TransportNodeId(4UL);
        public static readonly TransportNodeId CP = new TransportNodeId(5UL);
        public static readonly TransportNodeId S = new TransportNodeId(6UL);
        public static readonly GameDate Day1925 = GameDate.FromYmd(1925, 1, 1);
        public static readonly GameDate Day1935 = GameDate.FromYmd(1935, 1, 1);

        public static GeoPoint P(string lat, string lon) => new GeoPoint((int)Fixed64.Parse(lat).Raw, (int)Fixed64.Parse(lon).Raw);

        public static GeoRing Box(string minLat, string minLon, string maxLat, string maxLon) =>
            new GeoRing(new[] { P(minLat, minLon), P(minLat, maxLon), P(maxLat, maxLon), P(maxLat, minLon) });

        public static GeoMultiPolygon Square(string minLat, string minLon, string maxLat, string maxLon) =>
            new GeoMultiPolygon(new GeoPolygon(Box(minLat, minLon, maxLat, maxLon)));

        public static long[] LandCosts() => new long[] { 0L, 30L, 20L, 5L, 0L, 60L };

        public static long[] SeaCosts() => new long[] { 0L, 0L, 0L, 0L, 3L, 0L };

        public static GeoWorld Build()
        {
            var world = new GeoWorld();
            Ok(world.AddRegion(A, Square("40", "28", "41", "29"), Red));
            Ok(world.AddRegion(B, Square("40", "29", "41", "30"), Blue));
            Ok(world.AddRegion(C, Square("38", "28", "39", "29"), Red));
            Ok(world.Borders.AddBorder(A, B, 111_000L, true));

            Ok(world.Transport.AddNode(new TransportNode(A1, P("40.5", "28.5"), A, TransportNodeKind.City)));
            Ok(world.Transport.AddNode(new TransportNode(A2, P("40.5", "28.9"), A, TransportNodeKind.Junction)));
            Ok(world.Transport.AddNode(new TransportNode(AP, P("40.1", "28.5"), A, TransportNodeKind.Port)));
            Ok(world.Transport.AddNode(new TransportNode(B1, P("40.5", "29.5"), B, TransportNodeKind.City)));
            Ok(world.Transport.AddNode(new TransportNode(CP, P("38.9", "28.5"), C, TransportNodeKind.Port)));
            Ok(world.Transport.AddNode(new TransportNode(S, P("39.5", "28.5"), RegionId.None, TransportNodeKind.Sea)));

            Ok(world.Transport.AddEdge(Edge(1, A1, A2, TransportEdgeKind.Road, 40_000L, GameDate.MinValue)));
            Ok(world.Transport.AddEdge(Edge(2, A2, B1, TransportEdgeKind.Road, 60_000L, GameDate.MinValue)));
            Ok(world.Transport.AddEdge(Edge(3, A1, B1, TransportEdgeKind.Rail, 90_000L, GameDate.FromYmd(1930, 1, 1))));
            Ok(world.Transport.AddEdge(Edge(4, AP, A1, TransportEdgeKind.Road, 50_000L, GameDate.MinValue)));
            Ok(world.Transport.AddEdge(Edge(5, AP, S, TransportEdgeKind.SeaLane, 70_000L, GameDate.MinValue)));
            Ok(world.Transport.AddEdge(Edge(6, S, CP, TransportEdgeKind.SeaLane, 70_000L, GameDate.MinValue)));
            return world;
        }

        public static TransportEdge Edge(ulong id, TransportNodeId a, TransportNodeId b, TransportEdgeKind kind, long meters, GameDate from) =>
            new TransportEdge(new TransportEdgeId(id), a, b, kind, meters, 100, from, 2);

        public static void Ok(TarihStrateji.Core.Domain.Commands.ValidationResult result) =>
            NUnit.Framework.Assert.That(result.IsValid, NUnit.Framework.Is.True, result.ErrorKey);
    }
}
