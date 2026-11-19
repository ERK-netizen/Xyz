using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    public sealed class DisbandUnitHandler : MilitaryHandler<DisbandUnitCommand>
    {
        public DisbandUnitHandler(GameContent content) : base(content) { }

        public override string Kind => DisbandUnitCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, DisbandUnitCommand c) => UnitRules.CheckDisband(War(state), c.Issuer, c.Unit);

        protected override void ApplyTyped(CommandContext context, DisbandUnitCommand c)
        {
            UnitRules.Disband(War(context.State), c.Issuer, c.Unit);
        }

        protected override void WriteTyped(DisbandUnitCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
        }

        protected override DisbandUnitCommand ReadTyped(CountryId issuer, CanonicalReader r) => new DisbandUnitCommand(issuer, new LandUnitId(r.ReadUInt64()));
    }
}
