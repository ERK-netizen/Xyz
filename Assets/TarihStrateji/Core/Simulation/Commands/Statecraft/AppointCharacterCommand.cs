using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    /// <summary>Karakteri bir pozisyona atar (önceki görevli görevden alınır).</summary>
    public sealed class AppointCharacterCommand : GameCommand
    {
        public const string KindKey = "emir.karakter.ata";

        public AppointCharacterCommand(CountryId issuer, PositionKind position, CharacterId character)
            : base(issuer)
        {
            Position = position;
            Character = character;
        }

        public PositionKind Position { get; }

        public CharacterId Character { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
