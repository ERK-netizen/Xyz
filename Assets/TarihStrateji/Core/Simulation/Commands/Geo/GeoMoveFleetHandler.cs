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
    /// <summary>Filoyu deniz yolu grafında hedefe (kendi limanı, dost filo, nokta) gönderir. Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoMoveFleetHandler : GeoMilitaryHandler<GeoMoveFleetCommand>
    {
        public GeoMoveFleetHandler(GameContent content) : base(content) { }

        public override string Kind => GeoMoveFleetCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, GeoMoveFleetCommand c) =>
            GeoNavalRules.CheckMove(War(state), Finder(state), c.Issuer, c.Fleet, c.Target, new List<TransportEdgeId>());

        protected override void ApplyTyped(CommandContext context, GeoMoveFleetCommand c)
        {
            var route = new List<TransportEdgeId>();
            if (GeoNavalRules.CheckMove(War(context.State), Finder(context.State), c.Issuer, c.Fleet, c.Target, route).IsValid)
            {
                GeoNavalRules.SetRoute(context.State.Warfare.Fleets[c.Fleet], route);
            }
        }

        protected override void WriteTyped(GeoMoveFleetCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Fleet);
            WriteTarget(w, c.Target);
        }

        protected override GeoMoveFleetCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoMoveFleetCommand(issuer, new FleetId(r.ReadUInt64()), ReadTarget(r));
    }
}
