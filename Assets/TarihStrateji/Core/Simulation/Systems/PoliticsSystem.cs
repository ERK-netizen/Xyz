using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Politics;
using TarihStrateji.Core.Politics.Definitions;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Simulation.Phases;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P4: istikrar, kurumlar, krizler, seçimler. Kıtlık istikrar hedefini düşürür.</summary>
    public sealed class PoliticsSystem : ISimulationSystem
    {
        private readonly PoliticsCatalog _catalog;
        private readonly List<string> _notes = new List<string>();

        public PoliticsSystem(PoliticsCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string Name => "siyaset";

        public SimulationPhase Phase => SimulationPhase.P4Politics;

        public void Execute(SimulationContext context)
        {
            PoliticsState politics = context.State.Politics;
            _notes.Clear();
            for (int i = 0; i < politics.Countries.Count; i++)
            {
                CountryPolitics p = politics.Countries.ValueAt(i);
                Fixed64 penalty = Fixed64.Zero;
                if (context.State.Economy.Countries.TryGetValue(p.Country, out CountryEconomy economy))
                {
                    penalty = economy.FoodShortage * _catalog.Parameters.FamineStabilityPenalty;
                }

                PoliticsRules.AdvanceDay(p, _catalog, context.Effects, context.Today, context.State.WorldSeed, penalty, _notes);
            }

            for (int i = 0; i < _notes.Count; i++)
            {
                context.Note(_notes[i]);
            }
        }
    }
}
