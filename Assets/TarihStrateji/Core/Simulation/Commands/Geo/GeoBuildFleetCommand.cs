using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Ana limanda filo kurar (gemi envanterden, mürettebat nüfustan).</summary>
    public sealed class GeoBuildFleetCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.filoKur";

        public GeoBuildFleetCommand(CountryId issuer, UnitTypeId shipType, CityId port)
            : base(issuer)
        {
            ShipType = shipType;
            Port = port;
        }

        public UnitTypeId ShipType { get; }

        public CityId Port { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
