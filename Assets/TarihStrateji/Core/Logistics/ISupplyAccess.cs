using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Logistics
{
    /// <summary>
    /// İkmalin bir ülke için bir hücreden geçip geçemeyeceği. Varsayılan: yalnız kendi fiili
    /// kontrolündeki hücreler (<see cref="OwnTerritorySupplyAccess"/>); ittifaklar (Adım 8)
    /// başka bir uygulamayla eklenir.
    /// </summary>
    public interface ISupplyAccess
    {
        bool CanPass(CountryId supplier, HexCell cell);
    }
}
