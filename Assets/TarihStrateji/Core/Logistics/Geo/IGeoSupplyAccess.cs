using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Logistics.Geo
{
    /// <summary>İkmalin bir ulaşım düğümünden geçip geçemeyeceği (düşman kontrolü ikmali keser).</summary>
    public interface IGeoSupplyAccess
    {
        bool CanPass(CountryId country, TransportNode node);
    }
}
