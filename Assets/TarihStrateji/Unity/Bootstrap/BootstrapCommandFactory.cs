using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Simulation.Commands.Diplomacy;
using TarihStrateji.Core.Simulation.Commands.Geo;
using TarihStrateji.Core.Simulation.Commands.Military;

namespace TarihStrateji.Unity.Bootstrap
{
    /// <summary>
    /// Sunumun istediği emirleri somut emir türleriyle kurar (sunum Simulation'ı bilmez). Hedefler
    /// <see cref="TargetRef"/> ile gelir (ADR-011, hücre yok); emirler oyuncu ülkesi adınadır ve çekirdekçe doğrulanır.
    /// </summary>
    public sealed class BootstrapCommandFactory : IGameCommandFactory
    {
        private readonly Func<IGameReadModel> _readModel;

        public BootstrapCommandFactory(Func<IGameReadModel> readModel)
        {
            _readModel = readModel ?? throw new ArgumentNullException(nameof(readModel));
        }

        private CountryId Player => _readModel().PlayerCountry;

        public GameCommand MoveUnit(ulong unitId, TargetRef target) => new GeoMoveUnitCommand(Player, new LandUnitId(unitId), target);

        public GameCommand Attack(ulong unitId, TargetRef target) => new GeoOrderAttackCommand(Player, new LandUnitId(unitId), target);

        public GameCommand HoldPosition(ulong unitId) => new HoldPositionCommand(Player, new LandUnitId(unitId));

        public GameCommand RecruitInfantry(CityId city) => new GeoRecruitUnitCommand(Player, BaselineUnits.InfantryDivision, city);

        public GameCommand DeclareWar(CountryId target) => new DeclareWarCommand(Player, target, new RegionId[0]);

        public GameCommand ProposeWhitePeace(CountryId target) => new ProposeTreatyCommand(Player, ProposalKind.Peace, target, new RegionTransfer[0]);
    }
}
