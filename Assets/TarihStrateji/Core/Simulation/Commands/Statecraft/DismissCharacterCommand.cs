using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    /// <summary>Pozisyondaki karakteri görevden alır.</summary>
    public sealed class DismissCharacterCommand : GameCommand
    {
        public const string KindKey = "emir.karakter.gorevdenAl";

        public DismissCharacterCommand(CountryId issuer, PositionKind position)
            : base(issuer)
        {
            Position = position;
        }

        public PositionKind Position { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
