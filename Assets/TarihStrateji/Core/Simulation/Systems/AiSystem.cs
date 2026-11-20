using System;
using System.Collections.Generic;
using TarihStrateji.Core.AI;
using TarihStrateji.Core.AI.Perception;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// P15 YZ kararları: oyuncu dışındaki her ülke (ekonomisi olan, kimlik sırasıyla; Gözlemci
    /// modunda hepsi) için algı kurulur, <see cref="AiDirector"/> karar verir, niyetler emre çevrilip
    /// çekirdeğe verilir; çekirdek oyuncu emirleriyle aynı doğrulamadan geçirip ertesi günün P1
    /// kuyruğuna alır.
    /// </summary>
    public sealed class AiSystem : ISimulationSystem
    {
        private readonly GameContent _content;
        private readonly Domain.Time.GameDate _earliestWar;
        private readonly List<ulong> _borders = new List<ulong>();
        private readonly List<AiIntent> _intents = new List<AiIntent>();

        public AiSystem(GameContent content)
            : this(content, Domain.Time.GameDate.MinValue)
        {
        }

        /// <param name="earliestWar">Senaryonun YZ savaş ilanı için en erken tarihi (<see cref="SimulationSetup.AiEarliestWarDate"/>).</param>
        public AiSystem(GameContent content, Domain.Time.GameDate earliestWar)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _earliestWar = earliestWar;
        }

        public string Name => "yz";

        public SimulationPhase Phase => SimulationPhase.P15AiInvariantsAndSave;

        public void Execute(SimulationContext context)
        {
            GameState state = context.State;
            BorderIndex.Compute(state.World, _borders);
            for (int c = 0; c < state.Economy.Countries.Count; c++)
            {
                CountryId country = state.Economy.Countries.KeyAt(c);
                if (country == state.PlayerCountry)
                {
                    continue;
                }

                AiPerception perception = AiPerceptionBuilder.Build(state, _content, country, _borders);
                perception.EarliestWar = _earliestWar;
                _intents.Clear();
                AiDirector.Decide(perception, _content.Ai, _intents);
                for (int i = 0; i < _intents.Count; i++)
                {
                    GameCommand command = AiIntentMapper.ToCommand(country, _intents[i]);
                    if (command != null)
                    {
                        context.SubmitAiOrder(command);
                    }
                }
            }
        }
    }
}
