using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P14: işgal ve kuşatma durumu.</summary>
    public sealed class LandControlSystem : MilitarySystem
    {
        public LandControlSystem(GameContent content) : base(content) { }

        public override string Name => "savas.kontrol";

        public override SimulationPhase Phase => SimulationPhase.P14ControlAndIntelligence;

        protected override void Run(SimulationContext context, WarfareContext war)
        {
            WarfareControlRules.Occupy(war);
            WarfareControlRules.UpdateSieges(war);
        }
    }
}
