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
    /// <summary>P12: kara hareketi.</summary>
    public sealed class LandMovementSystem : MilitarySystem
    {
        private readonly LandMovementCostModel _movement;

        public LandMovementSystem(GameContent content) : base(content)
        {
            _movement = new LandMovementCostModel(content.Movement);
        }

        public override string Name => "savas.hareket";

        public override SimulationPhase Phase => SimulationPhase.P12Movement;

        protected override void Run(SimulationContext context, WarfareContext war) => MovementRules.AdvanceDay(war, _movement, Notes);
    }
}
