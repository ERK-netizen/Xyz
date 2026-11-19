using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Commands.Diplomacy;
using TarihStrateji.Core.Simulation.Commands.Economy;
using TarihStrateji.Core.Simulation.Commands.Military;
using TarihStrateji.Core.Simulation.Commands.Statecraft;
using TarihStrateji.Core.Simulation.Systems;

namespace TarihStrateji.Core.Simulation
{
    /// <summary>
    /// Standart kuralları (sistemler, emir işleyicileri, değişmezler) içerikle birlikte kurulumuna
    /// ekler. Bootstrap ve testler aynı bileşimi kullanır; sıra determinizmin parçasıdır.
    /// </summary>
    public static class StandardRules
    {
        public static SimulationSetup Install(SimulationSetup setup, GameContent content)
        {
            if (setup == null || content == null)
            {
                throw new ArgumentNullException(setup == null ? nameof(setup) : nameof(content));
            }

            setup.AddSystem(new EconomyDayOpenSystem());
            setup.AddSystem(new ActionPointSystem(content));
            setup.AddSystem(new NarrativeSystem(content));
            setup.AddSystem(new DiplomacySystem(content));
            setup.AddSystem(new CharacterServiceSystem(content.Characters));
            setup.AddSystem(new PoliticsSystem(content.Politics));
            setup.AddSystem(new ResearchSystem(content.Technology));
            setup.AddSystem(new DemographySystem(content.Demography));
            setup.AddSystem(new WorkforceSystem(content));
            setup.AddSystem(new ProductionSystem(content.Economy));
            setup.AddSystem(new ConstructionSystem(content.Economy));
            setup.AddSystem(new TradeAndBudgetSystem(content));
            setup.AddSystem(new SupplySystem(content));
            setup.AddSystem(new LandMovementSystem(content));
            setup.AddSystem(new NavalMovementSystem(content));
            setup.AddSystem(new AirCombatSystem(content));
            setup.AddSystem(new NavalCombatSystem(content));
            setup.AddSystem(new LandCombatSystem(content));
            setup.AddSystem(new LandControlSystem(content));
            setup.AddSystem(new IntelligenceSystem(content));

            setup.AddInvariant(new PopulationInvariantCheck(content.Demography));
            setup.AddInvariant(new EconomyInvariantCheck(content.Economy));
            setup.AddInvariant(new StatecraftInvariantCheck(content));
            setup.AddInvariant(new WarfareInvariantCheck(content));
            setup.AddInvariant(new DiplomacyInvariantCheck(content));

            setup.Commands.Register(new BuildFacilityHandler(content.Economy));
            setup.Commands.Register(new CancelConstructionHandler(content.Economy));
            setup.Commands.Register(new SetTaxRateHandler(content.Economy));
            setup.Commands.Register(new PlaceTradeOrderHandler(content.Economy));
            setup.Commands.Register(new CancelTradeOrderHandler());
            setup.Commands.Register(new SetResearchHandler(content.Technology));
            setup.Commands.Register(new EnqueueResearchHandler(content.Technology));
            setup.Commands.Register(new AppointCharacterHandler(content.Characters));
            setup.Commands.Register(new DismissCharacterHandler());
            setup.Commands.Register(new InvestInInstitutionHandler(content.Politics));
            setup.Commands.Register(new RecruitUnitHandler(content));
            setup.Commands.Register(new DisbandUnitHandler(content));
            setup.Commands.Register(new MoveUnitHandler(content));
            setup.Commands.Register(new OrderAttackHandler(content));
            setup.Commands.Register(new ImmediateAttackHandler(content));
            setup.Commands.Register(new HoldPositionHandler(content));
            setup.Commands.Register(new BuildFleetHandler(content));
            setup.Commands.Register(new MoveFleetHandler(content));
            setup.Commands.Register(new DisbandFleetHandler(content));
            setup.Commands.Register(new EmbarkUnitHandler(content));
            setup.Commands.Register(new NavalLandingHandler(content));
            setup.Commands.Register(new RecruitAirWingHandler(content));
            setup.Commands.Register(new RebaseAirWingHandler(content));
            setup.Commands.Register(new AssignAirMissionHandler(content));
            setup.Commands.Register(new DisbandAirWingHandler(content));
            setup.Commands.Register(new DeclareWarHandler(content));
            setup.Commands.Register(new ProposeTreatyHandler(content));
            setup.Commands.Register(new RespondProposalHandler(content));
            setup.Commands.Register(new ChooseEventOptionHandler(content));
            setup.Commands.Register(new SelectFocusHandler(content));
            return setup;
        }

        /// <summary>
        /// Katmanlı YZ'yi (P15) kurar: oyuncu dışındaki ülkeler karar verir. Oyun ve uzun başsız
        /// testler için <see cref="Install"/>'dan sonra çağrılır; mekanik testleri YZ'siz kalabilir.
        /// </summary>
        public static SimulationSetup InstallArtificialIntelligence(SimulationSetup setup, GameContent content)
        {
            if (setup == null || content == null)
            {
                throw new ArgumentNullException(setup == null ? nameof(setup) : nameof(content));
            }

            setup.AddSystem(new AiSystem(content));
            return setup;
        }
    }
}
