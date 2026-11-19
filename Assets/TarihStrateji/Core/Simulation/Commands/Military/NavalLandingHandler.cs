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
    /// <summary><see cref="NavalLandingCommand"/> işleyicisi; kural mantığı Warfare modülündedir.</summary>
    public sealed class NavalLandingHandler : MilitaryHandler<NavalLandingCommand>
    {
        public NavalLandingHandler(GameContent content) : base(content) { }

        public override string Kind => NavalLandingCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, NavalLandingCommand c) => AmphibiousRules.CheckLanding(War(state), c.Issuer, c.Unit, c.Target);

        protected override void ApplyTyped(CommandContext context, NavalLandingCommand c)
        {
            MovementRules.SetAttackTarget(context.State.Warfare.Units[c.Unit], context.State.World.Grid.ToId(c.Target));
        }

        protected override void WriteTyped(NavalLandingCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
            w.WriteInt32(c.Target.Column);
            w.WriteInt32(c.Target.Row);
        }

        protected override NavalLandingCommand ReadTyped(CountryId issuer, CanonicalReader r) => new NavalLandingCommand(issuer, new LandUnitId(r.ReadUInt64()), new HexCoord(r.ReadInt32(), r.ReadInt32()));
    }
}
