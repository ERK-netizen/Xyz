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
    /// <summary>Limandaki birliği aynı limandaki nakliye filosuna bindirir (kapasite > 0). Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoEmbarkHandler : GeoMilitaryHandler<GeoEmbarkCommand>
    {
        public GeoEmbarkHandler(GameContent content) : base(content) { }

        public override string Kind => GeoEmbarkCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, GeoEmbarkCommand c) =>
            GeoAmphibiousRules.CheckEmbark(War(state), c.Issuer, c.Unit, c.Fleet);

        protected override void ApplyTyped(CommandContext context, GeoEmbarkCommand c)
        {
            if (GeoAmphibiousRules.CheckEmbark(War(context.State), c.Issuer, c.Unit, c.Fleet).IsValid)
            {
                GeoAmphibiousRules.Embark(context.State.Warfare.Units[c.Unit], context.State.Warfare.Fleets[c.Fleet]);
            }
        }

        protected override void WriteTyped(GeoEmbarkCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
            w.WriteId(c.Fleet);
        }

        protected override GeoEmbarkCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoEmbarkCommand(issuer, new LandUnitId(r.ReadUInt64()), new FleetId(r.ReadUInt64()));
    }
}
