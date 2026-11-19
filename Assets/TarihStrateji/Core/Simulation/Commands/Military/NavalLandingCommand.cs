using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Gemideki birliğe çıkarma hedefi verir; P13'te çözülür (düşman varsa çıkarma muharebesi).</summary>
    public sealed class NavalLandingCommand : GameCommand
    {
        public const string KindKey = "emir.deniz.cikarma";

        public NavalLandingCommand(CountryId issuer, LandUnitId unit, HexCoord target)
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
