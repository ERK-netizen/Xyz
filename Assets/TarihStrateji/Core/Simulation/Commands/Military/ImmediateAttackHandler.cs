using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>
    /// Gün içi taktik saldırı (CLAUDE.md §2.5): AP harcar, <c>ResolutionId</c> alır ve hemen çözülür;
    /// birlik o gün P13'te ikinci kez saldırmaz.
    /// </summary>
    public sealed class ImmediateAttackHandler : MilitaryHandler<ImmediateAttackCommand>
    {
        public ImmediateAttackHandler(GameContent content) : base(content) { }

        public override string Kind => ImmediateAttackCommand.KindKey;

        public override bool ResolvesImmediately => true;

        protected override ValidationResult ValidateTyped(GameState state, ImmediateAttackCommand c) =>
            CombatRules.CheckImmediateAttack(War(state), c.Issuer, c.Unit, c.Target);

        protected override void ApplyTyped(CommandContext context, ImmediateAttackCommand c)
        {
            WarfareContext war = War(context.State);
            LandUnit unit = war.Warfare.Units[c.Unit];
            CombatRules.ResolveImmediate(war, unit, c.Target, context.Resolution, ref context.Rng, new List<string>());
        }

        protected override void WriteTyped(ImmediateAttackCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
            w.WriteInt32(c.Target.Column);
            w.WriteInt32(c.Target.Row);
        }

        protected override ImmediateAttackCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new ImmediateAttackCommand(issuer, new LandUnitId(r.ReadUInt64()), new HexCoord(r.ReadInt32(), r.ReadInt32()));
    }
}
