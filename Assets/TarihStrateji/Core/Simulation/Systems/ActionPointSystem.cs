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
    /// <summary>P0: aksiyon puanı yenileme.</summary>
    public sealed class ActionPointSystem : MilitarySystem
    {
        public ActionPointSystem(GameContent content) : base(content) { }

        public override string Name => "savas.aksiyonPuani";

        public override SimulationPhase Phase => SimulationPhase.P0DayOpen;

        protected override void Run(SimulationContext context, WarfareContext war) => UnitRules.RefreshActionPoints(war);
    }
}
