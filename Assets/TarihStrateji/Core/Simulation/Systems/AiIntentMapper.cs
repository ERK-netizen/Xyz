using TarihStrateji.Core.AI;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Simulation.Commands.Diplomacy;
using TarihStrateji.Core.Simulation.Commands.Economy;
using TarihStrateji.Core.Simulation.Commands.Military;
using TarihStrateji.Core.Simulation.Commands.Statecraft;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>YZ niyetini oyuncu emirleriyle aynı emir türlerine çevirir (YZ'nin ayrıcalıklı emri yoktur).</summary>
    public static class AiIntentMapper
    {
        private static readonly RegionTransfer[] NoTransfers = new RegionTransfer[0];

        public static GameCommand ToCommand(CountryId country, AiIntent intent)
        {
            switch (intent.Kind)
            {
                case AiIntentKind.Research:
                    return new SetResearchCommand(country, new TechnologyId(intent.Id));
                case AiIntentKind.SelectFocus:
                    return new SelectFocusCommand(country, new FocusId(intent.Id));
                case AiIntentKind.ChooseEventOption:
                    return new ChooseEventOptionCommand(country, new EventId(intent.Id), intent.Option);
                case AiIntentKind.AcceptProposal:
                    return new RespondProposalCommand(country, new ProposalId(intent.Id), true);
                case AiIntentKind.RejectProposal:
                    return new RespondProposalCommand(country, new ProposalId(intent.Id), false);
                case AiIntentKind.ProposePeace:
                    return new ProposeTreatyCommand(country, ProposalKind.Peace, intent.Country, Copy(intent));
                case AiIntentKind.ProposeAlliance:
                    return new ProposeTreatyCommand(country, ProposalKind.Alliance, intent.Country, NoTransfers);
                case AiIntentKind.DeclareWar:
                    return new DeclareWarCommand(country, intent.Country, new RegionId[0]);
                case AiIntentKind.Recruit:
                    return new RecruitUnitCommand(country, new UnitTypeId(intent.Id), intent.Target);
                case AiIntentKind.Move:
                    return new MoveUnitCommand(country, new LandUnitId(intent.Id), intent.Target);
                case AiIntentKind.Build:
                    return new BuildFacilityCommand(country, new FacilityTypeId(intent.Id), new SectorId(intent.Secondary));
                case AiIntentKind.SetTaxRate:
                    return new SetTaxRateCommand(country, intent.Amount);
                case AiIntentKind.Attack:
                    return new OrderAttackCommand(country, new LandUnitId(intent.Id), intent.Target);
                default:
                    return null;
            }
        }

        private static RegionTransfer[] Copy(AiIntent intent)
        {
            var transfers = new RegionTransfer[intent.Transfers.Count];
            for (int i = 0; i < transfers.Length; i++)
            {
                transfers[i] = intent.Transfers[i];
            }

            return transfers;
        }
    }
}
