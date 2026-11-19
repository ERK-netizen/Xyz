using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary><see cref="DeclareWarCommand"/>: savunanın müttefikleri otomatik katılır.</summary>
    public sealed class DeclareWarHandler : ContentHandler<DeclareWarCommand>
    {
        public DeclareWarHandler(GameContent content) : base(content) { }

        public override string Kind => DeclareWarCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, DeclareWarCommand c) =>
            DiplomacyRules.CheckDeclareWar(state.Diplomacy, state.World, c.Issuer, c.Target, c.Goals, state.Today);

        protected override void ApplyTyped(CommandContext context, DeclareWarCommand c)
        {
            DiplomacyRules.DeclareWar(context.State.Diplomacy, c.Issuer, c.Target, c.Goals, context.State.Today);
        }

        protected override void WriteTyped(DeclareWarCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Target);
            w.WriteInt32(c.Goals.Count);
            for (int i = 0; i < c.Goals.Count; i++)
            {
                w.WriteId(c.Goals[i]);
            }
        }

        protected override DeclareWarCommand ReadTyped(CountryId issuer, CanonicalReader r)
        {
            var target = new CountryId(r.ReadUInt64());
            var goals = new RegionId[r.ReadCount(r.Remaining)];
            for (int i = 0; i < goals.Length; i++)
            {
                goals[i] = new RegionId(r.ReadUInt64());
            }

            return new DeclareWarCommand(issuer, target, goals);
        }
    }
}
