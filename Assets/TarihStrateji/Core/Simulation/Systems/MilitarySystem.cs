using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// Askerî sistemlerin ortak tabanı: her çalıştırmada savaş bağlamını kurar ve sistemin
    /// notlarını gün raporuna aktarır. Kural mantığı Warfare modülündedir.
    /// </summary>
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
