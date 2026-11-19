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
    /// <summary>Gün içi taktik saldırı: AP harcar, hemen çözülür, P13'te tekrar çözülmez. Doğrulama emir anında ve P1 uygulamasında aynı kurallarla yapılır.</summary>
    public sealed class GeoImmediateAttackHandler : GeoMilitaryHandler<GeoImmediateAttackCommand>
    {
        public GeoImmediateAttackHandler(GameContent content) : base(content) { }

        public override string Kind => GeoImmediateAttackCommand.KindKey;

        public override bool ResolvesImmediately => true;

        protected override ValidationResult ValidateGeo(GameState state, GeoImmediateAttackCommand c) =>
            GeoCombatRules.CheckImmediateAttack(War(state), c.Issuer, c.Unit, c.Target);

        protected override void ApplyTyped(CommandContext context, GeoImmediateAttackCommand c)
        {
            GeoWarfareContext war = War(context.State);
            GeoCombatRules.ResolveImmediate(war, war.Warfare.Units[c.Unit], c.Target, context.Resolution, ref context.Rng, new List<string>());
        }

        protected override void WriteTyped(GeoImmediateAttackCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
            WriteTarget(w, c.Target);
        }

        protected override GeoImmediateAttackCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new GeoImmediateAttackCommand(issuer, new LandUnitId(r.ReadUInt64()), ReadTarget(r));
    }
}
