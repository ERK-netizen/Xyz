using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Kendi (sahip + kontrol, kuşatılmamış) şehrinde kara birliği toplar.</summary>
    public sealed class GeoRecruitUnitCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.birlikTopla";

        public GeoRecruitUnitCommand(CountryId issuer, UnitTypeId unitType, CityId city)
            : base(issuer)
        {
            UnitType = unitType;
            City = city;
        }

        public UnitTypeId UnitType { get; }

        public CityId City { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
