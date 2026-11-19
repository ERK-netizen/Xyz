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
    public sealed class OrderAttackHandler : MilitaryHandler<OrderAttackCommand>
    {
        public OrderAttackHandler(GameContent content) : base(content) { }

        public override string Kind => OrderAttackCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, OrderAttackCommand c) => CombatRules.CheckAttack(War(state), c.Issuer, c.Unit, c.Target);

        protected override void ApplyTyped(CommandContext context, OrderAttackCommand c)
        {
            SetAttack(context, c);
        }

        protected override void WriteTyped(OrderAttackCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit); w.WriteInt32(c.Target.Column); w.WriteInt32(c.Target.Row);
        }

        protected override OrderAttackCommand ReadTyped(CountryId issuer, CanonicalReader r) => new OrderAttackCommand(issuer, new LandUnitId(r.ReadUInt64()), new HexCoord(r.ReadInt32(), r.ReadInt32()));

        private static void SetAttack(CommandContext context, OrderAttackCommand c)
        {
            MovementRules.SetAttackTarget(context.State.Warfare.Units[c.Unit], context.State.World.Grid.ToId(c.Target));
        }
    }
}
