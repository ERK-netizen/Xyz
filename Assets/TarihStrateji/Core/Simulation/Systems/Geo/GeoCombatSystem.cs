using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Geo;

namespace TarihStrateji.Core.Simulation.Systems.Geo
{
    /// <summary>
    /// P13: deniz muharebeleri → çıkarmalar → kara muharebeleri. Bugün anında çözülmüş saldırılar
    /// (<c>ResolutionLedger</c>) atlanır (REG-016).
    /// </summary>
    public sealed class GeoCombatSystem : GeoMilitarySystem
    {
        public GeoCombatSystem(GameContent content, GeoSupplyCache supply) : base(content, supply) { }

        public override string Name => GeoMilitaryDaySystems.Combat;

        public override SimulationPhase Phase => SimulationPhase.P13Combat;

        protected override void Run(SimulationContext context, GeoWarfareContext war)
        {
            ulong seed = context.State.WorldSeed;
            GeoNavalRules.ResolveBattles(war, seed, Notes);
            GeoAmphibiousRules.ResolveLandings(war, seed, Notes);
            var resolved = new HashSet<ResolutionId>(context.Ledger.Resolved);
            GeoCombatRules.ResolveDay(war, seed, resolved, Notes);
        }
    }
}
