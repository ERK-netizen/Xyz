using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    /// <summary>Vergi oranını ayarlar (0 … içerikteki üst sınır).</summary>
    public sealed class SetTaxRateCommand : GameCommand
    {
        public const string KindKey = "emir.ekonomi.vergiOrani";

        public SetTaxRateCommand(CountryId issuer, Fixed64 rate)
            : base(issuer)
        {
            Rate = rate;
        }

        public Fixed64 Rate { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
