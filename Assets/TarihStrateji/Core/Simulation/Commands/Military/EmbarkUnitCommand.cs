using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Limandaki kara birliği aynı limandaki nakliye filosuna biner (nakliye kapasitesi 0 ise reddedilir).</summary>
    public sealed class EmbarkUnitCommand : GameCommand
    {
        public const string KindKey = "emir.deniz.gemiyeBin";

        public EmbarkUnitCommand(CountryId issuer, LandUnitId unit, FleetId fleet)
            : base(issuer)
        {
            Unit = unit;
            Fleet = fleet;
        }

        public LandUnitId Unit { get; }

        public FleetId Fleet { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
