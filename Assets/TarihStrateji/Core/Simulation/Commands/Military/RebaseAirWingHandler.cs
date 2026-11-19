using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary><see cref="RebaseAirWingCommand"/> işleyicisi; kural mantığı Warfare modülündedir.</summary>
    public sealed class RebaseAirWingHandler : MilitaryHandler<RebaseAirWingCommand>
    {
        public RebaseAirWingHandler(GameContent content) : base(content) { }

        public override string Kind => RebaseAirWingCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, RebaseAirWingCommand c) => AirRules.CheckRebase(War(state), c.Issuer, c.Wing, c.At);

        protected override void ApplyTyped(CommandContext context, RebaseAirWingCommand c)
        {
            AirRules.Rebase(context.State.Warfare.AirWings[c.Wing], context.State.World.Cell(c.At).City);
        }

        protected override void WriteTyped(RebaseAirWingCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Wing);
            w.WriteInt32(c.At.Column);
            w.WriteInt32(c.At.Row);
        }

        protected override RebaseAirWingCommand ReadTyped(CountryId issuer, CanonicalReader r) => new RebaseAirWingCommand(issuer, new AirWingId(r.ReadUInt64()), new HexCoord(r.ReadInt32(), r.ReadInt32()));
    }
}
