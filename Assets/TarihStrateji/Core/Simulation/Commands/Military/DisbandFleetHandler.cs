using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary><see cref="DisbandFleetCommand"/> işleyicisi; kural mantığı Warfare modülündedir.</summary>
    public sealed class DisbandFleetHandler : MilitaryHandler<DisbandFleetCommand>
    {
        public DisbandFleetHandler(GameContent content) : base(content) { }

        public override string Kind => DisbandFleetCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, DisbandFleetCommand c) => FleetRules.CheckDisband(War(state), c.Issuer, c.Fleet);

        protected override void ApplyTyped(CommandContext context, DisbandFleetCommand c)
        {
            FleetRules.Disband(War(context.State), c.Issuer, c.Fleet);
        }

        protected override void WriteTyped(DisbandFleetCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Fleet);
        }

        protected override DisbandFleetCommand ReadTyped(CountryId issuer, CanonicalReader r) => new DisbandFleetCommand(issuer, new FleetId(r.ReadUInt64()));
    }
}
