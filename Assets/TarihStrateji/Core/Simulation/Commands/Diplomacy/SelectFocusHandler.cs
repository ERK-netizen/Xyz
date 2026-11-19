using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary><see cref="SelectFocusCommand"/>: ön koşul, dışlama ve koşullar P1'de yeniden denetlenir.</summary>
    public sealed class SelectFocusHandler : ContentHandler<SelectFocusCommand>
    {
        public SelectFocusHandler(GameContent content) : base(content) { }

        public override string Kind => SelectFocusCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, SelectFocusCommand c) =>
            NarrativeRules.CheckSelectFocus(state.Narrative, Content.Narrative, new CountryConditions(state), c.Issuer, c.Focus);

        protected override void ApplyTyped(CommandContext context, SelectFocusCommand c)
        {
            NarrativeRules.SelectFocus(context.State.Narrative, c.Issuer, c.Focus);
        }

        protected override void WriteTyped(SelectFocusCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Focus);
        }

        protected override SelectFocusCommand ReadTyped(CountryId issuer, CanonicalReader r) => new SelectFocusCommand(issuer, new FocusId(r.ReadUInt64()));
    }
}
