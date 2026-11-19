using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Intelligence.State
{
    /// <summary>Bir ülkenin gözlem kaynağı: merkez hücre ve görüş yarıçapı (hücre).</summary>
    public readonly struct Sensor
    {
        public Sensor(HexCoord center, int radius)
        {
            Center = center;
            Radius = radius;
        }

        public HexCoord Center { get; }

        public int Radius { get; }
    }
}
