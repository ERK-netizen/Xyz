using System;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// P6: kohort akışı. Ölüm çarpanı = 1 + (bölgenin fiili kontrolcüsünün dünkü gıda açığı) ×
    /// kıtlık katsayısı.
    /// </summary>
    public sealed class DemographySystem : ISimulationSystem
    {
        private readonly DemographyParameters _parameters;

        public DemographySystem(DemographyParameters parameters)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public string Name => "nufus.demografi";

        public SimulationPhase Phase => SimulationPhase.P6Population;

        public void Execute(SimulationContext context)
        {
            PopulationState population = context.State.Population;
            EconomyState economy = context.State.Economy;
            for (int i = 0; i < population.Regions.Count; i++)
            {
                RegionPopulation p = population.Regions.ValueAt(i);
                Fixed64 shortage = Fixed64.Zero;
                if (context.World.TryGetControl(p.Region, out RegionControl region)
                    && economy.Countries.TryGetValue(region.Controller, out CountryEconomy controller))
                {
                    shortage = controller.FoodShortage;
                }

                DemographyRules.AdvanceDay(p, _parameters, Fixed64.One + (shortage * _parameters.FamineMortalityFactor));
            }
        }
    }
}
