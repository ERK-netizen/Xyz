using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary><see cref="ChooseEventOptionCommand"/>: seçeneğin etkileri ve hazine değişimi P1'de uygulanır.</summary>
    public sealed class ChooseEventOptionHandler : ContentHandler<ChooseEventOptionCommand>
    {
        public ChooseEventOptionHandler(GameContent content) : base(content) { }

        public override string Kind => ChooseEventOptionCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, ChooseEventOptionCommand c) =>
            NarrativeRules.CheckChoose(state.Narrative, Content.Narrative, c.Issuer, c.EventId, c.Option);

        protected override void ApplyTyped(CommandContext context, ChooseEventOptionCommand c)
        {
            NarrativeOutcome outcome = NarrativeRules.Choose(context.State.Narrative, c.Issuer, c.EventId, c.Option);
            NarrativeSystem.Apply(context.State, Content, outcome);
        }

        protected override void WriteTyped(ChooseEventOptionCommand c, CanonicalWriter w)
        {
            w.WriteId(c.EventId);
            w.WriteInt32(c.Option);
        }

        protected override ChooseEventOptionCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new ChooseEventOptionCommand(issuer, new EventId(r.ReadUInt64()), r.ReadInt32());
    }
}
