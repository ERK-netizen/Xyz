using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P13 (ilk): hava üstünlüğü muharebeleri; deniz ve kara desteği kalan güçlerle ölçeklenir.</summary>
    public sealed class AirCombatSystem : MilitarySystem
    {
        public AirCombatSystem(GameContent content) : base(content) { }

        public override string Name => "hava.muharebe";

        public override SimulationPhase Phase => SimulationPhase.P13Combat;

        protected override void Run(SimulationContext context, WarfareContext war)
        {
            var hexes = new List<HexId>();
            AirCombatRules.ContestedHexes(war, hexes);
            for (int i = 0; i < hexes.Count; i++)
            {
                RngStream rng = context.Rng(RngSubsystem.AirCombat, hexes[i].Value, 0UL);
                AirCombatRules.ResolveBattle(war, hexes[i], ref rng, Notes);
            }
        }
    }
}
