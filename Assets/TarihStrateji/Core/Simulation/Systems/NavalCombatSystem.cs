using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P13 (ikinci): çatışmalı deniz hücrelerinde filo muharebeleri.</summary>
    public sealed class NavalCombatSystem : MilitarySystem
    {
        public NavalCombatSystem(GameContent content) : base(content) { }

        public override string Name => "deniz.muharebe";

        public override SimulationPhase Phase => SimulationPhase.P13Combat;

        protected override void Run(SimulationContext context, WarfareContext war)
        {
            var hexes = new List<HexId>();
            NavalCombatRules.ContestedHexes(war, hexes);
            for (int i = 0; i < hexes.Count; i++)
            {
                RngStream rng = context.Rng(RngSubsystem.NavalCombat, hexes[i].Value, 0UL);
                NavalCombatRules.ResolveBattle(war, hexes[i], ref rng, Notes);
            }
        }
    }
}
