using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Kendi kontrolündeki şehirde hava kanadı kurar (şehir başına sınırlı). Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoRecruitAirWingHandler : GeoMilitaryHandler<GeoRecruitAirWingCommand>
    {
        public GeoRecruitAirWingHandler(GameContent content) : base(content) { }

        public override string Kind => GeoRecruitAirWingCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, GeoRecruitAirWingCommand c) =>
            GeoAirRules.CheckRecruit(War(state), c.Issuer, c.WingType, c.BaseCity);

        protected override void ApplyTyped(CommandContext context, GeoRecruitAirWingCommand c)
        {
            GeoAirRules.Recruit(War(context.State), c.Issuer, c.WingType, c.BaseCity);
        }

        protected override void WriteTyped(GeoRecruitAirWingCommand c, CanonicalWriter w)
        {
            w.WriteId(c.WingType);
            w.WriteId(c.BaseCity);
        }

        protected override GeoRecruitAirWingCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoRecruitAirWingCommand(issuer, new UnitTypeId(r.ReadUInt64()), new CityId(r.ReadUInt64()));
    }
}
