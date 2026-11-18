using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Politics;
using TarihStrateji.Core.Politics.Definitions;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    /// <summary>Kurum yatırımı: hazineden (deftere yazılarak) düşülür, kurum güçlenir.</summary>
    public sealed class InvestInInstitutionHandler : CommandHandler<InvestInInstitutionCommand>
    {
        private readonly PoliticsCatalog _catalog;

        public InvestInInstitutionHandler(PoliticsCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public override string Kind => InvestInInstitutionCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, InvestInInstitutionCommand c)
        {
            ValidationResult politics = PoliticsRules.CheckInvest(state.Politics, _catalog, c.Issuer, c.Institution, c.Money);
            return politics.IsValid ? TreasuryRules.CheckSpend(state.Economy, c.Issuer, c.Money) : politics;
        }

        protected override void ApplyTyped(CommandContext context, InvestInInstitutionCommand c)
        {
            TreasuryRules.Spend(context.State.Economy, c.Issuer, c.Money);
            PoliticsRules.Invest(context.State.Politics, _catalog, c.Issuer, c.Institution, c.Money);
        }

        protected override void WriteTyped(InvestInInstitutionCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Institution);
            w.WriteFixed64(c.Money);
        }

        protected override InvestInInstitutionCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new InvestInInstitutionCommand(issuer, new InstitutionId(r.ReadUInt64()), r.ReadFixed64());
    }
}
