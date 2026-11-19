using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Logistics
{
    /// <summary>İkmal yalnız ülkenin fiilen kontrol ettiği kara hücrelerinden geçer.</summary>
    public sealed class OwnTerritorySupplyAccess : ISupplyAccess
    {
        public static readonly OwnTerritorySupplyAccess Instance = new OwnTerritorySupplyAccess();

        public bool CanPass(CountryId supplier, HexCell cell)
        {
            return !cell.IsWater && cell.Controller == supplier;
        }
    }
}
