using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Commands.Economy;
using TarihStrateji.Core.Simulation.Commands.Statecraft;
using TarihStrateji.Core.Simulation.Scenario;
using TarihStrateji.Core.Simulation.Systems;

namespace TarihStrateji.Core.Simulation
{
    /// <summary>
    /// Standart kuralları (sistemler, emir işleyicileri, değişmezler) içerikle birlikte kurulumuna
    /// ekler. Bootstrap ve testler aynı bileşimi kullanır; sıra determinizmin parçasıdır.
    /// </summary>
    public static class StandardRules
    {
        public static SimulationSetup Install(SimulationSetup setup, StandardContent standard)
        {
            if (setup == null || standard == null)
            {
                throw new ArgumentNullException(setup == null ? nameof(setup) : nameof(standard));
            }

            GameContent content = standard.Core;
            setup.AddSystem(new EconomyDayOpenSystem());
            setup.AddSystem(new CharacterServiceSystem(standard.Characters));
            setup.AddSystem(new PoliticsSystem(standard.Politics));
            setup.AddSystem(new ResearchSystem(content.Technology));
            setup.AddSystem(new DemographySystem(content.Demography));
            setup.AddSystem(new WorkforceSystem(content));
            setup.AddSystem(new ProductionSystem(content.Economy));
            setup.AddSystem(new ConstructionSystem(content.Economy));
            setup.AddSystem(new TradeAndBudgetSystem(content));

            setup.AddInvariant(new PopulationInvariantCheck(content.Demography));
            setup.AddInvariant(new EconomyInvariantCheck(content.Economy));
            setup.AddInvariant(new StatecraftInvariantCheck(standard));

            setup.Commands.Register(new BuildFacilityHandler(content.Economy));
            setup.Commands.Register(new CancelConstructionHandler(content.Economy));
            setup.Commands.Register(new SetTaxRateHandler(content.Economy));
            setup.Commands.Register(new PlaceTradeOrderHandler(content.Economy));
            setup.Commands.Register(new CancelTradeOrderHandler());
            setup.Commands.Register(new SetResearchHandler(content.Technology));
            setup.Commands.Register(new EnqueueResearchHandler(content.Technology));
            setup.Commands.Register(new AppointCharacterHandler(standard.Characters));
            setup.Commands.Register(new DismissCharacterHandler());
            setup.Commands.Register(new InvestInInstitutionHandler(standard.Politics));
            return setup;
        }
    }
}
