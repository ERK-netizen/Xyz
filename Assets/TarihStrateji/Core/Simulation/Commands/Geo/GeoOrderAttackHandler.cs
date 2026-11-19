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
    /// <summary>Günlük saldırı emri: düşman birliği veya şehri (P13'te çözülür). Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoOrderAttackHandler : GeoMilitaryHandler<GeoOrderAttackCommand>
    {
        public GeoOrderAttackHandler(GameContent content) : base(content) { }

        public override string Kind => GeoOrderAttackCommand.KindKey;

        protected override ValidationResult ValidateGeo(GameState state, GeoOrderAttackCommand c) =>
            GeoCombatRules.CheckAttack(War(state), c.Issuer, c.Unit, c.Target, out _);

        protected override void ApplyTyped(CommandContext context, GeoOrderAttackCommand c)
        {
            if (GeoCombatRules.CheckAttack(War(context.State), c.Issuer, c.Unit, c.Target, out _).IsValid)
            {
                GeoCombatRules.SetAttack(context.State.Warfare.Units[c.Unit], c.Target);
            }
        }

        protected override void WriteTyped(GeoOrderAttackCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
            WriteTarget(w, c.Target);
        }

        protected override GeoOrderAttackCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoOrderAttackCommand(issuer, new LandUnitId(r.ReadUInt64()), ReadTarget(r));
    }
}
