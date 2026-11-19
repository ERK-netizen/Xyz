using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Simulation.Commands.Diplomacy;
using TarihStrateji.Core.Simulation.Commands.Military;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Unity.Bootstrap
{
    /// <summary>
    /// Sunumun istediği emirleri somut emir türleriyle kurar (sunum Simulation'ı bilmez). Hücre
    /// dizini = satır × genişlik + sütun; emirler oyuncu ülkesi adınadır ve çekirdekçe doğrulanır.
    /// </summary>
    public sealed class BootstrapCommandFactory : IGameCommandFactory
    {
        private readonly Func<IGameReadModel> _readModel;

        public BootstrapCommandFactory(Func<IGameReadModel> readModel)
        {
            _readModel = readModel ?? throw new ArgumentNullException(nameof(readModel));
        }

        private CountryId Player => _readModel().PlayerCountry;

        private HexCoord Coord(int cellIndex)
        {
            int width = _readModel().MapWidth;
            return new HexCoord(cellIndex % width, cellIndex / width);
        }

        public GameCommand MoveUnit(ulong unitId, int cellIndex) => new MoveUnitCommand(Player, new LandUnitId(unitId), Coord(cellIndex));

        public GameCommand Attack(ulong unitId, int cellIndex) => new OrderAttackCommand(Player, new LandUnitId(unitId), Coord(cellIndex));

        public GameCommand HoldPosition(ulong unitId) => new HoldPositionCommand(Player, new LandUnitId(unitId));

        public GameCommand RecruitInfantry(int cityCellIndex) => new RecruitUnitCommand(Player, BaselineUnits.InfantryDivision, Coord(cityCellIndex));

        public GameCommand DeclareWar(CountryId target) => new DeclareWarCommand(Player, target, new RegionId[0]);

        public GameCommand ProposeWhitePeace(CountryId target) => new ProposeTreatyCommand(Player, ProposalKind.Peace, target, new RegionTransfer[0]);
    }
}
