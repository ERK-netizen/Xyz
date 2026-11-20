using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Commands.Diplomacy;
using TarihStrateji.Core.Simulation.Commands.Economy;
using TarihStrateji.Core.Simulation.Commands.Geo;
using TarihStrateji.Core.Simulation.Commands.Military;
using TarihStrateji.Core.Simulation.Commands.Statecraft;
using TarihStrateji.Core.Simulation.Systems;
using TarihStrateji.Core.Simulation.Systems.Geo;

namespace TarihStrateji.Core.Simulation
{
    /// <summary>
    /// Standart kuralları (sistemler, emir işleyicileri, değişmezler) içerikle birlikte kurulumuna
    /// ekler (ADR-011: tek coğrafi model). Bootstrap ve testler aynı bileşimi kullanır; sıra
    /// determinizmin parçasıdır.
    /// </summary>
    public static class StandardRules
    {
        public static SimulationSetup Install(SimulationSetup setup, GameContent content)
        {
            if (setup == null || content == null)
            {
                throw new ArgumentNullException(setup == null ? nameof(setup) : nameof(content));
            }

            setup.Content = content;
            var supply = new GeoSupplyCache();
            setup.AddSystem(new EconomyDayOpenSystem());
            setup.AddSystem(new GeoActionPointSystem(content, supply));
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
            setup.AddSystem(new GeoSupplySystem(content, supply));
            setup.AddSystem(new GeoMovementSystem(content, supply));
            setup.AddSystem(new GeoCombatSystem(content, supply));
            setup.AddSystem(new GeoControlSystem(content, supply));
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
            setup.Commands.Register(new GeoRecruitUnitHandler(content));
            setup.Commands.Register(new DisbandUnitHandler(content));
            setup.Commands.Register(new GeoMoveUnitHandler(content));
            setup.Commands.Register(new GeoOrderAttackHandler(content));
            setup.Commands.Register(new GeoImmediateAttackHandler(content));
            setup.Commands.Register(new HoldPositionHandler(content));
            setup.Commands.Register(new GeoBuildFleetHandler(content));
            setup.Commands.Register(new GeoMoveFleetHandler(content));
            setup.Commands.Register(new GeoEmbarkHandler(content));
            setup.Commands.Register(new GeoLandingHandler(content));
            setup.Commands.Register(new GeoRecruitAirWingHandler(content));
            setup.Commands.Register(new GeoAirMissionHandler(content));
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
