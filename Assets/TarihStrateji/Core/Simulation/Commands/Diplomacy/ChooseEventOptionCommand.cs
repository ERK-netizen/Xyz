using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary>Bekleyen bir olayda seçenek seçer.</summary>
    public sealed class ChooseEventOptionCommand : GameCommand
    {
        public const string KindKey = "emir.olay.secenek";

        public ChooseEventOptionCommand(CountryId issuer, EventId eventId, int option)
            : base(issuer)
        {
            EventId = eventId;
            Option = option;
        }

        public EventId EventId { get; }

        public int Option { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
