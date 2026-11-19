using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    public sealed class RecruitUnitHandler : MilitaryHandler<RecruitUnitCommand>
    {
        public RecruitUnitHandler(GameContent content) : base(content) { }

        public override string Kind => RecruitUnitCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, RecruitUnitCommand c) => UnitRules.CheckRecruit(War(state), c.Issuer, c.UnitType, c.At);

        protected override void ApplyTyped(CommandContext context, RecruitUnitCommand c)
        {
            UnitRules.Recruit(War(context.State), c.Issuer, c.UnitType, c.At);
        }

        protected override void WriteTyped(RecruitUnitCommand c, CanonicalWriter w)
        {
            w.WriteId(c.UnitType); w.WriteInt32(c.At.Column); w.WriteInt32(c.At.Row);
        }

        protected override RecruitUnitCommand ReadTyped(CountryId issuer, CanonicalReader r) => new RecruitUnitCommand(issuer, new UnitTypeId(r.ReadUInt64()), new HexCoord(r.ReadInt32(), r.ReadInt32()));
    }
}
