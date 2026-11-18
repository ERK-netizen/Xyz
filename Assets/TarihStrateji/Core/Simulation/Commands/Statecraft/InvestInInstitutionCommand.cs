using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    /// <summary>Hazineden kuruma yatırım yapar.</summary>
    public sealed class InvestInInstitutionCommand : GameCommand
    {
        public const string KindKey = "emir.siyaset.kurumaYatirim";

        public InvestInInstitutionCommand(CountryId issuer, InstitutionId institution, Fixed64 money)
            : base(issuer)
        {
            Institution = institution;
            Money = money;
        }

        public InstitutionId Institution { get; }

        public Fixed64 Money { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
