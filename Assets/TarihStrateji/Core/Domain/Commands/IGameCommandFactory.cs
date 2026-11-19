using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// Sunum katmanının emir nesnelerini somut emir türlerini bilmeden kurması için (sunum yalnız
    /// Domain'e başvurur). Hücreler dizinle (satır × genişlik + sütun) verilir; üretilen emir
    /// <see cref="IGameCommandGateway"/> ile doğrulanıp gönderilir.
    /// </summary>
    public interface IGameCommandFactory
    {
        GameCommand MoveUnit(ulong unitId, int cellIndex);

        GameCommand Attack(ulong unitId, int cellIndex);

        GameCommand HoldPosition(ulong unitId);

        GameCommand RecruitInfantry(int cityCellIndex);

        GameCommand DeclareWar(CountryId target);

        GameCommand ProposeWhitePeace(CountryId target);
    }
}
