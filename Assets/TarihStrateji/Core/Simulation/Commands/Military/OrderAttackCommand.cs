using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Komşu hücreye günlük saldırı emri (P13'te her gün çözülür).</summary>
    public sealed class OrderAttackCommand : GameCommand
    {
        public const string KindKey = "emir.savas.saldiriEmri";

        public OrderAttackCommand(CountryId issuer, LandUnitId unit, HexCoord target)
            : base(issuer)
        {
            Unit = unit;
            Target = target;
        }

        public LandUnitId Unit { get; }

        public HexCoord Target { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
