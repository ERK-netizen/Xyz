using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// Sunum katmanının emir nesnelerini somut emir türlerini bilmeden kurması için (sunum yalnız
    /// Domain'e başvurur). Hedefler <see cref="TargetRef"/> ile verilir (birlik, şehir, nokta…);
    /// hücre kavramı yoktur. Üretilen emir <see cref="IGameCommandGateway"/> ile doğrulanıp gönderilir.
    /// </summary>
    public interface IGameCommandFactory
    {
        GameCommand MoveUnit(ulong unitId, TargetRef target);

        GameCommand Attack(ulong unitId, TargetRef target);

        GameCommand HoldPosition(ulong unitId);

        GameCommand RecruitInfantry(CityId city);

        GameCommand DeclareWar(CountryId target);

        GameCommand ProposeWhitePeace(CountryId target);

        GameCommand ProposeNonAggression(CountryId target);

        GameCommand ProposeAlliance(CountryId target);

        /// <summary>Oyuncuya gelen teklife yanıt (yalnız muhatap kabul/ret edebilir; çekirdek doğrular).</summary>
        GameCommand RespondProposal(ulong proposalId, bool accept);
    }
}
