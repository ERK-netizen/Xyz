using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Geo;

namespace TarihStrateji.Core.Simulation.Systems.Geo
{
    /// <summary>Coğrafi askerî sistemlerin tabanı: bağlamı kurar, notları gün raporuna aktarır.</summary>
    public abstract class GeoMilitarySystem : ISimulationSystem
    {
        protected GeoMilitarySystem(GameContent content, GeoSupplyCache supply)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Supply = supply ?? throw new ArgumentNullException(nameof(supply));
        }

        public abstract string Name { get; }

        public abstract SimulationPhase Phase { get; }

        protected GameContent Content { get; }

        protected GeoSupplyCache Supply { get; }

        protected List<string> Notes { get; } = new List<string>();

        public void Execute(SimulationContext context)
        {
            Notes.Clear();
            Run(context, GeoWarfareContexts.From(context.State, Content));
            for (int i = 0; i < Notes.Count; i++)
            {
                context.Note(Notes[i]);
            }
        }

        protected abstract void Run(SimulationContext context, GeoWarfareContext war);
    }
}
