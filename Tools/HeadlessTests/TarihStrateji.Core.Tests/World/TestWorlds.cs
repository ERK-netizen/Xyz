using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.World
{
    /// <summary>
    /// Test haritası (altıgensiz): batı bölgesi (40–41K, 28–29D) A'nın, doğu bölgesi (40–41K, 29–30D)
    /// B'nin; kara sınırıyla komşu. Batıda liman şehri (40,5K 28,2D) ve kavşak, doğuda iç şehir
    /// (40,5K 29,5D, tahkimat 1,5) ve kavşak; limandan batıdaki deniz düğümüne deniz yolu.
    /// Kenarlar: liman–batı kavşak, batı kavşak–iç şehir, batı kavşak–doğu kavşak, doğu kavşak–iç şehir.
    /// </summary>
    public static class TestWorlds
    {
        public static readonly CountryId CountryA = new CountryId(1);
        public static readonly CountryId CountryB = new CountryId(2);
        public static readonly CountryId CountryC = new CountryId(3);
        public static readonly RegionId West = new RegionId(1);
        public static readonly RegionId East = new RegionId(2);
        public static readonly TransportNodeId PortNode = new TransportNodeId(1);
        public static readonly TransportNodeId InlandNode = new TransportNodeId(2);
        public static readonly TransportNodeId WestJunction = new TransportNodeId(3);
        public static readonly TransportNodeId EastJunction = new TransportNodeId(4);
        public static readonly TransportNodeId SeaNode = new TransportNodeId(5);
        public static readonly CityId PortCity = new CityId(1);
        public static readonly CityId InlandCity = new CityId(2);

        public static GeoWorld Coastal(out CityId portCity, out CityId inlandCity)
        {
            portCity = PortCity;
            inlandCity = InlandCity;
            return Coastal();
        }

        public static GeoWorld Coastal()
        {
            var world = new GeoWorld();
            Ok(world.AddRegion(West, Square(40, 28, 41, 29), CountryA));
            Ok(world.AddRegion(East, Square(40, 29, 41, 30), CountryB));
            Ok(world.Borders.AddBorder(West, East, 111_000L, true));
            Ok(world.Transport.AddNode(new TransportNode(PortNode, P(4050, 2820), West, TransportNodeKind.Port)));
            Ok(world.Transport.AddNode(new TransportNode(InlandNode, P(4050, 2950), East, TransportNodeKind.City)));
            Ok(world.Transport.AddNode(new TransportNode(WestJunction, P(4050, 2870), West, TransportNodeKind.Junction)));
            Ok(world.Transport.AddNode(new TransportNode(EastJunction, P(4030, 2920), East, TransportNodeKind.Junction)));
            Ok(world.Transport.AddNode(new TransportNode(SeaNode, P(4050, 2750), RegionId.None, TransportNodeKind.Sea)));
            Ok(world.Transport.AddEdge(Edge(1, PortNode, WestJunction, TransportEdgeKind.Road, 45_000L)));
            Ok(world.Transport.AddEdge(Edge(2, WestJunction, InlandNode, TransportEdgeKind.Road, 75_000L)));
            Ok(world.Transport.AddEdge(Edge(3, WestJunction, EastJunction, TransportEdgeKind.Road, 50_000L)));
            Ok(world.Transport.AddEdge(Edge(4, EastJunction, InlandNode, TransportEdgeKind.Road, 35_000L)));
            Ok(world.Transport.AddEdge(Edge(5, PortNode, SeaNode, TransportEdgeKind.SeaLane, 70_000L)));
            Ok(world.AddCity(new City(PortCity, "sehir.test.liman", P(4050, 2820), West, PortNode, 3, CountryA, Fixed64.One)));
            Ok(world.AddCity(new City(InlandCity, "sehir.test.ic", P(4050, 2950), East, InlandNode, 4, CountryB, Fixed64.Parse("1.5"))));
            Ok(world.Validate());
            return world;
        }

        public static GeoPoint P(int latHundredths, int lonHundredths) => new GeoPoint(latHundredths * 10_000, lonHundredths * 10_000);

        private static GeoMultiPolygon Square(int minLat, int minLon, int maxLat, int maxLon) =>
            new GeoMultiPolygon(new GeoPolygon(new GeoRing(new[]
            {
                P(minLat * 100, minLon * 100), P(minLat * 100, maxLon * 100), P(maxLat * 100, maxLon * 100), P(maxLat * 100, minLon * 100),
            })));

        private static TransportEdge Edge(ulong id, TransportNodeId a, TransportNodeId b, TransportEdgeKind kind, long meters) =>
            new TransportEdge(new TransportEdgeId(id), a, b, kind, meters, 100, GameDate.MinValue, 2);

        private static void Ok(ValidationResult result) =>
            NUnit.Framework.Assert.That(result.IsValid, NUnit.Framework.Is.True, result.ErrorKey);
    }
}
