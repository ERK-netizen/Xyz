using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary><see cref="BuildFleetCommand"/> işleyicisi; kural mantığı Warfare modülündedir.</summary>
    public sealed class BuildFleetHandler : MilitaryHandler<BuildFleetCommand>
    {
        public BuildFleetHandler(GameContent content) : base(content) { }

        public override string Kind => BuildFleetCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, BuildFleetCommand c) => FleetRules.CheckBuild(War(state), c.Issuer, c.ShipType, c.At);

        protected override void ApplyTyped(CommandContext context, BuildFleetCommand c)
        {
            FleetRules.Build(War(context.State), c.Issuer, c.ShipType, c.At);
        }

        protected override void WriteTyped(BuildFleetCommand c, CanonicalWriter w)
        {
            w.WriteId(c.ShipType);
            w.WriteInt32(c.At.Column);
            w.WriteInt32(c.At.Row);
        }

        protected override BuildFleetCommand ReadTyped(CountryId issuer, CanonicalReader r) => new BuildFleetCommand(issuer, new UnitTypeId(r.ReadUInt64()), new HexCoord(r.ReadInt32(), r.ReadInt32()));
    }
}
