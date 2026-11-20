using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Intelligence.State
{
    /// <summary>Bir ülkenin gözlem kaynağı: merkez ulaşım düğümü ve görüş yarıçapı (metre, graf yolu).</summary>
    public readonly struct Sensor
    {
        public Sensor(TransportNodeId center, long radiusMeters)
        {
            Center = center;
            RadiusMeters = radiusMeters;
        }

        public TransportNodeId Center { get; }

        public long RadiusMeters { get; }
    }
}
