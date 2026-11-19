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
    /// <summary>Kendi (sahip + kontrol, kuşatılmamış) şehrinde kara birliği toplar. Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoRecruitUnitHandler : GeoMilitaryHandler<GeoRecruitUnitCommand>
    {
        public GeoRecruitUnitHandler(GameContent content) : base(content) { }

        public override string Kind => GeoRecruitUnitCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, GeoRecruitUnitCommand c) =>
            GeoUnitRules.CheckRecruit(War(state), c.Issuer, c.UnitType, c.City);

        protected override void ApplyTyped(CommandContext context, GeoRecruitUnitCommand c)
        {
            GeoUnitRules.Recruit(War(context.State), c.Issuer, c.UnitType, c.City);
        }

        protected override void WriteTyped(GeoRecruitUnitCommand c, CanonicalWriter w)
        {
            w.WriteId(c.UnitType);
            w.WriteId(c.City);
        }

        protected override GeoRecruitUnitCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoRecruitUnitCommand(issuer, new UnitTypeId(r.ReadUInt64()), new CityId(r.ReadUInt64()));
    }
}
