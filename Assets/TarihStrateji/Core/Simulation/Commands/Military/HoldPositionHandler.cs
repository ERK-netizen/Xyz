using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Commands.Geo;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    public sealed class HoldPositionHandler : GeoMilitaryHandler<HoldPositionCommand>
    {
        public HoldPositionHandler(GameContent content) : base(content) { }

        public override string Kind => HoldPositionCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, HoldPositionCommand c) => GeoUnitRules.CheckOwned(War(state), c.Issuer, c.Unit, out _);

        protected override void ApplyTyped(CommandContext context, HoldPositionCommand c)
        {
            GeoMovementRules.Stop(context.State.Warfare.Units[c.Unit]);
        }

        protected override void WriteTyped(HoldPositionCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
        }

        protected override HoldPositionCommand ReadTyped(CountryId issuer, CanonicalReader r) => new HoldPositionCommand(issuer, new LandUnitId(r.ReadUInt64()));
    }
}
