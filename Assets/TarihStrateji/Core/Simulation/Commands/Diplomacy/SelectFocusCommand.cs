using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary>Ulusal odak seçer (önceki odağın ilerlemesi sıfırlanır).</summary>
    public sealed class SelectFocusCommand : GameCommand
    {
        public const string KindKey = "emir.odak.sec";

        public SelectFocusCommand(CountryId issuer, FocusId focus)
            : base(issuer)
        {
            Focus = focus;
        }

        public FocusId Focus { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
