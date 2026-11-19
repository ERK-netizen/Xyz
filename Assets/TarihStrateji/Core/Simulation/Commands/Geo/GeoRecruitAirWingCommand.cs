using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Kendi kontrolündeki şehirde hava kanadı kurar (şehir başına sınırlı).</summary>
    public sealed class GeoRecruitAirWingCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.kanatKur";

        public GeoRecruitAirWingCommand(CountryId issuer, UnitTypeId wingType, CityId baseCity)
            : base(issuer)
        {
            WingType = wingType;
            BaseCity = baseCity;
        }

        public UnitTypeId WingType { get; }

        public CityId BaseCity { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
