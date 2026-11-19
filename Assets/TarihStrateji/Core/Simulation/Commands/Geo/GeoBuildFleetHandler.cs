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
    /// <summary>Ana limanda filo kurar (gemi envanterden, mürettebat nüfustan). Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoBuildFleetHandler : GeoMilitaryHandler<GeoBuildFleetCommand>
    {
        public GeoBuildFleetHandler(GameContent content) : base(content) { }

        public override string Kind => GeoBuildFleetCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, GeoBuildFleetCommand c) =>
            GeoNavalRules.CheckBuild(War(state), c.Issuer, c.ShipType, c.Port);

        protected override void ApplyTyped(CommandContext context, GeoBuildFleetCommand c)
        {
            GeoNavalRules.Build(War(context.State), c.Issuer, c.ShipType, c.Port);
        }

        protected override void WriteTyped(GeoBuildFleetCommand c, CanonicalWriter w)
        {
            w.WriteId(c.ShipType);
            w.WriteId(c.Port);
        }

        protected override GeoBuildFleetCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoBuildFleetCommand(issuer, new UnitTypeId(r.ReadUInt64()), new CityId(r.ReadUInt64()));
    }
}
