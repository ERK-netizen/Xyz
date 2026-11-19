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
    /// <summary>Hava kanadına görev verir; hedef üsten menzil içinde olmalı. Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoAirMissionHandler : GeoMilitaryHandler<GeoAirMissionCommand>
    {
        public GeoAirMissionHandler(GameContent content) : base(content) { }

        public override string Kind => GeoAirMissionCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, GeoAirMissionCommand c) =>
            GeoAirRules.CheckMission(War(state), c.Issuer, c.Wing, c.Mission, c.Target);

        protected override void ApplyTyped(CommandContext context, GeoAirMissionCommand c)
        {
            if (GeoAirRules.CheckMission(War(context.State), c.Issuer, c.Wing, c.Mission, c.Target).IsValid)
            {
                GeoAirRules.SetMission(context.State.Warfare.AirWings[c.Wing], c.Mission, c.Target);
            }
        }

        protected override void WriteTyped(GeoAirMissionCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Wing);
            w.WriteByte((byte)c.Mission);
            WriteTarget(w, c.Target);
        }

        protected override GeoAirMissionCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoAirMissionCommand(issuer, new AirWingId(r.ReadUInt64()), (AirMission)r.ReadByte(), ReadTarget(r));
    }
}
