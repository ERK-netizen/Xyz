using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    /// <summary>Süren bir inşaatı iptal eder (slotlar bırakılır, para kısmen iade edilir).</summary>
    public sealed class CancelConstructionCommand : GameCommand
    {
        public const string KindKey = "emir.ekonomi.insaatIptal";

        public CancelConstructionCommand(CountryId issuer, ConstructionId construction)
            : base(issuer)
        {
            Construction = construction;
        }

        public ConstructionId Construction { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
