using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary><see cref="EmbarkUnitCommand"/> işleyicisi; kural mantığı Warfare modülündedir.</summary>
    public sealed class EmbarkUnitHandler : MilitaryHandler<EmbarkUnitCommand>
    {
        public EmbarkUnitHandler(GameContent content) : base(content) { }

        public override string Kind => EmbarkUnitCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, EmbarkUnitCommand c) => AmphibiousRules.CheckEmbark(War(state), c.Issuer, c.Unit, c.Fleet);

        protected override void ApplyTyped(CommandContext context, EmbarkUnitCommand c)
        {
            AmphibiousRules.Embark(War(context.State), context.State.Warfare.Units[c.Unit], context.State.Warfare.Fleets[c.Fleet]);
        }

        protected override void WriteTyped(EmbarkUnitCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
            w.WriteId(c.Fleet);
        }

        protected override EmbarkUnitCommand ReadTyped(CountryId issuer, CanonicalReader r) => new EmbarkUnitCommand(issuer, new LandUnitId(r.ReadUInt64()), new FleetId(r.ReadUInt64()));
    }
}
