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
    /// <summary>Kara harbinin günlük sistemleri (P0, P11, P12, P13, P14) için ortak taban.</summary>
    public abstract class MilitarySystem : ISimulationSystem
    {
        protected MilitarySystem(GameContent content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public abstract string Name { get; }

        public abstract SimulationPhase Phase { get; }

        protected GameContent Content { get; }

        protected List<string> Notes { get; } = new List<string>();

        public void Execute(SimulationContext context)
        {
            Notes.Clear();
            Run(context, WarfareContexts.From(context.State, Content));
            for (int i = 0; i < Notes.Count; i++)
            {
                context.Note(Notes[i]);
            }
        }

        protected abstract void Run(SimulationContext context, WarfareContext war);
    }
}
