using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Simulation.Phases;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// P3 diplomasi: süresi dolan teklif ve antlaşmalar; kapitülasyon ve sürgün. Bir teslim
    /// olduysa birlikler <see cref="PeaceSettlement"/> ile yerleştirilir.
    /// </summary>
    public sealed class DiplomacySystem : ISimulationSystem
    {
        private readonly GameContent _content;

        public DiplomacySystem(GameContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => "diplomasi";

        public SimulationPhase Phase => SimulationPhase.P3Diplomacy;

        public void Execute(SimulationContext context)
        {
            var notes = new List<string>();
            var capitulated = new List<CountryId>();
            DiplomacyRules.ExpireDay(context.State.Diplomacy, context.Today);
            CapitulationRules.ApplyDay(context.State.Diplomacy, context.World, _content.Diplomacy, context.Today, capitulated, notes);
            if (capitulated.Count > 0)
            {
                PeaceSettlement.Relocate(WarfareContexts.From(context.State, _content), context.State.Diplomacy, notes);
            }

            for (int i = 0; i < notes.Count; i++)
            {
                context.Note(notes[i]);
            }
        }
    }
}
