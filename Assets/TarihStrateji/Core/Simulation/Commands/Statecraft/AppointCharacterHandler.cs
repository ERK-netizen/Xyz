using System;
using TarihStrateji.Core.Characters;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    public sealed class AppointCharacterHandler : CommandHandler<AppointCharacterCommand>
    {
        private readonly CharacterCatalog _catalog;

        public AppointCharacterHandler(CharacterCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public override string Kind => AppointCharacterCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, AppointCharacterCommand c) =>
            CharacterRules.CheckAppoint(state.Characters, _catalog, state.Research, c.Issuer, c.Position, c.Character, state.Today);

        protected override void ApplyTyped(CommandContext context, AppointCharacterCommand c) =>
            CharacterRules.Appoint(context.State.Characters, _catalog, context.State.Research, context.State.Effects, c.Issuer, c.Position, c.Character, context.State.Today);

        protected override void WriteTyped(AppointCharacterCommand c, CanonicalWriter w)
        {
            w.WriteByte((byte)c.Position);
            w.WriteId(c.Character);
        }

        protected override AppointCharacterCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new AppointCharacterCommand(issuer, (PositionKind)r.ReadByte(), new CharacterId(r.ReadUInt64()));
    }
}
