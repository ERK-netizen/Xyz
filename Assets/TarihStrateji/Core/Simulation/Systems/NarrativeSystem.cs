using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// P2 olay ve odaklar: süresi dolan olaylarda ilk seçenek, yeni tetiklemeler (ekonomisi olan
    /// ülkeler, kimlik sırasıyla), odak ilerlemesi. Sonuçların etkileri ve hazine değişimi uygulanır.
    /// </summary>
    public sealed class NarrativeSystem : ISimulationSystem
    {
        private readonly GameContent _content;

        public NarrativeSystem(GameContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => "olay.odak";

        public SimulationPhase Phase => SimulationPhase.P2EventsAndEffects;

        public void Execute(SimulationContext context)
        {
            GameState state = context.State;
            var outcomes = new List<NarrativeOutcome>();
            var notes = new List<string>();
            NarrativeRules.ExpireDay(state.Narrative, context.Today, outcomes);
            var countries = new List<CountryId>(state.Economy.Countries.Count);
            for (int i = 0; i < state.Economy.Countries.Count; i++)
            {
                countries.Add(state.Economy.Countries.KeyAt(i));
            }

            NarrativeRules.FireDay(state.Narrative, _content.Narrative, new CountryConditions(state), countries, state.WorldSeed, context.Today, notes);
            NarrativeRules.AdvanceFocusDay(state.Narrative, _content.Narrative, context.Today, outcomes, notes);
            for (int i = 0; i < outcomes.Count; i++)
            {
                Apply(state, _content, outcomes[i]);
            }

            for (int i = 0; i < notes.Count; i++)
            {
                context.Note(notes[i]);
            }
        }

        /// <summary>Sonucu uygular: etkiler (EffectEngine) ve hazine değişimi (Economy).</summary>
        public static void Apply(GameState state, GameContent content, NarrativeOutcome outcome)
        {
            NarrativeRules.ApplyGrants(state.Effects, content.Narrative, outcome, state.Today, new List<EffectId>());
            TreasuryRules.ApplyAdministrativeChange(state.Economy, outcome.Country, NarrativeRules.TreasuryChange(content.Narrative, outcome));
        }
    }
}
