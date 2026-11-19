using System;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Kenar türüne göre km başına maliyet (veri; kodda sabit yok). Kara kuvveti deniz yolunu, filo kara
    /// kenarını kullanamaz; açılış tarihi gelmemiş kenar kapalıdır. Maliyet = ⌈uzunluk × km maliyeti / 1000⌉.
    /// </summary>
    public sealed class EdgeKindCostModel : ITransportCostModel
    {
        private readonly long[] _costPerKm = new long[6];
        private readonly bool _naval;
        private readonly GameDate _date;

        /// <param name="costPerKmByKind">İndeks = <see cref="TransportEdgeKind"/> değeri; 0 = kullanılamaz.</param>
        public EdgeKindCostModel(long[] costPerKmByKind, bool naval, GameDate date)
        {
            if (costPerKmByKind == null || costPerKmByKind.Length != _costPerKm.Length)
            {
                throw new ArgumentException("Her kenar türü için bir değer gerekir.", nameof(costPerKmByKind));
            }

            long min = long.MaxValue;
            for (int i = 1; i < _costPerKm.Length; i++)
            {
                if (costPerKmByKind[i] < 0L)
                {
                    throw new ArgumentOutOfRangeException(nameof(costPerKmByKind));
                }

                _costPerKm[i] = costPerKmByKind[i];
                bool usable = costPerKmByKind[i] > 0L && (i == (int)TransportEdgeKind.SeaLane) == naval;
                if (usable && costPerKmByKind[i] < min)
                {
                    min = costPerKmByKind[i];
                }
            }

            _naval = naval;
            _date = date;
            MinCostPerKilometer = min == long.MaxValue ? 0L : min;
        }

        public long MinCostPerKilometer { get; }

        public bool TryGetCost(TransportEdge edge, TransportNode from, TransportNode to, out long cost)
        {
            long perKm = _costPerKm[(int)edge.Kind];
            if (perKm <= 0L || edge.IsNaval != _naval || !edge.IsOpenOn(_date))
            {
                cost = 0L;
                return false;
            }

            long product = checked(edge.LengthMeters * perKm);
            cost = (product + 999L) / 1000L;
            return true;
        }
    }
}
