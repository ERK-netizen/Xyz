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
    /// <summary>Gemideki birliğe liman şehrine çıkarma emri (P13'te çözülür). Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoLandingHandler : GeoMilitaryHandler<GeoLandingCommand>
    {
        public GeoLandingHandler(GameContent content) : base(content) { }

        public override string Kind => GeoLandingCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, GeoLandingCommand c) =>
            GeoAmphibiousRules.CheckLanding(War(state), c.Issuer, c.Unit, c.Target);

        protected override void ApplyTyped(CommandContext context, GeoLandingCommand c)
        {
            if (GeoAmphibiousRules.CheckLanding(War(context.State), c.Issuer, c.Unit, c.Target).IsValid)
            {
                GeoAmphibiousRules.SetLanding(context.State.Warfare.Units[c.Unit], c.Target);
            }
        }

        protected override void WriteTyped(GeoLandingCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
            w.WriteId(c.Target);
        }

        protected override GeoLandingCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoLandingCommand(issuer, new LandUnitId(r.ReadUInt64()), new CityId(r.ReadUInt64()));
    }
}
