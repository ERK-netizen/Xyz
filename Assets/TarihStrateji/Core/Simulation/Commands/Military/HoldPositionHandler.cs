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
    public sealed class HoldPositionHandler : MilitaryHandler<HoldPositionCommand>
    {
        public HoldPositionHandler(GameContent content) : base(content) { }

        public override string Kind => HoldPositionCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, HoldPositionCommand c) => UnitRules.CheckOwned(War(state), c.Issuer, c.Unit, out _);

        protected override void ApplyTyped(CommandContext context, HoldPositionCommand c)
        {
            MovementRules.ClearOrders(context.State.Warfare.Units[c.Unit]);
        }

        protected override void WriteTyped(HoldPositionCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
        }

        protected override HoldPositionCommand ReadTyped(CountryId issuer, CanonicalReader r) => new HoldPositionCommand(issuer, new LandUnitId(r.ReadUInt64()));
    }
}
