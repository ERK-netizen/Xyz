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
    /// <summary>Kara birliğini hedefe (şehir, bölge, nokta, dost birlik) yürütür; rota ulaşım grafında A*. Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoMoveUnitHandler : GeoMilitaryHandler<GeoMoveUnitCommand>
    {
        public GeoMoveUnitHandler(GameContent content) : base(content) { }

        public override string Kind => GeoMoveUnitCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, GeoMoveUnitCommand c) =>
            GeoMovementRules.CheckMove(War(state), Finder(state), c.Issuer, c.Unit, c.Target, new List<TransportEdgeId>());

        protected override void ApplyTyped(CommandContext context, GeoMoveUnitCommand c)
        {
            var route = new List<TransportEdgeId>();
            if (GeoMovementRules.CheckMove(War(context.State), Finder(context.State), c.Issuer, c.Unit, c.Target, route).IsValid)
            {
                GeoMovementRules.SetRoute(context.State.Warfare.Units[c.Unit], route);
            }
        }

        protected override void WriteTyped(GeoMoveUnitCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
            WriteTarget(w, c.Target);
        }

        protected override GeoMoveUnitCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoMoveUnitCommand(issuer, new LandUnitId(r.ReadUInt64()), ReadTarget(r));
    }
}
