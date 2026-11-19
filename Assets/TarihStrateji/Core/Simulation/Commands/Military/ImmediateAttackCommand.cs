using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Gün içi taktik saldırı: AP harcar, hemen çözülür, aynı gün P13'te tekrarlanmaz.</summary>
    public sealed class ImmediateAttackCommand : GameCommand
    {
        public const string KindKey = "emir.savas.anindaSaldiri";

        public ImmediateAttackCommand(CountryId issuer, LandUnitId unit, HexCoord target)
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
