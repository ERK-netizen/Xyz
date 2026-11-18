using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    /// <summary>Kendi sahip ve kontrol ettiğimiz bir sektörde tesis inşaatı başlatır.</summary>
    public sealed class BuildFacilityCommand : GameCommand
    {
        public const string KindKey = "emir.ekonomi.tesisInsaEt";

        public BuildFacilityCommand(CountryId issuer, FacilityTypeId facilityType, SectorId sector)
            : base(issuer)
        {
            FacilityType = facilityType;
            Sector = sector;
        }

        public FacilityTypeId FacilityType { get; }

        public SectorId Sector { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
