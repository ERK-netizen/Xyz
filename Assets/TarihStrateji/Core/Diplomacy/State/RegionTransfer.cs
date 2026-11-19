using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Diplomacy.State
{
    /// <summary>Barış maddesi: bölgenin hukuki sahipliği <see cref="NewOwner"/>'a geçer.</summary>
    public readonly struct RegionTransfer
    {
        public RegionTransfer(RegionId region, CountryId newOwner)
        {
            Region = region;
            NewOwner = newOwner;
        }

        public RegionId Region { get; }

        public CountryId NewOwner { get; }
    }
}
