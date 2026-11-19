using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary><see cref="RecruitAirWingCommand"/> işleyicisi; kural mantığı Warfare modülündedir.</summary>
    public sealed class RecruitAirWingHandler : MilitaryHandler<RecruitAirWingCommand>
    {
        public RecruitAirWingHandler(GameContent content) : base(content) { }

        public override string Kind => RecruitAirWingCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, RecruitAirWingCommand c) => AirRules.CheckRecruit(War(state), c.Issuer, c.WingType, c.At);

        protected override void ApplyTyped(CommandContext context, RecruitAirWingCommand c)
        {
            AirRules.Recruit(War(context.State), c.Issuer, c.WingType, c.At);
        }

        protected override void WriteTyped(RecruitAirWingCommand c, CanonicalWriter w)
        {
            w.WriteId(c.WingType);
            w.WriteInt32(c.At.Column);
            w.WriteInt32(c.At.Row);
        }

        protected override RecruitAirWingCommand ReadTyped(CountryId issuer, CanonicalReader r) => new RecruitAirWingCommand(issuer, new UnitTypeId(r.ReadUInt64()), new HexCoord(r.ReadInt32(), r.ReadInt32()));
    }
}
