using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary><see cref="DisbandAirWingCommand"/> işleyicisi; kural mantığı Warfare modülündedir.</summary>
    public sealed class DisbandAirWingHandler : MilitaryHandler<DisbandAirWingCommand>
    {
        public DisbandAirWingHandler(GameContent content) : base(content) { }

        public override string Kind => DisbandAirWingCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, DisbandAirWingCommand c) => AirRules.CheckDisband(War(state), c.Issuer, c.Wing);

        protected override void ApplyTyped(CommandContext context, DisbandAirWingCommand c)
        {
            AirRules.Remove(War(context.State), context.State.Warfare.AirWings[c.Wing], true);
        }

        protected override void WriteTyped(DisbandAirWingCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Wing);
        }

        protected override DisbandAirWingCommand ReadTyped(CountryId issuer, CanonicalReader r) => new DisbandAirWingCommand(issuer, new AirWingId(r.ReadUInt64()));
    }
}
