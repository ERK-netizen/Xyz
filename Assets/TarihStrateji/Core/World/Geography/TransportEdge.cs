using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Yönsüz ulaşım kenarı: uzunluk (metre, büyük çember mesafesinden kısa olamaz), günlük kapasite
    /// (ikmal birimi), açılış tarihi (dönemsel demiryolu/kanal) ve arazi kodu (hareket maliyeti veridir).
    /// </summary>
    public sealed class TransportEdge
    {
        public TransportEdge(TransportEdgeId id, TransportNodeId a, TransportNodeId b, TransportEdgeKind kind,
            long lengthMeters, int dailyCapacity, GameDate availableFrom, byte terrain)
        {
            Id = id;
            A = a;
            B = b;
            Kind = kind;
            LengthMeters = lengthMeters;
            DailyCapacity = dailyCapacity;
            AvailableFrom = availableFrom;
            Terrain = terrain;
        }

        public TransportEdgeId Id { get; }

        public TransportNodeId A { get; }

        public TransportNodeId B { get; }

        public TransportEdgeKind Kind { get; }

        public long LengthMeters { get; }

        public int DailyCapacity { get; }

        public GameDate AvailableFrom { get; }

        public byte Terrain { get; }

        /// <summary>Yalnız filoların kullandığı deniz yolu. Boğaz geçişi kara birliği içindir (feribot/köprü).</summary>
        public bool IsNaval => Kind == TransportEdgeKind.SeaLane;

        public TransportNodeId Other(TransportNodeId from) => from == A ? B : A;

        public bool IsOpenOn(GameDate date) => date >= AvailableFrom;
    }
}
