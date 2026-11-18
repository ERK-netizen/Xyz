using TarihStrateji.Core.Characters;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    public sealed class DismissCharacterHandler : CommandHandler<DismissCharacterCommand>
    {
        public override string Kind => DismissCharacterCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, DismissCharacterCommand c) =>
            CharacterRules.CheckDismiss(state.Characters, c.Issuer, c.Position);

        protected override void ApplyTyped(CommandContext context, DismissCharacterCommand c) =>
            CharacterRules.Dismiss(context.State.Characters, context.State.Effects, c.Issuer, c.Position);

        protected override void WriteTyped(DismissCharacterCommand c, CanonicalWriter w) => w.WriteByte((byte)c.Position);

        protected override DismissCharacterCommand ReadTyped(CountryId issuer, CanonicalReader r) => new DismissCharacterCommand(issuer, (PositionKind)r.ReadByte());
    }
}
